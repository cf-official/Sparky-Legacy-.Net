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
    [Group("profile")]
    public sealed class ProfileModule : SparkyModuleBase
    {
        [Command(RunMode = RunMode.Async)]
        public async Task ViewProfileAsync(SocketGuildUser user = null)
        {
            var users = await Session.Query<SparkyUser>().ToListAsync();
            var userData = users.First(u => u.Id == (user?.Id.ToString() ?? Context.User.Id.ToString()));

            var karmaRank = users.OrderByDescending(u => u.Karma).ToList().IndexOf(userData) + 1;
            var messageRank = users.OrderByDescending(u => u.MessageCount).ToList().IndexOf(userData) + 1;

            var eb = new EmbedBuilder()
                .WithTitle($"Profile of: {(user ?? Context.User as SocketGuildUser).Nickname ?? Context.User.Username}")
                .AddField($"Karma (Rank {karmaRank})", userData.Karma, true)
                .AddField($"Message Count (Rank {messageRank})", userData.MessageCount, true)
                .WithColor(Color.DarkBlue)
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(embed: eb);
        }
    }
}
