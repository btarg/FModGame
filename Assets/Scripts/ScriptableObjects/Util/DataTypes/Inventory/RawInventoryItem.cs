using System;
using ScriptableObjects.Skills;
using UnityEngine;

namespace ScriptableObjects.Util.DataTypes.Inventory
{
    [Serializable]
    public enum ItemType
    {
        Weapon,
        Wearable,
        ConsumableSkill,
        QuestItem,
        KeyItem
    }
    
    [Serializable]
    public class RawInventoryItem
    {
        [SerializeField]
        public string displayName;
        [SerializeField]
        public string description;
        [SerializeField]
        public ItemType itemType;

        // if it's a weapon or buff/debuff, it will have a Skill associated with it
        [SerializeField]
        public BaseSkill skill;
        [SerializeField]
        public int value;
    }
}