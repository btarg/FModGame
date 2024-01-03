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

        public void Init()
        {
            HealthManager.InitStats(Stats);
            HealthManager.OnDeath.AddListener(OnDeath);
            HealthManager.OnWeaknessEncountered.AddListener(OnWeaknessEncountered);
            HealthManager.OnDamage.AddListener(OnDamage);

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

        public void OnDeath()
        {
            Debug.Log($"{DisplayName} has died.");
        }

        public void OnWeaknessEncountered(ElementType elementType)
        {
            Debug.Log($"{DisplayName} is weak to {elementType}!");
            // TODO: log this
        }

    }

}