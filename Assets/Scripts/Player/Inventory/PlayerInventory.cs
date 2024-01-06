using System;
using System.Collections.Generic;

namespace Player.Inventory
{
    public class PlayerInventory
    {       
        // TODO: implement inventory:
        private List<InventoryItem> inventoryItems;

        public PlayerInventory()
        {
            inventoryItems = new List<InventoryItem>();
        }

        public List<InventoryItem> GetInventoryItems()
        {
            return inventoryItems;
        }

        public void AddInventoryItem(InventoryItem item)
        {
            inventoryItems.Add(item);
        }
    }
}
