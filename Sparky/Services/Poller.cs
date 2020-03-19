using Discord;
using Discord.WebSocket;
using Sparky.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sparky.Services
{
    public sealed class Poller
    {
        private Core _botCore;

        private DiscordSocketClient _client;

        private Task _timerTask;

        public Poller(Core botCore, DiscordSocketClient client)
        {
            _botCore = botCore;
            _client = client;
            _timerTask = Task.Run(async () => {
                while(true)
                {
                    await Task.Delay(Configuration.Get<int>("poll_interval"));

                    try
                    {
                        await _botCore.LogAsync(new LogMessage(LogSeverity.Debug, nameof(Poller), "Polling guild..."));
                        await PollGuildAsync(_client.Guilds.First());
                    }
                    catch (Exception ex)
                    {
                        await _botCore.LogAsync(new LogMessage(LogSeverity.Warning, nameof(Poller), "An exception was thrown while checking the guild.", ex));
                    }
                    finally
                    {
                        await _botCore.LogAsync(new LogMessage(LogSeverity.Debug, nameof(Poller), "Finished polling."));
                    }
                }
            });
        }

        private async Task PollGuildAsync(SocketGuild guild)
        {
            using (var dctx = new SparkyContext())
            {
                var limits = dctx.RoleLimits.ToList();
                foreach (var member in guild.Users.Where(m => !m.IsBot))
                {
                    var user = await dctx.FindAsync<SparkyUser>(Convert.ToInt64(member.Id));
                    var isNew = user == null;
                    if (isNew)
                        user = new SparkyUser { Id = Convert.ToInt64(member.Id) };

                    await DoRoleCheckAsync(member, user, limits);

                    var memberRoles = member.Roles.Select(r => r.Id).ToArray();
                    user.Roles = memberRoles;

                    if (isNew)
                        await dctx.AddAsync(user);
                }

                await dctx.SaveChangesAsync();
            }
        }

        private async Task DoRoleCheckAsync(SocketGuildUser member, SparkyUser user, List<RoleLimit> roleLimits)
        {
            foreach (var roleLimit in roleLimits)
            {
                var role = member.Guild.Roles.First(r => Convert.ToInt64(r.Id) == roleLimit.Id);
                if (KarmaService.GetKarma(member.Id) >= roleLimit.KarmaRequirement 
                    && user.Points >= roleLimit.PointRequirement)
                {
                    if (!member.Roles.Contains(role))
                    {
                        await _botCore.LogAsync(new LogMessage(LogSeverity.Info, nameof(Poller), 
                            $"{member.Username}#{member.Discriminator} fulfils requirement for {role.Name}, granting."));
                        await member.AddRoleAsync(role);
                    }
                }
                else if (member.Roles.Contains(role))
                {
                    await _botCore.LogAsync(new LogMessage(LogSeverity.Info, nameof(Poller),
                            $"{member.Username}#{member.Discriminator} no longer fulfils requirement for {role.Name}, removing."));
                    await member.RemoveRoleAsync(role);
                }
            }
        }
    }
}