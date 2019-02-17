using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Raven.Client.Documents;
using Sparky.Models;
using Sparky.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    [Group("profile")]
    [Summary("Get information I've got stored on you.")]
    public sealed class Profiles : SparkyModuleBase
    {
        [Command(RunMode = RunMode.Async)]
        [Summary("View your karma and message count.")]
        public async Task ViewProfileAsync([Summary("@user"), Remainder] SocketGuildUser member = null)
        {
            if (member?.IsBot ?? false)
                return;

            var targetId = member?.Id ?? Context.User.Id;
            var users = await Session.Query<SparkyUser>().ToListAsync();
            var userData = users.First(u => u.Id == targetId.ToString());

            (int karmaRank, int karmaCount) = await KarmaService.GetKarmaRankAsync(targetId);

            int moderatedKarma = 0;
            var giverList = new List<(ulong, int)>();
            using (var giverSession = Database.Store.OpenAsyncSession())
            {
                var giverEvents = await giverSession.Query<KarmaEvent>().Where(e => e.RecipientId == targetId).ToListAsync();
                var sparkyEvent = giverEvents.FirstOrDefault(e => e.Id == KarmaEvent.GetId(Context.Client.CurrentUser.Id, member?.Id ?? Context.User.Id));
                if (sparkyEvent != null)
                {
                    giverEvents.Remove(sparkyEvent);
                    moderatedKarma = sparkyEvent.Amount;
                }
                var giverGroups = giverEvents
                    .GroupBy(e => e.GiverId)
                    .OrderByDescending(g => g.Sum(e => e.Amount))
                    .Take(5);

                foreach (var giver in giverGroups)
                    giverList.Add((giver.Key, giver.Sum(e => e.Amount)));
            }
            var messageRank = users.OrderByDescending(u => u.MessageCount).ToList().IndexOf(userData) + 1;

            var eb = new EmbedBuilder()
                .WithTitle($"Profile of: {(member ?? Context.User as SocketGuildUser).Nickname ?? member?.Username ?? Context.User.Username}")
                .AddField($"Karma (Rank {karmaRank})", karmaCount, true)
                .AddField($"Message Count (Rank {messageRank})", userData.MessageCount, true)
                .AddField("Top 5 Karma Givers", string.Join(", ", giverList.Count == 0 ? new[] { "None" } : giverList.Select(tuple => $"<@{tuple.Item1}> {((tuple.Item2 / (double)karmaCount) * 100):.0}%")))
                .AddField("Moderated Karma", moderatedKarma.ToString(), true)
                .WithColor(Color.DarkBlue)
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(embed: eb);
        }
    }
}