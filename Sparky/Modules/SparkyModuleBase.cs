using Discord;
using Discord.Commands;
using Raven.Client.Documents.Session;
using Sparky.Models;
using Sparky.Services;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    public abstract class SparkyModuleBase : ModuleBase<SparkyCommandContext>
    {
        protected IAsyncDocumentSession Session { get; private set; }

        protected SparkyModuleBase()
        {
            Session = Database.Store.OpenAsyncSession();
        }

        protected Task OkAsync() => Context.Message.AddReactionAsync(new Emoji("👌"));

        protected override async void AfterExecute(CommandInfo command)
        {
            try
            {
                await Session.SaveChangesAsync();
            }
            finally
            {
                Session.Dispose();
            }
        }
    }
}