using ScriptableObjects.Skills;
using UnityEngine;

namespace ScriptableObjects.Util.DataTypes
{
    public enum ItemType {
        Weapon,
        Wearable,
        ConsumableSkill,
        QuestItem,
        KeyItem
    }
    [CreateAssetMenu(fileName = "InventoryItem", menuName = "Inventory Item")]
    public class InventoryItem : ScriptableObject
    {
        public string displayName;
        public string description;
        public ItemType itemType;
        // if it's a weapon or buff/debuff, it will have a Skill associated with it
        public BaseSkill skill;
        public int value;
    }
}