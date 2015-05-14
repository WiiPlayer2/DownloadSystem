using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DownloadSystem.Shared;

namespace DownloadSystem
{
    class ConfigSystem : IConfiguratorDatabase
    {
        private string configFile = "./system.conf";
        private Dictionary<string, IConfigurable> configs;
        private Dictionary<string, Dictionary<string, string>> setValues;
        private Regex cfgFileRegex;

        public ConfigSystem(CoreSystem system)
        {
            System = system;
            configs = new Dictionary<string, IConfigurable>();
            setValues = new Dictionary<string, Dictionary<string, string>>();
            cfgFileRegex = new Regex(@"^(?<mod>[A-z]+|\*)\ (?<var>[A-z]+)\ (?<value>.*)$");
        }

        public CoreSystem System { get; private set; }

        public void OnReady()
        {
            ExecCfg();
        }

        public void AddConfigurable(IConfigurable config)
        {
            configs[config.Name] = config;
            config.Configurator.SaveConfig = (var) =>
                SetConfig(config.Name, var, config.Configurator[var]);
        }

        private void SetConfig(string addon, string var, string value)
        {
            if (!setValues.ContainsKey(addon))
            {
                setValues[addon] = new Dictionary<string, string>();
            }
            setValues[addon][var] = value;
            //Console.WriteLine("{{CFG}} {1}.{2} <- {0}", value, addon, var);
            SaveCfgFile();
        }

        public void LoadCfgFile()
        {
            if (File.Exists(configFile))
            {
                var lines = File.ReadAllLines(configFile);
                foreach (var line in lines)
                {
                    var m = cfgFileRegex.Match(line);
                    if (m.Success)
                    {
                        SetConfig(m.Groups["mod"].Value, m.Groups["var"].Value, m.Groups["value"].Value);
                    }
                }
            }
        }

        private void SaveCfgFile()
        {
            var writer = new StreamWriter(configFile, false);
            foreach (var m in setValues.Keys)
            {
                foreach (var v in setValues[m].Keys)
                {
                    writer.WriteLine("{0} {1} {2}", m, v, setValues[m][v]);
                }
            }
            writer.Close();
        }

        private void ExecCfg()
        {
            var inits = new List<string>();

            foreach (var m in setValues.Keys)
            {
                if (configs.ContainsKey(m))
                {
                    var conf = configs[m].Configurator;
                    foreach (var v in setValues[m].Keys)
                    {
                        if (conf.HasKey(v))
                        {
                            conf[v] = setValues[m][v];
                        }
                    }
                    conf.Configurable.ConfigLoaded();
                    inits.Add(m);
                }
            }

            foreach(var c in configs.Keys
                .Where(o => !inits.Contains(o)))
            {
                configs[c].ConfigLoaded();
            }
        }

        public IConfigurator GetConfigurator(string name)
        {
            return configs[name].Configurator;
        }
    }
}
