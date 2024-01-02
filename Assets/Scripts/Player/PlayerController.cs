using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    private StateMachine<IState> stateMachine;
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = new PlayerInput();

        stateMachine = new StateMachine<IState>();
        stateMachine.SetState(new ExplorationState(this, playerInput));
    }

    private void Update()
    {
        stateMachine.Tick();
    }

    private void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }

    private void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
}