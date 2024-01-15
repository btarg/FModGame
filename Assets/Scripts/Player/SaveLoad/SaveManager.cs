using System.IO;
using Player.Inventory;
using UnityEngine;
using Util.DataTypes;

namespace Player.SaveLoad
{
    public static class SaveManager
    {
        private static readonly string jsonPath = Path.Combine(Application.persistentDataPath, "Save.json");
        private static SaveObject saveObject;
        
        // function to get a SaveObject from a json file
        public static SaveObject Load()
        {
            saveObject = new SaveObject();
            if (!File.Exists(jsonPath))
            {
                File.Create(jsonPath).Dispose();
                return saveObject;
            }
            string json = File.ReadAllText(jsonPath);
            saveObject = JsonUtility.FromJson<SaveObject>(json);
            return saveObject;
        }
        public static bool SaveAffinityLog(AffinityLogDictionary log)
        {
            saveObject.affinityLogDictionary = log;
            return SaveToFile();
        }
        public static bool SaveInventory(PlayerInventory inventory)
        {
            saveObject.inventory = inventory;
            return SaveToFile();
        }

        private static bool SaveToFile()
        {
            string json = JsonUtility.ToJson(saveObject);
            File.WriteAllText(jsonPath, json);
            Debug.Log("Saved to " + jsonPath);
            return true;
        }
    }
}
