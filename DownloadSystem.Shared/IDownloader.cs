using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public interface IDownloader : IAddon
    {
        IDownloadRegister DownloadRegister { get; set; }

        IDownload Download(string url, string path);

        IDownload Continue(Database.DataEntry data);

        IEnumerable<IDownload> GetDownloads();
    }
}
