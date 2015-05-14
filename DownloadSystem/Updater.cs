using DownloadSystem.Shared;
using Jayrock.Json;
using Jayrock.Json.Conversion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using Timer = System.Timers.Timer;

namespace DownloadSystem
{
    public class Updater : IConfigurable, IAddonInterface, ISystemInterface, IInvokable
    {
        private class Config : EasyConfigurator
        {
            public Config(Updater up)
                : base(up)
            {
                RegisterTupleParser<string, string>();

                RegisterVar<string>("last_core_update", up, "LastCoreUpdate");
                RegisterVar<Tuple<string, string>[]>("last_addon_updates",
                    s => up.LastAddonUpdates
                        .Select(o => new Tuple<string, string>(o.Key, o.Value))
                        .ToArray(),
                    (s, o) => up.LastAddonUpdates = o
                        .ToDictionary<Tuple<string, string>, string, string>(
                        o2 => o2.Item1, o2 => o2.Item2));
                //RegisterVar<bool>("autorestart",
                //    s => )
                RegisterVar<bool>("autoshutdown", up, "AutoShutdown");
            }
        }

        private class Invok : EasyInvoker
        {
            public Invok(Updater updater)
                : base(updater)
            {
                RegisterMethod("update", updater, "UpdateNow");
            }
        }

        private const string CORE_UPDATE = "http://dark-link.info/apps/dlsystem/core";
        private const string ADDON_UPDATE = "http://dark-link.info/apps/dlsystem/addons/{0}";

        private readonly string[] ignoreAddons = new[] { "updater" };

        private string[] updateAddons;
        private Config config;
        private Invok invoker;
        private MD5 md5;
        private Timer timer;

        public Updater()
        {
            config = new Config(this);
            invoker = new Invok(this);

            LastCoreUpdate = "";
            LastAddonUpdates = new Dictionary<string, string>();
            md5 = MD5.Create();
            timer = new Timer(7200000);
            timer.AutoReset = true;
            timer.Elapsed += timer_Elapsed;
        }


        public void UpdateNow()
        {
            timer_Elapsed(null, null);
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();

            if(!Update())
            {
                timer.Start();
            }
            else
            {
                System.Shutdown();
            }
        }

        public void Load()
        {
        }

        public void Ready()
        {
            new Thread(new ThreadStart(() => timer_Elapsed(null, null))).Start();
        }

        public void Unload()
        {
            timer.Stop();
        }

        #region Update Addons
        private void LoadUpdateAddons()
        {
            updateAddons = File.ReadAllLines("update.addons");
        }

        private void WriteUpdateAddons()
        {
            var writer = new StreamWriter("update.addons");
            var list = new List<string>();
            foreach(var a in AddonDatabse.GetAddons())
            {
                writer.WriteLine(a.Name);
                list.Add(a.Name);
            }
            writer.Close();
            updateAddons = list.ToArray();
        }

        private void CheckUpdateAddons()
        {
            updateAddons = updateAddons
                .Where(o => !ignoreAddons.Contains(o))
                .ToArray();
        }
        #endregion

        private bool Update()
        {
            string newMd5;
            var updated = false;
            try
            {
                if (NeedCoreUpdate(out newMd5))
                {
                    UpdateFiles(CORE_UPDATE);
                    LastCoreUpdate = newMd5;
                    config.SaveConfig("last_core_update");

                    updated = true;
                }
            }
            catch { }

            foreach(var a in updateAddons)
            {
                try
                {
                    if (NeedAddonUpdate(a, out newMd5))
                    {
                        UpdateFiles(string.Format(ADDON_UPDATE, a));
                        LastAddonUpdates[a] = newMd5;
                        config.SaveConfig("last_addon_updates");

                        updated = true;
                    }
                }
                catch { }
            }

            return updated;
        }

        private bool NeedCoreUpdate(out string newMd5)
        {
            newMd5 = "";
            try
            {
                newMd5 = GetRemoteMD5Hash(string.Format("{0}/md5.json", CORE_UPDATE));
                return newMd5 != LastCoreUpdate;
            }
            catch { }
            return false;
        }

        private bool NeedAddonUpdate(string addon, out string newMd5)
        {
            newMd5 = "";
            try
            {
                newMd5 = GetRemoteMD5Hash(string.Format("{0}/md5.json",
                    string.Format(ADDON_UPDATE, addon)));
                return newMd5 != LastAddonUpdates[addon];
            }
            catch { }
            return false;
        }

        private void UpdateFiles(string urlBase)
        {
            var jarr = GetFiles(string.Format("{0}/md5.json", urlBase));

            foreach(JsonObject j in jarr)
            {
                var path = (string)j["path"];
                var md5str = (string)j["md5"];
                var localmd5str = GetLocalOrUpdateMD5Hash(path);
                if(localmd5str != md5str)
                {
                    DownloadFile(urlBase, path);
                }
            }
        }

        private string GetRemoteMD5Hash(string url)
        {
            var web = new WebClient();
            var str = web.DownloadString(url);
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
            return string.Join("", bytes.Select(o => o.ToString("X2")));
        }

        private string GetLocalOrUpdateMD5Hash(string file)
        {
            var localFile = string.Format("./{0}", file);
            var updateFile = string.Format("./update/{0}", file);
            if(File.Exists(updateFile))
            {
                return GetLocalMD5Hash(updateFile);
            }
            else if(File.Exists(localFile))
            {
                return GetLocalMD5Hash(localFile);
            }
            return "";
        }

        private string GetLocalMD5Hash(string file)
        {
            var fstream = File.OpenRead(file);
            var bytes = md5.ComputeHash(fstream);
            fstream.Close();
            return string.Join("", bytes.Select(o => o.ToString("X2")));
        }

        private JsonArray GetFiles(string url)
        {
            var web = new WebClient();
            var str = web.DownloadString(url);
            var jarr = (JsonArray)JsonConvert.Import(str);
            return jarr;
        }

        private void DownloadFile(string urlBase, string file)
        {
            CreateNecessaryDirectories(Path.GetDirectoryName(string.Format("./update/{0}", file)));

            var web = new WebClient();
            web.DownloadFile(string.Format("{0}/{1}", urlBase, file),
                string.Format("./update/{0}", file));
        }

        private void CreateNecessaryDirectories(string folder)
        {
            var parent = Path.GetDirectoryName(folder);
            if (!Directory.Exists(parent))
            {
                CreateNecessaryDirectories(parent);
            }
            Directory.CreateDirectory(folder);
        }

        public string LastCoreUpdate { get; set; }

        public Dictionary<string, string> LastAddonUpdates { get; set; }

        public string Name
        {
            get { return "updater"; }
        }

        public string FullName
        {
            get { return "Downloader System Updater"; }
        }

        public IConfigurator Configurator
        {
            get { return config; }
        }

        public void ConfigLoaded()
        {
            if (File.Exists("update.addons"))
            {
                LoadUpdateAddons();
            }
            else
            {
                WriteUpdateAddons();
            }
            CheckUpdateAddons();

            foreach(var addon in updateAddons)
            {
                if(!LastAddonUpdates.ContainsKey(addon))
                {
                    LastAddonUpdates[addon] = "";
                }
            }
        }

        public bool AutoShutdown { get; set; }

        public IAddonDatabase AddonDatabse { get; set; }

        public ISystem System { get; set; }

        public IInvoker Invoker { get { return invoker; } }
    }
}
