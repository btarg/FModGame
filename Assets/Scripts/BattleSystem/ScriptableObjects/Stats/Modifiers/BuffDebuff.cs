using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem.ScriptableObjects.Stats.Modifiers
{
    [System.Serializable]
    public class StatModifier
    {
        public StatType StatType;
        public float Multiplier;
    }

    [CreateAssetMenu(fileName = "NewBuffDebuff", menuName = "Buff or Debuff")]
    public class BuffDebuff : ScriptableObject
    {
        public string DisplayName;
        [Tooltip("Stat string displayed in the character's stat screen")]
        public string StatString;
        public List<StatModifier> StatModifiers;
        public int Duration;
        [Tooltip("Determines if this buff/debuff can stack")]
        public bool CanStack = true;
    }
}
