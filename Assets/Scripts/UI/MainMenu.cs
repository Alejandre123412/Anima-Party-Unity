using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using AnimaParty.Core;
using DG.Tweening; // Añadido

namespace AnimaParty.UI
{
    public class MainMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Transform playerDisplayContainer;
        [SerializeField] private GameObject playerDisplayPrefab;
        [SerializeField] private Button minigamePartyButton;
        [SerializeField] private Button freePlayButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        
        [Header("Game Mode Selection")]
        [SerializeField] private GameObject modeSelectionPanel;
        [SerializeField] private Transform modeSelectionContainer;
        [SerializeField] private GameObject modeSelectionPrefab;
        
        [Header("Player Display")]
        [SerializeField] private Transform playerModelsContainer;
        [SerializeField] private Camera menuCamera;
        [SerializeField] private float cameraRotationSpeed = 10f;
        [SerializeField] private float playerSpacing = 2f;
        
        [Header("Settings")]
        [SerializeField] private GameModeData[] gameModes;
        
        private List<GameObject> playerModels = new List<GameObject>();
        private Dictionary<int, PlayerDisplayUI> playerDisplays = new Dictionary<int, PlayerDisplayUI>();
        private Tween titlePulseTween;
        private Tween messageTween;
        
        private class PlayerDisplayUI
        {
            public GameObject displayObject;
            public TextMeshProUGUI playerText;
            public TextMeshProUGUI characterText;
            public Image characterImage;
            public Image readyIndicator;
            public int playerId;
        }
        
        [System.Serializable]
        public class GameModeData
        {
            public string modeId;
            public string displayName;
            public string description;
            public Sprite icon;
            public int minPlayers = 2;
            public int maxPlayers = 4;
            public string sceneName;
        }
        
        private void Start()
        {
            Initialize();
            SetupPlayerDisplays();
            SetupGameModes();
            SpawnPlayerModels();
        }
        
        private void Initialize()
        {
            // Setup buttons
            if (minigamePartyButton != null)
                minigamePartyButton.onClick.AddListener(OnMinigamePartyClicked);
            
            if (freePlayButton != null)
                freePlayButton.onClick.AddListener(OnFreePlayClicked);
            
            if (optionsButton != null)
                optionsButton.onClick.AddListener(OnOptionsClicked);
            
            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCreditsClicked);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
            
