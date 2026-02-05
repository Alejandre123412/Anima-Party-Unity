using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AnimaParty.Core;
using TMPro;
using UnityEngine.UI;
using DG.Tweening; // Añadido

namespace AnimaParty.Minigames
{
    public class DictatorSwitchController : MinigameBase
    {
        [Header("Game Settings")]
        [SerializeField] private float roundTime = 3f;
        [SerializeField] private float promptDelay = 1f;
        [SerializeField] private float reactionWindow = 0.5f;
        [SerializeField] private int totalRounds = 3;
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private Image promptBackground;
        [SerializeField] private GameObject playerStatusPanel;
        [SerializeField] private GameObject playerStatusPrefab;
        
        [Header("Visual Effects")]
        [SerializeField] private Color promptReadyColor = Color.green;
        [SerializeField] private Color promptWaitingColor = Color.red;
        [SerializeField] private ParticleSystem eliminationEffect;
        [SerializeField] private AudioClip promptSound;
        [SerializeField] private AudioClip eliminationSound;
        
        private enum GamePhase
        {
            Waiting,
            PromptAppearing,
            PromptVisible,
            Elimination,
            RoundEnd,
            GameEnd
        }
        
        private GamePhase currentPhase;
        private float phaseTimer;
        private int currentRound = 0;
        private bool isPromptVisible = false;
        private bool gameRunning = false;
        
        private Dictionary<int, PlayerState> playerStates = new Dictionary<int, PlayerState>();
        private List<int> eliminatedPlayers = new List<int>();
        private Dictionary<int, PlayerStatusUI> playerStatusUIs = new Dictionary<int, PlayerStatusUI>();
        
        private AudioSource audioSource;
        private Sequence backgroundPulseSequence; // Para manejar la secuencia de pulso
        
        private class PlayerState
        {
            public bool isEliminated = false;
            public bool hasPressed = false;
            public float reactionTime = 0f;
            public bool pressedEarly = false;
            public PlayerController playerController;
        }
        
        private class PlayerStatusUI
        {
            public GameObject uiObject;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI scoreText;
            public TextMeshProUGUI winsText;
            public Image background;
            public RectTransform rectTransform;
        }
        
        protected override void Start()
        {
            base.Start();
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            
            InitializeGame();
        }
        
        public override void InitializeMinigame(List<PlayerController> players)
        {
            base.InitializeMinigame(players);
            InitializeGame();
        }
        
        private void InitializeGame()
        {
            playerStates.Clear();
            eliminatedPlayers.Clear();
            currentRound = 0;
            gameRunning = true;
            
            // Initialize player states
            foreach (var player in players)
            {
                playerStates[player.PlayerId] = new PlayerState
                {
                    playerController = player
                };
            }
            
            SetupPlayerStatusUI();
            StartNewRound();
        }
        
        private void SetupPlayerStatusUI()
        {
            // Clear existing UI
            foreach (Transform child in playerStatusPanel.transform)
            {
                Destroy(child.gameObject);
            }
            playerStatusUIs.Clear();
            
            // Create UI for each player
            foreach (var player in players)
            {
                GameObject statusObj = Instantiate(playerStatusPrefab, playerStatusPanel.transform);
                PlayerStatusUI statusUI = new PlayerStatusUI
                {
                    uiObject = statusObj,
                    nameText = statusObj.transform.Find("NameText").GetComponent<TextMeshProUGUI>(),
                    scoreText = statusObj.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>(),
                    winsText = statusObj.transform.Find("WinsText").GetComponent<TextMeshProUGUI>(),
                    background = statusObj.GetComponent<Image>(),
                    rectTransform = statusObj.GetComponent<RectTransform>()
                };
                
                statusUI.nameText.text = $"P{player.PlayerId + 1}";
                statusUI.scoreText.text = "0";
                statusUI.winsText.text = "0";
                statusUI.background.color = GetPlayerColor(player.PlayerId);
                
                playerStatusUIs[player.PlayerId] = statusUI;
            }
        }
        
