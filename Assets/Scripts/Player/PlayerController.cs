using System.Collections.Generic;
using BeatDetection.DataStructures;
using BeatDetection.QTE;
using Cinemachine;
using Player.Inventory;
using Player.PlayerStates;
using Player.SaveLoad;
using ScriptableObjects.Characters;
using ScriptableObjects.Skills;
using ScriptableObjects.Util.DataTypes;
using StateMachine;
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
        public Character playerCharacter;

        [FormerlySerializedAs("inventoryItems")]
        public List<InventoryItem> defaultInventoryItems = new();

        public List<Character> party;
        public List<Character> enemies;

        public SimpleQTE simpleQTE;
        private string jsonFilePath;
        private PlayerInput playerInput;
        public StateMachine<IState> stateMachine { get; private set; }
        public CinemachineStateDrivenCamera stateDrivenCamera { get; private set; }
        public PlayerInventory playerInventory { get; private set; }

        public UnityEvent<BaseSkill> SelectSkillEvent { get; } = new();
        public UnityEvent<BattleActionType> SelectActionEvent { get; } = new();
        public UnityEvent<BeatResult> PlayerUsedSkillEvent { get; } = new();

        private void Awake()
        {
            playerInput = new PlayerInput();
            stateMachine = new StateMachine<IState>();

            if (!party.Contains(playerCharacter)) party.Add(playerCharacter);

            // Register the callback for the BattleState input action
            playerInput.Debug.BattleState.performed += EnterBattleState;

            // load inventory from a file and add default items
            playerInventory = new PlayerInventory(SaveManager.Load().inventoryItems);
            if (playerInventory.inventoryItems.Count == 0)
                playerInventory.LoadInventoryItems(defaultInventoryItems);
            else
                foreach (KeyValuePair<InventoryItem, int> item in playerInventory.inventoryItems)
                    Debug.Log($"Loaded {item.Key.displayName} x {item.Value} from save file.");
        }

        private void Start()
        {
            // Get Cinemachine camera on start to avoid it being null
            stateDrivenCamera =
                Camera.main.gameObject.GetComponent<CinemachineBrain>().ActiveVirtualCamera as
                    CinemachineStateDrivenCamera;
            Debug.Log("Main Cinemachine camera:" + stateDrivenCamera);

            // default to ExplorationState
            EnterExplorationState();
        }

        private void Update()
        {
            stateMachine.Tick();
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

            SaveManager.SaveInventory(playerInventory);
        }

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

        public void EnterExplorationState()
        {
            if (stateMachine.GetCurrentState() is ExplorationState)
            {
                Debug.Log("Already in exploration state.");
                return;
            }

            stateMachine.SetState(new ExplorationState(this, playerInput));
        }

        private void EnterBattleState(InputAction.CallbackContext ctx)
        {
            if (stateMachine.GetCurrentState() is BattleState)
            {
                Debug.Log("Already in battle state.");
                return;
            }

            // TODO: set these values based on the encounter
            stateMachine.SetState(new BattleState(this, party, enemies, true));
        }
    }
}