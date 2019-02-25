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
using System.Threading.Tasks;

namespace Sparky.Modules
{
    [RequireUserPermission(GuildPermission.ManageGuild)]
    [Summary("All moderation-related commands.")]
    public sealed class Moderation : SparkyModuleBase
    {
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
