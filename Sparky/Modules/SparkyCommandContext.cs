using Discord.Commands;
using Discord.WebSocket;
using System;

namespace Sparky.Modules
{
    public sealed class SparkyCommandContext : SocketCommandContext
    {
        public IServiceProvider Services { get; }

        public SparkyCommandContext(DiscordSocketClient client, SocketUserMessage message, IServiceProvider services)
            : base(client, message)
        {
            Services = services;
        }
    }
}