using Discord.WebSocket;
using Raven.Client.Documents;
using Sparky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            _roleTimer = new Timer((_) => Task.Run(() => PollGuildAsync(_client.Guilds.First())), null, 10*1000, Timeout.Infinite);
            _client = client;
        }

        private async Task PollGuildAsync(SocketGuild guild)
        {
            using (var session = Database.Store.OpenAsyncSession())
            {
                var users = await session.Query<SparkyUser>().ToListAsync();
                foreach (var member in guild.Users.Where(m => !m.IsBot))
                {
                    var user = users.FirstOrDefault(u => u.Id == member.Id.ToString());
                    if (user is null)
                    {
                        user = SparkyUser.New(member.Id);
                        await session.StoreAsync(user);
                    }
                    await DoRoleCheckAsync(member, user);

                    user.RoleIds = member.Roles.Select(r => r.Id).ToArray();
                }

                await session.SaveChangesAsync();
            }
            _roleTimer = new Timer((_) => Task.Run(() => PollGuildAsync(_client.Guilds.First())), null, 10 * 1000, Timeout.Infinite);
        }

        private async Task DoRoleCheckAsync(SocketGuildUser member, SparkyUser user)
        {
            var roleLimits = Configuration.Get<RoleLimit[]>("role_limits");
            foreach (var roleLimit in roleLimits)
            {
                var role = member.Guild.Roles.First(r => r.Id == roleLimit.Id);
                if (user.Karma >= roleLimit.KarmaCount && user.MessageCount >= roleLimit.MessageCount)
                    await member.AddRoleAsync(role);
                /*
                 * Allowing this for the time being, might use alternative system in the future.
                 * else if (member.Roles.Any(r => r.Id == roleLimit.Id))
                 *    await member.RemoveRoleAsync(role);
                 */
            }
        }
    }
}
