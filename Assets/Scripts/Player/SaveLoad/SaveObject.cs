using System;
using Player.Inventory;
using UnityEngine;
using Util.DataTypes;

namespace Player.SaveLoad
{
    [Serializable]
    public class SaveObject
    {
        [SerializeField]
        public AffinityLogDictionary affinityLogDictionary = new();
        [SerializeField]
        public SerializableDictionary<InventoryItem, int> inventoryItems = new();
        [SerializeField]
        public long timestamp = DateTime.Now.ToFileTimeUtc();
    }
}