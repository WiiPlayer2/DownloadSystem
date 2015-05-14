using ICSharpCode.SharpZipLib.Zip;
using Jayrock.Json;
using Renci.SshNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DownloadSystem.UpdateUploader
{
    class Script
    {
        private List<Regex> regexes;
        private Dictionary<string, Action<Match>> actions;
        private string[] lines;
        private MD5 md5;
        private string sfile;

        private string rootFolder;
        private List<string> addFiles;
        private string packRootFolder;
        private List<string> packFiles;
        private List<string> packMakeDirs;
        private List<string> makeDirs;
        Dictionary<string, List<string>> packs;
        Dictionary<string, List<string>> packsMakeDirs;
        Dictionary<string, string> packsRoot;

        public Script(string file)
        {
            sfile = file;

            regexes = new List<Regex>();
            actions = new Dictionary<string, Action<Match>>();
            md5 = MD5.Create();

            Reg("root", "(?<path>.*)", SetRoot);
            Reg("add", "(?<path>.*)", AddFile);
            Reg("pack", "(?<path>.*)", PackStart);
            Reg("p.add", "(?<path>.*)", PackAddFile);
            Reg("p.mkdir", "(?<path>.*)", PackMakeDirectory);
            Reg("p.end", "(?<path>.*)", PackEnd);
            Reg("mkdir", "(?<path>.*)", MakeDirectory);

            lines = File.ReadAllLines(file);
        }

        private void Reg(string keyword, string regex, Action<Match> action)
        {
            regexes.Add(new Regex(string.Format(@"^(?<keyword>{0})\ {1}$", keyword, regex)));
            actions[keyword] = action;
        }

        #region Reg-Actions
        private void PackMakeDirectory(Match m)
        {
            packMakeDirs.Add(m.Groups["path"].Value);
        }

        private void MakeDirectory(Match m)
        {
            makeDirs.Add(m.Groups["path"].Value);
        }

        private void SetRoot(Match m)
        {
            rootFolder = m.Groups["path"].Value;
            if (!Directory.Exists(rootFolder))
            {
                throw new ArgumentException(string.Format("Directory does not exist. ({0})", rootFolder));
            }
        }

        private void AddFile(Match m)
        {
            var path = m.Groups["path"].Value;
            var file = Path.Combine(rootFolder, path);
            if (!File.Exists(file))
            {
                throw new ArgumentException(string.Format("File does not exist. ({0})", file));
            }
            addFiles.Add(path);
        }

        private void PackStart(Match m)
        {
            var path = Path.Combine(rootFolder, m.Groups["path"].Value);

            if (packFiles != null)
            {
                throw new InvalidOperationException("Pack has already been started.");
            }
            if (!Directory.Exists(path))
            {
                throw new ArgumentException(string.Format("Directory does not exist. ({0})", path));
            }

            packFiles = new List<string>();
            packRootFolder = path;
            packMakeDirs = new List<string>();
        }

        private void PackAddFile(Match m)
        {
            var path = m.Groups["path"].Value;
            var file = Path.Combine(packRootFolder, path);
            if (!File.Exists(file))
            {
                throw new ArgumentException(string.Format("File does not exist. ({0})", file));
            }
            packFiles.Add(path);
        }

        private void PackEnd(Match m)
        {
            if (packFiles == null)
            {
                throw new InvalidOperationException("Pack has not been started.");
            }
            var path = m.Groups["path"].Value;
            packs[path] = packFiles;
            packFiles = null;
            packsMakeDirs[path] = packMakeDirs;
            packsRoot[path] = packRootFolder;
        }
        #endregion

        private void Reset()
        {
            rootFolder = null;
            packRootFolder = null;
            addFiles = new List<string>();
            packFiles = null;
            packs = new Dictionary<string, List<string>>();
            makeDirs = new List<string>();
            packsMakeDirs = new Dictionary<string, List<string>>();
            packsRoot = new Dictionary<string, string>();
        }

        private void ScanAndAct()
        {
            foreach (var l in lines)
            {
                var found = false;
                Match m = null;
                for (var i = 0; i < regexes.Count && !found; i++)
                {
                    m = regexes[i].Match(l);
                    if (m.Success)
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    Console.Write("WARNING: No suitable action found for \"{0}\"", l);
                }
                else
                {
                    var keyword = m.Groups["keyword"].Value;
                    actions[keyword].Invoke(m);
                }
            }
        }

        private string FileToMD5(string file)
        {
            var fstream = File.OpenRead(file);
            var bytes = md5.ComputeHash(fstream);
            fstream.Close();
            return string.Join("", bytes.Select(o => o.ToString("X2")));
        }

        private void UploadFiles(string host, string user, PrivateKeyFile keyfile, string path)
        {
            var md5s = new Dictionary<string, string>();
            var client = new SftpClient(host, user, keyfile);
            client.Connect();

            if (!client.Exists(path))
            {
                client.CreateDirectory(path);
            }

            foreach (var d in makeDirs)
            {
                var remotePath = Path.Combine(path, d).Replace('\\', '/');
                if (!client.Exists(remotePath))
                {
                    Console.WriteLine("\tCreating directory {0}", d);
                    client.CreateDirectory(remotePath);
                }
            }

            foreach (var f in addFiles)
            {
                var file = Path.Combine(rootFolder, f);
                var remoteFile = Path.Combine(path, f).Replace('\\', '/');

                var md5str = FileToMD5(file);
                md5s[f] = md5str;

                var fstream = File.OpenRead(file);
                Console.WriteLine("\tUploading file {0}", f);
                client.UploadFile(fstream, remoteFile, true);
                fstream.Close();
            }

            foreach (var k in packs.Keys)
            {
                string file;
                var remoteFile = Path.Combine(path, k).Replace('\\', '/');

                Pack(k, out file);

                var md5str = FileToMD5(file);
                md5s[k] = md5str;

                var fstream = File.OpenRead(file);
                Console.WriteLine("\tUploading pack {0}", k);
                client.UploadFile(fstream, remoteFile, true);
                fstream.Close();
            }

            var md5file = CreateMD5File(md5s);
            var md5stream = File.OpenRead(md5file);

            Console.WriteLine("\tUploading md5 list");
            client.UploadFile(md5stream, Path.Combine(path, "md5.json").Replace('\\', '/'), true);
            md5stream.Close();

            client.Disconnect();
        }

        private string CreateMD5File(Dictionary<string, string> md5s)
        {
            var jarr = new JsonArray();
            foreach (var kv in md5s)
            {
                var jobj = new JsonObject();
                jobj["path"] = kv.Key;
                jobj["md5"] = kv.Value;
                jarr.Add(jobj);
            }
            var file = Path.GetTempFileName();
            File.WriteAllText(file, jarr.ToString());
            return file;
        }

        private void Pack(string pack, out string file)
        {
            file = Path.GetTempFileName();

            var zip = ZipFile.Create(file);
            zip.BeginUpdate();

            var root = packsRoot[pack];
            foreach (var d in packsMakeDirs[pack])
            {
                Console.WriteLine("\t\tCreating pack directory {0}", d);
                zip.AddDirectory(d);
            }

            foreach (var f in packs[pack])
            {
                Console.WriteLine("\t\tAdding pack file {0}", f);
                zip.Add(Path.Combine(packRootFolder, f), f);
            }

            zip.CommitUpdate();
            zip.Close();
        }

        public void Excecute(string host, string user, PrivateKeyFile keyfile, string path)
        {
            Console.WriteLine("Executing {0}", sfile);

            Reset();
            ScanAndAct();
            UploadFiles(host, user, keyfile, path);
        }
    }
}
