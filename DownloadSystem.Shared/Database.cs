using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DownloadSystem.Shared
{
    public class Database : IDictionary<string, Database.DataEntry>
    {
        #region DataEntry
        public abstract class DataEntry
        {
            internal abstract string Save();

            public event EventHandler DataChanged;

            public DataString AsString
            {
                get
                {
                    return this as DataString;
                }
            }

            public DataObject AsObject
            {
                get
                {
                    return this as DataObject;
                }
            }

            protected void CallDataChanged()
            {
                if (DataChanged != null)
                {
                    DataChanged(this, EventArgs.Empty);
                }
            }

            protected static DataEntry Parse(string str)
            {
                switch (str[0])
                {
                    case '$':
                        return new DataString(str.Substring(1));
                    case '{':
                        return new DataObject(str);
                    default:
                        throw new NotSupportedException();
                }
            }

            public static implicit operator DataEntry(string o)
            {
                return new DataString(o);
            }
        }

        public class DataString : DataEntry
        {
            private string value;

            internal DataString(string str)
            {
                Value = str;
            }

            public string Value
            {
                get
                {
                    return value;
                }
                set
                {
                    this.value = value != null ? value : "";
                    CallDataChanged();
                }
            }

            internal override string Save()
            {
                var str = string.Format("${0}", Value);
                return str;
            }

            public static implicit operator string(DataString o)
            {
                return o.Value;
            }

            public static implicit operator DataString(string o)
            {
                return new DataString(o);
            }
        }

        public class DataObject : DataEntry, IDictionary<string, DataEntry>
        {
            private Regex flatRegex;
            private Regex dataRegex;
            private Dictionary<string, DataEntry> entries;

            internal DataObject(string str)
                : this()
            {
                var m = flatRegex.Match(str);
                if (m.Success)
                {
                    var count = int.Parse(m.Groups["count"].Value);
                    var data = Uri.UnescapeDataString(m.Groups["data"].Value);

                    foreach (var sm in data.Split(':')
                        .Select(o => Uri.UnescapeDataString(o))
                        .Select(o => dataRegex.Match(o)))
                    {
                        if (sm.Success)
                        {
                            var key = Uri.UnescapeDataString(sm.Groups["key"].Value);
                            var value = Uri.UnescapeDataString(sm.Groups["value"].Value);

                            this[key] = Parse(value);
                        }
                    }
                }
                else
                {
                    throw new InvalidDataException();
                }
            }

            public DataObject()
            {
                flatRegex = new Regex(@"^\{(?<count>\d+)#(?<data>.*)$");
                dataRegex = new Regex(@"^(?<key>.*):(?<value>.*)$");
                entries = new Dictionary<string, DataEntry>();
            }

            void value_DataChanged(object sender, EventArgs e)
            {
                CallDataChanged();
            }
            internal override string Save()
            {
                var data = this.Select(o => string.Format("{0}:{1}",
                    Uri.EscapeDataString(o.Key),
                    Uri.EscapeDataString(o.Value.Save())));
                var str = string.Format("{{{0}#{1}",
                    data.Count(), Uri.EscapeDataString(string.Join(":", data
                    .Select(o=>Uri.EscapeDataString(o)))));
                return str;
            }

            #region Interface
            public void Add(string key, DataEntry value)
            {
                entries.Add(key, value);
                value.DataChanged += value_DataChanged;
                CallDataChanged();
            }

            public bool ContainsKey(string key)
            {
                return entries.ContainsKey(key);
            }

            public ICollection<string> Keys
            {
                get { return entries.Keys; }
            }

            public bool Remove(string key)
            {
                if (entries.ContainsKey(key))
                {
                    entries[key].DataChanged -= value_DataChanged;
                    entries.Remove(key);
                    CallDataChanged();
                    return true;
                }
                return false;
            }

            public bool TryGetValue(string key, out DataEntry value)
            {
                return entries.TryGetValue(key, out value);
            }

            public ICollection<DataEntry> Values
            {
                get { return entries.Values; }
            }

            public DataEntry this[string key]
            {
                get
                {
                    return entries[key];
                }
                set
                {
                    if (ContainsKey(key))
                    {
                        Remove(key);
                    }
                    Add(key, value);
                }
            }

            public void Add(KeyValuePair<string, DataEntry> item)
            {
                Add(item.Key, item.Value);
            }

            public void Clear()
            {
                foreach (var k in Keys.ToArray())
                {
                    Remove(k);
                }
            }

            public bool Contains(KeyValuePair<string, DataEntry> item)
            {
                return entries.Contains(item);
            }

            public void CopyTo(KeyValuePair<string, DataEntry>[] array, int arrayIndex)
            {
                var i = arrayIndex;
                foreach (var kv in entries)
                {
                    if (i >= array.Length)
                    {
                        break;
                    }

                    array[i] = kv;

                    i++;
                }
            }

            public int Count
            {
                get { return entries.Count; }
            }

            public bool IsReadOnly
            {
                get { return false; }
            }

            public bool Remove(KeyValuePair<string, DataEntry> item)
            {
                return Remove(item.Key);
            }

            public IEnumerator<KeyValuePair<string, DataEntry>> GetEnumerator()
            {
                return entries.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return entries.GetEnumerator();
            }
            #endregion
        }
        #endregion

        private DataObject data;
        private Mutex mutex;

        public Database(string file)
        {
            Path = file;

            mutex = new Mutex();

            if (File.Exists(file))
            {
                data = new DataObject(File.ReadAllText(file));
            }
            else
            {
                data = new DataObject();
            }

            data.DataChanged += data_DataChanged;
        }

        public string Path { get; private set; }

        void data_DataChanged(object sender, EventArgs e)
        {
            mutex.WaitOne();

            File.WriteAllText(Path, data.Save());

            mutex.ReleaseMutex();
        }

        #region Interface
        public void Add(string key, Database.DataEntry value)
        {
            data.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return data.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return data.Keys; }
        }

        public bool Remove(string key)
        {
            return data.Remove(key);
        }

        public bool TryGetValue(string key, out Database.DataEntry value)
        {
            return data.TryGetValue(key, out value);
        }

        public ICollection<Database.DataEntry> Values
        {
            get { return data.Values; }
        }

        public Database.DataEntry this[string key]
        {
            get
            {
                return data[key];
            }
            set
            {
                data[key] = value;
            }
        }

        public void Add(KeyValuePair<string, Database.DataEntry> item)
        {
            data.Add(item);
        }

        public void Clear()
        {
            data.Clear();
        }

        public bool Contains(KeyValuePair<string, Database.DataEntry> item)
        {
            return data.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, Database.DataEntry>[] array, int arrayIndex)
        {
            data.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return data.Count; }
        }

        public bool IsReadOnly
        {
            get { return data.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<string, Database.DataEntry> item)
        {
            return data.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, Database.DataEntry>> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }
        #endregion
    }
}
