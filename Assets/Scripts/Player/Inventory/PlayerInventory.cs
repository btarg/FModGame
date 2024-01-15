using System;
using System.Collections.Generic;
using System.IO;
using BeatDetection.DataStructures;
using Player.SaveLoad;
using UnityEngine;
using UnityEngine.Events;
using Util;
using Util.DataTypes;

namespace Player.Inventory
{
    [Serializable]
    public class PlayerInventory
    {
        public SerializableDictionary<InventoryItem, int> inventoryItems { get; private set; } = new();
        private PlayerController lastPlayerController;
       
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

        private UnityAction<BeatResult> UseItemListener(InventoryItem item)
        {
            RemoveInventoryItem(item);
            if (lastPlayerController != null)
            {
                lastPlayerController.PlayerUsedSkillEvent.RemoveListener(_ => UseItemListener(item));
            }
            return null;
        }
    }
}
