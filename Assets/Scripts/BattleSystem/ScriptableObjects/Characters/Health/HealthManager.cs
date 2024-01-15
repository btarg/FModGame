using UnityEngine;
using UnityEngine.Events;
using System;
using System.Linq;
using BattleSystem.ScriptableObjects.Stats.Modifiers;
using BattleSystem.DataTypes;
using BattleSystem.ScriptableObjects.Stats.CharacterStats;
using System.Collections.Generic;
using BattleSystem.ScriptableObjects.Stats;

namespace BattleSystem.ScriptableObjects.Characters
{
    [CreateAssetMenu(fileName = "NewHealthManager", menuName = "HealthManager")]
    public class HealthManager : ScriptableObject
    {
        public bool isAlive = true;

        public class OnDamagedEvent : UnityEvent<HealthManager, ElementType, int> { }
        public class OnStrengthEncounteredEvent : UnityEvent<ElementType, StrengthType> { }
        public class OnWeaknessEncounteredEvent : UnityEvent<ElementType> { }
        public class OnDamageEvadedEvent : UnityEvent { }
        public class OnDeathEvent : UnityEvent<string> { }
        public class OnReviveEvent : UnityEvent<string> { }

        public int critDamageMultiplier = 2;
        public OnDamagedEvent OnDamage = new();
        public OnStrengthEncounteredEvent OnStrengthEncountered = new();
        public OnWeaknessEncounteredEvent OnWeaknessEncountered = new();
        public OnDeathEvent OnDeath = new();
        public OnReviveEvent OnRevive = new();
        public OnDamageEvadedEvent OnDamageEvaded = new();


        private CharacterStats stats;
        private string UUID;
        private List<BuffDebuff> activeBuffDebuffs = new();
        public bool isGuarding { get; private set; }

        public int guardingTurnsLeft { get; private set; }
        public void StartGuarding(int turns)
        {
            isGuarding = true;
            guardingTurnsLeft = turns;
        }

        public void InitStats(CharacterStats _stats, string _UUID)
        {
            isAlive = true;
            stats = Instantiate(_stats);
            UUID = _UUID;
            // initialise max HP and SP
            MaxHP = stats.HP;
            MaxSP = stats.SP;
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
            get => GetCurrentStat(StatType.HP);
            private set => stats.HP = Mathf.Min(value, MaxHP);
        }

        public int CurrentSP
        {
            get => GetCurrentStat(StatType.SP);
            private set => stats.SP = Mathf.Min(value, MaxSP);
        }

        public float CurrentDEF => GetCurrentStatFloat(StatType.DEF);

        public float CurrentEVD => GetCurrentStatFloat(StatType.EVD);

        public int MaxHP;
        public int MaxSP;

        public void OnTurnStart()
        {
            if (guardingTurnsLeft > 0)
            {
                guardingTurnsLeft--;
                if (guardingTurnsLeft == 0)
                {
                    isGuarding = false;
                }
            }
            
            // get the duration of every buff/debuff and decrement it
            foreach (var buffDebuff in activeBuffDebuffs)
            {
                buffDebuff.Duration--;
            }
            // remove expired
            activeBuffDebuffs.RemoveAll(bd => bd.Duration <= 0);
        }
        
        public void TakeDamage(HealthManager attacker, int damage, ElementType elementType)
        {
            if (!isAlive) return;

            // Calculate evasion (if the attack is not Almighty or a weakness)
            float evasionChance = stats.Weaknesses.Contains(elementType) ? 0 : CurrentEVD;
            if (UnityEngine.Random.value < evasionChance && elementType != ElementType.Almighty)
            {
                // The attack is evaded, return
                OnDamageEvaded?.Invoke();
                return;
            }
            
            if (elementType != ElementType.Almighty)
            {
                // Calculate defense if not a weakness
                if (!stats.Weaknesses.Contains(elementType))
                {
                    damage = Mathf.CeilToInt(damage * (1 - CurrentDEF));
                }
                if (isGuarding)
                {
                    damage -= Mathf.RoundToInt(damage * 0.4f);
                }
                
                // Check for strengths
                if (Array.IndexOf(stats.Strengths, elementType) >= 0)
                {
                    foreach (ElementStrength strength in stats.Strengths)
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
                if (stats.Weaknesses.Contains(elementType) && !isGuarding)
                {
                    // Critical hit!
                    OnWeaknessEncountered?.Invoke(elementType);
                    damage *= critDamageMultiplier;
                }
            }
            
            CurrentHP = Mathf.Max(CurrentHP - damage, 0);
            OnDamage?.Invoke(this, elementType, damage);
            
            if (CurrentHP == 0) Die();
        }

        public void Revive(UUIDCharacterInstance character, int amount)
        {
            CurrentHP = amount;
            isAlive = true;
            OnRevive?.Invoke(character.UUID);
        }

        public void Heal(int amount)
        {
            if (!isAlive) return;
            Debug.Log($"Healing {amount}/{MaxHP} HP");
            CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
        }

        public void ChangeSP(int amount)
        {
            CurrentSP = Mathf.Max(Mathf.Min(CurrentSP + amount, MaxSP), 0);
        }
        public void RemoveAllStatModifiers()
        {
            activeBuffDebuffs.Clear();
        }

        public void Die()
        {
            isAlive = false;
            OnDeath?.Invoke(UUID);
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