using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using DG.Tweening;

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
    bool isRunning;

    [Header("Movement Settings")]
    public float rotationFactorPerFrame = 1f;
    public float movementThreshold = 0.1f;
    public float acceleration = 10f;
    public float deceleration = 20f;
    private float maxSpeed = 0.0f;
    public float maxWalkSpeed = 6.0f;
    public float maxRunSpeed = 10.0f;

    [Header("Gravity Settings")]
    public float gravity = -9.81f;
    public float groundedGravity = -0.5f;

    [Header("Falling Settings")]
    public float groundDistanceThreshold = 3f;

    [Header("Camera Settings")]
    public float normalFOV = 40f;
    public float runningFOV = 45f;
    public float fovChangeDuration = 0.5f;

    public bool enableInput = true;
    private float currentSpeed;

    // Hashes for animator parameters
    int isWalkingHash;
    int isRunningHash;
    int isFallingHash;

    Vector3 lastMovementDirection;

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
        Vector3 horizontalMovement = ConvertToCameraSpace(new Vector3(currentMovementInput.x, 0, currentMovementInput.y));
        lastMovementDirection = horizontalMovement.magnitude > 0 ? horizontalMovement : lastMovementDirection;

        float verticalMovement = characterController.isGrounded ? groundedGravity : gravity * Time.deltaTime;
        isRunning = isRunPressed && currentMovementInput.magnitude >= 0.9f;

        HandleAnimation();
        HandleRotation();

        maxSpeed = isRunning ? maxRunSpeed : maxWalkSpeed;

        if (!IsFalling())
        {
            currentSpeed = isMoving
                ? Mathf.Min(currentSpeed + acceleration * Time.deltaTime, maxSpeed)
                : Mathf.Max(currentSpeed - deceleration * Time.deltaTime, 0);
        }

        cameraRelativeMovement = lastMovementDirection * currentSpeed * Time.deltaTime;
        cameraRelativeMovement.y = verticalMovement;
        characterController.Move(cameraRelativeMovement);

        // Get the reference to the CinemachineVirtualCamera at runtime
        CinemachineFreeLook virtualCamera = Camera.main.gameObject.GetComponent<CinemachineBrain>().ActiveVirtualCamera as CinemachineFreeLook;

        // Change FOV based on running or walking
        if (virtualCamera != null)
        {
            float targetFOV = (isRunPressed && isMoving && !IsFalling()) ? runningFOV : normalFOV;
            DOTween.To(() => virtualCamera.m_Lens.FieldOfView, x => virtualCamera.m_Lens.FieldOfView = x, targetFOV, fovChangeDuration);
        }
        else
        {
            Debug.LogWarning("CinemachineVirtualCamera component is not found in the main camera.");
        }
    }

    private bool IsFalling()
    {
        bool isGroundBelow = Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hit);
        return !characterController.isGrounded && (!isGroundBelow || hit.distance > groundDistanceThreshold);
    }

    Vector3 ConvertToCameraSpace(Vector3 input)
    {
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;

        return (input.z * cameraForward.normalized) + (input.x * cameraRight.normalized);
    }

    private void HandleAnimation()
    {
        animator.SetBool(isWalkingHash, isMoving && !isRunning);
        animator.SetBool(isRunningHash, isMoving && isRunning);
        animator.SetBool(isFallingHash, IsFalling());
    }

    private void OnRun(InputAction.CallbackContext ctx)
    {
        isRunPressed = ctx.ReadValue<float>() > 0.1f && enableInput;
    }

    private void HandleRotation()
    {
        if (isMoving && !IsFalling() && enableInput)
        {
            Vector3 positionToLookAt = new(cameraRelativeMovement.x, 0, cameraRelativeMovement.z);
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }
    }

    private void OnMovementInput(InputAction.CallbackContext ctx)
    {
        if (!IsFalling() && enableInput)
        {
            currentMovementInput = ctx.ReadValue<Vector2>();
            isMoving = currentMovementInput.magnitude > movementThreshold;
        }
        else
        {
            isMoving = false;
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