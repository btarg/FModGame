using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ScriptableObjects.Stats.CharacterStats;
using ScriptableObjects.Util.DataTypes;
using UnityEngine;

namespace ScriptableObjects.Util.SaveLoad
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
            // Load the CharacterStats from the save file
            saveObject = new SaveObject();
            if (!File.Exists(jsonPath))
            {
                File.Create(jsonPath).Dispose();
                SaveToFile();
                return saveObject;
            }

            string json = File.ReadAllText(jsonPath);
            saveObject = JsonUtility.FromJson<SaveObject>(json);

            return saveObject;
        }

        public static void SaveAffinityLog(AffinityLogDictionary log)
        {
            saveObject.affinityLogDictionary = log;
        }

        public static void SaveInventory(SerializableDictionary<InventoryItem, int> inventory)
        {
            saveObject.inventoryItems = inventory;
        }

        public static void SaveStats(string characterID, CharacterStats characterStats)
        {
            Debug.Log("Saving character");
            // log some stats from this character
            Debug.Log($"SP: {characterStats.SP}");

            // save stats to file if a key doesn't exist
            if (!saveObject.characterStats.statsByCharacter.Exists(pair => pair.characterID == characterID))
            {
                saveObject.characterStats.statsByCharacter.Add(new CharacterStatsKeyValuePair
                    { characterID = characterID, stats = characterStats });
            }
            else
            {
                var pair = saveObject.characterStats.statsByCharacter.Find(pair => pair.characterID == characterID);
                pair.stats = characterStats;
            }
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