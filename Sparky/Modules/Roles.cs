using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Sparky.Database;
using Sparky.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    [Group("role")]
    [Alias("roles")]
    [Summary("Role-related commands & functionality.")]
    public sealed class Roles : SparkyModuleBase
    {
        public InteractiveService Interactive { get; }

        public Roles(InteractiveService interactive) => Interactive = interactive;

        [Command]
        [Summary("View the requirements for existing auto-roles.")]
        public async Task ViewRolesAsync()
        {
            var groupedRoles = DbCtx.RoleLimits.GroupBy(r => r.PointRequirement > 0);
            if (groupedRoles.Count() < 2)
            {
                await ErrorAsync();
                return;
            }

            var messageRoles = groupedRoles.FirstOrDefault(g => g.Key);
            var karmaRoles = groupedRoles.FirstOrDefault(g => !g.Key);

            var msgSb = new StringBuilder();
            if (messageRoles != null)
                foreach (var role in messageRoles.OrderByDescending(r => r.PointRequirement))
                    msgSb.AppendLine($"<@&{role.Id}>\nPoints: {role.PointRequirement}\n");

            var karmaSb = new StringBuilder();
            if (karmaRoles != null)
                foreach (var role in karmaRoles.OrderByDescending(r => r.KarmaRequirement))
                    karmaSb.AppendLine($"<@&{role.Id}>\nKarma: {role.KarmaRequirement}\n");

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle("Requirements for Auto-Roles")
                .AddField("Messages", msgSb.ToString(), true)
                .AddField("Karma", karmaSb.ToString(), true);

            await ReplyAsync(embed: eb.Build());
        }

        [Command("setup")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Summary("Set up a role to be an auto-role, or edit an existing one.")]
        public async Task SetupRoleAsync([Remainder, Summary("@role")] SocketRole role)
        {
            await ReplyAsync("How many points should it require?");

            var response = await Interactive.WaitForMessageAsync(InteractiveService.SameUserAndChannel(Context.User, Context.Channel));

            if (!int.TryParse(response.Content, out int messageCount))
                await ReplyAsync("Sorry, that's not a valid number.");

            await ReplyAsync("How much karma should you need?");

            response = await Interactive.WaitForMessageAsync(InteractiveService.SameUserAndChannel(Context.User, Context.Channel));

            if (!int.TryParse(response.Content, out int karmaCount))
                await ReplyAsync("Sorry, that's not a valid number.");

            var existingRole = DbCtx.RoleLimits.Find(Convert.ToInt64(role.Id));
            if (existingRole == null)
            {
                DbCtx.RoleLimits.Add(RoleLimit.New(role.Id, messageCount, karmaCount));
            }
            else
            {
                existingRole.PointRequirement = messageCount;
                existingRole.KarmaRequirement = karmaCount;
            }

            await ReplyAsync("Done!");
        }

        [Command("remove")]
        [Summary("removes a role from being automatically assigned.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task UnregisterRoleAsync([Remainder, Summary("@role")] SocketRole role)
        {
            var limit = DbCtx.RoleLimits.Find(Convert.ToInt64(role.Id));
            DbCtx.Remove(limit);

            await OkAsync();
        }

        [Command("new")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        [Summary("Creates a new, blank role.")]
        public async Task NewRoleAsync([Summary("#FFFFFF")] string hex, [Remainder, Summary("name")] string name)
        {
            if (uint.TryParse(hex.Replace("#", string.Empty), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var colorInt))
            {
                await Context.Guild.CreateRoleAsync(name, GuildPermissions.None, new Color(colorInt), true);
                await OkAsync();
            }
            else
                await ReplyAsync("Pick a proper color, dude.");
        }
    }
}
