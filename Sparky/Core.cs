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

        private readonly IServiceProvider _services;

        private readonly CancellationTokenSource _cts;

        private readonly CommandService _commands = new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Verbose,
            DefaultRunMode = RunMode.Async
        });

        private readonly DiscordSocketClient _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            LogLevel = LogSeverity.Info
        });

        public Core(CancellationTokenSource cts)
        {
            _cts = cts;

            _services = ConfigureServices();
        }

        public async Task IgniteAsync()
        {
            _client.MessageReceived += HandleMessageCreatedAsync;
            _client.UserJoined += HandleMemberJoinedAsync;
            _client.ReactionAdded += HandleReactionAddedAsync;
            _client.Ready += async () =>
            {
                await _client.SetGameAsync("the fireworks", type: ActivityType.Watching);
                _poller = new Poller(_client);
            };
            _client.Log += msg =>
            {
                Console.WriteLine(msg);
                return Task.CompletedTask;
            };
            _commands.Log += msg =>
            {
                Console.WriteLine(msg);
                return Task.CompletedTask;
            };

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

        private async Task HandleReactionAddedAsync(Cacheable<IUserMessage, ulong> cacheableMessage, IMessageChannel channel, SocketReaction reaction)
        {
            if (!(channel is SocketTextChannel guildChannel) || !reaction.Emote.Name.Equals(Configuration.Get<string>("karma_emote_name")))
            {
                return;
            }

            var message = await cacheableMessage.DownloadAsync();
            if (message.Author.Id == reaction.UserId)
                return;

            using (var session = Database.Store.OpenAsyncSession())
            {
                var user = await Database.EnsureCreatedAsync(session, message.Author.Id);
                var hasGivenKarma = user.KarmaGivers.TryGetValue(reaction.UserId, out var lastGivenAt);
                var isTimedOut = DateTimeOffset.UtcNow.Subtract(lastGivenAt).TotalMinutes >= Configuration.Get<int>("karma_limit_mins");

                if (hasGivenKarma && !isTimedOut)
                {
                    return;
                }
                else
                {
                    user.Karma += 1;
                    user.KarmaGivers[reaction.UserId] = DateTimeOffset.UtcNow;
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