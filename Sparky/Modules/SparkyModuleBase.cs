using Discord;
using Discord.Commands;
using Sparky.Database;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    public abstract class SparkyModuleBase : ModuleBase<SparkyCommandContext>
    {
        protected SparkyContext DbCtx { get; private set; }

        protected SparkyModuleBase()
        {
            DbCtx = new SparkyContext();
        }

        protected Task OkAsync() => Context.Message.AddReactionAsync(new Emoji("👌"));

        protected Task ErrorAsync() => Context.Message.AddReactionAsync(new Emoji("❌"));

        protected override async void AfterExecute(CommandInfo command)
        {
            try
            {
                await DbCtx.SaveChangesAsync();
            }
            finally
            {
                DbCtx.Dispose();
            }
        }
    }
}