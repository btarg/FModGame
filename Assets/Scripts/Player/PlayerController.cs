using System;
using System.Collections.Generic;
using BattleSystem.ScriptableObjects.Characters;
using BattleSystem.ScriptableObjects.Skills;
using BeatDetection.DataStructures;
using BeatDetection.QTE;
using Player.Inventory;
using Player.PlayerStates;
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
        public StateMachine<IState> stateMachine { get; private set; }
        private PlayerInput playerInput;

        public Character playerCharacter;
        public PlayerInventory playerInventory { get; private set; }
        public List<InventoryItem> inventoryItems = new();
        public List<Character> party;
        public List<Character> enemies;

        public SimpleQTE simpleQTE;
        
        public UnityEvent<BaseSkill> SelectSkillEvent{ get; private set; } = new();
        public UnityEvent<BattleActionType> SelectActionEvent{ get; private set; } = new();
        public UnityEvent<BeatResult> PlayerUsedSkillEvent { get; private set; } = new();
        
        public void UseSelectedSkill(BeatResult result)
        {
            PlayerUsedSkillEvent?.Invoke(result);
        }

        public void SelectSkill(BaseSkill skill)
        {
            SelectSkillEvent?.Invoke(skill);
        }
        
        public void SelectAction(BattleActionType action)
        {
            SelectActionEvent?.Invoke(action);
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
            
            // TODO: load this from a file
            playerInventory = new PlayerInventory();
            // add default items
            playerInventory.LoadInventoryItems(inventoryItems);
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