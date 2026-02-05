using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using AnimaParty.Core;
using DG.Tweening;

namespace AnimaParty.GameModes.MinigameParty
{
    public class MinigamePartyUI : MonoBehaviour
    {
        [Header("Main UI")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI currentMinigameText;
        
        [Header("Player Scores")]
        [SerializeField] private Transform scoreContainer;
        [SerializeField] private GameObject scorePrefab;
        
        [Header("Results Screen")]
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TextMeshProUGUI resultsTitle;
        [SerializeField] private TextMeshProUGUI resultsText;
        [SerializeField] private Button continueButton;
        
        [Header("Minigame Selection")]
        [SerializeField] private GameObject minigameSelectionPanel;
        [SerializeField] private Transform minigameSelectionContainer;
        [SerializeField] private GameObject minigameSelectionItemPrefab;
        
        [Header("Animations")]
        [SerializeField] private Animator uiAnimator;
        [SerializeField] private ParticleSystem celebrationParticles;
        
        private MinigamePartyManager partyManager;
        private Dictionary<int, PlayerScoreUI> scoreUIs = new Dictionary<int, PlayerScoreUI>();
        private Dictionary<int, Sequence> pulseSequences = new Dictionary<int, Sequence>();
        
        private class PlayerScoreUI
        {
            public GameObject uiObject;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI scoreText;
            public TextMeshProUGUI winsText;
            public Image background;
            public RectTransform rectTransform;
        }
        
        public void Initialize(MinigamePartyManager manager)
        {
            partyManager = manager;
            SetupScoreUI();
            HideAllPanels();
            
            // Subscribe to events
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        private void SetupScoreUI()
        {
            // Clear existing UI
            foreach (Transform child in scoreContainer)
            {
                Destroy(child.gameObject);
            }
            scoreUIs.Clear();
            
            // Limpia las secuencias de animación
            foreach (var sequence in pulseSequences.Values)
            {
                sequence?.Kill();
            }
            pulseSequences.Clear();
            
            // Get players from PlayerManager
            var players = PlayerManager.Instance?.players;
            if (players == null) return;
            
            // Create score UI for each player
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                GameObject scoreObj = Instantiate(scorePrefab, scoreContainer);
                
                PlayerScoreUI scoreUI = new PlayerScoreUI
                {
                    uiObject = scoreObj,
                    nameText = scoreObj.transform.Find("NameText").GetComponent<TextMeshProUGUI>(),
                    scoreText = scoreObj.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>(),
                    winsText = scoreObj.transform.Find("WinsText").GetComponent<TextMeshProUGUI>(),
                    background = scoreObj.GetComponent<Image>(),
                    rectTransform = scoreObj.GetComponent<RectTransform>()
                };
                
                scoreUI.nameText.text = player.playerName;
                scoreUI.scoreText.text = "0";
                scoreUI.winsText.text = "0";
                scoreUI.background.color = GetPlayerColor(player.playerId);
                
                scoreUIs[player.playerId] = scoreUI;
            }
        }
        
        public void UpdateUI(int currentRound, int totalRounds, float timer, string currentMinigame)
        {
            // Update round text
            if (roundText != null)
                roundText.text = $"RONDA {currentRound}/{totalRounds}";
            
            // Update timer
            if (timerText != null)
            {
                if (timer > 0)
                    timerText.text = $"{Mathf.Floor(timer / 60):00}:{timer % 60:00}";
                else
                    timerText.text = "";
            }
            
            // Update current minigame
            if (currentMinigameText != null)
                currentMinigameText.text = currentMinigame;
            
            // Update scores
            UpdateScores();
        }
        
        public void UpdateScores()
        {
            var players = PlayerManager.Instance?.players;
            if (players == null) return;
            
            foreach (var player in players)
            {
                if (scoreUIs.TryGetValue(player.playerId, out var scoreUI))
                {
                    scoreUI.scoreText.text = player.totalPoints.ToString();
                    scoreUI.winsText.text = player.wins.ToString();
                    
                    // Highlight leader
                    if (IsPlayerLeading(player.playerId))
                    {
                        scoreUI.background.color = Color.yellow;
                        PulseUI(scoreUI.rectTransform, player.playerId);
                    }
                    else
                    {
                        scoreUI.background.color = GetPlayerColor(player.playerId);
                        // Detén la animación de pulso si existe
                        if (pulseSequences.ContainsKey(player.playerId))
                        {
                            pulseSequences[player.playerId]?.Kill();
                            pulseSequences.Remove(player.playerId);
                            scoreUI.rectTransform.localScale = Vector3.one;
                        }
                    }
                }
            }
        }
        
        public void ShowRoundResults(int roundNumber, Dictionary<int, int> roundScores, int winnerId)
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(true);
                
                if (resultsTitle != null)
                    resultsTitle.text = $"Resultados Ronda {roundNumber}";
                
                if (resultsText != null)
                {
                    string results = "";
                    foreach (var score in roundScores)
                    {
                        var player = PlayerManager.Instance?.GetPlayerById(score.Key);
                        if (player != null)
                        {
                            string winnerMark = score.Key == winnerId ? "👑 " : "";
                            results += $"{winnerMark}{player.playerName}: +{score.Value} puntos\n";
                        }
                    }
                    resultsText.text = results;
                }
                
                // Play animation
                if (uiAnimator != null)
                    uiAnimator.Play("ShowResults");
                
                // Update continue button text for round results
                if (continueButton != null)
                {
                    var buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                        buttonText.text = "Continuar";
                }
            }
        }
        
