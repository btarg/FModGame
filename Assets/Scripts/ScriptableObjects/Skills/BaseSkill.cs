using ScriptableObjects.Characters;
using ScriptableObjects.Stats.Modifiers;
using ScriptableObjects.Util.DataTypes;
using UnityEngine;

namespace ScriptableObjects.Skills
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Skill")]
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
            if (IsAffectedByATK)
            {
                return Mathf.CeilToInt(baseDamage * character.Stats.ATK);
            }
            else
            {
                return baseDamage;
            }
        }

        public void Use(Character user, Character target)
        {
            Debug.Log($"{user.DisplayName} used {skillName} on {target.DisplayName}.");
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
                    target.HealthManager.Heal(target, user, healAmount);
                    break;
                case SkillType.ReplenishSP:
                    // Use the skill to replenish SP
                    target.HealthManager.ChangeSP(spAmount);
                    break;
                case SkillType.Revive:
                    // Use the skill to revive
                    if (!target.HealthManager.isAlive)
                        target.HealthManager.Revive(target, user.HealthManager, reviveAmount);
                    break;
            }

            if (costsHP)
                user.HealthManager.TakeDamage(user.HealthManager, cost, ElementType.Almighty);
            else
                user.HealthManager.ChangeSP(-cost);
        }
    }
}