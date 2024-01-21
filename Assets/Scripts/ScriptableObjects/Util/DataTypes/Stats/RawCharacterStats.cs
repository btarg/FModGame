using System;
using ScriptableObjects.Stats;
using UnityEngine;

namespace ScriptableObjects.Util.DataTypes.Stats
{
    [Serializable]
    public class RawCharacterStats
    {
        [Tooltip("Health points of the character")] [Range(0, 1000)]
        [SerializeField] public int MaxHP;
        [Tooltip("Skill points of the character")] [Range(0, 1000)]
        [SerializeField] public int MaxSP;
        
        [SerializeField]
        [HideInInspector]
        public int HP;
        [SerializeField]
        [HideInInspector]
        public int SP;

        [Tooltip("Multiplier for all types of damage")] [Range(0, 10)]
        [SerializeField]
        public float ATK;

        [Tooltip("Percentage reduction of all incoming damage (0-1)")] [Range(0, 1)]
        [SerializeField]
        public float DEF;

        [Tooltip("Percentage chance to evade an attack (0-1)")] [Range(0, 1)]
        [SerializeField]
        public float EVD;

        [Tooltip("Vitality")] [Range(0, 1)] 
        [SerializeField]
        public float VIT;

        [Header("Elemental Affinities")]
        [SerializeField]
        public ElementType[] Weaknesses;
        [SerializeField]
        public ElementStrength[] Strengths;
        [SerializeField]
        public int critDamageMultiplier = 2;

        [Header("Level Up")]
        [SerializeField] public int XPDroppedOnDeath;
        [SerializeField] public int currentXP;
        [SerializeField] public int currentLevel;
        [SerializeField] public int XPToLevelUp;
        [SerializeField] public int HPIncreasePerLevel;
        [SerializeField] public int SPIncreasePerLevel;
        [SerializeField] public float ATKIncreasePerLevel;
        [SerializeField] public float DEFIncreasePerLevel;
        [SerializeField] public float EVDIncreasePerLevel;
   }
}