﻿using Discord;
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
        private Core _botCore;

        private DiscordSocketClient _client;

        private Timer _roleTimer;

        private TimerCallback _callback;

        public Poller(Core botCore, DiscordSocketClient client)
        {
            _botCore = botCore;
            _client = client;
            _callback = async _ =>
            {
                _roleTimer.Dispose();
                try
                {
                    await _botCore.LogAsync(new LogMessage(LogSeverity.Verbose, nameof(Poller), "Polling guild..."));
                    await PollGuildAsync(_client.Guilds.First());
                }
                catch (Exception ex)
                {
                    await _botCore.LogAsync(new LogMessage(LogSeverity.Warning, nameof(Poller), "An exception was thrown while checking the guild.", ex));
                }
                finally
                {
                    await _botCore.LogAsync(new LogMessage(LogSeverity.Verbose, nameof(Poller), "Finished polling."));
                    _roleTimer = new Timer(_callback, null, 0, 30_000);
                }
            };
            _roleTimer = new Timer(_callback, null, 0, 30_000);
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
        }

        private async Task DoRoleCheckAsync(SocketGuildUser member, SparkyUser user, List<RoleLimit> roleLimits)
        {
            foreach (var roleLimit in roleLimits)
            {
                var role = member.Guild.Roles.First(r => r.Id.ToString() == roleLimit.Id);
                if ((await KarmaService.GetKarmaAsync(member.Id)) >= roleLimit.KarmaCount 
                    && user.MessageCount >= roleLimit.MessageCount)
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