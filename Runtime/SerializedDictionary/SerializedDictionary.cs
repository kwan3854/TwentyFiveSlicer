using System.Collections.Generic;
using UnityEngine;

namespace TwentyFiveSlicer.Runtime
{
    [System.Serializable]
    public class SerializedDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField] private List<SerializableKeyValuePair<TKey, TValue>> items = new();
        private Dictionary<TKey, TValue> _dictionary = new();

        public void OnBeforeSerialize()
        {
            items.Clear();
            foreach (var kvp in _dictionary)
            {
                items.Add(new SerializableKeyValuePair<TKey, TValue>(kvp.Key, kvp.Value));
            }
        }

        public void OnAfterDeserialize()
        {
            _dictionary.Clear();
            foreach (var item in items)
            {
                _dictionary[item.Key] = item.Value;
            }
        }

        public void Add(TKey key, TValue value) => _dictionary[key] = value;
        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
        public void Clear() => _dictionary.Clear();
        public Dictionary<TKey, TValue>.KeyCollection Keys => _dictionary.Keys;
        public Dictionary<TKey, TValue>.ValueCollection Values => _dictionary.Values;
    }
}