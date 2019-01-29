using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace Sparky.Services
{
    public sealed class InteractiveService
    {
        private readonly DiscordSocketClient _client;

        private readonly TimeSpan _timeout;

        public InteractiveService(DiscordSocketClient client, TimeSpan? timeout = null)
        {
            _client = client;
            _timeout = timeout ?? TimeSpan.FromSeconds(30);
        }

        public async Task<SocketMessage> WaitForMessageAsync(Predicate<SocketMessage> criterion)
        {
            var tcs = new TaskCompletionSource<SocketMessage>();
            Task MessageHook(SocketMessage message)
            {
                if (criterion(message))
                    tcs.SetResult(message);
                return Task.CompletedTask;
            }
            _client.MessageReceived += MessageHook;

            var timeoutTask = Task.Delay(_timeout);
            await Task.WhenAny(tcs.Task, timeoutTask);

            _client.MessageReceived -= MessageHook;

            return tcs.Task.Result;
        }

        public static Predicate<SocketMessage> SameUserAndChannel(IUser user, IMessageChannel channel)
            => m => m.Author.Id == user.Id && m.Channel.Id == channel.Id;
    }
}