using System.Collections.Generic;
using System.Linq;
using StateMachine;
using UnityEngine;
using ScriptableObjects.Skills;

namespace ScriptableObjects.Characters.AiStates
{
    public class AIThinkingState : IState
    {
        List<Character> turnOrder;
        Character currentCharacter;
        private Character target1;
        private float weaknessAttackThreshold = 0.3f; // 30% chance to use a skill that the player is weak to

        public AIThinkingState(Character character, List<Character> _turnOrder)
        {
            currentCharacter = character;
            turnOrder = _turnOrder;
        }

        public void OnEnter()
        {
            Debug.Log($"{currentCharacter.DisplayName} has {currentCharacter.HealthManager.CurrentHP} HP and {currentCharacter.HealthManager.CurrentSP} SP");
            currentCharacter.HealthManager.OnTurnStart();

            var playerCharacters = turnOrder.Where(c => c.IsPlayerCharacter).ToList();

            if (!playerCharacters.Any())
            {
                target1 = null;
                return;
            }

            target1 = playerCharacters[Random.Range(0, playerCharacters.Count)];
        }

        public void Tick()
        {
            if (target1 == null || currentCharacter.AvailableSkills.Count <= 0 || !currentCharacter.HealthManager.isAlive)
            {
                currentCharacter.NextTurnEvent?.Invoke();
                return;
            }

            BaseSkill skillToUse = currentCharacter.AvailableSkills[Random.Range(0, currentCharacter.AvailableSkills.Count)];

            // Check if the skill's elementType is a weakness of the target player character
            if (target1.HealthManager.GetCurrentStats().Weaknesses.Contains(skillToUse.elementType))
            {
                // If it is a weakness, there's a 30% chance to use the skill
                if (Random.value > weaknessAttackThreshold)
                {
                    skillToUse = currentCharacter.weapon.Skill;
                }
            }
            bool usedSkill = skillToUse.Use(currentCharacter, target1);
            if (!usedSkill)
            {
                // was unable to use skill, use weapon instead
                currentCharacter.weapon.Skill.Use(currentCharacter, target1);
            }

            currentCharacter.NextTurnEvent?.Invoke();
        }

        public void OnExit()
        {
        }
    }
}