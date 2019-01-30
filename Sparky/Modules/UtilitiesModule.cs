using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    [Summary("Provides commands useful in everyday use of the bot.")]
    public sealed class UtilitiesModule : SparkyModuleBase
    {
        [Command("cleanup")]
        [Summary("Deletes messages that triggered a command, and all bot responses.")]
        public async Task CleanupAsync([Summary("100")] int limit = 100)
        {
            var messages = await Context.Channel.GetMessagesAsync(limit).FlattenAsync();
            var messagesToDelete = messages.Where(m => m.Content.StartsWith(Configuration.Get<string>("prefix")) 
                || m.Content.StartsWith(Context.Client.CurrentUser.Mention) 
                || m.Author.Id == Context.Client.CurrentUser.Id);

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);
        }

        [Command("purge")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [Summary("Bulk deletes messages.")]
        public async Task PurgeAsync([Summary("100")] int limit = 100)
        {
            var messages = await Context.Channel.GetMessagesAsync(limit).FlattenAsync();

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);
        }

        [Command("purge")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [Summary("Bulk deletes messages, filtering by user.")]
        public async Task PurgeAsync([Summary("@user")] IUser user, [Summary("100")] int limit = 100)
        {
            var messages = await Context.Channel.GetMessagesAsync(limit).FlattenAsync();
            var messagesToDelete = messages.Where(m => m.Author.Id == user.Id);

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messagesToDelete);
        }

        [Command("purge")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [Summary("Bulk deletes messages, filtering by user(s).")]
        public async Task PurgeAsync([Summary("100")] int limit, [Summary("@user")] params IUser[] users)
        {
            var messages = await Context.Channel.GetMessagesAsync(limit).FlattenAsync();
            var messagesToDelete = messages.Where(m => users.Any(u => u.Id == m.Author.Id));

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messagesToDelete);
        }
    }
}
