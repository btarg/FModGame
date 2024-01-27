using StateMachine;
using UnityEngine;

namespace ScriptableObjects.Characters.AiStates
{
    public class CharacterIdleState : IState
    {
        public void OnEnter()
        {
            Debug.Log("Waiting for turn...");
        }

        public void Tick()
        {
        }

        public void OnExit()
        {
            
        }
    }
}