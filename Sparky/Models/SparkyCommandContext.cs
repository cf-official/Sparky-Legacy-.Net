using Discord.Commands;
using Discord.WebSocket;

namespace Sparky.Models
{
    public sealed class SparkyCommandContext : SocketCommandContext
    {
        public SparkyCommandContext(DiscordSocketClient client, SocketUserMessage message)
            : base(client, message)
        {
        }
    }
}