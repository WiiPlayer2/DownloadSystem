using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DownloadSystem.Shared;

namespace DownloadSystem
{
    class CoreSystem : ISystem
    {
        private Dictionary<string, IConfigurable> configurables;
        private bool shuttedDown;
        private Mutex shutdownMutex;

        public CoreSystem()
        {
            shutdownMutex = new Mutex();
            configurables = new Dictionary<string, IConfigurable>();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            AddonLoader = new AddonLoader(this);
            ConfigSystem = new ConfigSystem(this);
            DownloadsSystem = new DownloadsSystem(this);
            InvokerSystem = new InvokerSystem(this);
        }

        public void Init()
        {
            AddonLoader.LoadAddons();
            ConfigSystem.LoadCfgFile();

            ConfigSystem.OnReady();

            DownloadsSystem.LoadDownloads();

            AddonLoader.Ready();
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Shutdown();
        }

        public AddonLoader AddonLoader { get; private set; }

        public ConfigSystem ConfigSystem { get; private set; }

        public DownloadsSystem DownloadsSystem { get; private set; }

        public InvokerSystem InvokerSystem { get; private set; }

        public void Shutdown()
        {
            if (!shuttedDown)
            {
                shuttedDown = true;
                shutdownMutex.WaitOne();

                try
                {
                    Console.WriteLine("Shutdown Initiated");

                    Console.WriteLine("Unload Addons");
                    AddonLoader.UnloadAddons();

                    Console.WriteLine("Unload DownloadsSystem");
                    DownloadsSystem.Unload();
                }
                catch (Exception e)
                {
                    Console.WriteLine("[SHUTDOWN ERROR] {0}", e);
                }

                //Console.WriteLine("Exit.");

                //new Thread(new ThreadStart(() =>
                //{
                //    Thread.Sleep(5000);
                Console.WriteLine("Force Exit.");
                Process.GetCurrentProcess().Kill();
                //})).Start();

                Environment.Exit(0);
                shutdownMutex.ReleaseMutex();
            }
        }
    }
}
