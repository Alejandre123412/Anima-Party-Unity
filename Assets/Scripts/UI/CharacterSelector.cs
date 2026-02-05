using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using AnimaParty.Core;
using DG.Tweening; // Añadido

namespace AnimaParty.UI
{
    public class CharacterSelector : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Transform playerPanelsContainer;
        [SerializeField] private GameObject playerPanelPrefab;
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;
        
        [Header("Character Display")]
        [SerializeField] private Transform characterPreviewContainer;
        [SerializeField] private Camera characterPreviewCamera;
        [SerializeField] private Light characterPreviewLight;
        [SerializeField] private float rotationSpeed = 30f;
        
        [Header("Settings")]
        [SerializeField] private CharacterData characterData;
        [SerializeField] private Color[] playerColors;
        
        private List<PlayerPanelUI> playerPanels = new List<PlayerPanelUI>();
        private Dictionary<int, GameObject> characterPreviews = new Dictionary<int, GameObject>();
        private Dictionary<int, int> playerSelections = new Dictionary<int, int>();
        private Dictionary<int, bool> playerReady = new Dictionary<int, bool>();
        private Tween startButtonTween;
        
        private class PlayerPanelUI
        {
            public GameObject panelObject;
            public TextMeshProUGUI playerText;
            public TextMeshProUGUI characterNameText;
            public TextMeshProUGUI statsText;
            public Image characterImage;
            public Button prevButton;
            public Button nextButton;
            public Button readyButton;
            public TextMeshProUGUI readyButtonText;
            public Image readyIndicator;
            public int playerId;
        }
        
        private void Start()
        {
            Initialize();
            SetupPlayerPanels();
            LoadCharacterData();
        }
        
        private void Initialize()
        {
            if (characterData == null)
            {
                Debug.LogError("CharacterData not assigned!");
                return;
            }
            
            characterData.Initialize();
            
            // Setup buttons
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
                startButton.interactable = false;
            }
            
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
            
