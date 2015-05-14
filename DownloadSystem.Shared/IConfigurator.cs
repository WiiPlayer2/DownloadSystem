using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public interface IConfigurator
    {
        IConfigurable Configurable { get; }

        string ID { get; }

        SaveConfigDelegate SaveConfig { get; set; }

        IEnumerable<string> Keys { get; }

        string this[string key] { get; set; }

        bool HasKey(string key);

        Type GetTypeOfKey(string key);
    }
}
