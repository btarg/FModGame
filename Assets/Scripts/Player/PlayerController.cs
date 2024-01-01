using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    PlayerInput playerInput;
    Vector2 currentMovementInput;
    Vector3 cameraRelativeMovement;
    CharacterController characterController;
    Animator animator;
    bool isMoving;
    bool isRunPressed;

    [Header("Movement Settings")]
    public float rotationFactorPerFrame = 1.0f;
    public float runMultiplier = 3.0f;
    public float movementThreshold = 0.1f;

    [Header("Gravity Settings")]
    public float gravity = -9.81f;
    public float groundedGravity = -0.5f;

    [Header("Falling Settings")]
    public float groundDistanceThreshold = 3f;

    public bool enableInput = true;

    // Hashes for animator parameters
    int isWalkingHash;
    int isRunningHash;
    int isFallingHash;

    private void Awake()
    {
        playerInput = new PlayerInput();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        playerInput.CharacterControls.Move.started += OnMovementInput;
        playerInput.CharacterControls.Move.canceled += OnMovementInput;
        playerInput.CharacterControls.Move.performed += OnMovementInput;

        playerInput.CharacterControls.Run.started += OnRun;
        playerInput.CharacterControls.Run.canceled += OnRun;
        playerInput.CharacterControls.Run.performed += OnRun;

        // Calculate hashes
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isFallingHash = Animator.StringToHash("isFalling");
    }

    private void Update()
    {
        cameraRelativeMovement = ConvertToCameraSpace(new Vector3(currentMovementInput.x, 0, currentMovementInput.y));
        if (characterController.isGrounded)
        {
            // Apply a small downward force when the character is grounded
            cameraRelativeMovement.y = groundedGravity;
        }
        else
        {
            cameraRelativeMovement.y += gravity;
        }

        HandleAnimation();
        HandleRotation();

        // Apply runMultiplier only when the character is grounded and not falling
        float speedMultiplier = characterController.isGrounded && isRunPressed ? runMultiplier : 1;
        characterController.Move(cameraRelativeMovement * speedMultiplier * Time.deltaTime);
    }

    private bool IsFalling()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -Vector3.up, out hit))
        {
            return hit.distance > groundDistanceThreshold;
        }
        return false;
    }

    Vector3 ConvertToCameraSpace(Vector3 input)
    {
        float currentYValue = input.y;
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;

        cameraForward = cameraForward.normalized;
        cameraRight = cameraRight.normalized;

        Vector3 cameraForwardZProduct = input.z * cameraForward;
        Vector3 cameraRightXProduct = input.x * cameraRight;
        Vector3 result = cameraForwardZProduct + cameraRightXProduct;
        result.y = currentYValue;
        return result;
    }

    private void HandleAnimation()
    {
        animator.SetBool(isWalkingHash, isMoving && !isRunPressed && !IsFalling());
        animator.SetBool(isRunningHash, isMoving && isRunPressed && !IsFalling());
        animator.SetBool(isFallingHash, IsFalling());
    }

    private void OnRun(InputAction.CallbackContext ctx)
    {
        isRunPressed = ctx.ReadValue<float>() > 0.1f && enableInput;
    }

    private void HandleRotation()
    {
        // Only rotate when there is movement input and the player is not falling
        if (isMoving && !IsFalling() && enableInput)
        {
            // Use cameraRelativeMovement instead of currentMovementInput
            Vector3 positionToLookAt;
            positionToLookAt.x = cameraRelativeMovement.x;
            positionToLookAt.y = 0;
            positionToLookAt.z = cameraRelativeMovement.z;

            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }
    }

    private void OnMovementInput(InputAction.CallbackContext ctx)
    {
        // Ignore movement input when the player is falling
        if (!IsFalling() && enableInput)
        {
            currentMovementInput = ctx.ReadValue<Vector2>();
            isMoving = currentMovementInput.magnitude > movementThreshold;
        }
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