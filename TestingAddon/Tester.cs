using DownloadSystem.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TestingAddon
{
    public class Tester : IDownloaderInterface
    {
        private Timer timer;
        private Timer progressTimer;
        private IDownload download;

        public IDownloaderDatabase DownloaderDatabase { get; set; }

        public void Load()
        {
            timer = new Timer(10000);
            timer.Elapsed += timer_Elapsed;
            timer.AutoReset = false;

            progressTimer = new Timer(1000);
            progressTimer.Elapsed += progressTimer_Elapsed;
            progressTimer.AutoReset = true;

            //timer.Start();
        }

        void progressTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Current progress: {0:0.##}%", download.Progress * 100);
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var down = DownloaderDatabase.GetDownloader("torrent");
            download = down.Download(@"http://www.nyaa.se/?page=download&tid=675337", "./downloads");
            download.DownloadStatusChanged += d_DownloadStatusChanged;
            progressTimer.Start();
        }

        void d_DownloadStatusChanged(IDownload download, DownloadStatus oldState, DownloadStatus newState)
        {
            Console.WriteLine("Status changed: {0} -> {1}", oldState, newState);
        }

        public void Unload()
        {
            //throw new NotImplementedException();
        }

        public string Name { get { return "test"; } }

        public string FullName { get { return "Testing Addon"; } }


        public void Ready()
        {
        }
    }
}
