using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AnimaParty.Core;
using AnimaParty.Minigames;
using TMPro;
using DG.Tweening;
using AnimaParty.GameModes.MinigameParty;

namespace AnimaParty.GameModes
{
    public class MinigamePartyManager : MinigameManager
    {
        [Header("Party Settings")]
        [SerializeField] private int totalRounds = 5;
        
        [Header("UI References")]
        [SerializeField] private MinigamePartyUI minigamePartyUI;
        
        [Header("Audio")]
        [SerializeField] private AudioClip roundStartSound;
        [SerializeField] private AudioClip roundEndSound;
        [SerializeField] private AudioClip gameEndSound;
        
        private int currentRound = 0;
        private GameState currentState = GameState.Lobby;
        private MinigameBase currentMinigame;
        private Dictionary<int, int> playerWins = new Dictionary<int, int>();
        private AudioSource audioSource;
        
        private enum GameState
        {
            Lobby,
            MinigameSelection,
            MinigameLoading,
            MinigamePlaying,
            RoundResults,
            GameResults
        }
        
        protected override void Awake()
        {
            // Primero llama al Awake base
            base.Awake();
            
            // Configura la instancia específica del Party Manager
            if (Instance == null || Instance == this)
            {
                Instance = this;
            }
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        private void Start()
        {
            InitializeParty();
        }
        
        private void InitializeParty()
        {
            currentRound = 0;
            playerWins.Clear();
            playedMinigames.Clear();
            
            // Initialize player scores and wins
            if (PlayerManager.Instance != null)
            {
                foreach (var player in PlayerManager.Instance.players)
                {
                    sessionScores[player.playerId] = 0;
                    playerWins[player.playerId] = 0;
                }
            }
            
            // Initialize UI
            if (minigamePartyUI != null)
            {
                minigamePartyUI.Initialize(this);
            }
            
            // Iniciar la sesión
            StartMinigameSession();
        }
        
        public override void StartMinigameSession()
        {
            StartNextRound();
        }
        
        private void StartNextRound()
        {
            currentRound++;
            
            if (currentRound > totalRounds)
            {
                EndGame();
                return;
            }
            
            currentState = GameState.MinigameSelection;
            
            // Update UI
            if (minigamePartyUI != null)
            {
                minigamePartyUI.UpdateUI(currentRound, totalRounds, 0f, "Seleccionando minijuego...");
            }
            
            // Play round start sound
            if (roundStartSound != null)
                audioSource.PlayOneShot(roundStartSound);
            
            Debug.Log($"Starting round {currentRound}/{totalRounds}");
            
            // Show minigame selection usando el método base
            ShowMinigameSelection();
        }
        
        protected override void ShowMinigameSelection()
        {
            if (minigamePrefabs.Count == 0)
            {
                Debug.LogError("No minigame prefabs assigned!");
                return;
            }
            
            // Si tenemos UI personalizada del Party, úsala
            if (minigamePartyUI != null)
            {
                // Get minigame names
                List<string> availableMinigameNames = new List<string>();
                var availableMinigames = minigamePrefabs
                    .Where(m => !playedMinigames.Contains(m))
                    .ToList();
                
                // If all minigames have been played, reset the list
                if (availableMinigames.Count == 0)
                {
                    availableMinigames = new List<GameObject>(minigamePrefabs);
                    playedMinigames.Clear();
                }
                
                // Get minigame names
                foreach (var minigamePrefab in availableMinigames)
                {
                    var minigame = minigamePrefab.GetComponent<MinigameBase>();
                    if (minigame != null)
                    {
                        availableMinigameNames.Add(minigame.Name);
                    }
                }
                
                // Show selection UI
                minigamePartyUI.ShowMinigameSelection(availableMinigameNames);
            }
            else
            {
                // Usar la implementación base
                base.ShowMinigameSelection();
            }
        }
        
        // Este método debe ser público para que el UI pueda llamarlo
        public void OnMinigameSelected(string minigameName)
        {
            Debug.Log($"Minigame selected: {minigameName}");
            
            // Find the minigame prefab by name
            GameObject selectedMinigamePrefab = null;
            foreach (var prefab in minigamePrefabs)
            {
                var minigame = prefab.GetComponent<MinigameBase>();
                if (minigame != null && minigame.Name == minigameName)
                {
                    selectedMinigamePrefab = prefab;
                    break;
                }
            }
            
            if (selectedMinigamePrefab == null)
            {
                Debug.LogError($"Minigame prefab not found: {minigameName}");
                return;
            }
            
            // Usar el método base para seleccionar el minijuego
            SelectMinigame(selectedMinigamePrefab);
        }
        
        protected override void LoadMinigame(GameObject minigamePrefab)
        {
            currentState = GameState.MinigameLoading;
            
            // Usar la implementación base pero con lógica adicional
            base.LoadMinigame(minigamePrefab);
            
            // Actualizar estado y UI
            currentState = GameState.MinigamePlaying;
            
            if (minigamePartyUI != null && currentMinigame != null)
            {
                minigamePartyUI.UpdateUI(currentRound, totalRounds, 0f, currentMinigame.Name);
            }
        }
        
        protected override void OnMinigameCompleted(Dictionary<int, int> results)
        {
            currentState = GameState.RoundResults;
            
            // Play round end sound
            if (roundEndSound != null)
                audioSource.PlayOneShot(roundEndSound);
            
            // Procesar resultados específicos del party
            ProcessResults(results);
            
            // También llamar a la implementación base para actualización básica de scores
            base.OnMinigameCompleted(results);
        }
        
        private void ProcessResults(Dictionary<int, int> results)
        {
            if (results == null || results.Count == 0)
            {
                Debug.LogWarning("No results from minigame");
                return;
            }
            
            // Find round winner (player with highest points in this round)
            int roundWinnerId = -1;
            int maxRoundPoints = -1;
            
            foreach (var result in results)
            {
                if (result.Value > maxRoundPoints)
                {
                    maxRoundPoints = result.Value;
                    roundWinnerId = result.Key;
                }
            }
            
            // Update wins for round winner
            if (roundWinnerId >= 0 && playerWins.ContainsKey(roundWinnerId))
            {
                playerWins[roundWinnerId]++;
            }
            
            Debug.Log($"Round winner: Player {roundWinnerId} with {maxRoundPoints} points");
            
            // Mostrar resultados de la ronda
            ShowRoundResults(results, roundWinnerId);
        }
        
        public override void ShowRoundResults(Dictionary<int, int> roundResults = null, int winnerId = -1)
        {
            if (minigamePartyUI != null)
            {
                if (roundResults != null && roundResults.Count > 0)
                {
                    minigamePartyUI.ShowRoundResults(currentRound, roundResults, winnerId);
                }
                else
                {
                    // Show generic round completion
                    Debug.Log("Round completed");
                }
            }
            
            // Update scores in UI
            if (minigamePartyUI != null)
            {
                minigamePartyUI.UpdateScores();
            }
        }
        
        // Este método debe ser público para que el UI pueda llamarlo
        public void OnResultsContinue()
        {
            // Hide results panel
            // (el UI se encarga de ocultar su propio panel)
            
            // Destruir minijuego actual si existe
            if (currentMinigame != null)
            {
                Destroy(currentMinigame.gameObject);
                currentMinigame = null;
            }
            
            // Start next round after a short delay
            Invoke(nameof(ContinueToNextRoundDelayed), 0.5f);
        }
        
        private void ContinueToNextRoundDelayed()
        {
            // Continue session usando el método base
            ContinueSession();
        }
        
        protected override void ContinueSession()
        {
            // En lugar de mostrar selección inmediatamente, ir a la siguiente ronda
            StartNextRound();
        }
        
        private void EndGame()
        {
            currentState = GameState.GameResults;
            
            // Play game end sound
            if (gameEndSound != null)
                audioSource.PlayOneShot(gameEndSound);
            
            // Determine overall winner
            int finalWinnerId = -1;
            int maxScore = -1;
            
            foreach (var score in sessionScores)
            {
                if (score.Value > maxScore)
                {
                    maxScore = score.Value;
                    finalWinnerId = score.Key;
                }
            }
            
            Debug.Log($"Game winner: Player {finalWinnerId} with {maxScore} points");
            
            // Mostrar resultados finales
            ShowFinalResults(sessionScores, finalWinnerId);
        }
        
        public override void ShowFinalResults(Dictionary<int, int> finalScores, int winnerId)
        {
            if (minigamePartyUI != null)
            {
                minigamePartyUI.ShowGameResults(finalScores, winnerId);
            }
        }
        
        protected override void EndSession()
        {
            // El party maneja el fin de sesión de manera diferente
            EndGame();
        }
        
        private void Update()
        {
            // Update timer if needed
            if (currentState == GameState.MinigamePlaying && currentMinigame != null)
            {
                // Actualizar UI con tiempo restante si el minijuego tiene temporizador
                if (minigamePartyUI != null)
                {
                    // Ejemplo: minigamePartyUI.UpdateUI(currentRound, totalRounds, currentMinigame.GetRemainingTime(), currentMinigame.Name);
                }
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            // Limpiar todos los tweens al destruir
            DOTween.KillAll();
        }
        
        // Métodos específicos del Party
        public int GetCurrentRound() => currentRound;
        public int GetTotalRounds() => totalRounds;
        public Dictionary<int, int> GetPlayerWins() => playerWins;
    }
}