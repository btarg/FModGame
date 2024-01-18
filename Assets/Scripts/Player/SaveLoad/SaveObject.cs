using System;
using ScriptableObjects.Characters.Health;
using ScriptableObjects.Util.DataTypes;
using UnityEngine;

namespace Player.SaveLoad
{
    [Serializable]
    public class SaveObject
    {
        [SerializeField] public AffinityLogDictionary affinityLogDictionary = new();
        [SerializeField] public SerializableDictionary<InventoryItem, int> inventoryItems = new();
        [SerializeField] public long timestamp = DateTime.Now.ToFileTimeUtc();
        [SerializeField] public SerializableDictionary<string, HealthManager> characterHealths = new();
        
    }
}