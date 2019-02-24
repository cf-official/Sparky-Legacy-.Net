using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sparky.Database;
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
            var user = DbCtx.Users.Find(member.Id);
            if (user.Points - toAdd < 0)
            {
                await ReplyAsync("You can't give a user a negative message count.");
                return;
            }

            user.Points += toAdd;
            
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
            if ((KarmaService.GetKarma(member.Id)) + toAdd < 0)
            {
                await ReplyAsync("You can't give a user negative karma.");
                return;
            }

            var sparkyEvent = DbCtx.KarmaEvents.Find(KarmaEvent.GetId(Context.Client.CurrentUser.Id, member.Id));
            if (sparkyEvent == null)
                DbCtx.Add(KarmaEvent.New(Context.Client.CurrentUser.Id, member.Id, member.Id, toAdd));
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
