using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public interface IDownloadsDatabase
    {
        IEnumerable<IDownload> GetDownloads();

        IDownload GetDownload(int id);

        void UnregisterDownload(IDownload download, bool cleanUp);

        event DownloadAddedEventHandler DownloadAdded;

        event DownloadRemovedEventHandler DownloadRemoved;
    }
}
