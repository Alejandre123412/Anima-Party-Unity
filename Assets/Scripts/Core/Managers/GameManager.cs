using UnityEngine;
using System.Collections.Generic;

namespace AnimaParty.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Game Settings")]
        public int maxPlayers = 4;
        public int minPlayers = 2;
        
        [Header("References")]
        public PlayerManager playerManager;
        public InputManager inputManager;
        public SceneLoader sceneLoader;
        
        private GameState currentState = GameState.TitleScreen;
        
        public enum GameState
        {
            TitleScreen,
            CharacterSelect,
            MainMenu,
            MinigamePartyLobby,
            MinigamePartyGame,
            MinigamePlaying,
            Results
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManagers();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeManagers()
        {
            playerManager = GetComponent<PlayerManager>();
            inputManager = GetComponent<InputManager>();
            sceneLoader = GetComponent<SceneLoader>();
            
            // Initialize input system
            inputManager.Initialize();
            
            Debug.Log("GameManager initialized");
        }
        
        public void ChangeState(GameState newState)
        {
            currentState = newState;
            OnStateChanged(newState);
        }
        
        private void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.TitleScreen:
                    break;
                case GameState.CharacterSelect:
                    break;
                case GameState.MainMenu:
                    break;
                case GameState.MinigamePartyLobby:
                    break;
                case GameState.MinigamePartyGame:
                    break;
                case GameState.MinigamePlaying:
                    break;
                case GameState.Results:
                    break;
            }
            
            Debug.Log($"Game state changed to: {state}");
        }
        
        public GameState GetCurrentState() => currentState;
    }
}