using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using AnimaParty.Core;
using DG.Tweening; // Añadido

namespace AnimaParty.UI
{
    public class ModeSelector : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Transform modeGridContainer;
        [SerializeField] private GameObject modeButtonPrefab;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI playerCountText;
        
        [Header("Mode Settings")]
        [SerializeField] private ModeData[] availableModes;
        [SerializeField] private int minPlayers = 2;
        [SerializeField] private int maxPlayers = 4;
        
        [Header("Player Management")]
        [SerializeField] private Transform playerSlotsContainer;
        [SerializeField] private GameObject playerSlotPrefab;
        [SerializeField] private Button addPlayerButton;
        [SerializeField] private Button removePlayerButton;
        
        [System.Serializable]
        public class ModeData
        {
            public string modeId;
            public string displayName;
            public string description;
            public Sprite icon;
            public Color themeColor = Color.white;
            public int requiredPlayers = 2;
            public bool unlocked = true;
            public string sceneName;
        }
        
        private List<PlayerSlotUI> playerSlots = new List<PlayerSlotUI>();
        private Dictionary<string, ModeButtonUI> modeButtons = new Dictionary<string, ModeButtonUI>();
        private int currentPlayerCount = 2;
        private ModeData selectedMode;
        private Tween titlePulseTween;
        private Tween messageTween;
        private Dictionary<string, Tween> modeButtonTweens = new Dictionary<string, Tween>();
        
        private class PlayerSlotUI
        {
            public GameObject slotObject;
            public Image playerIcon;
            public TextMeshProUGUI playerText;
            public GameObject aiIndicator;
            public int slotIndex;
        }
        
        private class ModeButtonUI
        {
            public GameObject buttonObject;
            public Button button;
            public Image background;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI descriptionText;
            public Image iconImage;
            public GameObject lockIcon;
            public ModeData modeData;
        }
        
        private void Start()
        {
            Initialize();
            SetupPlayerSlots();
            SetupModeButtons();
            UpdateUI();
        }
        
        private void Initialize()
        {
            currentPlayerCount = Mathf.Clamp(currentPlayerCount, minPlayers, maxPlayers);
            
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
            
            if (addPlayerButton != null)
                addPlayerButton.onClick.AddListener(OnAddPlayer);
            
            if (removePlayerButton != null)
                removePlayerButton.onClick.AddListener(OnRemovePlayer);
            
            // Initialize player manager
            InitializePlayers();
        }
        
        private void InitializePlayers()
        {
            // Clear existing players
            PlayerManager.Instance?.ClearPlayers();
            
            // Add players based on current count
            for (int i = 0; i < currentPlayerCount; i++)
            {
                PlayerManager.Instance?.AddPlayer(i, $"Jugador {i + 1}");
            }
            
            UpdatePlayerSlots();
        }
        
        private void SetupPlayerSlots()
        {
            // Clear existing slots
            foreach (Transform child in playerSlotsContainer)
            {
                Destroy(child.gameObject);
            }
            playerSlots.Clear();
            
            // Create slots for max players
            for (int i = 0; i < maxPlayers; i++)
            {
                GameObject slot = Instantiate(playerSlotPrefab, playerSlotsContainer);
                
                PlayerSlotUI slotUI = new PlayerSlotUI
                {
                    slotObject = slot,
                    playerIcon = slot.transform.Find("PlayerIcon").GetComponent<Image>(),
                    playerText = slot.transform.Find("PlayerText").GetComponent<TextMeshProUGUI>(),
                    aiIndicator = slot.transform.Find("AIIndicator").gameObject,
                    slotIndex = i
                };
                
                // Set initial state
                bool isActive = i < currentPlayerCount;
                slot.SetActive(isActive);
                slotUI.aiIndicator.SetActive(false); // No AI by default
                slotUI.playerText.text = $"P{i + 1}";
                slotUI.playerText.color = GetPlayerColor(i);
                slotUI.playerIcon.color = GetPlayerColor(i);
                
                playerSlots.Add(slotUI);
            }
            
            UpdatePlayerSlots();
        }
        
