using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DtbSynthesizerLibrary
{
    public class ChangableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged
    {
        private readonly Dictionary<TKey, TValue> baseDictionary = new Dictionary<TKey, TValue>();

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return baseDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            baseDictionary.Clear();
            CollectionChanged?.Invoke(
                this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset,
                    null));
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return baseDictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            baseDictionary.ToList().CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public int Count => baseDictionary.Count;
        public bool IsReadOnly => false;
        public bool ContainsKey(TKey key)
        {
            return baseDictionary.ContainsKey(key);
        }

        public void Add(TKey key, TValue value)
        {
            baseDictionary.Add(key, value);
            CollectionChanged?.Invoke(
                this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    new[] { new KeyValuePair<TKey, TValue>(key, value) }.ToList()));
        }

        public bool Remove(TKey key)
        {
            if (TryGetValue(key, out var value))
            {
                var res = baseDictionary.Remove(key);
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        new[] { new KeyValuePair<TKey, TValue>(key, value) }.ToList()));
                return res;

            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return baseDictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => baseDictionary[key];
            set
            {
                var list = new List<KeyValuePair<TKey, TValue>>() { new KeyValuePair<TKey, TValue>(key, value) };
                var action = NotifyCollectionChangedAction.Replace;
                if (ContainsKey(key))
                {
                    list.Insert(0, new KeyValuePair<TKey, TValue>(key, baseDictionary[key]));
                }
                baseDictionary[key] = value;
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, list));
            }
        }

        public ICollection<TKey> Keys => baseDictionary.Keys;
        public ICollection<TValue> Values => baseDictionary.Values;
    }
}
