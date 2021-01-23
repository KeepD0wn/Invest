using System;
using System.Threading.Tasks;
using System.Threading;

namespace Invest
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            var token = "t.84BlJbnuiEon73i3I-zxDuUqNHrZ6TuJZWO_G3lEEQJeT7L1qkIqOZ1jhFFl27B0fdNDuva-u0xAZvpX9hSjyw";
            await using var bot = new SandboxMy(token);
            
                await bot.StartAsync();
                Thread.Sleep(2000);
           
            

            Console.ReadKey();

        }
    }
}
