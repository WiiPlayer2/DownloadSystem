using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DownloadSystem.Shared;

namespace DownloadSystem
{
    class DownloadsSystem : IDownloaderDatabase, IDownloadsDatabase, IDownloadRegister
    {
        private Dictionary<string, IDownloader> downloaders;
        private List<IDownload> downloads;
        private int nextDownloadID = 0;
        private Database downloadsData;

        public DownloadsSystem(CoreSystem system)
        {
            System = system;

            downloaders = new Dictionary<string, IDownloader>();
            downloads = new List<IDownload>();
            downloadsData = new Database("downloads.data");

            if (downloadsData.ContainsKey("nextID"))
            {
                nextDownloadID = int.Parse(downloadsData["nextID"].AsString);
            }
            if (!downloadsData.ContainsKey("downloads"))
            {
                downloadsData["downloads"] = new Database.DataObject();
            }
        }

        public CoreSystem System { get; private set; }

        public void AddDownloader(IDownloader downloader)
        {
            downloaders[downloader.Name] = downloader;
        }

        public IDownloader GetDownloader(string name)
        {
            return downloaders[name];
        }

        public IEnumerable<IDownload> GetDownloads()
        {
            return downloads;
        }

        public void UnregisterDownload(IDownload download, bool cleanUp)
        {
            if(!cleanUp && download.Status != DownloadStatus.Finished)
            {
                throw new NotSupportedException();
            }
            if (cleanUp)
            {
                download.CleanUp();
            }
            download.DownloadStatusChanged -= download_DownloadStatusChanged;
            downloadsData["downloads"].AsObject.Remove(download.ID.ToString());
            downloads.Remove(download);

            if (DownloadRemoved != null)
            {
                DownloadRemoved(download);
            }
        }

        public event DownloadAddedEventHandler DownloadAdded;

        public event DownloadRemovedEventHandler DownloadRemoved;

        public void RegisterDownload(IDownload download, bool isNew)
        {
            downloads.Add(download);
            download.DownloadStatusChanged += download_DownloadStatusChanged;

            if (isNew)
            {
                var dobj = new Database.DataObject();
                var now = DateTime.Now;
                download.Started = now;
                download.ID = NextDownloadID();
                dobj["started"] = now.ToString(CultureInfo.InvariantCulture);
                dobj["downloader"] = download.Downloader.Name;
                dobj["id"] = download.ID.ToString();
                downloadsData["downloads"]
                    .AsObject[download.ID.ToString()] = dobj;
            }

            if (DownloadAdded != null)
            {
                DownloadAdded(download);
            }
        }

        void download_DownloadStatusChanged(IDownload download, DownloadStatus oldState, DownloadStatus newState)
        {
            if (newState == DownloadStatus.Finished)
            {
                var dobj = downloadsData["downloads"]
                    .AsObject[download.ID.ToString()].AsObject;

                var now = DateTime.Now;
                download.Finished = now;
                dobj["finished"] = now.ToString(CultureInfo.InvariantCulture);
                dobj["downloader"] = "";
                dobj["continueData"] = download.Path;
            }
        }

        private int NextDownloadID()
        {
            var ret = nextDownloadID;
            do
            {
                nextDownloadID++;
            } while (downloads.Any(o => o.ID == nextDownloadID));
            downloadsData["nextID"] = nextDownloadID.ToString();
            return ret;
        }

        public void LoadDownloads()
        {
            foreach (var kv in downloadsData["downloads"].AsObject.ToArray())
            {
                IDownload down;
                var dobj = kv.Value.AsObject;
                try
                {
                    if (dobj["downloader"].AsString == "")
                    {
                        down = new FinishedDownload(dobj["continueData"].AsString);
                        down.Finished = DateTime.Parse(dobj["finished"].AsString, CultureInfo.InvariantCulture);
                        RegisterDownload(down, false);
                    }
                    else
                    {
                        var downl = GetDownloader(dobj["downloader"].AsString);
                        down = downl.Continue(dobj["continueData"]);
                    }
                    down.Started = DateTime.Parse(dobj["started"].AsString, CultureInfo.InvariantCulture);
                    down.ID = int.Parse(dobj["id"].AsString);
                }
                catch(Exception e)
                {
                    downloadsData["downloads"].AsObject.Remove(dobj["id"].AsString);
                }
            }
        }

        public void Unload()
        {
            foreach (var d in downloads
                .Where(o => o.Status == DownloadStatus.Downloading))
            {
                if (d.Continuable)
                {
                    downloadsData["downloads"]
                        .AsObject[d.ID.ToString()]
                        .AsObject["continueData"] = d.SaveContinuable();
                }
                else
                {
                    d.CleanUp();
                    downloadsData["downloads"].AsObject.Remove(d.ID.ToString());
                }
            }
        }

        public IDownload GetDownload(int id)
        {
            return downloads.FirstOrDefault(o => o.ID == id);
        }

        public void RegisterContinueData(IDownload download)
        {
            if(download.Continuable)
            {
                downloadsData["downloads"]
                    .AsObject[download.ID.ToString()]
                    .AsObject["continueData"] = download.SaveContinuable();
            }
            else
            {
                throw new DownloadSystemInternalException("This download is not continuable.");
            }
        }
    }
}
