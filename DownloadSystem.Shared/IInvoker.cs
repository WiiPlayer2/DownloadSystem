using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public interface IInvoker
    {
        object Invoke(string method, params object[] args);

        T Invoke<T>(string method, params object[] args);

        Type GetReturnType(string method);

        Type[] GetParameterTypes(string method);

        IEnumerable<string> Methods { get; }

        IInvokable Invokable { get; }
    }
}
