using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    [Summary("Provides commands useful in everyday use of the bot.")]
    public sealed class Utilities : SparkyModuleBase
    {
        [Command("cleanup")]
        [Summary("Deletes messages that triggered a command, and all bot responses.")]
        public async Task CleanupAsync([Summary("100")] int limit = 100)
        {
            var messages = await Context.Channel.GetMessagesAsync(limit).FlattenAsync();
            var messagesToDelete = messages.Where(m => m.Content.StartsWith(Configuration.Get<string>("prefix")) 
                || m.Content.StartsWith(Context.Client.CurrentUser.Mention) 
                || m.Author.Id == Context.Client.CurrentUser.Id);

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messagesToDelete);
        }

        [Command("purge")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [Summary("Bulk deletes messages.")]
        public async Task PurgeAsync([Summary("100")] int limit = 100)
        {
            var messages = await Context.Channel.GetMessagesAsync(limit).FlattenAsync();

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messages);
        }

        [Command("purge")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [Summary("Bulk deletes messages, filtering by user.")]
        public async Task PurgeAsync([Summary("@user")] IUser user, [Summary("100")] int limit = 100)
        {
            var messages = await Context.Channel.GetMessagesAsync(limit).FlattenAsync();
            var messagesToDelete = messages.Where(m => m.Author.Id == user.Id);

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messagesToDelete);
        }

        [Command("purge")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        [Summary("Bulk deletes messages, filtering by user(s).")]
        public async Task PurgeAsync([Summary("100")] int limit, [Summary("@user")] params IUser[] users)
        {
            var messages = await Context.Channel.GetMessagesAsync(limit).FlattenAsync();
            var messagesToDelete = messages.Where(m => users.Any(u => u.Id == m.Author.Id));

            await (Context.Channel as SocketTextChannel).DeleteMessagesAsync(messagesToDelete);
        }

        [Command("eval")]
        [Alias("```cs", "```csharp")]
        [RequireOwner]
        public async Task EvalAsync([Remainder] string code)
        {
            var codeMatch = GetCode(code);
            if (codeMatch is null && (code.StartsWith('`') || code.EndsWith('`')))
                code = code.Trim(' ', '`');

            else if (codeMatch != null)
                code = codeMatch.Value.Code;

            var message = await ReplyAsync(embed: new EmbedBuilder()
                .WithAuthor(Context.User)
                .WithDescription("Evaluating...")
                .WithColor(Color.Orange)
                .Build());

            var globals = new Globals(Context, Context.Services);
            var options = ScriptOptions.Default
                .WithImports(EvalNamespaces)
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)));

            var csw = Stopwatch.StartNew();
            var script = CSharpScript.Create(code, options, typeof(Globals));
            var compiledScript = script.Compile();
            csw.Stop();

            if (compiledScript.Any(x => x.Severity == DiagnosticSeverity.Error))
            {
                var eb = new EmbedBuilder()
                    .WithTitle("Evaluation failed during compilation")
                    .WithDescription($"{compiledScript.Length} {(compiledScript.Length > 1 ? "errors" : "error")}")
                    .WithColor(Color.Red)
                    .WithFooter($"{csw.ElapsedMilliseconds}ms");

                foreach (var diagnostic in compiledScript.Take(4))
                {
                    var span = diagnostic.Location.GetLineSpan().Span;
                    eb.AddField($"Error `{diagnostic.Id}` at {span.Start} - {span.End}", diagnostic.GetMessage());
                }

                if (compiledScript.Length > 4)
                    eb.AddField($"Skipped {compiledScript.Length - 5} {(compiledScript.Length - 5 > 1 ? "errors" : "error")}", "You should be able to fix it.");

                await message.ModifyAsync(x => x.Embed = eb.Build());
                return;
            }

            ScriptState<object> st = null;
            Exception ex = null;
            var rsw = Stopwatch.StartNew();

            try
            {
                st = await script.RunAsync(globals);
            }
            catch (Exception e)
            {
                ex = e;
            }

            rsw.Stop();

            if (ex is null)
            {
                var rv = st.ReturnValue;
                if (rv != null && (!string.IsNullOrWhiteSpace(rv.ToString()) || rv is Embed || rv is EmbedBuilder))
                {
                    if (rv.ToString().Length > 2000)
                    {
                        _ = message.DeleteAsync();
                        await Context.Channel.SendFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(rv.ToString())), $"eval-{Context.Message.Id}.txt", "");
                    }

                    else
                    {
                        if (rv is Embed embed)
                            await ReplyAsync(embed: embed);

                        else if (rv is EmbedBuilder embedBuilder)
                        {
                            try
                            {
                                embed = embedBuilder.Build();
                                await ReplyAsync(embed: embed);
                            }
                            catch (Exception exception) when (!(exception is HttpException))
                            {
                                await ReplyAsync($"Failed to build embed;\n{exception.GetType()}: {exception.Message}");
                            }
                        }

                        await message.ModifyAsync(x => x.Embed = new EmbedBuilder()
                        .WithTitle($"[{rv.GetType()}]")
                        .WithDescription(rv.ToString())
                        .WithColor(Color.Green)
                        .WithFooter($"Compiled in {csw.ElapsedMilliseconds}ms | Executed in {rsw.ElapsedMilliseconds}ms")
                        .Build());
                    }
                }

                else
                    await message.ModifyAsync(x => x.Embed = new EmbedBuilder()
                        .WithDescription("No result was returned.")
                        .WithColor(Color.Green)
                        .WithFooter($"Compiled in {csw.ElapsedMilliseconds}ms | Executed in {rsw.ElapsedMilliseconds}ms")
                        .Build());
            }

            if (ex != null)
                await message.ModifyAsync(x => x.Embed = new EmbedBuilder()
                    .WithTitle("Evaluation failed during execution")
                    .WithDescription($"**{ex.GetType()}**: {ex.Message}")
                    .WithColor(Color.Red)
                    .WithFooter($"{rsw.ElapsedMilliseconds}ms")
                    .Build());
        }

        private static readonly string[] EvalNamespaces = new[]
           {
            "System", "System.Diagnostics", "System.Threading.Tasks", "System.Linq",
            "System.Text", "System.Collections.Generic", "System.Reflection", "Microsoft.Extensions.DependencyInjection",
            "Discord", "Discord.Rest", "Discord.WebSocket"
        };

        public static Regex SingleCodeBlockRegex = new Regex(@"^```(?<language>(?:\w+)?)(?:\n)?(?<code>.*?)```$", RegexOptions.Compiled | RegexOptions.Singleline);

        private static (string Language, string Code, Match Match)? GetCode(string text)
        {
            var match = SingleCodeBlockRegex.Match(text);
            if (!match.Success)
                return null;

            return (match.Groups["language"].Value, match.Groups["code"].Value, match);
        }
    }
}
