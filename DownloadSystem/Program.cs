using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DownloadSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            var update = new UpdateSystem();
            if (!update.IsUpdating())
            {
                if (update.IsUpToDate())
                {
                    Console.WriteLine("Starting DownloadSystem");

                    var system = new CoreSystem();
                    system.Init();
                    Thread.Sleep(-1);
                }
                else
                {
                    Console.WriteLine("Starting to update DownloadSystem");

                    update.Update();
                }
            }
        }
    }
}