            // Initialize player selections
            var players = PlayerManager.Instance?.players;
            if (players != null)
            {
                foreach (var player in players)
                {
                    playerSelections[player.playerId] = 0;
                    playerReady[player.playerId] = false;
                }
            }
        }
        
        private void SetupPlayerPanels()
        {
            // Clear existing panels
            foreach (Transform child in playerPanelsContainer)
            {
                Destroy(child.gameObject);
            }
            playerPanels.Clear();
            
            // Get players
            var players = PlayerManager.Instance?.players;
            if (players == null || players.Count == 0)
            {
                Debug.LogWarning("No players found!");
                return;
            }
            
            // Create panel for each player
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                GameObject panel = Instantiate(playerPanelPrefab, playerPanelsContainer);
                
                PlayerPanelUI panelUI = new PlayerPanelUI
                {
                    panelObject = panel,
                    playerText = panel.transform.Find("PlayerText").GetComponent<TextMeshProUGUI>(),
                    characterNameText = panel.transform.Find("CharacterName").GetComponent<TextMeshProUGUI>(),
                    statsText = panel.transform.Find("StatsText").GetComponent<TextMeshProUGUI>(),
                    characterImage = panel.transform.Find("CharacterImage").GetComponent<Image>(),
                    prevButton = panel.transform.Find("PrevButton").GetComponent<Button>(),
                    nextButton = panel.transform.Find("NextButton").GetComponent<Button>(),
                    readyButton = panel.transform.Find("ReadyButton").GetComponent<Button>(),
                    readyButtonText = panel.transform.Find("ReadyButton/Text").GetComponent<TextMeshProUGUI>(),
                    readyIndicator = panel.transform.Find("ReadyIndicator").GetComponent<Image>(),
                    playerId = player.playerId
                };
                
                // Setup player text
                panelUI.playerText.text = $"JUGADOR {i + 1}";
                panelUI.playerText.color = GetPlayerColor(player.playerId);
                
                // Setup buttons
                int playerId = player.playerId; // Capture for closure
                panelUI.prevButton.onClick.AddListener(() => OnPrevCharacter(playerId));
                panelUI.nextButton.onClick.AddListener(() => OnNextCharacter(playerId));
                panelUI.readyButton.onClick.AddListener(() => OnReadyToggle(playerId));
                
                // Setup ready indicator
                panelUI.readyIndicator.color = Color.red;
                panelUI.readyButtonText.text = "LISTO";
                
                playerPanels.Add(panelUI);
            }
            
            // Update all panels
            foreach (var panel in playerPanels)
            {
                UpdatePlayerPanel(panel.playerId);
                CreateCharacterPreview(panel.playerId);
            }
            
            CheckStartCondition();
        }
        
        private void LoadCharacterData()
        {
            if (characterData == null) return;
            
            // You could load character data from resources or asset bundles here
            Debug.Log($"Loaded {characterData.GetTotalCharacters()} characters");
        }
        
        private void UpdatePlayerPanel(int playerId)
        {
            var panel = playerPanels.Find(p => p.playerId == playerId);
            if (panel == null) return;
            
            int selection = playerSelections[playerId];
            var characters = characterData.GetUnlockedCharacters();
            
            if (characters.Count == 0)
            {
                Debug.LogError("No unlocked characters!");
                return;
            }
            
            int charIndex = selection % characters.Count;
            var character = characters[charIndex];
            
            // Update UI
            panel.characterNameText.text = character.displayName;
            panel.statsText.text = $"Vel: {character.speed} | Agi: {character.agility}\nFue: {character.strength} | Srt: {character.luck}";
            
            if (panel.characterImage != null && character.icon != null)
                panel.characterImage.sprite = character.icon;
            
            // Update ready button
            bool isReady = playerReady[playerId];
            panel.readyButtonText.text = isReady ? "CANCELAR" : "LISTO";
            panel.readyIndicator.color = isReady ? Color.green : Color.red;
            
            // Update character preview
            UpdateCharacterPreview(playerId, charIndex);
        }
        
        private void CreateCharacterPreview(int playerId)
        {
            if (characterPreviewContainer == null) return;
            
            // Destroy existing preview
            if (characterPreviews.ContainsKey(playerId) && characterPreviews[playerId] != null)
            {
                Destroy(characterPreviews[playerId]);
            }
            
            // Create preview container
            GameObject previewContainer = new GameObject($"Preview_{playerId}");
            previewContainer.transform.SetParent(characterPreviewContainer);
            previewContainer.transform.localPosition = GetPreviewPosition(playerId);
            previewContainer.transform.localRotation = Quaternion.identity;
            
            // Create character model
            int selection = playerSelections[playerId];
            var characters = characterData.GetUnlockedCharacters();
            int charIndex = selection % characters.Count;
            
            GameObject characterModel = characterData.GetCharacterPrefab(
                characters[charIndex].characterId,
                0,
                GetPlayerColor(playerId)
            );
            
            if (characterModel != null)
            {
                characterModel.transform.SetParent(previewContainer.transform);
                characterModel.transform.localPosition = Vector3.zero;
                characterModel.transform.localRotation = Quaternion.identity;
                
                // Scale for preview
                characterModel.transform.localScale = Vector3.one * 0.5f;
                
                // Disable unnecessary components
                var characterScript = characterModel.GetComponent<Character>();
                if (characterScript != null)
                    Destroy(characterScript);
                    
                var colliders = characterModel.GetComponentsInChildren<Collider>();
                foreach (var collider in colliders)
                    Destroy(collider);
                
                characterPreviews[playerId] = previewContainer;
            }
        }
        
        private void UpdateCharacterPreview(int playerId, int characterIndex)
        {
            if (!characterPreviews.ContainsKey(playerId) || characterPreviews[playerId] == null)
            {
                CreateCharacterPreview(playerId);
                return;
            }
            
            // Update model
            var previewContainer = characterPreviews[playerId];
            
            // Destroy old model
            foreach (Transform child in previewContainer.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Create new model
            var characters = characterData.GetUnlockedCharacters();
            if (characterIndex >= 0 && characterIndex < characters.Count)
            {
                var character = characters[characterIndex];
                GameObject characterModel = characterData.GetCharacterPrefab(
                    character.characterId,
                    0,
                    GetPlayerColor(playerId)
                );
                
                if (characterModel != null)
                {
                    characterModel.transform.SetParent(previewContainer.transform);
                    characterModel.transform.localPosition = Vector3.zero;
                    characterModel.transform.localRotation = Quaternion.identity;
                    characterModel.transform.localScale = Vector3.one * 0.5f;
                    
                    // Disable components
                    var characterScript = characterModel.GetComponent<Character>();
                    if (characterScript != null)
                        Destroy(characterScript);
                        
                    var colliders = characterModel.GetComponentsInChildren<Collider>();
                    foreach (var collider in colliders)
                        Destroy(collider);
                }
            }
        }
        
        private void OnPrevCharacter(int playerId)
        {
            if (playerReady[playerId]) return;
            
            var characters = characterData.GetUnlockedCharacters();
            if (characters.Count == 0) return;
            
            playerSelections[playerId] = (playerSelections[playerId] - 1 + characters.Count) % characters.Count;
            UpdatePlayerPanel(playerId);
            
            // Play sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("Navigate");
        }
        
        private void OnNextCharacter(int playerId)
        {
            if (playerReady[playerId]) return;
            
            var characters = characterData.GetUnlockedCharacters();
            if (characters.Count == 0) return;
            
            playerSelections[playerId] = (playerSelections[playerId] + 1) % characters.Count;
            UpdatePlayerPanel(playerId);
            
            // Play sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play("Navigate");
        }
        
        private void OnReadyToggle(int playerId)
        {
            playerReady[playerId] = !playerReady[playerId];
            UpdatePlayerPanel(playerId);
            CheckStartCondition();
            
            // Play sound
            if (AudioManager.Instance != null)
                AudioManager.Instance.Play(playerReady[playerId] ? "Select" : "Cancel");
            
            // Save character selection
            var characters = characterData.GetUnlockedCharacters();
            int charIndex = playerSelections[playerId] % characters.Count;
            var character = characters[charIndex];
            
            if (PlayerData.Instance != null)
            {
                PlayerData.Instance.SetCharacterSelection(
                    playerId,
                    character.characterId,
                    GetPlayerColor(playerId),
                    0
                );
            }
        }
        
        private void CheckStartCondition()
        {
            bool allReady = true;
            int readyCount = 0;
            
            foreach (var kvp in playerReady)
            {
                if (!kvp.Value)
                    allReady = false;
                else
                    readyCount++;
            }
            
            bool canStart = readyCount >= 2 && allReady;
            
            if (startButton != null)
            {
                startButton.interactable = canStart;
                
                // Cancel existing tween
                startButtonTween?.Kill();
                
                // Pulse effect when ready - DOTween
                if (canStart)
                {
                    startButtonTween = startButton.transform.DOScale(Vector3.one * 1.1f, 0.5f)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                        
                    if (titleText != null)
                        titleText.text = "¡TODOS LISTOS!";
                }
                else
                {
                    startButton.transform.localScale = Vector3.one;
                    
                    if (titleText != null)
                        titleText.text = $"SELECCIÓN DE PERSONAJES ({readyCount}/{playerReady.Count})";
                }
            }
        }
        
        private void OnStartClicked()
        {
            if (AudioManager.Instance)
                AudioManager.Instance.Play("StartGame");
            
            // Save player data
            if (PlayerData.Instance != null)
            {
                PlayerData.Instance.SaveToFile();
            }
            
            // Load main menu
            SceneLoader.Instance?.LoadScene("MainMenu");
        }
        
        private void OnBackClicked()
        {
            if (AudioManager.Instance)
                AudioManager.Instance.Play("Cancel");
            
            // Go back to title screen
            SceneLoader.Instance?.LoadScene("TitleScreen");
        }
        
        private void Update()
        {
            // Rotate character previews
            foreach (var kvp in characterPreviews)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                }
            }
            
            // Handle keyboard input for testing
            if (Input.GetKeyDown(KeyCode.Return) && startButton.interactable)
            {
                OnStartClicked();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnBackClicked();
            }
        }
        
        private Vector3 GetPreviewPosition(int playerId)
        {
            int playerCount = playerPanels.Count;
            float spacing = 3f;
            float startX = -(playerCount - 1) * spacing / 2f;
            
            int index = playerPanels.FindIndex(p => p.playerId == playerId);
            return new Vector3(startX + index * spacing, 0, 0);
        }
        
        private Color GetPlayerColor(int playerId)
        {
            if (playerColors != null && playerId >= 0 && playerId < playerColors.Length)
                return playerColors[playerId];
            
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
            startButtonTween?.Kill();
            
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);
            
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);
            
            foreach (var panel in playerPanels)
            {
                if (panel.prevButton != null)
                    panel.prevButton.onClick.RemoveAllListeners();
                    
                if (panel.nextButton != null)
                    panel.nextButton.onClick.RemoveAllListeners();
                    
                if (panel.readyButton != null)
                    panel.readyButton.onClick.RemoveAllListeners();
            }
            
            // Clean up previews
            foreach (var preview in characterPreviews.Values)
            {
                if (preview != null)
                    Destroy(preview);
            }
            characterPreviews.Clear();
        }
    }
}