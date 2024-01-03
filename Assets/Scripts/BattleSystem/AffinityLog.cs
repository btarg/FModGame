using System;
using System.Collections.Generic;
using System.IO;
using BattleSystem.DataTypes;
using SerializableCollections;
using UnityEngine;

namespace BattleSystem
{
    [Serializable]
    public class AffinityData
    {
        [SerializeField]
        public SerializableDictionary<ElementType, StrengthType> strengths = new();
        [SerializeField]
        public List<ElementType> weaknesses = new();
    }

    [Serializable]
    public class KeyValuePair
    {
        public string key;
        public AffinityData value;
    }

    [Serializable]
    public class AffinityLogDictionary
    {
        [SerializeField]
        public List<KeyValuePair> dataByCharacter = new();
    }

    public static class AffinityLog
    {
        private static AffinityLogDictionary log = new();
        private static readonly string jsonFilePath = Path.Combine(Application.persistentDataPath, "AffinityLog.json");

        private static AffinityData GetOrCreateData(string characterName)
        {
            var pair = log.dataByCharacter.Find(p => p.key == characterName);
            if (pair == null)
            {
                pair = new KeyValuePair { key = characterName, value = new AffinityData() };
                log.dataByCharacter.Add(pair);
            }
            return pair.value;
        }

        public static void LogStrength(string characterName, ElementType strength, StrengthType type)
        {
            GetOrCreateData(characterName).strengths[strength] = type;
            Save();
        }

        public static void LogWeakness(string characterName, ElementType weakness)
        {
            GetOrCreateData(characterName).weaknesses.Add(weakness);
            Save();
        }

        public static Dictionary<ElementType, StrengthType> GetStrengthsEncountered(string characterName)
        {
            return GetOrCreateData(characterName).strengths;
        }

        public static List<ElementType> GetWeaknessesEncountered(string characterName)
        {
            return GetOrCreateData(characterName).weaknesses;
        }

        public static bool LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Dispose();
                log = new AffinityLogDictionary();
                return false;
            }
            string json = File.ReadAllText(filePath);
            log = JsonUtility.FromJson<AffinityLogDictionary>(json);
            return true;
        }

        public static void Save()
        {
            SaveToFile(jsonFilePath);
        }

        public static bool Load()
        {
            return LoadFromFile(jsonFilePath);
        }

        private static void SaveToFile(string filePath)
        {
            Debug.Log("Saving affinity log to " + filePath);
            string json = JsonUtility.ToJson(log);
            File.WriteAllText(filePath, json);
        }
    }
}