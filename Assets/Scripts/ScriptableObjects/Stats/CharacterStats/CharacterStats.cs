using System;
using ScriptableObjects.Util.DataTypes;
using UnityEngine;

namespace ScriptableObjects.Stats.CharacterStats
{
    [CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Character Stats")]
    [Serializable]
    public class CharacterStats : ScriptableObject
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
        [SerializeField] public int ATKIncreasePerLevel;
        [SerializeField] public int DEFIncreasePerLevel;
        [SerializeField] public int EVDIncreasePerLevel;

        private void OnEnable()
        {
            HP = MaxHP;
            SP = MaxSP;
        }

        public float GetStat(StatType statType)
        {
            return statType switch
            {
                StatType.HP => HP,
                StatType.SP => SP,
                StatType.ATK => ATK,
                StatType.DEF => DEF,
                StatType.EVD => EVD,
                StatType.VIT => VIT,
                _ => 0
            };
        }
        
        
        public void GainXP(int amount, Action<CharacterStats, bool> callback)
        {
            bool leveledUp = false;
            currentXP += amount * (int)VIT;
            if (currentXP >= XPToLevelUp * currentLevel)
            {
                currentXP -= (XPToLevelUp * currentLevel);
                currentLevel++;

                // Increase base stats
                HP += HPIncreasePerLevel;
                SP += SPIncreasePerLevel;
                ATK += ATKIncreasePerLevel;
                DEF += DEFIncreasePerLevel;
                EVD += EVDIncreasePerLevel;
                leveledUp = true;
            }

            callback.Invoke(this, leveledUp);
        }
        
    }
}