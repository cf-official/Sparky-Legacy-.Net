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
    public sealed class HelpModule : SparkyModuleBase
    {
        private readonly CommandService _commands;

        public HelpModule(CommandService commands) => _commands = commands;

        [Command]
        public async Task HelpAsync()
        {
            var modules = _commands.Modules.Where(m => !m.IsSubmodule);

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkBlue)
                .WithTitle("Modules");

            foreach (var moduleInfo in modules)
                MapModule(eb, moduleInfo);

            await ReplyAsync(embed: eb.Build());
        }

        [Command]
        public async Task HelpAsync(string commandName)
        {
            var matches = _commands.Commands.Where(c => c.Name.Equals(commandName, StringComparison.InvariantCultureIgnoreCase));
            if (matches.Count() == 0)
            {
                await ReplyAsync("No command matching your query was found.");
                return;
            }

            var eb = new EmbedBuilder()
                .WithColor(Color.DarkBlue);
            foreach (var commandInfo in matches)
                MapCommand(eb, commandInfo);

            await ReplyAsync(embed: eb.Build());
        }

        private EmbedBuilder MapModule(EmbedBuilder eb, ModuleInfo moduleInfo)
        {
            var sb = new StringBuilder()
                .AppendLine($"Aliases: {string.Join(", ", moduleInfo.Aliases)}")
                .AppendLine($"Commands: {moduleInfo.Commands.Count}")
                .AppendLine()
                .AppendLine($"Remarks: {moduleInfo.Summary}");

            eb.AddField(moduleInfo.Group ?? moduleInfo.Name, sb.ToString());

            return eb;
        }

        private EmbedBuilder MapCommand(EmbedBuilder eb, CommandInfo commandInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Aliases: {string.Join(", ", commandInfo.Aliases)}")
                .AppendLine($"Parameters: {string.Join(", ", commandInfo.Parameters.Select(p => GetVisualRepresentation(p)))}")
                .AppendLine()
                .AppendLine($"Remarks: {commandInfo.Summary}");

            eb.AddField(commandInfo.Name, sb.ToString());

            return eb;
        }

        private string GetVisualRepresentation(ParameterInfo parameterInfo)
        {
            var fs = parameterInfo.IsOptional ? "[{0}]" : "<{0}>";
            if (parameterInfo.IsRemainder)
                fs += " ...";
            else if (parameterInfo.IsOptional)
                fs = "... " + fs;

            return string.Format(fs, parameterInfo.Summary ?? parameterInfo.Name);
        }
    }
}
