using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace robin.Data.Utils
{
	class LinkedHashMap<T,V> : IDictionary<T,V>, IList<KeyValuePair<T,V>>
	{
		private readonly List<KeyValuePair<T,V>> values = new List<KeyValuePair<T, V>>();
		private readonly Dictionary<T,int> indexes = new Dictionary<T, int>();

		public IEnumerator<KeyValuePair<T, V>> GetEnumerator()
		{
			return values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(KeyValuePair<T, V> item)
		{
			values.Add(item);
			indexes.Add(item.Key,values.Count-1);
		}

		public void Clear()
		{
			values.Clear();
			indexes.Clear();
		}

		public bool Contains(KeyValuePair<T, V> item)
		{
			return values.Contains(item);
		}

		public void CopyTo(KeyValuePair<T, V>[] array, int arrayIndex)
		{
			values.CopyTo(array,arrayIndex);
		}

		public bool Remove(KeyValuePair<T, V> item)
		{
			indexes.Remove(item.Key);
			return values.Remove(item);
		}

		public int Count
		{
			get { return values.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool ContainsKey(T key)
		{
			return indexes.ContainsKey(key);
		}

		public void Add(T key, V value)
		{
			values.Add(new KeyValuePair<T, V>(key,value));
			indexes.Add(key,values.Count -1 );
		}

		public bool Remove(T key)
		{
			int i = indexes[key];
			values.RemoveAt(i);
			return true;
		}

		public bool TryGetValue(T key, out V value)
		{
			try
			{
				int i = indexes[key];
				value = values[i].Value;
				return true;
			}
			catch (Exception)
			{
				value = default(V);
				return false;
			}
		}

		public V this[T key]
		{
			get
			{
				int i = indexes[key];
				return values[i].Value;
			}
			set
			{
				int i = indexes[key];
				values[i] = new KeyValuePair<T, V>(key,value);
			}
		}

		public ICollection<T> Keys
		{
			get
			{
				List<T> keys = new List<T>();
				foreach (KeyValuePair<T, V> keyValuePair in values)
					keys.Add(keyValuePair.Key);
				return keys;
			}
		}

		public ICollection<V> Values
		{
			get
			{
				return values.Select(keyValuePair => keyValuePair.Value).ToList();
			}
		}

		public int IndexOf(KeyValuePair<T, V> item)
		{
			return indexes[item.Key];
		}

		public void Insert(int index, KeyValuePair<T, V> item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		KeyValuePair<T, V> IList<KeyValuePair<T, V>>.this[int index]
		{
			get { return values[index]; }
			set { values[index] = value; }
		}
	}
}
