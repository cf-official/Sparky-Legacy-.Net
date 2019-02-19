using Discord;
using Discord.Commands;
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
    [Summary("Check how you stack up against other users on the server.")]
    public sealed class Leaderboard : SparkyModuleBase
    {
        private const int _timeout = 30 * 1000;

        [Command]
        [Summary("See the top 5 message senders, and the top 5 users by karma.")]
        public async Task LeaderboardAsync()
        {
            var top5Messages = await Session.Query<SparkyUser>().OrderByDescending(u => u.MessageCount).Take(5).ToListAsync();

            var users = await Session.Query<SparkyUser>().ToListAsync();
            var events = await Session.Query<KarmaEvent>().ToListAsync();
            var top5Karma = KarmaEvent.GetForAllUsers(events, users).Take(5);

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithCurrentTimestamp();

            BuildLeaderboardEmbed(eb, "Message Leaderboard", top5Messages, u => u.MessageCount);
            BuildLeaderboardEmbed(eb, "Karma Leaderboard", top5Karma.ToList());

            var response = await ReplyAsync(embed: eb.Build());
            //await WaitAndDeleteAsync(response);
        }

        [Command("messages")]
        [Alias("message", "msgs", "msg")]
        [Summary("See the top 10 message senders in the server.")]
        public async Task GetMessageLeaderboardAsync()
        {
            var top10 = await Session.Query<SparkyUser>().OrderByDescending(u => u.MessageCount).Take(10).ToListAsync();
            var eb = new EmbedBuilder()
                .WithCurrentTimestamp()
                .WithColor(Color.DarkBlue);

            var response = await ReplyAsync(embed: BuildLeaderboardEmbed(eb, "Message Leaderboard", top10.ToList(), u => u.MessageCount).Build());
            await WaitAndDeleteAsync(response);
        }

        [Command("karma")]
        [Summary("See the top 10 users by karma in the server.")]
        public async Task GetKarmaLeaderboardAsync()
        {
            var users = await Session.Query<SparkyUser>().ToListAsync();
            var events = await Session.Query<KarmaEvent>().ToListAsync();
            var eb = new EmbedBuilder()
                .WithCurrentTimestamp()
                .WithColor(Color.DarkBlue);

            var ranks = KarmaEvent.GetForAllUsers(events, users).Take(10);

            var response = await ReplyAsync(embed: BuildLeaderboardEmbed(eb, "Karma Leaderboard", ranks.ToList()).Build());
            //await WaitAndDeleteAsync(response);
        }

        private async Task WaitAndDeleteAsync(IMessage response)
        {
            await Task.Delay(_timeout);
            try
            {
                await response.DeleteAsync();
            }
            catch
            {
            }
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

        private EmbedBuilder BuildLeaderboardEmbed(EmbedBuilder eb, string title, List<(string, int)> rankList)
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