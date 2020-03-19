using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Npgsql;
using Sparky.Database;
using Sparky.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [Summary("All moderation-related commands.")]
    public sealed class Moderation : SparkyModuleBase
    {
        private readonly InteractiveService _interactive;

        public Moderation(InteractiveService interactive)
            => _interactive = interactive;

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

        [Command("ban")]
        [Summary("Ban a user.")]
        public async Task BanUserAsync([Summary("@user"), Remainder] SocketGuildUser member)
        {
            try
            {
                await member.BanAsync();

                await ReplyAsync("👌");
            }
            catch
            {
                await ErrorAsync();
            }
        }

        [Command("hackban")]
        [Summary("Ban a user by id.")]
        public async Task BanUserAsync([Summary("123456789")] params ulong[] userIds)
        {
            var sb = new StringBuilder()
                .AppendLine("**Results:** ");

            for (int i = 0; i < userIds.Length; i++)
            {
                try
                {
                    await Context.Guild.AddBanAsync(userIds[i]);

                    sb.AppendLine($"- {userIds[i]} 👌");
                }
                catch
                {
                    sb.AppendLine($"- {userIds[i]} ❌");
                }
            }

            await ReplyAsync(sb.ToString());
        }

        [Command("massban")]
        [Summary("Mass ban all users that joined in the last n minutes.")]
        public async Task MassBanUsersAsync(int minutes)
        {
            var now = DateTimeOffset.UtcNow;

            var users = Context.Guild.Users
                .Where(u => now.Subtract(u.JoinedAt ?? now).TotalMinutes <= minutes)
                .ToList();

            if (users.Count == 0)
            {
                await ReplyAsync("No users were found in that timeframe.");
                return;
            }

            await ReplyAsync($"I found {users.Count} member(s) that joined in the last {minutes} minutes. \nWould you like me to ban them? (y/n)");

            var message = await _interactive.WaitForMessageAsync(InteractiveService.SameUserAndChannel(Context.User, Context.Channel));

            if (message.Content.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                int bans = 0;
                foreach (var user in users)
                {
                    try
                    {
                        await user.BanAsync();

                        bans++;
                    }
                    catch
                    {
                    }
                }

                await ReplyAsync($"👌 ({bans} bans)");
            }
        }

        [Command("massban")]
        [Summary("Ban all users that match a given regex.")]
        public async Task MassBanUsersAsync([Remainder] string pattern)
        {
            var regex = new Regex(pattern);

            var users = Context.Guild.Users
                .Where(u => regex.IsMatch(u.Username))
                .ToList();

            if (users.Count == 0)
            {
                await ReplyAsync("No users were found in that timeframe.");
                return;
            }

            await ReplyAsync($"I found {users.Count} member(s) that matched your regex. \nWould you like me to ban them? (y/n)");

            var message = await _interactive.WaitForMessageAsync(InteractiveService.SameUserAndChannel(Context.User, Context.Channel));

            if (message.Content.Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                int bans = 0;
                foreach (var user in users)
                {
                    try
                    {
                        await user.BanAsync();

                        bans++;
                    }
                    catch
                    {
                    }
                }

                await ReplyAsync($"👌 ({bans} bans)");
            }
        }

        [Command("prefix")]
        [Summary("Change the prefix that the bot responds to.")]
        public async Task SetPrefixAsync([Summary("s."), Remainder] string prefix)
        {
            Configuration.Write<string>("prefix", prefix);

            await OkAsync();
        }

        [Command("sql")]
        [Summary("Send raw SQL queries to the database.")]
        [RequireOwner]
        public async Task SendSQLAsync([Summary("DROP TABLE users;"), Remainder] string query)
        {
            using (var conn = new NpgsqlConnection(Configuration.Get<string>("conn_string")))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    try
                    {
                        var rowsEdited = await cmd.ExecuteNonQueryAsync();
                        await ReplyAsync($"{rowsEdited} rows changed.");
                    }
                    catch(Exception e)
                    {
                        await ReplyAsync(e.Message);
                        await ErrorAsync();
                        return;
                    }
                }
            }

            await OkAsync();
        }

        [Command("query")]
        [Summary("Send raw SQL queries to the database.")]
        [RequireOwner]
        public async Task SendQueryAsync([Summary("SELECT * FROM;"), Remainder] string query)
        {
            if (query.StartsWith("```sql"))
                query = query.Substring(6, query.Length - 9);

            using (var conn = new NpgsqlConnection(Configuration.Get<string>("conn_string")))
            {
                conn.Open();

                try
                {
                    using (var cmd = new NpgsqlCommand(query, conn))
                    using (var reader = (await cmd.ExecuteReaderAsync()))
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("```prolog");

                        var requiredPadding = new int[reader.FieldCount];
                        var dbObjects = new Dictionary<int, List<string>>();

                        int row = 0;
                        while (reader.Read() && row < 20)
                        {
                            var objBuffer = new object[reader.FieldCount];
                            reader.GetValues(objBuffer);

                            if (row == 0)
                            {
                                for (int col = 0; col < reader.FieldCount; col++)
                                {
                                    if (col == 0)
                                        dbObjects[0] = new List<string>();
                                    var value = "'" + reader.GetName(col) + "'";
                                    requiredPadding[col] = requiredPadding[col] < value.Length ? value.Length : requiredPadding[col];
                                    dbObjects[0].Add(value);
                                }
                                row++;
                            }

                            for (int field = 0; field < reader.FieldCount; field++)
                            {
                                if (field == 0)
                                    dbObjects[row] = new List<string>();

                                var value = objBuffer[field].ToString();
                                if (value.Length > 20)
                                    value = value.Substring(0, 17) + "...";
                                requiredPadding[field] = requiredPadding[field] < value.Length ? value.Length : requiredPadding[field];

                                dbObjects[row].Add(value);
                            }

                            row++;
                        }

                        for (int i = 0; i < dbObjects.Count; i++)
                        {
                            int listPos = 0;
                            sb.AppendLine(string.Join(" | ", dbObjects[i].Select(s => s.PadRight(requiredPadding[listPos++]))));
                            if (i == 0)
                                sb.AppendLine("".PadRight(requiredPadding.Sum() + (Math.Max((dbObjects[i].Count - 1) * 3, 0)), '-'));
                        }

                        await ReplyAsync(sb.AppendLine("```").ToString());
                    }
                }
                catch (Exception e)
                {
                    await ReplyAsync(e.Message);
                    await ErrorAsync();
                    return;
                }
            }

            await OkAsync();
        }
    }
}
