using System;
using System.Globalization;
using System.IO;
using Player.Inventory;
using UnityEngine;
using Util.DataTypes;

namespace Player.SaveLoad
{
    public static class SaveManager
    {
        private static SaveObject saveObject;
        
        // when getting the jsonPath, we should determine the file name based on the save slot stored in player prefs
        private static string jsonPath
        {
            get
            {
                int saveSlot = PlayerPrefs.GetInt("SaveSlot", 0);
                return Path.Combine(Application.persistentDataPath, "Save" + saveSlot + ".json");
            }
        }

        public static SaveObject Load()
        {
            saveObject = new SaveObject();
            if (!File.Exists(jsonPath))
            {
                File.Create(jsonPath).Dispose();
                SaveToFile();
                return saveObject;
            }
            string json = File.ReadAllText(jsonPath);
            saveObject = JsonUtility.FromJson<SaveObject>(json);

            Debug.Log($"Loaded from {jsonPath} at {DateTime.FromFileTimeUtc(saveObject.timestamp).ToString(CultureInfo.CurrentCulture)}");
            return saveObject;
        }
        public static void SaveAffinityLog(AffinityLogDictionary log)
        {
            saveObject.affinityLogDictionary = log;
        }
        public static void SaveInventory(PlayerInventory inventory)
        {
            saveObject.inventoryItems = inventory.inventoryItems;
        }

        public static bool SaveToFile()
        {
            try
            {
                saveObject.timestamp = DateTime.Now.ToFileTimeUtc();
                string json = JsonUtility.ToJson(saveObject);
                File.WriteAllText(jsonPath, json);
                Debug.Log("Saved to " + jsonPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to save to " + jsonPath);
                Debug.LogWarning(e.Message);
                return false;
            }

            return true;
        }
    }
}
