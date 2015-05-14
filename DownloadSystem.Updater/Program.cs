using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            FileStream fstream = null;
            bool autorestart = false;
            try
            {
                Console.WriteLine("Updating DownloadSystem");
                Console.WriteLine("Args: {0}", string.Join(" ", args.Select(o =>
                {
                    if(o.Contains(" "))
                    {
                        return string.Format("\"{0}\"", o);
                    }
                    return o;
                })));

                var pid = int.Parse(args[0]);
                var folder = args[1];
                autorestart = bool.Parse(args[2]);

                //Console.WriteLine(pid);
                //Console.WriteLine(folder);

                try
                {
                    var process = Process.GetProcessById(pid);
                    if (process != null && !process.HasExited)
                    {
                        process.WaitForExit();
                    }
                }
                catch { }

                Environment.CurrentDirectory = folder;

                fstream = File.Create("update.lock");

                foreach (var f in Directory.EnumerateFiles("./update"))
                {
                    UpdateFile(f);
                }

                foreach (var d in Directory.EnumerateDirectories("./update"))
                {
                    UpdateDirectory(d);
                }

                Console.WriteLine("Update finished!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            if(fstream != null)
            {
                fstream.Close();
            }

            if(File.Exists("update.lock"))
            {
                File.Delete("update.lock");
            }

            if (autorestart)
            {
                if (Environment.OSVersion.VersionString.Contains("Windows"))
                {
                    Process.Start("DownloadSystem.exe");
                }
                else
                {
                    Process.Start("mono", "DownloadSystem.exe");
                }
            }
        }

        private static void UpdateFile(string f)
        {
            var newF = f.Replace("./update", ".");
            Console.WriteLine("Update {0}...", newF);
            if (File.Exists(newF))
            {
                File.Delete(newF);
            }
            File.Move(f, newF);
        }

        private static void UpdateDirectory(string d)
        {
            var newD = d.Replace("./update", ".");
            if(!Directory.Exists(newD))
            {
                Directory.CreateDirectory(newD);
            }

            foreach(var f in Directory.EnumerateFiles(d))
            {
                UpdateFile(f);
            }

            foreach(var subD in Directory.EnumerateDirectories(d))
            {
                UpdateDirectory(subD);
            }

            Directory.Delete(d);
        }
    }
}
