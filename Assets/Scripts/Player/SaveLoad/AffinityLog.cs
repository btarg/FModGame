using System.Collections.Generic;
using Util.DataTypes;

namespace Player.SaveLoad
{
    public static class AffinityLog
    {
        private static AffinityLogDictionary log = new();

        private static AffinityData GetOrCreateData(string characterName)
        {
            var pair = log.dataByCharacter.Find(p => p.key == characterName);
            if (pair == null)
            {
                pair = new AffinityKeyValuePair { key = characterName, value = new AffinityData() };
                log.dataByCharacter.Add(pair);
            }
            return pair.value;
        }

        public static void LogStrength(string characterName, ElementType strength, StrengthType type)
        {
            GetOrCreateData(characterName).strengths[strength] = type;
        }

        public static void LogWeakness(string characterName, ElementType weakness)
        {
            GetOrCreateData(characterName).weaknesses.Add(weakness);
        }

        public static Dictionary<ElementType, StrengthType> GetStrengthsEncountered(string characterName)
        {
            return GetOrCreateData(characterName).strengths;
        }

        public static List<ElementType> GetWeaknessesEncountered(string characterName)
        {
            return GetOrCreateData(characterName).weaknesses;
        }

        public static void Load()
        {
            AffinityLogDictionary loaded = SaveManager.Load().affinityLogDictionary;
            if (loaded != null)
            {
                log = loaded;
            }
        }

        public static void Save()
        {
            SaveManager.SaveAffinityLog(log);
        }
    }
}