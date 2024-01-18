using Cinemachine;
using DG.Tweening;
using StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.PlayerStates
{
    public class ExplorationState : IState
    {
        public float acceleration = 10f;
        private readonly Animator animator;
        private Vector3 cameraRelativeMovement;
        private readonly CharacterController characterController;
        private Vector2 currentMovementInput;
        private float currentSpeed;
        public float deceleration = 20f;

        public bool enableInput = true;
        public float fovChangeDuration = 0.5f;

        [Header("Gravity Settings")] public float gravity = -9.81f;

        [Header("Falling Settings")] public float groundDistanceThreshold = 3f;

        public float groundedGravity = -0.5f;
        private readonly int isFallingHash = Animator.StringToHash("isFalling");
        private bool isMoving;
        private bool isRunning;
        private readonly int isRunningHash = Animator.StringToHash("isRunning");
        private bool isRunPressed;

        // Hashes for animator parameters
        private readonly int isWalkingHash = Animator.StringToHash("isWalking");

        private Vector3 lastMovementDirection;
        public float maxRunSpeed = 6f;
        private float maxSpeed;
        public float maxWalkSpeed = 2f;
        public float movementThreshold = 0.1f;

        [Header("Camera Settings")] public float normalFOV = 40f;

        private readonly PlayerInput playerInput;

        [Header("Movement Settings")] public float rotationFactorPerFrame = 10f;

        public float runningFOV = 30f;
        private CinemachineStateDrivenCamera stateDrivenCamera;

        public ExplorationState(PlayerController playerController, PlayerInput playerInput)
        {
            characterController = playerController.gameObject.GetComponent<CharacterController>();
            animator = playerController.gameObject.GetComponent<Animator>();
            this.playerInput = playerInput;
        }

        public void OnEnter()
        {
            playerInput.CharacterControls.Move.started += OnMovementInput;
            playerInput.CharacterControls.Move.canceled += OnMovementInput;
            playerInput.CharacterControls.Move.performed += OnMovementInput;

            playerInput.CharacterControls.Run.started += OnRun;
            playerInput.CharacterControls.Run.canceled += OnRun;
            playerInput.CharacterControls.Run.performed += OnRun;

            stateDrivenCamera =
                Camera.main.gameObject.GetComponent<CinemachineBrain>().ActiveVirtualCamera as
                    CinemachineStateDrivenCamera;
        }

        public void OnExit()
        {
            playerInput.CharacterControls.Move.started -= OnMovementInput;
            playerInput.CharacterControls.Move.canceled -= OnMovementInput;
            playerInput.CharacterControls.Move.performed -= OnMovementInput;

            playerInput.CharacterControls.Run.started -= OnRun;
            playerInput.CharacterControls.Run.canceled -= OnRun;
            playerInput.CharacterControls.Run.performed -= OnRun;

            enableInput = false;

            // reset animator parameters
            animator.SetBool(isWalkingHash, false);
            animator.SetBool(isRunningHash, false);
            animator.SetBool(isFallingHash, false);
        }

        public void Tick()
        {
            Vector3 horizontalMovement =
                ConvertToCameraSpace(new Vector3(currentMovementInput.x, 0, currentMovementInput.y));
            lastMovementDirection = horizontalMovement.magnitude > 0 ? horizontalMovement : lastMovementDirection;

            float verticalMovement = characterController.isGrounded ? groundedGravity : gravity * Time.deltaTime;
            isRunning = isRunPressed && currentMovementInput.magnitude >= 0.9f;

            HandleAnimation();
            HandleRotation();

            maxSpeed = isRunning ? maxRunSpeed : maxWalkSpeed;

            if (!IsFalling())
                currentSpeed = isMoving
                    ? Mathf.Min(currentSpeed + acceleration * Time.deltaTime, maxSpeed)
                    : Mathf.Max(currentSpeed - deceleration * Time.deltaTime, 0);

            cameraRelativeMovement = lastMovementDirection * (currentSpeed * Time.deltaTime);
            cameraRelativeMovement.y = verticalMovement;
            characterController.Move(cameraRelativeMovement);


            if (stateDrivenCamera != null)
            {
                ICinemachineCamera virtualCamera = stateDrivenCamera.LiveChild;
                // Change FOV based on running or walking
                if (virtualCamera is CinemachineFreeLook)
                {
                    CinemachineFreeLook freeLook = virtualCamera as CinemachineFreeLook;
                    float targetFOV = isRunPressed && isMoving && !IsFalling() ? runningFOV : normalFOV;
                    DOTween.To(() => freeLook.m_Lens.FieldOfView, x => freeLook.m_Lens.FieldOfView = x, targetFOV,
                        fovChangeDuration);
                }
            }
        }

        private bool IsFalling()
        {
            bool isGroundBelow = Physics.Raycast(characterController.gameObject.transform.position, -Vector3.up,
                out RaycastHit hit);
            return !characterController.isGrounded && (!isGroundBelow || hit.distance > groundDistanceThreshold);
        }

        private Vector3 ConvertToCameraSpace(Vector3 input)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;

            cameraForward.y = 0;
            cameraRight.y = 0;

            return input.z * cameraForward.normalized + input.x * cameraRight.normalized;
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
                characterController.gameObject.transform.rotation = Quaternion.Slerp(
                    characterController.gameObject.transform.rotation, targetRotation,
                    rotationFactorPerFrame * Time.deltaTime);
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
    }
}