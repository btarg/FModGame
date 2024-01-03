using BattleSystem.DataTypes;
using BattleSystem.ScriptableObjects.Stats.CharacterStats;
using UnityEngine;

namespace BattleSystem.ScriptableObjects.Characters
{
    [System.Serializable]
    public class UUIDCharacterInstance
    {
        public Character Character { get; private set; }
        public string UUID { get; private set; }

        public UUIDCharacterInstance(Character baseCharacter)
        {
            UUID = System.Guid.NewGuid().ToString();
            Character = ScriptableObject.CreateInstance<Character>();
            // clone character values from base character
            Character.name = baseCharacter.name;
            Character.DisplayName = baseCharacter.DisplayName;
            Character.Color = baseCharacter.Color;
            Character.attackSkill = baseCharacter.attackSkill;
            Character.AvailableSkills = baseCharacter.AvailableSkills;
            Character.prefab = baseCharacter.prefab;
            Character.Stats = baseCharacter.Stats;
            Character.IsPlayerCharacter = baseCharacter.IsPlayerCharacter;
            Character.HealthManager = ScriptableObject.CreateInstance<HealthManager>();
            Character.HealthManager.critDamageMultiplier = baseCharacter.HealthManager.critDamageMultiplier;
            // we only need to define stats on the base character
            Character.HealthManager.InitStats(baseCharacter.Stats, UUID);
        }
    }
}

