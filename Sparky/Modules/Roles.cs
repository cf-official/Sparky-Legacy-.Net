using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Raven.Client.Documents;
using Sparky.Models;
using Sparky.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public async Task ViewRolesASync()
        {
            var roleInfos = await Session.Query<RoleLimit>().ToListAsync();
            var sb = new StringBuilder();
            foreach (var role in roleInfos)
                sb.AppendLine($"<@&{role.Id}>\nMessages: {role.MessageCount}\nKarma: {role.KarmaCount}\n");

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle("Requirements for Auto-Roles")
                .WithDescription(sb.ToString());

            await ReplyAsync(embed: eb.Build());
        }

        [Command("setup")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [Summary("Set up a role to be an auto-role, or edit an existing one.")]
        public async Task SetupRoleAsync([Remainder, Summary("@role")] SocketRole role)
        {
            await ReplyAsync("How many messages should it require?");

            var response = await Interactive.WaitForMessageAsync(InteractiveService.SameUserAndChannel(Context.User, Context.Channel));

            if (!int.TryParse(response.Content, out int messageCount))
                await ReplyAsync("Sorry, that's not a valid number.");

            await ReplyAsync("How much karma should you need?");

            response = await Interactive.WaitForMessageAsync(InteractiveService.SameUserAndChannel(Context.User, Context.Channel));

            if (!int.TryParse(response.Content, out int karmaCount))
                await ReplyAsync("Sorry, that's not a valid number.");

            var existingRole = await Session.LoadAsync<RoleLimit>(role.Id.ToString());
            if (existingRole == null)
            {
                await Session.StoreAsync(RoleLimit.New(role.Id, messageCount, karmaCount));
            }
            else
            {
                existingRole.MessageCount = messageCount;
                existingRole.KarmaCount = karmaCount;
            }

            await ReplyAsync("Done!");
        }

        [Command("unregister")]
        [Summary("Unregisters a role from being automatically assigned.")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task UnregisterRoleAsync([Remainder, Summary("@role")] SocketRole role)
        {
            Session.Delete(role.Id.ToString());

            await OkAsync();
        }


        [Command("new")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
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
