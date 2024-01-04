using System.Collections.Generic;
using BattleSystem.ScriptableObjects.Characters;
using BattleSystem.ScriptableObjects.Skills;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        private StateMachine<IState> stateMachine;
        private PlayerInput playerInput;

        public Character playerCharacter;
        public List<Character> party;
        public List<Character> enemies;

        [FormerlySerializedAs("OnSkillAndTargetSelected")] public UnityEvent<BaseSkill, UUIDCharacterInstance> PlayerUsedSkillEvent = new();
        public void SelectSkillAndTarget(BaseSkill skill, UUIDCharacterInstance target)
        {
            PlayerUsedSkillEvent?.Invoke(skill, target);
        }

        private void Awake()
        {
            playerInput = new PlayerInput();
            stateMachine = new StateMachine<IState>();
            if (!party.Contains(playerCharacter))
            {
                party.Add(playerCharacter);
            }

            // default to ExplorationState
            EnterExplorationState();

            // Register the callback for the BattleState input action
            playerInput.Debug.BattleState.performed += EnterBattleState;
        }


        public void EnterExplorationState()
        {
            if (stateMachine.GetCurrentState() is ExplorationState)
            {
                Debug.Log("Already in exploration state.");
                return;
            }
            stateMachine.SetState(new ExplorationState(this, playerInput));
        }

        private void Update()
        {
            if (stateMachine != null)
                stateMachine.Tick();
        }

        private void EnterBattleState(InputAction.CallbackContext ctx)
        {
            if (stateMachine.GetCurrentState() is BattleState)
            {
                Debug.Log("Already in battle state.");
                return;
            }
            // TODO: set these values based on the encounter
            stateMachine.SetState(new BattleState(this, party, enemies, true, 1));
        }

        private void OnEnable()
        {
            if (playerInput != null)
                playerInput.Enable();
        }

        private void OnDisable()
        {
            if (playerInput != null)
                playerInput.Disable();
        }
    }
}