using System;
using System.Collections.Generic;
using ScriptableObjects.Stats.CharacterStats;
using UnityEngine;

namespace ScriptableObjects.Util.DataTypes
{
    [Serializable]
    public class CharacterStatsKeyValuePair
    {
        [SerializeField] public string characterID;
        [SerializeField] public CharacterStats stats;
    }

    [Serializable]
    public class CharacterStatsDictionary
    {
        [SerializeField] public List<CharacterStatsKeyValuePair> statsByCharacter = new();
    }
    
}