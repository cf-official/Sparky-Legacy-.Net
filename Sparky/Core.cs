using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Sparky.Models;
using Sparky.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Sparky
{
    public sealed class Core
    {
        private Poller _poller;

        private KarmaService _karma;

        private readonly IServiceProvider _services;

        private readonly CancellationTokenSource _cts;

        private readonly SemaphoreSlim _colorLock = new SemaphoreSlim(1, 1);

        private readonly CommandService _commands = new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Verbose,
            DefaultRunMode = RunMode.Async
        });

        private readonly DiscordSocketClient _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            LogLevel = LogSeverity.Verbose
        });

        public Core(CancellationTokenSource cts)
        {
            _cts = cts;

            _services = ConfigureServices();
        }

        public async Task IgniteAsync()
        {
            _karma = new KarmaService(_client);
            _client.MessageReceived += HandleMessageCreatedAsync;
            _client.UserJoined += HandleMemberJoinedAsync;
            _client.Ready += async () =>
            {
                await _client.SetGameAsync("the fireworks", type: ActivityType.Watching);
                _poller = new Poller(_client);
            };
            _client.Log += LogAsync;
            _commands.Log += LogAsync;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            await _client.LoginAsync(TokenType.Bot, Configuration.Get<string>("token"));

            await _client.StartAsync();

            await Task.Delay(-1, _cts.Token)
                .ContinueWith(async task =>
                {
                    await _client.StopAsync();
                    await _client.LogoutAsync();

                    _client.Dispose();
                });
        }

        private async Task LogAsync(LogMessage message)
        {
            try
            {
                await _colorLock.WaitAsync();
                ConsoleColor color = ConsoleColor.White;
                switch(message.Severity)
                {
                    case LogSeverity.Debug:
                        color = ConsoleColor.Gray;
                        break;
                    case LogSeverity.Verbose:
                        color = ConsoleColor.White;
                        break;
                    case LogSeverity.Info:
                        color = ConsoleColor.Green;
                        break;
                    case LogSeverity.Warning:
                        color = ConsoleColor.DarkYellow;
                        break;
                    case LogSeverity.Error:
                    case LogSeverity.Critical:
                        color = ConsoleColor.Red;
                        break;
                }
                Console.ForegroundColor = color;

                Console.WriteLine($"[{message.Severity.ToString().PadRight(7)}] {message.Source.PadRight(17)}@{DateTimeOffset.UtcNow.ToString("HH:mm:ss dd/mm")} {message.Message}{(message.Exception != null ? Environment.NewLine : "")}{message.Exception?.Message ?? ""}");
            }
            finally
            {
                _colorLock.Release();
            }
        }

        private async Task HandleMessageCreatedAsync(SocketMessage msg)
        {
            if (!(msg is SocketUserMessage message) || message.Author.IsBot)
            {
                return;
            }

            using (var session = Database.Store.OpenAsyncSession())
            {
                var user = await Database.EnsureCreatedAsync(session, message.Author.Id);

                if (DateTimeOffset.UtcNow.Subtract(user.LastMessageAt ?? DateTimeOffset.UtcNow.AddMinutes(-2)).TotalMinutes >= 1)
                {
                    user.MessageCount += 1;
                    user.LastMessageAt = DateTimeOffset.UtcNow;
                }

                await session.SaveChangesAsync();
            }
            var argPos = 0;
            if (message.HasStringPrefix(Configuration.Get<string>("prefix"), ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                var context = new SparkyCommandContext(_client, message);

                await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
            }
        }

        private async Task HandleMemberJoinedAsync(SocketGuildUser member)
        {
            using (var session = Database.Store.OpenAsyncSession())
            {
                var user = await Database.EnsureCreatedAsync(session, member.Id);

                foreach (var roleId in user.RoleIds)
                {
                    var role = member.Guild.Roles.FirstOrDefault(r => r.Id == roleId);
                    if (role != null)
                    {
                        await member.AddRoleAsync(role);
                    }
                }

                await session.SaveChangesAsync();
            }
        }

        private IServiceProvider ConfigureServices()
            => new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(new InteractiveService(_client, TimeSpan.FromSeconds(30)))
                .BuildServiceProvider();
    }
}