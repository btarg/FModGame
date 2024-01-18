using System;
using System.Collections.Generic;
using ScriptableObjects.Characters.Health;
using ScriptableObjects.Skills;
using ScriptableObjects.Stats.CharacterStats;
using ScriptableObjects.Util.DataTypes;
using ScriptableObjects.Util.SaveLoad;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScriptableObjects.Characters
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Character")]
    public class Character : ScriptableObject
    {
        public string characterID;
        public string DisplayName;
        public Color Color;
        public CharacterStats Stats;

        public InventoryItem weapon;
        public List<BaseSkill> AvailableSkills;
        public bool IsPlayerCharacter;
        public GameObject prefab;

        public Character()
        {
            UUID = Guid.NewGuid().ToString();
        }

        public string UUID { get; }
        [HideInInspector] public HealthManager HealthManager;

        private void Awake()
        {
            InitCharacter(Stats.rawCharacterStats);
        }
        public void InitCharacter(RawCharacterStats rawCharacterStats, bool loadFromSave = true)
        {
            HealthManager = CreateInstance<HealthManager>();
            
            CharacterStats useStats = CreateInstance<CharacterStats>();
            useStats.rawCharacterStats = rawCharacterStats;

            if (loadFromSave)
            {
                var loadedSaveObject = SaveManager.Load();
                if (loadedSaveObject.characterStats == null)
                {
                    Debug.LogError("Null stats! Everybody panic!!");
                    loadedSaveObject.characterStats = new CharacterStatsDictionary();
                }
                // Look up the CharacterStats in the dictionary
                foreach (var statsKeyValuePair in loadedSaveObject.characterStats.statsByCharacter)
                {
                    if (statsKeyValuePair.characterID == characterID)
                    {
                        useStats.rawCharacterStats = statsKeyValuePair.stats;
                        break;
                    }
                }
            }
            HealthManager.InitStats(useStats, UUID);
        }
    }
}