using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using BattleSystem.ScriptableObjects.Characters;
using UnityEngine.Events;
using BattleSystem.ScriptableObjects.Skills;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    private StateMachine<IState> stateMachine;
    private PlayerInput playerInput;

    public Character playerCharacter;
    public List<Character> party;
    public List<Character> enemies;

    public UnityEvent<BaseSkill, UUIDCharacterInstance> OnSkillAndTargetSelected;


    public void SelectSkillAndTarget(BaseSkill skill, UUIDCharacterInstance target)
    {
        OnSkillAndTargetSelected?.Invoke(skill, target);
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
        playerInput.Enable();
    }

    private void OnDisable()
    {
        playerInput.Disable();
    }
}