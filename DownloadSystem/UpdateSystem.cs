using DownloadSystem.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem
{
    static class UpdateSystem
    {
        public static bool IsUpToDate()
        {
            if (Directory.Exists("./update"))
            {
                return !Directory.EnumerateFileSystemEntries("./update").Any();
            }
            else
            {
                Directory.CreateDirectory("./update");
            }
            return true;
        }

        public static bool IsUpdating()
        {
            return File.Exists("update.lock");
        }

        public static void Update()
        {
            var tmpFile = Path.GetTempFileName();
            var tmpExe = string.Format("{0}.exe", tmpFile);

            var file = File.OpenWrite(tmpFile);
            var names = typeof(UpdateSystem).Assembly.GetManifestResourceNames();
            var stream = typeof(UpdateSystem).Assembly.GetManifestResourceStream("DownloadSystem.updater.exe");
            stream.CopyTo(file);
            file.Close();
            File.Move(tmpFile, tmpExe);

            Console.Write("Starting updater...");
            if (Environment.OSVersion.VersionString.Contains("Windows"))
            {
                Process.Start(tmpExe, string.Format("{0} \"{1}\" {2}",
                    Process.GetCurrentProcess().Id,
                    Environment.CurrentDirectory,
                    Settings.Default.UpdaterAutoRestart));
            }
            else
            {
                Process.Start("mono", string.Format("\"{0}\" {1} \"{2}\" {3}",
                    tmpExe,
                    Process.GetCurrentProcess().Id,
                    Environment.CurrentDirectory,
                    Settings.Default.UpdaterAutoRestart));
            }
            Console.WriteLine("!");
        }
    }
}
