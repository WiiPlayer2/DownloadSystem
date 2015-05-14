using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.UpdateUploader
{
    class Program
    {
        static void Main(string[] args)
        {
            //UpdateAddon("webinterface");

            UpdateCore();

            foreach(var f in Directory.EnumerateFiles("./", "*.addon.update"))
            {
                var name = Path.GetFileName(f);
                name = name.Substring(0, name.Length - 13);
                UpdateAddon(name);
            }

            Console.WriteLine("Finished!");
            Console.ReadKey(true);
        }

        private static void UpdateCore()
        {
            Update("core.update", "/var/www/apps/dlsystem/core");
        }

        private static void Update(string file, string path)
        {
            var script = new Script(file);
            script.Excecute("dark-link.info", "root", new PrivateKeyFile("id_rsa"), path);
        }

        private static void UpdateAddon(string name)
        {
            Update(string.Format("{0}.addon.update", name),
                string.Format("/var/www/apps/dlsystem/addons/{0}", name));
        }
    }
}
