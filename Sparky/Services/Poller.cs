using Discord.WebSocket;
using Raven.Client.Documents;
using Sparky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sparky.Services
{
    public sealed class Poller
    {
        private DiscordSocketClient _client;

        private Timer _roleTimer;

        public Poller(DiscordSocketClient client)
        {
            _roleTimer = new Timer((_) => Task.Run(() => PollGuildAsync(_client.Guilds.First())), null, 0, Timeout.Infinite);
            _client = client;
        }

        private async Task PollGuildAsync(SocketGuild guild)
        {
            using (var session = Database.Store.OpenAsyncSession())
            using (var roleSession = Database.Store.OpenAsyncSession())
            {
                var users = await session.Query<SparkyUser>().ToListAsync();
                var limits = await roleSession.Query<RoleLimit>().ToListAsync();
                foreach (var member in guild.Users.Where(m => !m.IsBot))
                {
                    var user = users.FirstOrDefault(u => u.Id == member.Id.ToString());
                    if (user is null)
                    {
                        user = SparkyUser.New(member.Id);
                        await session.StoreAsync(user);
                    }
                    await DoRoleCheckAsync(member, user, limits);

                    var memberRoles = member.Roles.Select(r => r.Id).ToArray();
                    if (user.RoleIds != memberRoles)
                        user.RoleIds = memberRoles;
                }

                await session.SaveChangesAsync();
            }
            _roleTimer = new Timer((_) => Task.Run(() => PollGuildAsync(_client.Guilds.First())), null, 30 * 1000, Timeout.Infinite);
        }

        private async Task DoRoleCheckAsync(SocketGuildUser member, SparkyUser user, List<RoleLimit> roleLimits)
        {
            foreach (var roleLimit in roleLimits)
            {
                var role = member.Guild.Roles.First(r => r.Id.ToString() == roleLimit.Id);
                if (user.Karma >= roleLimit.KarmaCount && user.MessageCount >= roleLimit.MessageCount)
                {
                    await member.AddRoleAsync(role);
                }
                else if (member.Roles.Any(r => r.Id.ToString() == roleLimit.Id))
                {
                    await member.RemoveRoleAsync(role);
                }
            }
        }
    }
}