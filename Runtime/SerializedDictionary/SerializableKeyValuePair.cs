using UnityEngine.Serialization;

namespace Twentyfiveslicer.Runtime.SerializedDictionary
{
    [System.Serializable]
    public class SerializableKeyValuePair<TKey, TValue>
    {
        [FormerlySerializedAs("Key")] public TKey key;
        [FormerlySerializedAs("Value")] public TValue value;

        public SerializableKeyValuePair(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }
    }
}