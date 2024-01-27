using System.Collections.Generic;
using StateMachine;
using UnityEngine;
using ScriptableObjects.Skills;
using UnityEngine.Events;

namespace ScriptableObjects.Characters.AiStates
{
    public class AIThinkingState : IState
    {
        List<Character> turnOrder;
        Character currentCharacter;
        private Character target1;

        public AIThinkingState(Character character, List<Character> _turnOrder)
        {
            currentCharacter = character;
            turnOrder = _turnOrder;
        }

        public void OnEnter()
        {
            // log how much HP and SP
            Debug.Log($"{currentCharacter.DisplayName} has {currentCharacter.HealthManager.CurrentHP} HP and {currentCharacter.HealthManager.CurrentSP} SP");
            currentCharacter.HealthManager.OnTurnStart();
            target1 = turnOrder.Find(character => character.IsPlayerCharacter);
        }

        public void Tick()
        {
            Debug.Log("tick from thinking state");
            // If a target was found, use the first available skill on them
            if (target1 != null && currentCharacter.AvailableSkills.Count > 0)
            {
                BaseSkill skillToUse = currentCharacter.AvailableSkills[0];
                // Logs the correct amount (30)
                Debug.Log($"{currentCharacter.DisplayName} has {currentCharacter.HealthManager.CurrentSP} SP");
                
                skillToUse.Use(currentCharacter, target1);
                
                currentCharacter.NextTurnEvent?.Invoke();

            }
        }

        public void OnExit()
        {
        }
    }
}