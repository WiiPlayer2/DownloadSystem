using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public interface IInvokerInterface : IAddon
    {
        IInvokerDatabase InvokerDatabase { get; set; }
    }
}
