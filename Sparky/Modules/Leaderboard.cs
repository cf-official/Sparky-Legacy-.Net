using Discord;
using Discord.Commands;
using Sparky.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    [Group("leaderboard")]
    [Alias("lb")]
    [Summary("Check how you stack up against other users on the server.")]
    public sealed class Leaderboard : SparkyModuleBase
    {
        private const int _timeout = 30_000;

        [Command]
        [Summary("See the top 5 users by points, and the top 5 users by karma.")]
        public async Task LeaderboardAsync()
        {
            var top5Messages = DbCtx.Users.OrderByDescending(u => u.Points).Take(5).ToList();

            var top5Karma = KarmaEvent.GetForAllUsers(DbCtx.KarmaEvents.ToList(), DbCtx.Users.ToList()).Take(5);

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithCurrentTimestamp();

            BuildLeaderboardEmbed(eb, "Points Leaderboard", top5Messages, u => u.Points);
            BuildLeaderboardEmbed(eb, "Karma Leaderboard", top5Karma.ToList());

            var response = await ReplyAsync(embed: eb.Build());
            //await WaitAndDeleteAsync(response);
        }

        [Command("messages")]
        [Alias("message", "msgs", "msg")]
        [Summary("See the top 10 message senders in the server.")]
        public async Task GetMessageLeaderboardAsync()
        {
            var top10 = DbCtx.Users.OrderByDescending(u => u.Points).Take(10);
            var eb = new EmbedBuilder()
                .WithCurrentTimestamp()
                .WithColor(Color.DarkBlue);

            var response = await ReplyAsync(embed: BuildLeaderboardEmbed(eb, "Points Leaderboard", top10.ToList(), u => u.Points).Build());
        }

        [Command("karma")]
        [Summary("See the top 10 users by karma in the server.")]
        public async Task GetKarmaLeaderboardAsync()
        {
            var eb = new EmbedBuilder()
                .WithCurrentTimestamp()
                .WithColor(Color.DarkBlue);

            var ranks = KarmaEvent.GetForAllUsers(DbCtx.KarmaEvents.ToList(), DbCtx.Users.ToList()).Take(10);

            var response = await ReplyAsync(embed: BuildLeaderboardEmbed(eb, "Karma Leaderboard", ranks.ToList()).Build());
        }

        private EmbedBuilder BuildLeaderboardEmbed(EmbedBuilder eb, string title, List<SparkyUser> users, Func<SparkyUser, object> selectFunc)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < users.Count; i++)
            {
                sb.AppendLine($"**{i + 1}.** <@{users[i].Id}> {selectFunc(users[i])}");
            }

            eb.AddField(title, sb.ToString());
            return eb;
        }

        private EmbedBuilder BuildLeaderboardEmbed(EmbedBuilder eb, string title, List<(ulong, int)> rankList)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rankList.Count; i++)
            {
                sb.AppendLine($"**{i + 1}.** <@{rankList[i].Item1}> {rankList[i].Item2}");
            }

            eb.AddField(title, sb.ToString());
            return eb;
        }
    }
}