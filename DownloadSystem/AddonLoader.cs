using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DownloadSystem.Shared;

namespace DownloadSystem
{
    class AddonLoader : IAddonDatabase
    {
        private Dictionary<string, IAddon> addons;

        public AddonLoader(CoreSystem system)
        {
            System = system;

            addons = new Dictionary<string, IAddon>();
        }

        public void UnloadAddons()
        {
            foreach(var a in addons.Values)
            {
                a.Unload();
            }
        }

        public CoreSystem System { get; private set; }

        public void LoadAddons()
        {
            if(!Directory.Exists("./addons"))
            {
                Directory.CreateDirectory("./addons");
            }

            RegisterAssembly(typeof(AddonLoader).Assembly);

            var files = Directory.EnumerateFiles("./addons", "*.dll");
            foreach (var f in files)
            {
                LoadAssemblyFile(f);
            }
        }

        public void Ready()
        {
            foreach(var a in addons.Values)
            {
                a.Ready();
            }
        }

        private void LoadAssemblyFile(string path)
        {
            try
            {
                var ass = Assembly.LoadFile(Path.GetFullPath(path));
                RegisterAssembly(ass);
            }
            catch (Exception e)
            {

            }
        }

        private void RegisterAssembly(Assembly ass)
        {
            var types = ass.GetTypes();
            var addonTypes = types
                .Where(o => typeof(IAddon).IsAssignableFrom(o));
            foreach (var t in addonTypes)
            {
                try
                {
                    var a = Activator.CreateInstance(t);
                    RegisterAddon((IAddon)a);
                }
                catch(Exception e)
                {
                    Console.WriteLine("Error while loading {0}", t);
                    Console.WriteLine(e);
                }
            }
        }

        public void RegisterAddon(IAddon addon)
        {
            CheckDo<IConfigurable>(addon, o =>
                {
                    System.ConfigSystem.AddConfigurable(o);
                });
            CheckDo<IInvokable>(addon, o =>
                {
                    System.InvokerSystem.AddInvokable(o);
                });
            CheckDo<IDownloader>(addon, o =>
                {
                    o.DownloadRegister = System.DownloadsSystem;
                    System.DownloadsSystem.AddDownloader(o);
                });

            //Interfaces
            CheckDo<IInvokerInterface>(addon, o =>
                {
                    o.InvokerDatabase = System.InvokerSystem;
                });
            CheckDo<IConfiguratorInterface>(addon, o =>
                {
                    o.ConfiguratorDatabase = System.ConfigSystem;
                });
            CheckDo<IDownloaderInterface>(addon, o =>
                {
                    o.DownloaderDatabase = System.DownloadsSystem;
                });
            CheckDo<IDownloadsInterface>(addon, o =>
                {
                    o.DownloadsDatabase = System.DownloadsSystem;
                });
            CheckDo<IAddonInterface>(addon, o =>
                {
                    o.AddonDatabse = this;
                });
            CheckDo<ISystemInterface>(addon, o =>
                {
                    o.System = System;
                });

            addon.Load();
            addons[addon.Name] = addon;
        }

        private void CheckDo<T>(IAddon addon, Action<T> action)
            where T : IAddon
        {
            if (typeof(T).IsInstanceOfType(addon))
            {
                action((T)addon);
            }
        }

        public IAddon GetAddon(string name)
        {
            return addons[name];
        }

        public IEnumerable<IAddon> GetAddons()
        {
            return addons.Values;
        }
    }
}
