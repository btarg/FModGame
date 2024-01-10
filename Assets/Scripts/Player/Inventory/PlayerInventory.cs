using System;
using System.Collections.Generic;
using BattleSystem.ScriptableObjects.Characters;

namespace Player.Inventory
{
    public class PlayerInventory
    {       
        private Dictionary<InventoryItem, int> inventoryItems = new();

        public Dictionary<InventoryItem, int> GetInventoryItems()
        {
            return inventoryItems;
        }

        public void AddInventoryItem(InventoryItem item, int count)
        {
            // if already exists, add to the count, otherwise add a new entry
            if (!inventoryItems.TryAdd(item, count))
            {
                inventoryItems[item] += count;
            }
        }
        public void RemoveInventoryItem(InventoryItem item, int count)
        {
            if (!inventoryItems.ContainsKey(item)) return;
            inventoryItems[item] -= count;
            if (inventoryItems[item] <= 0)
            {
                inventoryItems.Remove(item);
            }
        }

        public void UseItem(PlayerController playerController, UUIDCharacterInstance playerCharacterInstance, InventoryItem item, UUIDCharacterInstance target)
        {
            if (!inventoryItems.ContainsKey(item)) return;
            inventoryItems[item] -= 1;
            if (inventoryItems[item] <= 0)
            {
                inventoryItems.Remove(item);
            }

            switch (item.itemType)
            {
                case ItemType.ConsumableBuffDebuff:
                    playerCharacterInstance.Character.HealthManager.ApplyBuffDebuff(item.buffDebuff);
                    break;
                case ItemType.ConsumableSkill:
                    playerController.SelectSkill(item.skill);
                    break;
                
            }
        }
    }
}
