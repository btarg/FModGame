using ScriptableObjects.Util.DataTypes;
using UnityEngine;

namespace ScriptableObjects.Stats.CharacterStats
{
    [CreateAssetMenu(fileName = "NewCharacterStats", menuName = "Character Stats")]
    public class CharacterStats : ScriptableObject
    {
        [Tooltip("Health points of the character")] [Range(0, 1000)]
        public int HP;

        [Tooltip("Skill points of the character")] [Range(0, 1000)]
        public int SP;

        [Tooltip("Multiplier for all types of damage")] [Range(0, 10)]
        public float ATK;

        [Tooltip("Percentage reduction of all incoming damage (0-1)")] [Range(0, 1)]
        public float DEF;

        [Tooltip("Percentage chance to evade an attack (0-1)")] [Range(0, 1)]
        public float EVD;

        [Tooltip("Vitality")] [Range(0, 1)] public float VIT;

        [Header("Elemental Affinities")] public ElementType[] Weaknesses;

        public ElementStrength[] Strengths;
        public int critDamageMultiplier = 2;

        [Header("Level Up")] public int XPDroppedOnDeath;

        public int currentXP;
        public int currentLevel;
        public int XPToLevelUp;
        public int HPIncreasePerLevel;
        public int SPIncreasePerLevel;
        public int ATKIncreasePerLevel;
        public int DEFIncreasePerLevel;
        public int EVDIncreasePerLevel;
        public int MaxHP => HP;
        public int MaxSP => SP;

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

        public void GainXP(int amount)
        {
            currentXP += amount * (int)VIT;

            while (currentXP >= XPToLevelUp * currentLevel) LevelUp();
        }

        private void LevelUp()
        {
            currentXP -= XPToLevelUp;
            currentLevel++;

            // Increase base stats
            HP += HPIncreasePerLevel;
            SP += SPIncreasePerLevel;
            ATK += ATKIncreasePerLevel;
            DEF += DEFIncreasePerLevel;
            EVD += EVDIncreasePerLevel;
        }
    }
}