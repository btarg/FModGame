using UnityEngine;

namespace BattleSystem.ScriptableObjects.Skills
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Skill")]
    public class BaseSkill : ScriptableObject
    {
        public string DisplayName;
        public int SPCost;
        public bool TargetsAll;
        public bool CanTargetAllies;
        public bool CanTargetEnemies;
        public ElementType DamageElement;
        public int MinDamage;
        public int MaxDamage;

        public int GetDamage()
        {
            return Mathf.RoundToInt(Random.Range(MinDamage, MaxDamage));
        }
    }
}