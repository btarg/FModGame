using System;
using ScriptableObjects.Util.DataTypes;
using ScriptableObjects.Util.DataTypes.Stats;
using UnityEngine;

namespace ScriptableObjects.Stats.CharacterStats
{
    [CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Character Stats")]
    [Serializable]
    public class CharacterStats : ScriptableObject
    {
        public RawCharacterStats rawCharacterStats;
        public int MaxHP => rawCharacterStats.MaxHP;
        public int MaxSP => rawCharacterStats.MaxSP;
        
        public int HP
        {
            get => rawCharacterStats.HP;
            set => rawCharacterStats.HP = value;
        }

        public int SP
        {
            get => rawCharacterStats.SP;
            set => rawCharacterStats.SP = value;
        }

        public float ATK
        {
            get => rawCharacterStats.ATK;
            set => rawCharacterStats.ATK = value;
        }

        public float DEF
        {
            get => rawCharacterStats.DEF;
            set => rawCharacterStats.DEF = value;
        }

        public float EVD
        {
            get => rawCharacterStats.EVD;
            set => rawCharacterStats.EVD = value;
        }

        public float VIT => rawCharacterStats.VIT;
        public ElementType[] Weaknesses => rawCharacterStats.Weaknesses;
        public ElementStrength[] Strengths => rawCharacterStats.Strengths;
        public int critDamageMultiplier => rawCharacterStats.critDamageMultiplier;
        public int XPDroppedOnDeath => rawCharacterStats.XPDroppedOnDeath;
        public int currentXP
        {
            get => rawCharacterStats.currentXP;
            set => rawCharacterStats.currentXP = value;
        }

        public int currentLevel
        {
            get => rawCharacterStats.currentLevel;
            set => rawCharacterStats.currentLevel = value;
        }

        public int XPToLevelUp => rawCharacterStats.XPToLevelUp;
        public int HPIncreasePerLevel => rawCharacterStats.HPIncreasePerLevel;
        public int SPIncreasePerLevel => rawCharacterStats.SPIncreasePerLevel;
        public float ATKIncreasePerLevel => rawCharacterStats.ATKIncreasePerLevel;
        public float DEFIncreasePerLevel => rawCharacterStats.DEFIncreasePerLevel;
        public float EVDIncreasePerLevel => rawCharacterStats.EVDIncreasePerLevel;
        
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
            if (XPToLevelUp != 0 && currentXP >= XPToLevelUp * currentLevel)
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