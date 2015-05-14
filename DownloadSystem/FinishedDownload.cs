using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DownloadSystem.Shared;

namespace DownloadSystem
{
    class FinishedDownload : IDownload
    {
        public FinishedDownload(string path)
        {
            Name = System.IO.Path.GetFileName(path);
            Path = path;

            if(Directory.Exists(path))
            {
                Files = Directory.EnumerateFiles(path);
            }
            else
            {
                Files = new string[] { path };
            }

            Size = Files
                .Aggregate<string, long>(0, (acc, curr) => acc + new FileInfo(curr).Length);
        }

        public DownloadStatus Status
        {
            get { return DownloadStatus.Finished; }
        }

        public double Progress
        {
            get { return 1; }
        }

        public string Path { get; private set; }

        public string Name { get; private set; }

        public IDownloader Downloader
        {
            get { return null; }
        }

        public void CleanUp()
        {
            foreach(var f in Files)
            {
                File.Delete(f);
            }
            if(Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }

        public override string ToString()
        {
            return string.Format("[{0}] <#{1}>", Name, ID);
        }

        public int ID { get; set; }

        public event DownloadStatusChangedEventHandler DownloadStatusChanged;

        public event DownloadProgressUpdatedEventHandler DownloadProgressUpdated;

        public IEnumerable<string> Files { get; private set; }

        public bool Continuable
        {
            get { return false; }
        }


        public Database.DataEntry SaveContinuable()
        {
            throw new NotSupportedException();
        }


        public DateTime Started { get; set; }

        public DateTime Finished { get; set; }


        public int DownloadSpeed
        {
            get { return 0; }
        }

        public long Size { get; private set; }


        public long DownloadedBytes
        {
            get { return Size; }
        }
    }
}
