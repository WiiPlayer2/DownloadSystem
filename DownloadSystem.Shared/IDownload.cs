using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public interface IDownload
    {
        DownloadStatus Status { get; }

        double Progress { get; }

        int DownloadSpeed { get; }

        long DownloadedBytes { get; }

        long Size { get; }

        string Path { get; }

        string Name { get; }

        bool Continuable { get; }

        IEnumerable<string> Files { get; }

        IDownloader Downloader { get; }

        void CleanUp();

        Database.DataEntry SaveContinuable();

        int ID { get; set; }

        DateTime Started { get; set; }

        DateTime Finished { get; set; }

        event DownloadStatusChangedEventHandler DownloadStatusChanged;

        event DownloadProgressUpdatedEventHandler DownloadProgressUpdated;
    }
}