        private void Update()
        {
            if (!gameRunning) return;
            
            float deltaTime = Time.deltaTime;
            
            switch (currentPhase)
            {
                case GamePhase.Waiting:
                    UpdateWaitingPhase(deltaTime);
                    break;
                    
                case GamePhase.PromptAppearing:
                    UpdatePromptAppearingPhase(deltaTime);
                    break;
                    
                case GamePhase.PromptVisible:
                    UpdatePromptVisiblePhase(deltaTime);
                    break;
                    
                case GamePhase.Elimination:
                    UpdateEliminationPhase(deltaTime);
                    break;
                    
                case GamePhase.RoundEnd:
                    UpdateRoundEndPhase(deltaTime);
                    break;
            }
            
            UpdateUI();
        }
        
        private void UpdateWaitingPhase(float deltaTime)
        {
            phaseTimer -= deltaTime;
            
            // Update countdown
            if (countdownText != null)
            {
                countdownText.text = Mathf.Ceil(phaseTimer).ToString();
                countdownText.color = Color.Lerp(Color.red, Color.yellow, phaseTimer % 1f);
            }
            
            if (phaseTimer <= 0f)
            {
                currentPhase = GamePhase.PromptAppearing;
                phaseTimer = 0.5f;
                ShowPrompt();
            }
        }
        
        private void UpdatePromptAppearingPhase(float deltaTime)
        {
            phaseTimer -= deltaTime;
            
            // Pulse effect
            if (promptBackground != null)
            {
                float pulse = Mathf.PingPong(Time.time * 10f, 1f);
                promptBackground.color = Color.Lerp(promptWaitingColor, promptReadyColor, pulse);
            }
            
            if (phaseTimer <= 0f)
            {
                currentPhase = GamePhase.PromptVisible;
                phaseTimer = reactionWindow;
                isPromptVisible = true;
                StartReactionTimer();
                
                // Play prompt sound
                if (promptSound != null)
                    audioSource.PlayOneShot(promptSound);
            }
        }
        
        private void UpdatePromptVisiblePhase(float deltaTime)
        {
            phaseTimer -= deltaTime;
            
            // Update reaction timer display
            if (timerText != null)
                timerText.text = $"REACCIONA: {phaseTimer:F2}s";
            
            CheckPlayerInput();
            
            if (phaseTimer <= 0f)
            {
                EliminateSlowestPlayer();
                currentPhase = GamePhase.Elimination;
                phaseTimer = 1f;
                isPromptVisible = false;
                
                if (promptText != null)
                    promptText.text = "";
            }
        }
        
        private void UpdateEliminationPhase(float deltaTime)
        {
            phaseTimer -= deltaTime;
            
            if (phaseTimer <= 0f)
            {
                currentPhase = GamePhase.RoundEnd;
                phaseTimer = 1.5f;
                
                // Check if game should end
                int activePlayers = players.Count(p => !playerStates[p.PlayerId].isEliminated);
                if (activePlayers <= 1)
                {
                    EndGame();
                    return;
                }
            }
        }
        
        private void UpdateRoundEndPhase(float deltaTime)
        {
            phaseTimer -= deltaTime;
            
            if (phaseTimer <= 0f)
            {
                StartNewRound();
            }
        }
        
