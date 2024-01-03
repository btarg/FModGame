using UnityEngine;
using BattleSystem.ScriptableObjects.Characters;
using BattleSystem.ScriptableObjects.Stats.Modifiers;
using BattleSystem.DataTypes;

namespace BattleSystem.ScriptableObjects.Skills
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Skill")]
    public class BaseSkill : ScriptableObject
    {
        public string skillName;
        public string description;
        public string onUsedMessage;

        public int MinDamage;
        public int MaxDamage;

        public SkillType skillType;
        public ElementType elementType; // for offensive skills
        public BuffDebuff buffDebuff; // for buff/debuff skills
        public int cost;
        public int healAmount; // for heal skills
        public int spAmount; // for replenish SP skills
        public int reviveAmount; // for revive skills

        public bool TargetsAll;
        public bool CanTargetAllies;
        public bool CanTargetEnemies;

        public int GetDamage(Character character)
        {
            int baseDamage = Random.Range(MinDamage, MaxDamage + 1);
            // round damage with mod to the nearest integer
            return Mathf.CeilToInt(baseDamage * character.Stats.ATK);
        }

        public void Use(UUIDCharacterInstance userInstance, UUIDCharacterInstance targetInstance)
        {
            var target = targetInstance.Character;
            var user = userInstance.Character;
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
                    target.HealthManager.Heal(healAmount);
                    break;
                case SkillType.ReplenishSP:
                    // Use the skill to replenish SP
                    target.HealthManager.ReplenishSP(spAmount);
                    break;
                case SkillType.Revive:
                    // Use the skill to revive
                    if (!target.HealthManager.isAlive)
                    {
                        target.HealthManager.Revive(targetInstance, reviveAmount);
                    }
                    break;
            }
        }
    }
}