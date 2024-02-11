using System;
using ScriptableObjects.Characters;
using ScriptableObjects.Stats.Modifiers;
using ScriptableObjects.Util.DataTypes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ScriptableObjects.Skills
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Skill")]
    [Serializable]
    public class BaseSkill : ScriptableObject
    {
        public string skillName;
        public string description;
        public string onUsedMessage;

        public int MinDamage;
        public int MaxDamage;
        public bool IsAffectedByATK;

        public SkillType skillType;
        public ElementType elementType; // for offensive skills
        public BuffDebuff buffDebuff; // for buff/debuff skills
        public int cost;
        public bool costsHP;
        public int healAmount; // for heal skills
        public int spAmount; // for replenish SP skills
        public int reviveAmount; // for revive skills

        public bool TargetsAll;
        public bool CanTargetAllies;
        public bool CanTargetEnemies;

        public int GetDamage(Character character)
        {
            int baseDamage = Random.Range(MinDamage - 1, MaxDamage + 1);
            // round damage with mod to the nearest integer
            return IsAffectedByATK ? Mathf.CeilToInt(baseDamage * character.Stats.ATK) : baseDamage;
        }

        public bool Use(Character user, Character target, bool negateCost = false)
        {
            if (user == null || user.HealthManager == null || target == null || target.HealthManager == null)
            {
                Debug.LogError("User or target is null!");
                return false;
            }
            
            if (!negateCost)
            {
                if (costsHP)
                {
                    if (user.HealthManager.CurrentHP - cost < 0)
                    {
                        Debug.Log($"{user.DisplayName} does not have enough HP!");
                        return false;
                    }
                    user.HealthManager.TakeDamage(user.HealthManager, cost, ElementType.Almighty);
                }
                else
                {
                    if (user.HealthManager.CurrentHP - cost < 0)
                    {
                        Debug.Log($"{user.DisplayName} does not have enough SP!");
                        return false;
                    }
                    user.HealthManager.ChangeSP(-cost);
                }
            }
            
            switch (skillType)
            {
                case SkillType.Offensive:
                    // Use the skill offensively
                    target.HealthManager.TakeDamage(user.HealthManager, GetDamage(user), elementType);
                    break;
                case SkillType.BuffDebuff:
                    // Use the skill to apply a buff or debuff
                    target.HealthManager.ApplyBuffDebuff(buffDebuff);
                    break;
                case SkillType.Heal:
                    // Use the skill to heal
                    if (target.HealthManager.isAlive)
                    {
                        target.HealthManager.Heal(target, user, healAmount);
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case SkillType.ReplenishSP:
                    // Use the skill to replenish SP
                    target.HealthManager.ChangeSP(spAmount);
                    break;
                case SkillType.Revive:
                    // Use the skill to revive
                    if (!target.HealthManager.isAlive)
                    {
                        target.HealthManager.Revive(target, user.HealthManager, reviveAmount);
                    }
                    else
                    {
                        return false;
                    }
                    break;
            }
            
            Debug.Log($"{user.DisplayName} used {skillName} on {target.DisplayName} (costed {cost}). SP left: {user.HealthManager.CurrentSP} / {user.HealthManager.MaxSP}");
            return true;
        }
    }
}