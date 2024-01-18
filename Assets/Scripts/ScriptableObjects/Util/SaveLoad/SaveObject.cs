using System;
using ScriptableObjects.Stats.CharacterStats;
using ScriptableObjects.Util.DataTypes;
using UnityEngine;

namespace ScriptableObjects.Util.SaveLoad
{
    [Serializable]
    public class SaveObject
    {
        [SerializeField] public AffinityLogDictionary affinityLogDictionary = new();
        [SerializeField] public SerializableDictionary<InventoryItem, int> inventoryItems = new();
        [SerializeField] public long timestamp = DateTime.Now.ToFileTimeUtc();
        [SerializeField] public CharacterStatsDictionary characterStats = new();
        
    }
}