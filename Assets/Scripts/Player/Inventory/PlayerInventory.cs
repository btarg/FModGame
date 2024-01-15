using System;
using System.Collections.Generic;
using BattleSystem.ScriptableObjects.Characters;
using BeatDetection.DataStructures;
using UnityEngine;
using UnityEngine.Events;

namespace Player.Inventory
{
    [Serializable]
    public class PlayerInventory
    {
        public Dictionary<InventoryItem, int> inventoryItems { get; private set; } = new();
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

        public void UseItem(PlayerController playerController, UUIDCharacterInstance playerCharacterInstance, InventoryItem item)
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
        public void LoadInventoryItems(List<InventoryItem> items)
        {
            foreach (var item in items)
            {
                AddInventoryItem(item, 1);
            }
        }

        public void LoadFromFile()
        {
            // TODO: get a json file in the persistent data path and load it
        }

        public void SaveToFile()
        {
        }
    }
}
