using UnityEngine;
using System.Collections.Generic;
using System;
using BattleSystem.ScriptableObjects.Skills;
using BattleSystem.ScriptableObjects.Stats.CharacterStats;
using BattleSystem.DataTypes;

namespace BattleSystem.ScriptableObjects.Characters
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Character")]
    public class Character : ScriptableObject
    {
        public String DisplayName;
        public Color Color;
        public CharacterStats Stats;
        public BaseSkill attackSkill;
        public List<BaseSkill> AvailableSkills;
        public HealthManager HealthManager;
        public bool IsPlayerCharacter;

        public GameObject prefab;

        public void Init()
        {
            HealthManager.OnDeath.AddListener(OnDeath);
            HealthManager.OnWeaknessEncountered.AddListener(OnWeaknessEncountered);
            HealthManager.OnStrengthEncountered.AddListener(OnStrengthEncountered);
            HealthManager.OnDamage.AddListener(OnDamage);
            HealthManager.OnDamageEvaded.AddListener(OnDamageEvaded);
            HealthManager.OnRevive.AddListener(OnRevive);

            Debug.Log($"{DisplayName} has been initialized.");
        }

        public void OnDamage(HealthManager healthManager, int damage)
        {
            Debug.Log($"{DisplayName} took {damage} damage. ({healthManager.CurrentHP} HP left)");
        }
        public void OnDamageEvaded()
        {
            Debug.Log($"{DisplayName} evaded the attack!");
        }

        public void OnDeath(string uuid)
        {
            Debug.Log($"{DisplayName} has died.");
        }
        public void OnRevive(string uuid)
        {
            Debug.Log($"{DisplayName} has been revived.");
        }

        public void OnWeaknessEncountered(ElementType elementType)
        {
            Debug.Log($"{DisplayName} is weak to {elementType}!");
            if (!AffinityLog.GetWeaknessesEncountered(name).Contains(elementType))
            {
                AffinityLog.LogWeakness(name, elementType);
            }
        }

        public void OnStrengthEncountered(ElementType elementType, StrengthType strengthType)
        {
            Debug.Log($"{DisplayName} is strong against {strengthType}!");
            if (!AffinityLog.GetStrengthsEncountered(name).ContainsKey(elementType))
            {
                AffinityLog.LogStrength(name, elementType, strengthType);
            }
        }

    }

}