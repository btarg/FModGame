using System.Collections.Generic;
using ScriptableObjects.Skills;
using ScriptableObjects.Util.DataTypes;
using ScriptableObjects.Util.DataTypes.Inventory;
using UnityEngine;

namespace Player.Inventory
{
    public class PlayerInventory
    {
        private PlayerController lastPlayerController;

        public PlayerInventory(SerializableDictionary<RawInventoryItem, int> items)
        {
            LoadInventoryItems(items);
            // for debugging, add a default item
            var item = new RawInventoryItem();
            item.itemType = ItemType.ConsumableSkill;
            item.displayName = "Test Item";
            item.description = "This is a test item";
            item.skill = ScriptableObject.CreateInstance<BaseSkill>();
            item.skill.skillName = "Test Skill";
            item.skill.description = "This is a test skill";
            item.skill.skillType = SkillType.Offensive;
            item.skill.CanTargetEnemies = true;
            item.skill.CanTargetAllies = true;
            item.skill.TargetsAll = false;
            item.skill.cost = 0;
            item.skill.MaxDamage = 15;
            item.skill.MinDamage = 5;
            item.skill.IsAffectedByATK = true;
            item.skill.elementType = ElementType.Phys;
            AddInventoryItem(item, 3);
        }

        public SerializableDictionary<InventoryItem, int> inventoryItems { get; } = new();

        public void AddInventoryItem(RawInventoryItem rawItem, int count = 1)
        {
            // try to create an instance of the inventory item and set its raw item to rawItem
            InventoryItem item = ScriptableObject.CreateInstance<InventoryItem>();
            item.rawInventoryItem = rawItem;
            // then we check if the item is already in the inventory and just update the count if it is
            if (!inventoryItems.TryAdd(item, count))
            {
                inventoryItems[item] += count;
            }
        }

        public void RemoveInventoryItem(InventoryItem item, int count = 1)
        {
            // if an item exists reduce its count
            if (inventoryItems.ContainsKey(item))
            {
                inventoryItems[item] -= count;
                // if the count is 0, remove the item from the inventory
                if (inventoryItems[item] == 0)
                {
                    inventoryItems.Remove(item);
                }
            }
        }

        public void UseItem(PlayerController playerController, InventoryItem item)
        {
            // log the current inventory in the format item:count
            foreach (KeyValuePair<InventoryItem, int> inventoryItem in inventoryItems)
            {
                Debug.Log($"{inventoryItem.Key.displayName}:{inventoryItem.Value}");
            }
            
            lastPlayerController = playerController;
            switch (item.itemType)
            {
                case ItemType.ConsumableSkill:
                    lastPlayerController.SelectSkill(item.skill);
                    // when we use a skill, we need to remove it from the inventory
                    lastPlayerController.PlayerUsedSkillEvent.AddListener(_ => UseItemListener(item));
                    break;
            }
        }

        public void LoadInventoryItems(SerializableDictionary<RawInventoryItem, int> items)
        {
            foreach (KeyValuePair<RawInventoryItem, int> item in items)
            {
                AddInventoryItem(item.Key, item.Value);
                Debug.Log($"Added {item.Value} {item.Key.displayName} to inventory");
            }
        }
        private void UseItemListener(InventoryItem item)
        {
            RemoveInventoryItem(item);
            if (lastPlayerController != null)
            {
                Debug.Log("Removing listener");
                lastPlayerController.PlayerUsedSkillEvent.RemoveAllListeners();
            }
        }
    }
}