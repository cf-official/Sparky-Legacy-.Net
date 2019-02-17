using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Raven.Client.Documents;
using Sparky.Models;
using Sparky.Services;
using System.Linq;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [Summary("All moderation-related commands.")]
    public sealed class Moderation : SparkyModuleBase
    {
        [Command("editmsg")]
        [Summary("Manually add to a user's message count (to remove, specify a negative amount).")]
        public async Task AddMessageCountAsync([Summary("@user")] SocketGuildUser member, [Summary("1")] int toAdd)
        {
            if (member.IsBot)
            {
                await ReplyAsync("You can't edit the message count of bots.");
                return;
            }
            var user = await Session.LoadAsync<SparkyUser>(member.Id.ToString());
            if (user.MessageCount - toAdd < 0)
            {
                await ReplyAsync("You can't give a user a negative message count.");
                return;
            }

            user.MessageCount += toAdd;
            
            await OkAsync();
        }

        [Command("editkarma")]
        [Summary("Manually add to a user's karma count (to remove, specify a negative amount).")]
        public async Task AddKarmaAsync([Summary("@user")] SocketGuildUser member, [Summary("1")] int toAdd)
        {
            if (member.IsBot)
            {
                await ReplyAsync("You can't edit bot karma.");
                return;
            }
            if ((await KarmaService.GetKarmaAsync(member.Id)) + toAdd < 0)
            {
                await ReplyAsync("You can't give a user negative karma.");
                return;
            }

            var sparkyEvent = await Session.LoadAsync<KarmaEvent>(KarmaEvent.GetId(Context.Client.CurrentUser.Id, member.Id));
            if (sparkyEvent == null)
                await Session.StoreAsync(KarmaEvent.New(Context.Client.CurrentUser.Id, member.Id, member.Id, toAdd));
            else
                sparkyEvent.Amount += toAdd;

            await OkAsync();
        }

        [Command("prefix")]
        [Summary("Change the prefix that the bot responds to.")]
        public async Task SetPrefixAsync([Summary("s."), Remainder] string prefix)
        {
            Configuration.Write<string>("prefix", prefix);

            await OkAsync();
        }
    }
}
