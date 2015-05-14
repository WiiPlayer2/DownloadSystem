using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public class EasyInvoker : IInvoker
    {
        private struct MethodData
        {
            public string Name;
            public Delegate Method;
            public Type ReturnType;
            public Type[] ParameterTypes;
        }

        Dictionary<string, MethodData> methods;

        protected EasyInvoker(IInvokable invokable)
        {
            Invokable = invokable;

            methods = new Dictionary<string, MethodData>();
        }

        protected void RegisterMethod(string name, object obj, string methodName)
        {
            var method = obj.GetType().GetMethod(methodName);
            RegisterMethod(name, ToDelegate(method, obj));
        }

        protected void RegisterMethod(string name, Delegate method)
        {
            if (!methods.ContainsKey(name))
            {
                var data = new MethodData()
                {
                    Name = name,
                    Method = method,
                    ReturnType = method.Method.ReturnType,
                    ParameterTypes = method.Method.GetParameters()
                        .Select(o => o.ParameterType)
                        .ToArray(),
                };
            }
            else
            {
                throw new ArgumentException(string.Format("<{0}> is already in use.", name));
            }
        }

        /// <summary>
        /// Builds a Delegate instance from the supplied MethodInfo object and a target to invoke against.
        /// </summary>
        public static Delegate ToDelegate(MethodInfo mi, object target)
        {
            if (mi == null) throw new ArgumentNullException("mi");

            Type delegateType;

            var typeArgs = mi.GetParameters()
                .Select(p => p.ParameterType)
                .ToList();

            // builds a delegate type
            if (mi.ReturnType == typeof(void))
            {
                delegateType = Expression.GetActionType(typeArgs.ToArray());

            }
            else
            {
                typeArgs.Add(mi.ReturnType);
                delegateType = Expression.GetFuncType(typeArgs.ToArray());
            }

            // creates a binded delegate if target is supplied
            var result = (target == null)
                ? Delegate.CreateDelegate(delegateType, mi)
                : Delegate.CreateDelegate(delegateType, target, mi);

            return result;
        }

        public object Invoke(string method, params object[] args)
        {
            var meth = methods[method].Method;
            return meth.DynamicInvoke(args);
        }

        public T Invoke<T>(string method, params object[] args)
        {
            return (T)Invoke(method, args);
        }

        public Type GetReturnType(string method)
        {
            return methods[method].ReturnType;
        }

        public Type[] GetParameterTypes(string method)
        {
            return methods[method].ParameterTypes;
        }

        public IEnumerable<string> Methods { get { return methods.Keys; } }


        public IInvokable Invokable { get; private set; }
    }

}
