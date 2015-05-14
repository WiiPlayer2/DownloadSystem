using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public class DownloadSystemInternalException : Exception
    {
        public DownloadSystemInternalException(string message)
            : base(message)
        {

        }
    }
}