        private void ShowPrompt()
        {
            if (promptText != null)
            {
                promptText.text = "¡PRESIONA A!";
                promptText.gameObject.SetActive(true);
                
                // Animation - DOTween
                promptText.transform.localScale = Vector3.zero;
                promptText.transform.DOScale(Vector3.one * 1.2f, 0.2f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => {
                        promptText.transform.DOScale(Vector3.one, 0.1f);
                    });
            }
            
            if (promptBackground != null)
            {
                promptBackground.gameObject.SetActive(true);
                promptBackground.color = promptWaitingColor;
                
                // Cancel existing pulse sequence
                backgroundPulseSequence?.Kill();
                
                // Create new pulse animation - DOTween
                backgroundPulseSequence = DOTween.Sequence();
                backgroundPulseSequence
                    .Append(promptBackground.DOColor(promptReadyColor, 0.5f).SetEase(Ease.InOutSine))
                    .Append(promptBackground.DOColor(promptWaitingColor, 0.5f).SetEase(Ease.InOutSine))
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }
        
        private void StartReactionTimer()
        {
            foreach (var player in players)
            {
                if (!playerStates[player.PlayerId].isEliminated)
                {
                    playerStates[player.PlayerId].hasPressed = false;
                    playerStates[player.PlayerId].reactionTime = 0f;
                    playerStates[player.PlayerId].pressedEarly = false;
                }
            }
        }
        
        private void CheckPlayerInput()
        {
            if (!isPromptVisible) return;
            
            foreach (var player in players)
            {
                var state = playerStates[player.PlayerId];
                if (state.isEliminated || state.hasPressed) continue;
                
                // Check for button press using InputManager
                if (InputManager.Instance.GetButtonDown(player.PlayerId, "Action"))
                {
                    if (currentPhase == GamePhase.PromptVisible)
                    {
                        // Valid reaction
                        state.hasPressed = true;
                        state.reactionTime = reactionWindow - phaseTimer;
                        
                        // Update UI
                        if (playerStatusUIs.TryGetValue(player.PlayerId, out var statusUI))
                        {
                            statusUI.scoreText.text = $"{state.reactionTime:F3}s";
                            statusUI.background.color = Color.green;
                        }
                        
                        Debug.Log($"Player {player.PlayerId} reacted in {state.reactionTime:F3}s");
                    }
                    else if (currentPhase == GamePhase.Waiting || currentPhase == GamePhase.PromptAppearing)
                    {
                        // Pressed too early
                        state.pressedEarly = true;
                        state.hasPressed = true;
                        
                        if (playerStatusUIs.TryGetValue(player.PlayerId, out var statusUI))
                        {
                            statusUI.scoreText.text = "¡TEMPRANO!";
                            statusUI.background.color = Color.red;
                        }
                        
                        Debug.Log($"Player {player.PlayerId} pressed too early!");
                    }
                }
            }
        }
        
        private void EliminateSlowestPlayer()
        {
            // Find active players
            var activePlayers = players
                .Where(p => !playerStates[p.PlayerId].isEliminated)
                .ToList();
                
            if (activePlayers.Count == 0) return;
            
            // 1. Eliminate players who pressed too early
            var earlyPressers = activePlayers
                .Where(p => playerStates[p.PlayerId].pressedEarly)
                .ToList();
                
            foreach (var player in earlyPressers)
            {
                EliminatePlayer(player.PlayerId, "¡Demasiado pronto!");
            }
            
            // Check remaining players
            var remainingPlayers = activePlayers
                .Where(p => !playerStates[p.PlayerId].isEliminated)
                .ToList();
                
            if (remainingPlayers.Count == 0) return;
            
            // 2. Eliminate players who didn't press at all
            var noPressers = remainingPlayers
                .Where(p => !playerStates[p.PlayerId].hasPressed)
                .ToList();
                
            if (noPressers.Count > 0)
            {
                foreach (var player in noPressers)
                {
                    EliminatePlayer(player.PlayerId, "¡No reaccionó!");
                }
                return;
            }
            
            // 3. Eliminate slowest player among those who pressed
            var playersWithReactions = remainingPlayers
                .Where(p => playerStates[p.PlayerId].hasPressed && !playerStates[p.PlayerId].pressedEarly)
                .ToList();
                
            if (playersWithReactions.Count == 0) return;
            
            var slowestPlayer = playersWithReactions
                .OrderByDescending(p => playerStates[p.PlayerId].reactionTime)
                .First();
                
            EliminatePlayer(slowestPlayer.PlayerId, 
                $"¡Demasiado lento! ({playerStates[slowestPlayer.PlayerId].reactionTime:F3}s)");
        }
        
        private void EliminatePlayer(int playerId, string reason)
        {
            if (playerStates.ContainsKey(playerId))
            {
                playerStates[playerId].isEliminated = true;
                eliminatedPlayers.Add(playerId);
                
                // Update UI
                if (playerStatusUIs.TryGetValue(playerId, out var statusUI))
                {
                    statusUI.scoreText.text = "ELIMINADO";
                    statusUI.background.color = Color.gray;
                    
                    // Strike through effect
                    statusUI.nameText.fontStyle = FontStyles.Strikethrough;
                }
                
                // Visual effect
                if (eliminationEffect != null)
                {
                    var playerObj = playerStates[playerId].playerController?.gameObject;
                    if (playerObj != null)
                    {
                        Instantiate(eliminationEffect, playerObj.transform.position, Quaternion.identity);
                    }
                }
                
                // Play sound
                if (eliminationSound != null)
                    audioSource.PlayOneShot(eliminationSound);
                
                // Show status message
                if (statusText != null)
                {
                    statusText.text = $"¡Jugador {playerId + 1} eliminado!\n{reason}";
                    statusText.color = Color.red;
                    statusText.DOFade(0f, 1f).SetDelay(1f);
                }
                
                Debug.Log($"Player {playerId} eliminated: {reason}");
            }
        }
        
        private void StartNewRound()
        {
            currentRound++;
            
            if (currentRound > totalRounds)
            {
                EndGame();
                return;
            }
            
            // Reset for new round
            currentPhase = GamePhase.Waiting;
            phaseTimer = promptDelay;
            isPromptVisible = false;
            
            if (promptText != null)
                promptText.gameObject.SetActive(false);
                
            if (promptBackground != null)
            {
                promptBackground.gameObject.SetActive(false);
                backgroundPulseSequence?.Kill();
            }
                
            if (countdownText != null)
                countdownText.text = "";
                
            Debug.Log($"Starting round {currentRound}/{totalRounds}");
        }
        
        private void EndGame()
        {
            gameRunning = false;
            currentPhase = GamePhase.GameEnd;
            
            // Cancel any ongoing animations
            backgroundPulseSequence?.Kill();
            
            // Determine winner
            var winner = players.FirstOrDefault(p => !playerStates[p.PlayerId].isEliminated);
            
            if (winner)
            {
                Debug.Log($"Player {winner.PlayerId} wins Dictator Switch!");
                
                if (promptText)
                {
                    promptText.text = $"¡GANADOR!\nJugador {winner.PlayerId + 1}";
                    promptText.gameObject.SetActive(true);
                    promptText.color = Color.yellow;
                    
                    // Celebration effect - DOTween
                    promptText.transform.DOScale(Vector3.one * 1.5f, 0.5f)
                        .SetEase(Ease.OutElastic);
                }
                
                // Assign points
                AssignPoints(winner.PlayerId);
            }
            
            // End minigame after delay
            Invoke(nameof(FinishMinigame), 3f);
        }
        
        private void AssignPoints(int winnerId)
        {
            // Calculate points based on performance
            foreach (var player in players)
            {
                int points = 0;
                
                if (player.PlayerId == winnerId)
                    points = 10; // Winner
                else if (!playerStates[player.PlayerId].isEliminated)
                    points = 5;  // Survived but didn't win
                else if (currentRound > 1)
                    points = 2;  // Eliminated in later rounds
                else
                    points = 1;  // Eliminated in first round
                
                // Store results
                if (minigameResults.ContainsKey(player.PlayerId))
                    minigameResults[player.PlayerId] = points;
                else
                    minigameResults.Add(player.PlayerId, points);
                
                Debug.Log($"Player {player.PlayerId} gets {points} points");
            }
        }
        
        private void FinishMinigame()
        {
            OnCompleted();
            Debug.Log("Dictator Switch completed!");
        }
        
        private void UpdateUI()
        {
            // Update round text
            if (roundText != null)
                roundText.text = $"RONDA: {currentRound}/{totalRounds}";
            
            // Update prompt visibility
            if (promptText != null)
                promptText.gameObject.SetActive(isPromptVisible);
                
            if (promptBackground != null)
                promptBackground.gameObject.SetActive(currentPhase == GamePhase.PromptAppearing || 
                                                    currentPhase == GamePhase.PromptVisible);
            
            // Update timer based on phase
            if (timerText != null)
            {
                switch (currentPhase)
                {
                    case GamePhase.Waiting:
                        timerText.text = $"Siguiente: {phaseTimer:F1}s";
                        break;
                    case GamePhase.PromptVisible:
                        timerText.text = $"¡Reacciona! {phaseTimer:F2}s";
                        break;
                    case GamePhase.Elimination:
                        timerText.text = "Eliminando...";
                        break;
                    case GamePhase.RoundEnd:
                        timerText.text = "Siguiente ronda...";
                        break;
                    default:
                        timerText.text = "";
                        break;
                }
            }
        }
        
        private Color GetPlayerColor(int playerId)
        {
            return playerId switch
            {
                0 => new Color(1f, 0.3f, 0.3f), // Red
                1 => new Color(0.3f, 0.6f, 1f), // Blue
                2 => new Color(0.9f, 0.7f, 0.2f), // Gold
                3 => new Color(0.4f, 0.8f, 0.2f), // Green
                _ => Color.white
            };
        }
        
        private void OnDestroy()
        {
            // Clean up DOTween animations
            backgroundPulseSequence?.Kill();
            DOTween.Kill(promptText);
            DOTween.Kill(promptBackground);
            DOTween.Kill(statusText);
        }
    }
}