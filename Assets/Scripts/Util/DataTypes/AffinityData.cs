using System;
using System.Collections.Generic;
using UnityEngine;

namespace Util.DataTypes
{
    [Serializable]
    public class AffinityKeyValuePair
    {
        public string key;
        public AffinityData value;
    }

    [Serializable]
    public class AffinityLogDictionary
    {
        [SerializeField]
        public List<AffinityKeyValuePair> dataByCharacter = new();
    }
    
    [Serializable]
    public class AffinityData
    {
        [SerializeField]
        public SerializableDictionary<ElementType, StrengthType> strengths = new();
        [SerializeField]
        public List<ElementType> weaknesses = new();
    }
}