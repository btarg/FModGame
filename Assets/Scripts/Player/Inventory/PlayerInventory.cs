using System.Collections.Generic;
using UnityEngine;
using Util.DataTypes;
namespace Player.Inventory
{
    public class PlayerInventory
    {
        public SerializableDictionary<InventoryItem, int> inventoryItems { get; }
        private PlayerController lastPlayerController;
       
        public PlayerInventory(SerializableDictionary<InventoryItem, int> items)
        {
            inventoryItems = items;
        }
        
        public void AddInventoryItem(InventoryItem item, int count)
        {
            // if already exists, add to the count, otherwise add a new entry
            if (!inventoryItems.TryAdd(item, count))
            {
                inventoryItems[item] += count;
            }
        }
        public void RemoveInventoryItem(InventoryItem item, int count = 1)
        {
            if (!inventoryItems.ContainsKey(item)) return;
            inventoryItems[item] -= count;
            if (inventoryItems[item] <= 0)
            {
                inventoryItems.Remove(item);
            }
        }

        public void UseItem(PlayerController playerController, InventoryItem item)
        {
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
        
        public void LoadInventoryItems(List<InventoryItem> items)
        {
            foreach (var item in items)
            {
                AddInventoryItem(item, 1);
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
