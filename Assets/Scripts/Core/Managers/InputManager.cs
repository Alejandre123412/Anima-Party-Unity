using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace AnimaParty.Core
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        
        [Header("Input Settings")]
        public PlayerInputManager playerInputManager;
        public InputActionAsset inputActions;
        
        private List<PlayerInput> playerInputs = new List<PlayerInput>();
        private Dictionary<int, PlayerInput> playerInputMap = new Dictionary<int, PlayerInput>();
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void Initialize()
        {
            // Setup PlayerInputManager
            if (playerInputManager == null)
                playerInputManager = gameObject.AddComponent<PlayerInputManager>();
            
            playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
            playerInputManager.playerPrefab = null; // We'll handle instantiation manually
            
            Debug.Log("InputManager initialized with new Input System");
        }
        
        public void OnPlayerJoined(PlayerInput playerInput)
        {
            int playerIndex = playerInputs.Count;
            playerInput.playerIndex = playerIndex;
            
            playerInputs.Add(playerInput);
            playerInputMap[playerIndex] = playerInput;
            
            // Add player to game
            PlayerManager.Instance.AddPlayer(playerIndex);
            
            Debug.Log($"Player {playerIndex} joined with device: {playerInput.devices[0].name}");
        }
        
        public void OnPlayerLeft(PlayerInput playerInput)
        {
            int playerIndex = playerInput.playerIndex;
            playerInputs.Remove(playerInput);
            playerInputMap.Remove(playerIndex);
            
            Debug.Log($"Player {playerIndex} left the game");
        }
        
        public bool GetButtonDown(int playerId, string actionName)
        {
            if (playerInputMap.TryGetValue(playerId, out PlayerInput playerInput))
            {
                var action = playerInput.actions[actionName];
                if (action != null && action.triggered)
                {
                    return true;
                }
            }
            return false;
        }
        
        public float GetAxis(int playerId, string actionName)
        {
            if (playerInputMap.TryGetValue(playerId, out PlayerInput playerInput))
            {
                var action = playerInput.actions[actionName];
                if (action != null)
                {
                    return action.ReadValue<float>();
                }
            }
            return 0f;
        }
        
        public Vector2 GetVector2(int playerId, string actionName)
        {
            if (playerInputMap.TryGetValue(playerId, out PlayerInput playerInput))
            {
                var action = playerInput.actions[actionName];
                if (action != null)
                {
                    return action.ReadValue<Vector2>();
                }
            }
            return Vector2.zero;
        }
        
        public void EnablePlayerInput(bool enable)
        {
            foreach (var playerInput in playerInputs)
            {
                if (enable)
                    playerInput.ActivateInput();
                else
                    playerInput.DeactivateInput();
            }
        }
    }
}