using System;
using ScriptableObjects.Skills;
using UnityEngine;

namespace ScriptableObjects.Util.DataTypes
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

    [CreateAssetMenu(fileName = "InventoryItem", menuName = "Inventory Item")]
    [Serializable]
    public class InventoryItem : ScriptableObject
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