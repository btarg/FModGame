using UnityEngine;
using System.Collections.Generic;
using System;
using BattleSystem.ScriptableObjects.Skills;
namespace BattleSystem.ScriptableObjects.Characters
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Character")]
    public class Character : ScriptableObject
    {
        public String DisplayName;
        public Color Color;
        public CharacterStats Stats;
        public List<BaseSkill> AvailableSkills;
        public HealthManager HealthManager;
        public bool IsPlayerCharacter;

        public void Init()
        {
            HealthManager.InitStats(Stats);
            HealthManager.OnDeath.AddListener(OnDeath);
            Debug.Log($"{DisplayName} has been initialized.");
        }

        public void OnDeath(HealthManager healthManager)
        {
            Debug.Log($"{DisplayName} has died.");
        }

    }

}