        private void UpdatePlayerSlots()
        {
            for (int i = 0; i < playerSlots.Count; i++)
            {
                var slot = playerSlots[i];
                bool isActive = i < currentPlayerCount;
                slot.slotObject.SetActive(isActive);
                
                if (isActive)
                {
                    // Update player info
                    var player = PlayerManager.Instance?.GetPlayerById(i);
                    if (player != null)
                    {
                        slot.playerText.text = player.playerName;
                        slot.playerText.color = GetPlayerColor(i);
                        slot.playerIcon.color = GetPlayerColor(i);
                        slot.aiIndicator.SetActive(player.isAI);
                    }
                }
            }
            
            // Update buttons
            if (addPlayerButton != null)
                addPlayerButton.interactable = currentPlayerCount < maxPlayers;
            
            if (removePlayerButton != null)
                removePlayerButton.interactable = currentPlayerCount > minPlayers;
            
            if (playerCountText != null)
                playerCountText.text = $"{currentPlayerCount} JUGADORES";
                
            // Update mode availability
            UpdateModeAvailability();
        }
        
        private void SetupModeButtons()
        {
            // Clear existing buttons
            foreach (Transform child in modeGridContainer)
            {
                Destroy(child.gameObject);
            }
            modeButtons.Clear();
            
            // Create buttons for each mode
            foreach (var mode in availableModes)
            {
                GameObject buttonObj = Instantiate(modeButtonPrefab, modeGridContainer);
                
                ModeButtonUI buttonUI = new ModeButtonUI
                {
                    buttonObject = buttonObj,
                    button = buttonObj.GetComponent<Button>(),
                    background = buttonObj.GetComponent<Image>(),
                    nameText = buttonObj.transform.Find("NameText").GetComponent<TextMeshProUGUI>(),
                    descriptionText = buttonObj.transform.Find("DescriptionText").GetComponent<TextMeshProUGUI>(),
                    iconImage = buttonObj.transform.Find("Icon").GetComponent<Image>(),
                    lockIcon = buttonObj.transform.Find("LockIcon").gameObject,
                    modeData = mode
                };
                
                // Setup UI
                buttonUI.nameText.text = mode.displayName;
                buttonUI.descriptionText.text = mode.description;
                buttonUI.iconImage.sprite = mode.icon;
                buttonUI.background.color = mode.themeColor * 0.8f;
                
                // Setup button
                buttonUI.button.onClick.AddListener(() => OnModeSelected(mode));
                
                // Check if unlocked
                buttonUI.lockIcon.SetActive(!mode.unlocked);
                buttonUI.button.interactable = mode.unlocked;
                
                modeButtons[mode.modeId] = buttonUI;
            }
            
            UpdateModeAvailability();
        }
        
        private void UpdateModeAvailability()
        {
            foreach (var kvp in modeButtons)
            {
                var mode = kvp.Value.modeData;
                bool isAvailable = mode.unlocked && currentPlayerCount >= mode.requiredPlayers;
                
                kvp.Value.button.interactable = isAvailable;
                kvp.Value.lockIcon.SetActive(!isAvailable);
                
                // Visual feedback
                if (!isAvailable)
                {
                    kvp.Value.background.color = Color.gray;
                    kvp.Value.nameText.color = Color.gray;
                }
                else
                {
                    kvp.Value.background.color = mode.themeColor * 0.8f;
                    kvp.Value.nameText.color = Color.white;
                }
            }
        }
        
        private void OnAddPlayer()
        {
            if (currentPlayerCount >= maxPlayers) return;
            
            currentPlayerCount++;
            
            // Add player to PlayerManager
            PlayerManager.Instance?.AddPlayer(currentPlayerCount - 1, $"Jugador {currentPlayerCount}");
            
            UpdatePlayerSlots();
            PlayButtonSound();
        }
        
        private void OnRemovePlayer()
        {
            if (currentPlayerCount <= minPlayers) return;
            
            // Remove last player from PlayerManager
            // Note: PlayerManager doesn't have remove method, so we'll just decrease count
            currentPlayerCount--;
            
            UpdatePlayerSlots();
            PlayButtonSound();
        }
        
