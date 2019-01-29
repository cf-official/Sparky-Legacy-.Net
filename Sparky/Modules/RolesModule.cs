using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
    public sealed class RolesModule : SparkyModuleBase
    {
        public InteractiveService Interactive { get; }

        public RolesModule(InteractiveService interactive) => Interactive = interactive;

        [Command("setup")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task SetupRoleAsync([Remainder] SocketRole role)
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

        [Command("new")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task NewRoleAsync(string hex, [Remainder] string name)
        {
            if (uint.TryParse(hex.Replace("0x", string.Empty), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var colorInt))
            {
                await Context.Guild.CreateRoleAsync(name, GuildPermissions.None, new Color(colorInt), true);
                await Context.Message.AddReactionAsync(new Emoji("👌"));
            }
            else
                await ReplyAsync("Pick a proper color, dude.");
        }
    }
}
