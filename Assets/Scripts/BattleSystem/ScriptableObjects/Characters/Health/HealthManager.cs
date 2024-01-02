using UnityEngine;
using UnityEngine.Events;
using System;
namespace BattleSystem.ScriptableObjects.Characters
{
    [CreateAssetMenu(fileName = "NewHealthManager", menuName = "HealthManager")]
    public class HealthManager : ScriptableObject
    {
        public bool isAlive = true;

        [System.Serializable]
        public class DamageEvent : UnityEvent<HealthManager, int> { }

        [System.Serializable]
        public class NullifyEvent : UnityEvent { }

        [System.Serializable]
        public class ReflectEvent : UnityEvent<HealthManager, int> { }

        [System.Serializable]
        public class ResistEvent : UnityEvent<int> { }

        [System.Serializable]
        public class DeathEvent : UnityEvent<HealthManager> { }

        public DamageEvent OnDamage;
        public NullifyEvent OnNullify;
        public ReflectEvent OnReflect;
        public ResistEvent OnResist;
        public DeathEvent OnDeath;

        private CharacterStats stats;
        public int currentHP;
        public int currentSP;
        public int currentATK;
        public int currentDEF;
        public int currentEVD;

        public void InitStats(CharacterStats _stats)
        {
            isAlive = true;
            stats = _stats;
            currentHP = _stats.HP;
            currentSP = _stats.SP;
            currentATK = _stats.ATK;
            currentDEF = _stats.DEF;
            currentEVD = _stats.EVD;
        }

        public void TakeDamage(HealthManager attacker, int damage, ElementType elementType)
        {
            if (Array.IndexOf(stats.Strengths, elementType) >= 0)
            {
                foreach (var strength in stats.Strengths)
                {
                    switch (strength.StrengthType)
                    {
                        case StrengthType.Nullify:
                            OnNullify?.Invoke();
                            return;
                        case StrengthType.Reflect:
                            attacker.TakeDamage(this, damage, elementType);
                            OnReflect?.Invoke(attacker, damage);
                            return;
                        case StrengthType.Resist:
                            damage = damage * (100 - strength.ResistPercentage) / 100;
                            OnResist?.Invoke(damage);
                            break;
                    }
                }
            }

            if (Array.IndexOf(stats.Weaknesses, elementType) >= 0)
            {
                damage *= 2;
            }

            currentHP = Mathf.Max(currentHP - damage, 0);

            // If the character's health reaches 0, invoke the death event
            if (currentHP == 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            currentHP = Mathf.Min(currentHP + amount, stats.HP);
        }

        public void Die()
        {
            isAlive = false;
            OnDeath?.Invoke(this);
        }
    }
}