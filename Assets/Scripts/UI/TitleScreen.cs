using AnimaParty.Core;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening; // Añadido

namespace AnimaParty.UI
{
    public class TitleScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI pressStartText;
        [SerializeField] private GameObject playerJoinPanel;
        [SerializeField] private Transform playerJoinContainer;
        [SerializeField] private GameObject playerJoinPrefab;
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        
        [Header("Settings")]
        [SerializeField] private float titlePulseSpeed = 1f;
        [SerializeField] private float pressStartBlinkSpeed = 2f;
        [SerializeField] private int minPlayers = 2;
        
        [Header("Audio")]
        [SerializeField] private AudioClip selectSound;
        [SerializeField] private AudioClip joinSound;
        [SerializeField] private AudioClip leaveSound;
        
        private PlayerInputManager inputManager;
        private bool canStart = false;
        private int joinedPlayers = 0;
        private Tween titlePulseTween;
        private Tween pressStartBlinkTween;
        private Tween startButtonPulseTween;
        
        private void Start()
        {
            Initialize();
            SetupUI();
            StartTitleAnimation();
        }
        
        private void Initialize()
        {
            // Get or create InputManager
            inputManager = FindObjectOfType<PlayerInputManager>();
            if (inputManager == null)
            {
                GameObject inputObj = new GameObject("PlayerInputManager");
                inputManager = inputObj.AddComponent<PlayerInputManager>();
                inputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
            }
            
            // Subscribe to events
            if (inputManager != null)
            {
                inputManager.onPlayerJoined += OnPlayerJoined;
                inputManager.onPlayerLeft += OnPlayerLeft;
            }
            
            // Setup buttons
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
                startButton.interactable = false;
            }
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
            
            // Clear existing players
            PlayerManager.Instance?.ClearPlayers();
        }
        
        private void SetupUI()
        {
            if (playerJoinPanel != null)
                playerJoinPanel.SetActive(true);
            
            // Clear existing join indicators
            foreach (Transform child in playerJoinContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create join indicators for max players
            for (int i = 0; i < GameManager.Instance.maxPlayers; i++)
            {
                GameObject joinIndicator = Instantiate(playerJoinPrefab, playerJoinContainer);
                var indicatorUI = joinIndicator.GetComponent<PlayerJoinIndicator>();
                if (indicatorUI != null)
                {
                    indicatorUI.Initialize(i, false);
                }
            }
        }
        
        private void StartTitleAnimation()
        {
            // Title pulse animation - DOTween
            if (titleText != null)
            {
                titlePulseTween = titleText.transform.DOScale(Vector3.one * 1.1f, titlePulseSpeed)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            
            // Press start blink animation - DOTween
            if (pressStartText != null)
            {
                pressStartBlinkTween = pressStartText.DOFade(0f, pressStartBlinkSpeed)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }
        
        private void OnPlayerJoined(PlayerInput playerInput)
        {
            int playerIndex = joinedPlayers;
            joinedPlayers++;
            
            // Update join indicator
            UpdateJoinIndicator(playerIndex, true);
            
            // Add player to PlayerManager
            PlayerManager.Instance?.AddPlayer(playerIndex, $"Jugador {playerIndex + 1}");
            
            // Play join sound
            if (AudioManager.Instance != null && joinSound != null)
                AudioManager.Instance.Play("PlayerJoin");
            
            // Check if we can start
            CheckStartCondition();
            
            Debug.Log($"Player {playerIndex} joined");
        }
        
        private void OnPlayerLeft(PlayerInput playerInput)
        {
            int playerIndex = playerInput.playerIndex;
            
            // Update join indicator
            UpdateJoinIndicator(playerIndex, false);
            
            // Remove player from PlayerManager
            // Note: PlayerManager doesn't have remove method in current implementation
            
            // Play leave sound
            if (AudioManager.Instance != null && leaveSound != null)
                AudioManager.Instance.Play("PlayerLeave");
            
            joinedPlayers--;
            CheckStartCondition();
            
            Debug.Log($"Player {playerIndex} left");
        }
        
        private void UpdateJoinIndicator(int playerIndex, bool joined)
        {
            if (playerJoinContainer != null && playerIndex < playerJoinContainer.childCount)
            {
                Transform indicator = playerJoinContainer.GetChild(playerIndex);
                var indicatorUI = indicator.GetComponent<PlayerJoinIndicator>();
                if (indicatorUI != null)
                {
                    indicatorUI.SetJoined(joined);
                }
            }
        }
        
        private void CheckStartCondition()
        {
            canStart = joinedPlayers >= minPlayers;
            
            if (startButton != null)
            {
                startButton.interactable = canStart;
                
                // Cancel existing pulse
                startButtonPulseTween?.Kill();
                
                // Pulse effect when ready - DOTween
                if (canStart)
                {
                    startButtonPulseTween = startButton.transform.DOScale(Vector3.one * 1.1f, 0.5f)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                }
                else
                {
                    startButton.transform.localScale = Vector3.one;
                }
            }
        }
        
        private void OnStartClicked()
        {
            if (!canStart) return;
            
            // Play select sound
            if (AudioManager.Instance != null && selectSound != null)
                AudioManager.Instance.Play("Select");
            
            // Load next scene
            SceneLoader.Instance?.LoadScene("CharacterSelect");
            
            Debug.Log("Starting game...");
        }
        
        private void OnSettingsClicked()
        {
            if (AudioManager.Instance != null && selectSound != null)
                AudioManager.Instance.Play("Select");
            
            // Show settings menu
            // You would implement a settings panel here
            Debug.Log("Opening settings...");
        }
        
        private void OnQuitClicked()
        {
            if (AudioManager.Instance != null && selectSound != null)
                AudioManager.Instance.Play("Select");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        private void Update()
        {
            // Keyboard shortcuts
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnQuitClicked();
            }
            
            if (Keyboard.current.enterKey.wasPressedThisFrame && canStart)
            {
                OnStartClicked();
            }
        }
        
        private void OnDestroy()
        {
            // Clean up tweens
            titlePulseTween?.Kill();
            pressStartBlinkTween?.Kill();
            startButtonPulseTween?.Kill();
            
            if (inputManager != null)
            {
                inputManager.onPlayerJoined -= OnPlayerJoined;
                inputManager.onPlayerLeft -= OnPlayerLeft;
            }
            
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);
            
            if (settingsButton != null)
                settingsButton.onClick.RemoveListener(OnSettingsClicked);
            
            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
        }
    }
    
    // UI component for player join indicator
    public class PlayerJoinIndicator : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI playerText;
        [SerializeField] private GameObject joinedIcon;
        [SerializeField] private GameObject pressToJoinIcon;
        
        [Header("Colors")]
        [SerializeField] private Color waitingColor = Color.gray;
        [SerializeField] private Color joinedColor = Color.green;
        
        private int playerIndex;
        private bool isJoined = false;
        
        public void Initialize(int index, bool joined)
        {
            playerIndex = index;
            playerText.text = $"P{index + 1}";
            SetJoined(joined);
        }
        
        public void SetJoined(bool joined)
        {
            isJoined = joined;
            
            if (background != null)
                background.color = joined ? joinedColor : waitingColor;
            
            if (joinedIcon != null)
                joinedIcon.SetActive(joined);
            
            if (pressToJoinIcon != null)
                pressToJoinIcon.SetActive(!joined);
                
            // Animation - DOTween
            if (joined)
            {
                transform.DOScale(Vector3.one * 1.2f, 0.2f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        transform.DOScale(Vector3.one, 0.1f);
                    });
            }
        }
    }
}