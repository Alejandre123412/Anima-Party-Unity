using UnityEngine;
using UnityEngine.InputSystem;

namespace AnimaParty.Core
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private int playerId = 0;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpForce = 8f;
        [SerializeField] private float gravity = 20f;
        [SerializeField] private float rotationSpeed = 10f;
        
        [Header("Components")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject model;
        
        [Header("Input")]
        private Vector2 moveInput;
        private bool isSprinting;
        private bool jumpPressed;
        
        // Properties
        public int PlayerId => playerId;
        private Vector3 velocity;
        private bool isGrounded;
        
        private void Start()
        {
            // Get components if not assigned
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
                
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
                
            if (model == null && transform.childCount > 0)
                model = transform.GetChild(0).gameObject;
                
            Debug.Log($"PlayerController initialized for Player {playerId}");
        }
        
        public void Initialize(PlayerInfo playerInfo)
        {
            playerId = playerInfo.playerId;
            gameObject.name = $"Player_{playerId}_{playerInfo.playerName}";
            
            // Set player color/material based on playerId
            SetPlayerColor();
        }
        
        private void SetPlayerColor()
        {
            Color playerColor = playerId switch
            {
                0 => Color.red,
                1 => Color.blue,
                2 => Color.green,
                3 => Color.yellow,
                _ => Color.white
            };
            
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = playerColor;
            }
        }
        
        private void Update()
        {
            HandleMovement();
            UpdateAnimations();
        }
        
        public void SetMoveInput(Vector2 input)
        {
            moveInput = input;
        }
        
        public void SetSprint(bool sprint)
        {
            isSprinting = sprint;
        }
        
        public void SetJump(bool jump)
        {
            jumpPressed = jump;
        }
        
        private void HandleMovement()
        {
            // Check if grounded
            isGrounded = characterController.isGrounded;
            
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small downward force
            }
            
            // Calculate movement
            Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
            
            // Normalize if magnitude > 1 (diagonal movement)
            if (moveDirection.magnitude > 1f)
                moveDirection.Normalize();
            
            // Apply speed
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
            Vector3 moveVelocity = moveDirection * currentSpeed;
            
            // Convert to world space (if needed)
            moveVelocity = transform.TransformDirection(moveVelocity);
            
            // Apply movement
            characterController.Move(moveVelocity * Time.deltaTime);
            
            // Handle rotation
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    rotationSpeed * Time.deltaTime);
            }
            
            // Handle jumping
            if (jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
                jumpPressed = false;
                
                if (animator != null)
                    animator.SetTrigger("Jump");
            }
            
            // Apply gravity
            velocity.y -= gravity * Time.deltaTime;
            
            // Apply vertical velocity
            characterController.Move(velocity * Time.deltaTime);
        }
        
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            // Calculate movement speed for animation
            float speed = moveInput.magnitude * (isSprinting ? 2f : 1f);
            animator.SetFloat("Speed", speed);
            
            // Set grounded state
            animator.SetBool("IsGrounded", isGrounded);
            
            // Set vertical velocity for jump/fall animations
            animator.SetFloat("VerticalVelocity", velocity.y);
        }
        
        // Input System callbacks (if using new Input System)
        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }
        
        public void OnSprint(InputAction.CallbackContext context)
        {
            isSprinting = context.ReadValueAsButton();
        }
        
        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                jumpPressed = true;
            }
        }
        
        public void OnAction(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                // Perform action (minigame specific)
                Debug.Log($"Player {playerId} performed action");
                
                if (animator != null)
                    animator.SetTrigger("Action");
            }
        }
        
        public void Teleport(Vector3 position)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }
        
        public void ResetVelocity()
        {
            velocity = Vector3.zero;
        }
        
        public void EnableMovement(bool enable)
        {
            characterController.enabled = enable;
            enabled = enable;
        }
    }
}