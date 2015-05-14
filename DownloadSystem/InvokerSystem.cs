﻿using DownloadSystem.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem
{
    class InvokerSystem : IInvokerDatabase
    {
        private Dictionary<string, IInvokable> invokables;

        public InvokerSystem(CoreSystem coreSystem)
        {
            System = coreSystem;

            invokables = new Dictionary<string, IInvokable>();
        }

        public CoreSystem System { get; private set; }

        public IInvoker GetInvoker(string name)
        {
            return invokables[name].Invoker;
        }

        public void AddInvokable(IInvokable o)
        {
            invokables[o.Name] = o;
        }
    }
}
