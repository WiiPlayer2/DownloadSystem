using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public delegate void DownloadStatusChangedEventHandler(
        IDownload download, DownloadStatus oldState, DownloadStatus newState);

    public delegate void DownloadProgressUpdatedEventHandler(
        IDownload download, double progress);

    public delegate void SaveConfigDelegate(string name);

    public delegate void DownloadAddedEventHandler(IDownload download);

    public delegate void DownloadRemovedEventHandler(IDownload download);
}
