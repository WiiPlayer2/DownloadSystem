using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public abstract class EasyConfigurator : IConfigurator
    {
        #region Parser (Class + Interface)
        public abstract class AbstractParser<T> : IParser<T>
        {
            public abstract string ToString(T obj);

            public abstract T Parse(string str);

            public string ToString(object obj)
            {
                return ToString((T)obj);
            }

            object IParser.Parse(string str)
            {
                return Parse(str);
            }
        }

        public interface IParser<T> : IParser
        {
            string ToString(T obj);
            T Parse(string str);
        }

        public interface IParser
        {
            string ToString(object obj);
            object Parse(string str);
        }
        #endregion

        #region BasicParser
        private class StringParser : AbstractParser<string>
        {
            public override string ToString(string obj)
            {
                if (obj != null)
                {
                    return obj;
                }
                return "";
            }

            public override string Parse(string str)
            {
                return str;
            }
        }

        private class BoolParser : AbstractParser<bool>
        {
            public override string ToString(bool obj)
            {
                return obj.ToString();
            }

            public override bool Parse(string str)
            {
                bool val;
                if (bool.TryParse(str, out val))
                {
                    return val;
                }
                return false;
            }
        }

        private class IntParser : AbstractParser<int>
        {
            public override string ToString(int obj)
            {
                return obj.ToString();
            }

            public override int Parse(string str)
            {
                int val;
                if (int.TryParse(str, out val))
                {
                    return val;
                }
                return 0;
            }
        }

        private class DateTimeParser : AbstractParser<DateTime>
        {
            public override string ToString(DateTime obj)
            {
                return obj.ToString();
            }

            public override DateTime Parse(string str)
            {
                DateTime val;
                if (DateTime.TryParse(str, out val))
                {
                    return val;
                }
                return DateTime.MinValue;
            }
        }

        private class ArrayParser<T> : AbstractParser<T[]>
        {
            private const string SPLITTER = ":";

            public override string ToString(T[] obj)
            {
                if (obj != null)
                {
                    var strs = obj.Select(o =>
                    {
                        var str = EasyConfigurator.ToString(o, typeof(T));
                        str = Uri.EscapeDataString(str);
                        return str;
                    });
                    return string.Format("{0}#{1}", obj.Length, string.Join(SPLITTER, strs));
                }
                return "0#";
            }

            public override T[] Parse(string str)
            {
                //var splits = new List<string>();

                //var tmp = str;
                //var index = str.IndexOf(SPLITTER);
                //var stuffedIndex = str.IndexOf(SPLITTER + SPLITTER);

                //while(index != -1)
                //{
                //    if(index != stuffedIndex)
                //    {
                //        splits.Add(tmp.Substring(0, index).Replace(SPLITTER + SPLITTER, SPLITTER));
                //        tmp = tmp.Substring(index + 1);
                //    }
                //}
                var index = str.IndexOf('#');
                if (index != -1)
                {
                    var countstr = str.Substring(0, index);
                    int count;
                    if (int.TryParse(countstr, out count))
                    {
                        if (count == 0)
                        {
                            return new T[0];
                        }

                        var tmp = str.Substring(str.IndexOf('#') + 1);
                        var splits = tmp.Split(new[] { SPLITTER }, StringSplitOptions.None);
                        var arr = splits.Select(o =>
                        {
                            var obj = (T)EasyConfigurator.Parse(Uri.UnescapeDataString(o), typeof(T));
                            return obj;
                        });
                        return arr.ToArray();
                    }
                }
                return new T[0];
            }
        }

        private class EnumParser<T> : AbstractParser<T>
            where T : struct
        {
            public override string ToString(T obj)
            {
                return obj.ToString();
            }

            public override T Parse(string str)
            {
                T var = default(T);
                if (Enum.TryParse<T>(str, out var))
                {
                    return var;
                }
                return default(T);
            }
        }

        private class TupleParser<T1, T2> : AbstractParser<Tuple<T1, T2>>
        {
            private const string SPLITTER = ":";

            public override string ToString(Tuple<T1, T2> obj)
            {
                if (obj != null)
                {
                    var str1 = EasyConfigurator.ToString(obj.Item1, typeof(T1));
                    var str2 = EasyConfigurator.ToString(obj.Item2, typeof(T2));
                    return string.Format("{0}{1}{2}",
                        Uri.EscapeDataString(str1), SPLITTER, Uri.EscapeDataString(str2));
                }
                return "";

            }

            public override Tuple<T1, T2> Parse(string str)
            {
                if (str == "")
                {
                    return null;
                }
                var splits = str.Split(new[] { SPLITTER }, StringSplitOptions.None);
                if (splits.Length == 2)
                {
                    var obj1 = (T1)EasyConfigurator.Parse(Uri.UnescapeDataString(splits[0]), typeof(T1));
                    var obj2 = (T2)EasyConfigurator.Parse(Uri.UnescapeDataString(splits[1]), typeof(T2));
                    return new Tuple<T1, T2>(obj1, obj2);
                }
                return null;
            }
        }
        #endregion

        #region Static Stuff
        private static Dictionary<Type, IParser> parsers;

        static EasyConfigurator()
        {
            parsers = new Dictionary<Type, IParser>();

            //Basics
            RegisterParser(new BoolParser());
            RegisterParser(new IntParser());
            RegisterParser(new StringParser());
            RegisterParser(new DateTimeParser());
        }

        public static void RegisterParser<T>(IParser<T> parser)
        {
            var t = typeof(T);
            if (!parsers.ContainsKey(t))
            {
                parsers[t] = parser;
            }

            t = typeof(T[]);
            if (!parsers.ContainsKey(t))
            {
                parsers[t] = new ArrayParser<T>();
            }
        }

        public static void RegisterArrayParser<T>()
        {
            RegisterParser(new ArrayParser<T>());
        }

        public static void RegisterEnumParser<T>()
            where T : struct
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("Type is not enum.");
            }

            RegisterParser(new EnumParser<T>());
        }

        public static void RegisterTupleParser<T1, T2>()
        {
            RegisterParser(new TupleParser<T1, T2>());
        }

        private static object Parse(string str, Type type)
        {
            if (parsers.ContainsKey(type))
            {
                var parser = parsers[type];
                return parser.Parse(str);
            }
            else
            {
                throw new ArgumentException(string.Format("Type {0} has no registered parser.", type));
            }
        }

        private static string ToString(object obj, Type type)
        {
            if (parsers.ContainsKey(type))
            {
                var parser = parsers[type];
                return parser.ToString(obj);
            }
            else
            {
                throw new ArgumentException(string.Format("Type {0} has no registered parser.", type));
            }
        }

        private struct Variable
        {
            public string Name;
            public Type Type;
            public Delegate GetCallback;
            public Delegate SetCallback;
        }
        #endregion

        private Dictionary<string, Variable> vars;
        private IConfigurable addon;

        protected EasyConfigurator(IConfigurable addon)
        {
            this.addon = addon;

            vars = new Dictionary<string, Variable>();
        }

        protected void RegisterVar(Type t, string name,
            Delegate getCallback, Delegate setCallback)
        {
            vars[name] = new Variable()
            {
                Name = name,
                Type = t,
                SetCallback = setCallback,
                GetCallback = getCallback,
            };
        }

        protected void RegisterVar<T>(string name,
            Func<string, T> getCallback, Action<string, T> setCallback)
        {
            //vars[name] = new Variable()
            //{
            //    Name = name,
            //    Type = typeof(T),
            //    SetCallback = setCallback,
            //    GetCallback = getCallback,
            //};
            RegisterVar(typeof(T), name, getCallback, setCallback);
        }

        protected void RegisterVar(string name, object instance, string property)
        {
            var type = instance.GetType();
            var prop = type.GetProperty(property);
            RegisterVar(prop.PropertyType, name,
                new Func<string, object>(key => prop.GetValue(instance)),
                new Action<string, object>((key, val) => { prop.SetValue(instance, val); }));
        }

        protected void RegisterVar<T>(string name, object instance, string property)
        {
            RegisterVar(name, instance, property);
        }

        public string ID { get { return addon.Name; } }

        public SaveConfigDelegate SaveConfig { get; set; }

        public IEnumerable<string> Keys
        {
            get { return vars.Keys; }
        }

        public string this[string key]
        {
            get
            {
                return ToString(vars[key].GetCallback.DynamicInvoke(key), vars[key].Type);
            }
            set
            {
                var var = vars[key];
                var val = Parse(value, var.Type);
                var.SetCallback.DynamicInvoke(key, val);
            }
        }

        public bool HasKey(string key)
        {
            return vars.ContainsKey(key);
        }

        public IConfigurable Configurable
        {
            get { return addon; }
        }

        public Type GetTypeOfKey(string key)
        {
            return vars[key].Type;
        }
    }
}
