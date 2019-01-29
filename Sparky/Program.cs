using System.Threading;
using System.Threading.Tasks;

namespace Sparky
{
    internal class Program
    {
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private static Task Main(string[] args)
            => new Core(_cts).IgniteAsync();
    }
}