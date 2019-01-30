using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    [Group("help")]
    [Summary("Contains utilities that help people use my commands!")]
    public sealed class Help : SparkyModuleBase
    {
        private readonly CommandService _commands;

        public Help(CommandService commands) => _commands = commands;

        [Command]
        public async Task HelpAsync()
        {
            var modules = _commands.Modules.Where(m => !m.IsSubmodule);

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle("Modules");
            var sb = new StringBuilder();
            foreach (var moduleInfo in modules)
                AppendModuleHelp(sb, moduleInfo);

            eb.WithDescription(sb.ToString());

            await ReplyAsync(embed: eb.Build());
        }

        [Command]
        public async Task HelpAsync([Remainder] string searchString)
        {
            var modules = _commands.Modules.Where(m => m.Name.Equals(searchString, StringComparison.OrdinalIgnoreCase) 
                || m.Aliases.Any(a => a.Equals(searchString, StringComparison.OrdinalIgnoreCase)));
            var commands = _commands.Commands
                .Where(c => c.Aliases.Any(a => a.Equals(searchString, StringComparison.OrdinalIgnoreCase)) 
                    && !modules.Any(m => m == c.Module));

            if (!modules.Any() && !commands.Any())
            {
                await ReplyAsync("No command or module matching that name was found.");
                return;
            }
            var msb = new StringBuilder();
            var csb = new StringBuilder();
            foreach (var module in modules)
            {
                AppendModuleHelp(msb, module);
                foreach (var cinfo in module.Commands)
                {
                    if (await ValidatePreconditionsAsync(cinfo))
                        AppendCommandHelp(msb, cinfo);
                }
            }

            foreach (var command in commands)
                if (await ValidatePreconditionsAsync(command))
                    AppendCommandHelp(csb, command);

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkBlue);
            if (msb.Length > 0)
                eb.AddField("Modules", msb.ToString());
            if (csb.Length > 0)
                eb.AddField("Commands", csb.ToString());

            eb.WithFooter("<> = required, [] = optional, ... = multiword");

            await ReplyAsync(embed: eb.Build());
        }

        private async Task<bool> ValidatePreconditionsAsync(CommandInfo info)
        {
            foreach (var precon in info.Preconditions)
            {
                if (!(await precon.CheckPermissionsAsync(Context, info, null)).IsSuccess)
                    return false;
            }
            return true;
        }

        private void AppendModuleHelp(StringBuilder sb, ModuleInfo moduleInfo)
        {
            sb.AppendLine($"**{char.ToUpper(moduleInfo.Name[0]) + moduleInfo.Name.Substring(1, moduleInfo.Name.Length - 1)}**");
            if (moduleInfo.Aliases.Count() > 1)
                sb.AppendLine($"Aliases: {string.Join(", ", moduleInfo.Aliases.Where(a => !a.Equals(moduleInfo.Name, StringComparison.OrdinalIgnoreCase)))}");
            if (moduleInfo.Summary != null)
                sb.AppendLine($"{moduleInfo.Summary}");
            sb.AppendLine();
        }

        private void AppendCommandHelp(StringBuilder sb, CommandInfo commandInfo)
        {
            sb.AppendLine($"**{(commandInfo.Name.Contains("ASync") ? commandInfo.Aliases.First() : commandInfo.Name)}**");
            if (commandInfo.Aliases.Count() > 1)
                sb.AppendLine($"Aliases: {string.Join(", ", commandInfo.Aliases.Where(a => !a.Equals(commandInfo.Name, StringComparison.OrdinalIgnoreCase)))}");
            sb.AppendLine($"Signature: {(commandInfo.Parameters.Count() > 0 ? string.Join(", ", commandInfo.Parameters.Select(p => FormatParameter(p))) : "none.")}");
            if (commandInfo.Summary != null)
                sb.AppendLine($"Summary: {commandInfo.Summary}");
        }

        private string FormatParameter(ParameterInfo info)
        {
            var fs = info.IsOptional ? "<{0}>" : "[{0}]";
            if (info.IsRemainder)
                fs += " ...";

            return string.Format(fs, info.Summary ?? info.Name);
        }
    }
}
