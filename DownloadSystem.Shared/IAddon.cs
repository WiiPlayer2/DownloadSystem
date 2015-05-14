using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public interface IAddon
    {
        void Load();

        void Ready();

        void Unload();

        string Name { get; }

        string FullName { get; }
    }
}
