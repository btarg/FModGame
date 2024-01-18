using ScriptableObjects.Util.DataTypes;
using UnityEngine;

namespace ScriptableObjects.Stats
{
    [System.Serializable]
    public struct ElementStrength
    {
        public ElementType ElementType;
        public StrengthType StrengthType;
        [Range(0, 100)]
        public int ResistPercentage; // Only used when StrengthType is Resist
    }
}

