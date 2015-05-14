﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public interface IConfiguratorInterface : IAddon
    {
        IConfiguratorDatabase ConfiguratorDatabase { get; set; }
    }
}