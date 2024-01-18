using ScriptableObjects.Skills;
using UnityEngine;

namespace ScriptableObjects.Util.DataTypes.Inventory
{
    [CreateAssetMenu(fileName = "InventoryItem", menuName = "Inventory Item")]
    public class InventoryItem : ScriptableObject
    {
        public RawInventoryItem rawInventoryItem;
        
        public string displayName => rawInventoryItem.displayName;
        public string description => rawInventoryItem.description;
        public ItemType itemType => rawInventoryItem.itemType;
        public BaseSkill skill => rawInventoryItem.skill;
        public int value => rawInventoryItem.value;
    }
}