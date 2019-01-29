using Discord.Commands;
using Discord.WebSocket;
using Raven.Client.Documents.Session;
using Sparky.Services;
using System;
using System.Collections.Generic;
using System.Text;

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
