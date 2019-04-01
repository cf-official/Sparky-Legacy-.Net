using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Sparky.Database;
using Sparky.Modules;
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
            LogLevel = LogSeverity.Debug,
            DefaultRunMode = RunMode.Async
        });

        private readonly DiscordSocketClient _client = new DiscordSocketClient(new DiscordSocketConfig
        {   
            LogLevel = LogSeverity.Debug,
            AlwaysDownloadUsers = true
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
                _poller = new Poller(this, _client);
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

        public async Task LogAsync(LogMessage message)
        {
            if (message.Severity > LogSeverity.Verbose)
                return;
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

            using (var dctx = new SparkyContext())
            {
                var user = dctx.GetOrCreateUser(msg.Author.Id);

                if (DateTime.UtcNow.Subtract(user.LastMessageAt ?? DateTime.UtcNow.AddMinutes(-2)).TotalMinutes >= 1)
                {
                    user.Points += 1;
                    user.LastMessageAt = DateTime.UtcNow;
                }

                await dctx.SaveChangesAsync();
            }
            var argPos = 0;
            if (message.HasStringPrefix(Configuration.Get<string>("prefix"), ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                var context = new SparkyCommandContext(_client, message, _services);

                await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);
            }
        }

        private async Task HandleMemberJoinedAsync(SocketGuildUser member)
        {
            using (var dctx = new SparkyContext())
            {
                var user = dctx.GetOrCreateUser(member.Id);

                foreach (var roleId in user.Roles)
                {
                    var role = member.Guild.Roles.FirstOrDefault(r => r.Id == roleId);
                    if (role != null)
                    {
                        await member.AddRoleAsync(role);
                    }
                }

                await dctx.SaveChangesAsync();
            }
        }

        private IServiceProvider ConfigureServices()
            => new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(new InteractiveService(_client, TimeSpan.FromSeconds(30)))
                .BuildServiceProvider();
    }
}