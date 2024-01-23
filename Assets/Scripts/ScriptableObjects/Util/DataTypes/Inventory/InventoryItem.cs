using ScriptableObjects.Characters.PlayerCharacters;
using ScriptableObjects.Skills;
using UnityEngine;

namespace ScriptableObjects.Util.DataTypes.Inventory
{
    [CreateAssetMenu(fileName = "InventoryItem", menuName = "Inventory Item")]
    public class InventoryItem : ScriptableObject
    {
        public RawInventoryItem rawInventoryItem;

        public string id => rawInventoryItem.id;
        public string displayName => rawInventoryItem.displayName;
        public string description => rawInventoryItem.description;
        public ItemType itemType => rawInventoryItem.itemType;
        public int value => rawInventoryItem.value;

        public BaseSkill Skill => string.IsNullOrEmpty(rawInventoryItem.skillID) ? null : SkillManager.GetSkillById(rawInventoryItem.skillID);
    }
}