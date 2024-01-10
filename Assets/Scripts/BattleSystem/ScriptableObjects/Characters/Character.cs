using UnityEngine;
using System.Collections.Generic;
using System;
using BattleSystem.ScriptableObjects.Skills;
using BattleSystem.ScriptableObjects.Stats.CharacterStats;

namespace BattleSystem.ScriptableObjects.Characters
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Character")]
    public class Character : ScriptableObject
    {
        public String DisplayName;
        public Color Color;
        public CharacterStats Stats;
        // TODO: use a weapon item equipped by the character instead of a skill. The weapon holds the skill
        public BaseSkill attackSkill;
        public List<BaseSkill> AvailableSkills;
        public HealthManager HealthManager;
        public bool IsPlayerCharacter;

        public GameObject prefab;

    }

}