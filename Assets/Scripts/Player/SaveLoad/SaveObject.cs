using System;
using Player.Inventory;
using Util.DataTypes;

namespace Player.SaveLoad
{
    [Serializable]
    public class SaveObject
    {
        public AffinityLogDictionary affinityLogDictionary = new();
        public SerializableDictionary<InventoryItem, int> inventoryItems = new();
        public long timestamp = DateTime.Now.ToFileTimeUtc();
    }
}