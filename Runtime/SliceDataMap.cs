using UnityEngine;
using System.Collections.Generic;
using Twentyfiveslicer.Runtime.SerializedDictionary;

namespace TwentyFiveSlicer.Runtime
{
    [CreateAssetMenu(fileName = "SliceDataMap", menuName = "TwentyFiveSlicer/SliceDataMap", order = 0)]
    public class SliceDataMap : ScriptableObject
    {
        [SerializeField]
        private SerializedDictionary<Sprite, TwentyFiveSliceData> sliceDataMap = new();

        public bool TryGetSliceData(Sprite sprite, out TwentyFiveSliceData data)
        {
            return sliceDataMap.TryGetValue(sprite, out data);
        }

        public void AddSliceData(Sprite sprite, TwentyFiveSliceData data)
        {
            sliceDataMap.Add(sprite, data);
        }

        public void RemoveSliceData(Sprite sprite)
        {
            sliceDataMap.Remove(sprite);
        }

        public IEnumerable<Sprite> GetAllSprites()
        {
            return sliceDataMap.Keys;
        }
        
        public IEnumerable<KeyValuePair<Sprite, TwentyFiveSliceData>> GetAllEntries()
        {
            return sliceDataMap.GetAllEntries();
        }
    }
}