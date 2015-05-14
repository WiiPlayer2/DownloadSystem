﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public interface IConfigurable : IAddon
    {
        IConfigurator Configurator { get; }

        void ConfigLoaded();
    }
}