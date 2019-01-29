using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Raven.Client.Documents;
using Sparky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    [Group("leaderboard")]
    [Alias("lb")]
    public sealed class LeaderboardModule : SparkyModuleBase
    {
        [Command("messages")]
        public async Task GetMessageLeaderboardAsync()
        {
            var top10 = await Session.Query<SparkyUser>().OrderByDescending(u => u.MessageCount).Take(10).ToListAsync();
            var eb = new EmbedBuilder()
                .WithTitle($"Message leaderboard for: {Context.Guild.Name}");

            await ReplyAsync(embed: BuildLeaderboardEmbed(eb, top10.ToList(), u => u.MessageCount));
        }

        [Command("karma")]
        public async Task GetKarmaLeaderboardAsync()
        {
            var top10 = await Session.Query<SparkyUser>().OrderByDescending(u => u.Karma).Take(10).ToListAsync();
            var eb = new EmbedBuilder()
                .WithTitle($"Karma leaderboard for: {Context.Guild.Name}");

            await ReplyAsync(embed: BuildLeaderboardEmbed(eb, top10.ToList(), u => u.Karma));
        }

        private static Embed BuildLeaderboardEmbed(EmbedBuilder eb, List<SparkyUser> users, Func<SparkyUser, object> selectFunc)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < users.Count; i++)
                sb.AppendLine($"**{i + 1}.** <@{users[i].Id}> {selectFunc(users[i])}\n");
            eb.WithDescription(sb.ToString())
                .WithColor(Color.DarkBlue)
                .WithCurrentTimestamp();
            return eb.Build();
        }
    }
}
