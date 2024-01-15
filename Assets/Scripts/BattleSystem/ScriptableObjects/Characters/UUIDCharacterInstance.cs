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
            Character = Object.Instantiate(baseCharacter);
            Character.HealthManager = Object.Instantiate(baseCharacter.HealthManager);
            // we only need to define stats on the base character
            Character.HealthManager.InitStats(baseCharacter.Stats, UUID);
        }
    }
}

