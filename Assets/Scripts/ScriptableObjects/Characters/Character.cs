using System;
using System.Collections.Generic;
using ScriptableObjects.Characters.AiStates;
using ScriptableObjects.Characters.Health;
using ScriptableObjects.Skills;
using ScriptableObjects.Stats.CharacterStats;
using ScriptableObjects.Util.DataTypes;
using ScriptableObjects.Util.DataTypes.Inventory;
using ScriptableObjects.Util.DataTypes.Stats;
using ScriptableObjects.Util.SaveLoad;
using StateMachine;
using UnityEngine;
using UnityEngine.Events;
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

        public StateMachine<IState> CharacterStateMachine { get; private set; } = new();
        public UnityEvent NextTurnEvent { get; } = new();

        public Character()
        {
            UUID = Guid.NewGuid().ToString();
        }

        private void Awake()
        {
            InitCharacter();
        }

        public string UUID { get; }
        [HideInInspector] public HealthManager HealthManager;

        public void InitCharacter(bool loadFromSave = true)
        {
            HealthManager = CreateInstance<HealthManager>();

            CharacterStats useStats = CreateInstance<CharacterStats>();
            useStats.rawCharacterStats = Stats.rawCharacterStats;

            bool hasLoadedFromSave = false;
            if (loadFromSave)
            {
                var loadedSaveObject = SaveManager.Load();

                if (loadedSaveObject.characterStats == null)
                {
                    Debug.LogError("Null stats! Everybody panic!!");
                    loadedSaveObject.characterStats = new CharacterStatsDictionary();
                }

                if (loadedSaveObject.characterStats.statsByCharacter.Count > 0)
                {
                    foreach (var statsKeyValuePair in loadedSaveObject.characterStats.statsByCharacter)
                    {
                        if (statsKeyValuePair.characterID == characterID)
                        {
                            useStats.rawCharacterStats = statsKeyValuePair.stats;
                            hasLoadedFromSave = true;
                            break;
                        }
                    }
                }
                else
                {
                    Debug.Log("No stats found!");
                }
            }
            
            HealthManager.InitStats(useStats, UUID, hasLoadedFromSave);
            
            // log HP and SP
            Debug.Log($"{DisplayName} has {HealthManager.CurrentHP} HP and {HealthManager.CurrentSP} SP");

            CharacterStateMachine = new StateMachine<IState>();
            CharacterStateMachine.SetState(new CharacterIdleState());
        }
    }
}