        private void OnModeSelected(ModeData mode)
        {
            if (!mode.unlocked || currentPlayerCount < mode.requiredPlayers)
            {
                ShowMessage($"Se necesitan {mode.requiredPlayers} jugadores");
                return;
            }
            
            selectedMode = mode;
            
            // Highlight selected mode
            foreach (var kvp in modeButtons)
            {
                bool isSelected = kvp.Value.modeData == mode;
                kvp.Value.background.color = isSelected ? 
                    mode.themeColor : 
                    mode.themeColor * 0.8f;
                
                // Manage pulse animation
                if (isSelected)
                {
                    // Start pulse animation
                    if (!modeButtonTweens.ContainsKey(kvp.Key) || modeButtonTweens[kvp.Key] == null)
                    {
                        modeButtonTweens[kvp.Key] = kvp.Value.buttonObject.transform
                            .DOScale(Vector3.one * 1.1f, 0.3f)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    }
                }
                else
                {
                    // Stop pulse animation
                    if (modeButtonTweens.ContainsKey(kvp.Key) && modeButtonTweens[kvp.Key] != null)
                    {
                        modeButtonTweens[kvp.Key].Kill();
                        modeButtonTweens.Remove(kvp.Key);
                    }
                    kvp.Value.buttonObject.transform.localScale = Vector3.one;
                }
            }
            
            // Show confirmation
            ShowConfirmation(mode);
            PlayButtonSound();
        }
        
        private void ShowConfirmation(ModeData mode)
        {
            // Cancel existing title pulse
            titlePulseTween?.Kill();
            
            // Update title
            if (titleText != null)
            {
                titleText.text = $"¿Comenzar {mode.displayName}?";
                titleText.color = mode.themeColor;
                
                // Pulse effect - DOTween
                titlePulseTween = titleText.transform.DOScale(Vector3.one * 1.2f, 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
            
            // Show start prompt
            // You could implement a confirmation dialog here
        }
        
        private void StartSelectedMode()
        {
            if (selectedMode == null) return;
            
            if (!string.IsNullOrEmpty(selectedMode.sceneName))
            {
                // Save player data
                if (PlayerData.Instance != null)
                {
                    PlayerData.Instance.SaveToFile();
                }
                
                // Load scene
                SceneLoader.Instance?.LoadScene(selectedMode.sceneName);
            }
            else
            {
                Debug.LogError($"No scene assigned for mode: {selectedMode.modeId}");
            }
        }
        
        private void OnBackClicked()
        {
            PlayButtonSound();
            SceneLoader.Instance?.LoadScene("MainMenu");
        }
        
        private void ShowMessage(string message)
        {
            // Cancel existing message tween
            messageTween?.Kill();
            
            if (titleText != null)
            {
                string originalText = titleText.text;
                Color originalColor = titleText.color;
                
                titleText.text = message;
                titleText.color = Color.red;
                
                // Restore original text after delay - DOTween
                messageTween = DOVirtual.DelayedCall(2f, () =>
                {
                    if (titleText != null)
                    {
                        titleText.text = originalText;
                        titleText.color = originalColor;
                    }
                });
            }
        }
        
        private void PlayButtonSound()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("Select");
        }
        
        private void Update()
        {
            // Handle keyboard input
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnBackClicked();
            }
            
            if (Input.GetKeyDown(KeyCode.Return) && selectedMode != null)
            {
                StartSelectedMode();
            }
            
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
            {
                OnAddPlayer();
            }
            
            if (Input.GetKeyDown(KeyCode.Minus))
            {
                OnRemovePlayer();
            }
        }
        
        private Color GetPlayerColor(int playerId)
        {
            return playerId switch
            {
                0 => Color.red,
                1 => Color.blue,
                2 => Color.green,
                3 => Color.yellow,
                _ => Color.white
            };
        }
        
        private void UpdateUI()
        {
            // Update player count text
            if (playerCountText != null)
                playerCountText.text = $"{currentPlayerCount} JUGADORES";
            
            // Update title based on selection
            if (titleText != null && selectedMode == null)
            {
                titleText.text = "SELECCIONA UN MODO";
                titleText.color = Color.white;
            }
        }
        
        private void OnDestroy()
        {
            // Clean up tweens
            titlePulseTween?.Kill();
            messageTween?.Kill();
            
            foreach (var tween in modeButtonTweens.Values)
            {
                tween?.Kill();
            }
            modeButtonTweens.Clear();
            
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);
            
            if (addPlayerButton != null)
                addPlayerButton.onClick.RemoveListener(OnAddPlayer);
            
            if (removePlayerButton != null)
                removePlayerButton.onClick.RemoveListener(OnRemovePlayer);
            
            foreach (var kvp in modeButtons)
            {
                if (kvp.Value.button != null)
                    kvp.Value.button.onClick.RemoveAllListeners();
            }
        }
    }
}