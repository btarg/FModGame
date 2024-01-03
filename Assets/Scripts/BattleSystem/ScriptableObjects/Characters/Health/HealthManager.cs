using UnityEngine;
using UnityEngine.Events;
using System;
using System.Linq;
using BattleSystem.ScriptableObjects.Stats.Modifiers;
using BattleSystem.DataTypes;
using BattleSystem.ScriptableObjects.Stats.CharacterStats;
using System.Collections.Generic;

namespace BattleSystem.ScriptableObjects.Characters
{
    [CreateAssetMenu(fileName = "NewHealthManager", menuName = "HealthManager")]
    public class HealthManager : ScriptableObject
    {
        public bool isAlive = true;

        [System.Serializable]
        public class OnDamagedEvent : UnityEvent<HealthManager, int> { }

        [System.Serializable]
        public class OnStrengthEncounteredEvent : UnityEvent<ElementType, StrengthType> { }

        [System.Serializable]
        public class OnWeaknessEncounteredEvent : UnityEvent<ElementType> { }

        [System.Serializable]
        public class OnDamageEvadedEvent : UnityEvent { }
        [System.Serializable]
        public class OnDeathEvent : UnityEvent { }
        [System.Serializable]
        public class OnReviveEvent : UnityEvent<Character> { }


        public int critDamageMultiplier = 2;

        public OnDamagedEvent OnDamage;
        public OnStrengthEncounteredEvent OnStrengthEncountered;
        public OnWeaknessEncounteredEvent OnWeaknessEncountered;
        public OnDeathEvent OnDeath;
        public OnReviveEvent OnRevive;
        public OnDamageEvadedEvent OnDamageEvaded;

        private CharacterStats stats;
        private List<BuffDebuff> activeBuffDebuffs = new List<BuffDebuff>();

        public void InitStats(CharacterStats _stats)
        {
            isAlive = true;
            stats = _stats;
        }

        private int GetCurrentStat(StatType statType)
        {
            return Mathf.CeilToInt(GetCurrentStatFloat(statType));
        }

        private float GetCurrentStatFloat(StatType statType)
        {
            float multiplier = 1;
            foreach (var buffDebuff in activeBuffDebuffs)
            {
                var modifier = buffDebuff.StatModifiers.FirstOrDefault(m => m.StatType == statType);
                if (modifier != null)
                {
                    multiplier *= modifier.Multiplier;
                }
            }
            return stats.GetStat(statType) * multiplier;
        }

        public int CurrentHP
        {
            get { return GetCurrentStat(StatType.HP); }
            set { stats.HP = Mathf.Min(value, MaxHP); }
        }

        public int CurrentSP
        {
            get { return GetCurrentStat(StatType.SP); }
            set { stats.SP = Mathf.Min(value, MaxSP); }
        }

        public float CurrentDEF
        {
            get { return GetCurrentStatFloat(StatType.DEF); }
        }

        public float CurrentEVD
        {
            get { return GetCurrentStatFloat(StatType.EVD); }
        }

        public int MaxHP => stats.MaxHP;
        public int MaxSP => stats.MaxSP;

        public void TakeDamage(HealthManager attacker, int damage, ElementType elementType)
        {
            if (!isAlive) return;

            // Calculate evasion
            float evasionChance = CurrentEVD;
            if (UnityEngine.Random.value < evasionChance)
            {
                // The attack is evaded, return
                OnDamageEvaded?.Invoke();
                return;
            }

            // Calculate defense
            float defenseMultiplier = CurrentDEF;
            damage = Mathf.CeilToInt(damage * (1 - defenseMultiplier));

            if (Array.IndexOf(stats.Strengths, elementType) >= 0)
            {
                foreach (var strength in stats.Strengths)
                {
                    switch (strength.StrengthType)
                    {
                        case StrengthType.Nullify:
                            return;
                        case StrengthType.Reflect:
                            attacker.TakeDamage(this, damage, elementType);
                            return;
                        case StrengthType.Resist:
                            damage = damage * (100 - strength.ResistPercentage) / 100;
                            break;
                    }
                    OnStrengthEncountered?.Invoke(strength.ElementType, strength.StrengthType);
                }
            }

            if (Array.IndexOf(stats.Weaknesses, elementType) >= 0)
            {
                // Critical hit!
                OnWeaknessEncountered?.Invoke(elementType);
                damage *= critDamageMultiplier;
            }

            CurrentHP = Mathf.Max(CurrentHP - damage, 0);

            OnDamage?.Invoke(this, damage);

            // If the character's health reaches 0, invoke the death event
            if (CurrentHP == 0)
            {
                Die();
            }
        }

        public void Revive(Character character, int amount)
        {
            CurrentHP = amount;
            isAlive = true;
            OnRevive?.Invoke(character);
        }

        public void Heal(int amount)
        {
            CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
        }

        public void ReplenishSP(int amount)
        {
            CurrentSP = Mathf.Min(CurrentSP + amount, MaxSP);
        }
        public void RemoveAllStatModifiers()
        {
            activeBuffDebuffs.Clear();
        }

        public void Die()
        {
            isAlive = false;
            OnDeath?.Invoke();
        }

        public void ApplyBuffDebuff(BuffDebuff buffDebuff)
        {
            if (!isAlive) return;

            // If the buff/debuff is already active, remove the original before adding the new one
            if (activeBuffDebuffs.Contains(buffDebuff) && !buffDebuff.CanStack)
            {
                activeBuffDebuffs.Remove(buffDebuff);
            }
            activeBuffDebuffs.Add(buffDebuff);
        }
    }
}