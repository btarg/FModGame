using System.Collections.Generic;
using BeatDetection.DataStructures;
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
            RawInventoryItem item = new()
            {
                itemType = ItemType.ConsumableSkill,
                id = "test_item",
                displayName = "Test Item",
                description = "This is a test item",
                skillID = "MeleeAttack"
            };
            AddInventoryItem(item, 3);
        }

        public SerializableDictionary<InventoryItem, int> inventoryItems { get; } = new();

        public void AddInventoryItem(RawInventoryItem rawItem, int count = 1)
        {
            // try to create an instance of the inventory item and set its raw item to rawItem
            InventoryItem item = ScriptableObject.CreateInstance<InventoryItem>();
            item.rawInventoryItem = rawItem;
            // then we check if the item is already in the inventory and just update the count if it is
            foreach (InventoryItem inventoryItem in inventoryItems.Keys)
            {
                if (inventoryItem.rawInventoryItem.id == rawItem.id)
                {
                    inventoryItems[inventoryItem] += count;
                    return;
                }
            }
            // otherwise we add the item to the inventory
            inventoryItems.Add(item, count);
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
                    lastPlayerController.SelectSkill(item.Skill);
                    // when we use a skill, we need to remove it from the inventory
                    lastPlayerController.PlayerUsedItemSkillEvent.AddListener(UseItemListener);
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

        private void UseItemListener(BeatResult result, InventoryItem inventoryItem)
        {
            if (inventoryItem != null)
            {
                // Remove the item
                RemoveInventoryItem(inventoryItem);
                if (lastPlayerController != null)
                {
                    Debug.Log("Removing listener");
                    // Remove the listener
                    lastPlayerController.PlayerUsedItemSkillEvent.RemoveListener(UseItemListener);
                }
            }
        }
    }
}