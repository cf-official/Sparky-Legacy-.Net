using Discord;
using Discord.WebSocket;
using Sparky.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Sparky
{
    public class Globals
    {
        public SparkyCommandContext Context { get; }

        public DiscordSocketClient Client { get; }

        public IServiceProvider Provider { get; }

        public IServiceProvider Services => Provider;

        public Globals(SparkyCommandContext context, IServiceProvider provider)
        {
            Context = context;
            Client = context.Client;
            Provider = provider;
        }

        public async Task<IUserMessage> ReplyAsync(string message = null, Embed embed = null, RequestOptions options = null)
        {
            try
            {
                return await Context.Channel.SendMessageAsync(message, false, embed, options);
            }
            catch
            {
                return null;
            }
        }
    }
}