            // Hide mode selection initially
            if (modeSelectionPanel != null)
                modeSelectionPanel.SetActive(false);
        }
        
        private void SetupPlayerDisplays()
        {
            // Clear existing displays
            foreach (Transform child in playerDisplayContainer)
            {
                Destroy(child.gameObject);
            }
            playerDisplays.Clear();
            
            // Get players from PlayerManager
            var players = PlayerManager.Instance?.players;
            if (players == null || players.Count == 0)
            {
                Debug.LogWarning("No players in MainMenu!");
                return;
            }
            
            // Create display for each player
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                GameObject display = Instantiate(playerDisplayPrefab, playerDisplayContainer);
                
                PlayerDisplayUI displayUI = new PlayerDisplayUI
                {
                    displayObject = display,
                    playerText = display.transform.Find("PlayerText").GetComponent<TextMeshProUGUI>(),
                    characterText = display.transform.Find("CharacterText").GetComponent<TextMeshProUGUI>(),
                    characterImage = display.transform.Find("CharacterImage").GetComponent<Image>(),
                    readyIndicator = display.transform.Find("ReadyIndicator").GetComponent<Image>(),
                    playerId = player.playerId
                };
                
                // Update display
                UpdatePlayerDisplay(player.playerId);
                
                playerDisplays[player.playerId] = displayUI;
            }
        }
        
        private void UpdatePlayerDisplay(int playerId)
        {
            if (!playerDisplays.TryGetValue(playerId, out var display)) return;
            
            var player = PlayerManager.Instance?.GetPlayerById(playerId);
            if (player == null) return;
            
            // Get character selection
            string characterName = "Unknown";
            Sprite characterIcon = null;
            
            if (PlayerData.Instance != null)
            {
                var selection = PlayerData.Instance.GetCharacterSelection(playerId);
                if (selection != null)
                {
                    // Get character data
                    var characterData = Resources.Load<CharacterData>("CharacterData");
                    if (characterData != null)
                    {
                        var character = characterData.GetCharacter(selection.characterId);
                        if (character != null)
                        {
                            characterName = character.displayName;
                            characterIcon = character.icon;
                        }
                    }
                }
            }
            
            // Update UI
            display.playerText.text = player.playerName;
            display.playerText.color = GetPlayerColor(playerId);
            display.characterText.text = characterName;
            
            if (display.characterImage != null && characterIcon != null)
                display.characterImage.sprite = characterIcon;
            
            // Ready indicator (always ready in main menu)
            display.readyIndicator.color = Color.green;
        }
        
        private void SetupGameModes()
        {
            if (modeSelectionContainer == null) return;
            
            // Clear existing modes
            foreach (Transform child in modeSelectionContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create mode selection items
            foreach (var mode in gameModes)
            {
                GameObject modeItem = Instantiate(modeSelectionPrefab, modeSelectionContainer);
                var modeUI = modeItem.GetComponent<GameModeUI>();
                if (modeUI != null)
                {
                    modeUI.Initialize(mode, OnGameModeSelected);
                }
            }
        }
        
        private void SpawnPlayerModels()
        {
            // Clear existing models
            foreach (var model in playerModels)
            {
                if (model != null)
                    Destroy(model);
            }
            playerModels.Clear();
            
            // Get players
            var players = PlayerManager.Instance?.players;
            if (players == null || players.Count == 0) return;
            
            // Spawn models
            float startX = -(players.Count - 1) * playerSpacing / 2f;
            
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                
                // Get character selection
                if (PlayerData.Instance != null)
                {
                    var selection = PlayerData.Instance.GetCharacterSelection(player.playerId);
                    if (selection != null)
                    {
                        // Load character data
                        var characterData = Resources.Load<CharacterData>("CharacterData");
                        if (characterData != null)
                        {
                            GameObject characterModel = characterData.GetCharacterPrefab(
                                selection.characterId,
                                selection.skinIndex,
                                selection.playerColor
                            );
                            
                            if (characterModel != null)
                            {
                                // Position model
                                Vector3 position = new Vector3(startX + i * playerSpacing, 0, 0);
                                characterModel.transform.position = position;
                                characterModel.transform.rotation = Quaternion.Euler(0, 180, 0);
                                
                                // Disable unnecessary components
                                var characterScript = characterModel.GetComponent<Character>();
                                if (characterScript != null)
                                    Destroy(characterScript);
                                    
                                var colliders = characterModel.GetComponentsInChildren<Collider>();
                                foreach (var collider in colliders)
                                    collider.enabled = false;
                                    
                                var rigidbodies = characterModel.GetComponentsInChildren<Rigidbody>();
                                foreach (var rb in rigidbodies)
                                    rb.isKinematic = true;
                                
                                playerModels.Add(characterModel);
                            }
                        }
                    }
                }
            }
            
            // Position camera
            if (menuCamera != null && players.Count > 0)
            {
                float cameraDistance = Mathf.Max(5f, players.Count * 1.5f);
                menuCamera.transform.position = new Vector3(0, 3, -cameraDistance);
            }
        }
        
        private void OnMinigamePartyClicked()
        {
            PlayButtonSound();
            ShowModeSelection();
        }
        
        private void OnFreePlayClicked()
        {
            PlayButtonSound();
            // Load free play mode
            SceneLoader.Instance?.LoadScene("FreePlay");
        }
        
        private void OnOptionsClicked()
        {
            PlayButtonSound();
            // Show options menu
            Debug.Log("Opening options...");
        }
        
        private void OnCreditsClicked()
        {
            PlayButtonSound();
            // Show credits
            SceneLoader.Instance?.LoadScene("Credits");
        }
        
        private void OnQuitClicked()
        {
            PlayButtonSound();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        private void ShowModeSelection()
        {
            if (modeSelectionPanel != null)
            {
                modeSelectionPanel.SetActive(true);
                
                // Update mode availability based on player count
                var players = PlayerManager.Instance?.players;
                if (players != null)
                {
                    int playerCount = players.Count;
                    
                    foreach (Transform child in modeSelectionContainer)
                    {
                        var modeUI = child.GetComponent<GameModeUI>();
                        if (modeUI != null)
                        {
                            modeUI.SetAvailable(playerCount >= modeUI.MinPlayers && playerCount <= modeUI.MaxPlayers);
                        }
                    }
                }
            }
        }
        
        private void OnGameModeSelected(GameModeData mode)
        {
            PlayButtonSound();
            
            // Check player count
            var players = PlayerManager.Instance?.players;
            if (players != null)
            {
                if (players.Count < mode.minPlayers)
                {
                    ShowMessage($"Se necesitan al menos {mode.minPlayers} jugadores");
                    return;
                }
                
                if (players.Count > mode.maxPlayers)
                {
                    ShowMessage($"Máximo {mode.maxPlayers} jugadores");
                    return;
                }
            }
            
            // Load game mode scene
            if (!string.IsNullOrEmpty(mode.sceneName))
            {
                SceneLoader.Instance?.LoadScene(mode.sceneName);
            }
            else
            {
                Debug.LogError($"No scene assigned for mode: {mode.modeId}");
            }
        }
        
        private void ShowMessage(string message)
        {
            // Cancel existing message tween
            messageTween?.Kill();
            
            // Implement a message popup
            Debug.LogWarning(message);
            
            // You could show a UI popup here
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
            // Rotate camera
            if (menuCamera != null)
            {
                menuCamera.transform.RotateAround(
                    Vector3.zero,
                    Vector3.up,
                    cameraRotationSpeed * Time.deltaTime
                );
            }
            
            // Animate player models
            for (int i = 0; i < playerModels.Count; i++)
            {
                if (playerModels[i] != null)
                {
                    // Gentle bounce animation
                    float bounce = Mathf.Sin(Time.time * 2f + i) * 0.1f;
                    Vector3 position = playerModels[i].transform.position;
                    position.y = bounce;
                    playerModels[i].transform.position = position;
                }
            }
            
            // Keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnQuitClicked();
            }
            
            if (Input.GetKeyDown(KeyCode.Return) && modeSelectionPanel.activeSelf)
            {
                // Select first available mode
                foreach (Transform child in modeSelectionContainer)
                {
                    var modeUI = child.GetComponent<GameModeUI>();
                    if (modeUI && modeUI.IsAvailable())
                    {
                        modeUI.Select();
                        break;
                    }
                }
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
        
        private void OnDestroy()
        {
            // Clean up tweens
            titlePulseTween?.Kill();
            messageTween?.Kill();
            
            if (minigamePartyButton != null)
                minigamePartyButton.onClick.RemoveListener(OnMinigamePartyClicked);
            
            if (freePlayButton != null)
                freePlayButton.onClick.RemoveListener(OnFreePlayClicked);
            
            if (optionsButton != null)
                optionsButton.onClick.RemoveListener(OnOptionsClicked);
            
            if (creditsButton != null)
                creditsButton.onClick.RemoveListener(OnCreditsClicked);
            
            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuitClicked);
            
            // Clean up models
            foreach (var model in playerModels)
            {
                if (model != null)
                    Destroy(model);
            }
            playerModels.Clear();
        }
    }
    
    // UI component for game mode selection
    public class GameModeUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject unavailableOverlay;
        
        private MainMenu.GameModeData modeData;
        private System.Action<MainMenu.GameModeData> onSelect;
        private Tween pulseTween;
        
        public int MinPlayers => modeData?.minPlayers ?? 0;
        public int MaxPlayers => modeData?.maxPlayers ?? 0;
        
        public void Initialize(MainMenu.GameModeData data, System.Action<MainMenu.GameModeData> selectAction)
        {
            modeData = data;
            onSelect = selectAction;
            
            // Update UI
            if (nameText != null)
                nameText.text = data.displayName;
            
            if (descriptionText != null)
                descriptionText.text = data.description;
            
            if (iconImage != null && data.icon != null)
                iconImage.sprite = data.icon;
            
            // Setup button
            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelect);
            
            SetAvailable(true);
        }
        
        public void SetAvailable(bool available)
        {
            if (selectButton != null)
                selectButton.interactable = available;
            
            if (unavailableOverlay != null)
                unavailableOverlay.SetActive(!available);
            
            // Visual feedback
            if (!available)
            {
                if (nameText != null)
                    nameText.color = Color.gray;
                    
                if (descriptionText != null)
                    descriptionText.color = Color.gray;
            }
            else
            {
                if (nameText != null)
                    nameText.color = Color.white;
                    
                if (descriptionText != null)
                    descriptionText.color = Color.white;
            }
        }
        
        public bool IsAvailable() => selectButton.interactable;
        
        public void Select()
        {
            OnSelect();
        }
        
        private void OnSelect()
        {
            onSelect?.Invoke(modeData);
        }
        
        private void OnDestroy()
        {
            pulseTween?.Kill();
            
            if (selectButton != null)
                selectButton.onClick.RemoveListener(OnSelect);
        }
    }
}