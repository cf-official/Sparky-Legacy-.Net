using Discord.Commands;
using Raven.Client.Documents.Session;
using Sparky.Models;
using Sparky.Services;

namespace Sparky.Modules
{
    public abstract class SparkyModuleBase : ModuleBase<SparkyCommandContext>
    {
        protected IAsyncDocumentSession Session { get; private set; }

        protected override async void BeforeExecute(CommandInfo command)
        {
            try
            {
                Session = Database.Store.OpenAsyncSession();
            }
            finally
            {
            }
        }

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