        public void ShowGameResults(Dictionary<int, int> finalScores, int winnerId)
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(true);
                
                if (resultsTitle != null)
                    resultsTitle.text = "¡FIN DEL JUEGO!";
                
                if (resultsText != null)
                {
                    string results = "Puntuaciones Finales:\n\n";
                    int position = 1;
                    
                    foreach (var score in finalScores.OrderByDescending(s => s.Value))
                    {
                        var player = PlayerManager.Instance?.GetPlayerById(score.Key);
                        if (player != null)
                        {
                            string medal = position == 1 ? "🥇 " : position == 2 ? "🥈 " : position == 3 ? "🥉 " : "";
                            string winnerMark = score.Key == winnerId ? "👑 " : "";
                            results += $"{medal}{winnerMark}{player.playerName}: {score.Value} puntos\n";
                            position++;
                        }
                    }
                    resultsText.text = results;
                }
                
                // Show celebration for winner
                if (celebrationParticles != null)
                    celebrationParticles.Play();
                
                // Change continue button text
                if (continueButton != null)
                {
                    var buttonText = continueButton.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                        buttonText.text = "Volver al Menú";
                }
                
                // Play victory animation
                if (uiAnimator != null)
                    uiAnimator.Play("ShowVictory");
            }
        }
        
        public void ShowMinigameSelection(List<string> availableMinigames)
        {
            if (minigameSelectionPanel != null)
            {
                minigameSelectionPanel.SetActive(true);
                PopulateMinigameSelection(availableMinigames);
            }
        }
        
        private void PopulateMinigameSelection(List<string> minigames)
        {
            // Clear existing items
            foreach (Transform child in minigameSelectionContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create selection items
            foreach (var minigameName in minigames)
            {
                GameObject item = Instantiate(minigameSelectionItemPrefab, minigameSelectionContainer);
                var itemUI = item.GetComponent<MinigameSelectionItemUI>();
                if (itemUI != null)
                {
                    itemUI.Initialize(minigameName, () => OnMinigameSelected(minigameName));
                }
            }
        }
        
        private void OnMinigameSelected(string minigameName)
        {
            if (minigameSelectionPanel != null)
                minigameSelectionPanel.SetActive(false);
            
            // Notify MinigamePartyManager
            if (partyManager != null)
            {
                partyManager.OnMinigameSelected(minigameName);
            }
        }
        
        private void OnContinueClicked()
        {
            if (resultsPanel != null)
                resultsPanel.SetActive(false);
            
            // Notify MinigamePartyManager to continue
            if (partyManager != null)
            {
                partyManager.OnResultsContinue();
            }
        }
        
        private void PulseUI(RectTransform uiElement, int playerId)
        {
            // Si ya hay una animación para este jugador, deténla
            if (pulseSequences.ContainsKey(playerId))
            {
                pulseSequences[playerId]?.Kill();
            }
            
            // Crea una nueva secuencia de DOTween
            Sequence pulseSequence = DOTween.Sequence();
            pulseSequence
                .Append(uiElement.DOScale(Vector3.one * 1.1f, 0.5f).SetEase(Ease.InOutSine))
                .Append(uiElement.DOScale(Vector3.one, 0.5f).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo);
            
            // Guarda la secuencia para poder detenerla después
            pulseSequences[playerId] = pulseSequence;
        }
        
        private Color GetPlayerColor(int playerId)
        {
            return playerId switch
            {
                0 => new Color(1f, 0.3f, 0.3f, 0.7f), // Red
                1 => new Color(0.3f, 0.6f, 1f, 0.7f), // Blue
                2 => new Color(0.3f, 0.8f, 0.3f, 0.7f), // Green
                3 => new Color(1f, 0.8f, 0.3f, 0.7f), // Yellow
                _ => new Color(0.8f, 0.8f, 0.8f, 0.7f) // Gray
            };
        }
        
        private bool IsPlayerLeading(int playerId)
        {
            var players = PlayerManager.Instance?.players;
            if (players == null || players.Count == 0) return false;
            
            int maxPoints = players.Max(p => p.totalPoints);
            var player = players.Find(p => p.playerId == playerId);
            return player != null && player.totalPoints == maxPoints;
        }
        
        private void HideAllPanels()
        {
            if (resultsPanel != null) resultsPanel.SetActive(false);
            if (minigameSelectionPanel != null) minigameSelectionPanel.SetActive(false);
        }
        
        private void OnDestroy()
        {
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);
            
            // Limpia todas las animaciones de DOTween
            foreach (var sequence in pulseSequences.Values)
            {
                sequence?.Kill();
            }
            pulseSequences.Clear();
            
            // También puedes limpiar todos los tweens si es necesario
            DOTween.Kill(transform);
        }
    }
    
    // UI Item for minigame selection
    public class MinigameSelectionItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Button selectButton;
        
        private System.Action onSelect;
        
        public void Initialize(string minigameName, System.Action selectAction)
        {
            nameText.text = minigameName;
            onSelect = selectAction;
            
            selectButton.onClick.AddListener(OnSelect);
        }
        
        private void OnSelect()
        {
            onSelect?.Invoke();
        }
        
        private void OnDestroy()
        {
            selectButton.onClick.RemoveListener(OnSelect);
        }
    }
}