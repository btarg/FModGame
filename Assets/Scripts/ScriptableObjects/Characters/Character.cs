using System;
using System.Collections.Generic;
using ScriptableObjects.Characters.Health;
using ScriptableObjects.Skills;
using ScriptableObjects.Stats.CharacterStats;
using ScriptableObjects.Util.DataTypes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScriptableObjects.Characters
{
    [CreateAssetMenu(fileName = "NewCharacter", menuName = "Character")]
    public class Character : ScriptableObject
    {
        public new string name { get; private set; }
        public string DisplayName;
        public Color Color;
        public CharacterStats Stats;

        public InventoryItem weapon;

        public List<BaseSkill> AvailableSkills;
        public bool IsPlayerCharacter;
        public GameObject prefab;

        public Character()
        {
            UUID = Guid.NewGuid().ToString();
        }

        public string UUID { get; }
        public HealthManager HealthManager { get; private set; }

        private void OnEnable()
        {
            HealthManager = CreateInstance<HealthManager>();
            HealthManager.InitStats(Stats, UUID);
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(name)) name = ((Object)this).name;
        }
    }
}