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

        protected static Emoji OkEmoji => new Emoji("👌");

        protected static Emoji ErrorEmoji => new Emoji("❌");

        protected Task OkAsync() => Context.Message.AddReactionAsync(OkEmoji);

        protected Task ErrorAsync() => Context.Message.AddReactionAsync(ErrorEmoji);

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