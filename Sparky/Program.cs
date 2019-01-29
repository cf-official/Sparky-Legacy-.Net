using System.Threading;
using System.Threading.Tasks;

namespace Sparky
{
    class Program
    {
        private static CancellationTokenSource _cts = new CancellationTokenSource();

        static Task Main(string[] args)
            => new Core(_cts).IgniteAsync();
    }
}
