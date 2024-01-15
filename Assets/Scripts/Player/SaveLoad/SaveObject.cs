using System;
using Player.Inventory;
using Util.DataTypes;

namespace Player.SaveLoad
{
    [Serializable]
    public class SaveObject
    {
        public AffinityLogDictionary affinityLogDictionary = new();
        public PlayerInventory inventory = new();
    }
}