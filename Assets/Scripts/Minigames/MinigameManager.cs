using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using AnimaParty.Core;

namespace AnimaParty.Minigames
{
    public abstract class MinigameManager : MonoBehaviour
    {
        public static MinigameManager Instance { get; protected set; }
        
        [Header("Minigame Settings")]
        [SerializeField] protected List<GameObject> minigamePrefabs;
        [SerializeField] protected int minigamesPerSession = 5;
        
        [Header("Current Game")]
        [SerializeField] protected MinigameBase currentMinigame;
        [SerializeField] protected List<GameObject> playedMinigames = new List<GameObject>();
        
        [Header("UI")]
        [SerializeField] protected GameObject minigameSelectionUI;
        [SerializeField] protected Transform minigameSelectionContainer;
        [SerializeField] protected GameObject minigameSelectionPrefab;
        
        protected List<PlayerController> currentPlayers = new List<PlayerController>();
        protected Dictionary<int, int> sessionScores = new Dictionary<int, int>();
        protected bool isMinigameActive = false;
        
        protected virtual void Awake()
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
        
        public virtual void Initialize(List<PlayerController> players)
        {
            currentPlayers = players;
            sessionScores.Clear();
            
            foreach (var player in players)
            {
                sessionScores[player.PlayerId] = 0;
            }
            
            Debug.Log($"MinigameManager initialized with {players.Count} players");
        }
        
        public abstract void StartMinigameSession();
        
        protected virtual void ShowMinigameSelection()
        {
            if (minigameSelectionUI != null)
            {
                minigameSelectionUI.SetActive(true);
                PopulateMinigameSelection();
            }
            else
            {
                // Auto-select random minigame
                SelectRandomMinigame();
            }
        }
        
        protected virtual void PopulateMinigameSelection()
        {
            // Clear existing items
            foreach (Transform child in minigameSelectionContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Show available minigames
            var availableMinigames = minigamePrefabs
                .Where(m => !playedMinigames.Contains(m))
                .ToList();
                
            if (availableMinigames.Count == 0)
            {
                availableMinigames = new List<GameObject>(minigamePrefabs);
                playedMinigames.Clear();
            }
            
            foreach (var minigamePrefab in availableMinigames)
            {
                var minigame = minigamePrefab.GetComponent<MinigameBase>();
                if (minigame == null) continue;
                
                GameObject selectionItem = Instantiate(minigameSelectionPrefab, minigameSelectionContainer);
                var uiItem = selectionItem.GetComponent<MinigameSelectionItem>();
                if (uiItem)
                {
                    uiItem.Initialize(minigame.Name, minigame.Description, 
                        () => SelectMinigame(minigamePrefab));
                }
            }
        }
        
        public virtual void SelectMinigame(GameObject minigamePrefab)
        {
            if (minigameSelectionUI)
                minigameSelectionUI.SetActive(false);
            
            playedMinigames.Add(minigamePrefab);
            LoadMinigame(minigamePrefab);
        }
        
        public virtual void SelectRandomMinigame()
        {
            var availableMinigames = minigamePrefabs
                .Where(m => !playedMinigames.Contains(m))
                .ToList();
                
            if (availableMinigames.Count == 0)
            {
                availableMinigames = new List<GameObject>(minigamePrefabs);
                playedMinigames.Clear();
            }
            
            int randomIndex = Random.Range(0, availableMinigames.Count);
            SelectMinigame(availableMinigames[randomIndex]);
        }
        
        protected virtual void LoadMinigame(GameObject minigamePrefab)
        {
            if (currentMinigame != null)
            {
                Destroy(currentMinigame.gameObject);
            }
            
            GameObject minigameObj = Instantiate(minigamePrefab, transform);
            currentMinigame = minigameObj.GetComponent<MinigameBase>();
            
            if (currentMinigame != null)
            {
                currentMinigame.InitializeMinigame(currentPlayers);
                currentMinigame.OnMinigameCompleted += OnMinigameCompleted;
                
                isMinigameActive = true;
                currentMinigame.StartMinigame();
                
                Debug.Log($"Started minigame: {currentMinigame.Name}");
            }
            else
            {
                Debug.LogError("Minigame prefab doesn't have MinigameBase component!");
            }
        }
        
        protected virtual void OnMinigameCompleted(Dictionary<int, int> results)
        {
            if (currentMinigame)
            {
                currentMinigame.OnMinigameCompleted -= OnMinigameCompleted;
            }
            
            isMinigameActive = false;
            
            // Update session scores
            foreach (var result in results)
            {
                if (sessionScores.ContainsKey(result.Key))
                {
                    sessionScores[result.Key] += result.Value;
                }
                else
                {
                    sessionScores[result.Key] = result.Value;
                }
            }
            
            // Show results
            ShowMinigameResults(results);
            
            // Check if session should continue
            if (playedMinigames.Count < minigamesPerSession)
            {
                Invoke(nameof(ContinueSession), 3f);
            }
            else
            {
                Invoke(nameof(EndSession), 3f);
            }
        }
        
        protected virtual void ShowMinigameResults(Dictionary<int, int> results)
        {
            // Implement UI to show results
            Debug.Log("Minigame Results:");
            foreach (var result in results.OrderByDescending(r => r.Value))
            {
                Debug.Log($"Player {result.Key}: {result.Value} points");
            }
            
            // You could show a results screen here
        }
        
        protected virtual void ContinueSession()
        {
            ShowMinigameSelection();
        }
        
        protected virtual void EndSession()
        {
            // Show final session results
            Debug.Log("Session Ended - Final Scores:");
            foreach (var score in sessionScores.OrderByDescending(s => s.Value))
            {
                Debug.Log($"Player {score.Key}: {score.Value} total points");
            }
        }
        
        public virtual void ForceEndCurrentMinigame()
        {
            if (currentMinigame != null && isMinigameActive)
            {
                currentMinigame.EndMinigame();
                isMinigameActive = false;
            }
        }
        
        public bool IsMinigameActive() => isMinigameActive;
        public MinigameBase GetCurrentMinigame() => currentMinigame;
        
        protected virtual void OnDestroy()
        {
            if (currentMinigame != null)
            {
                currentMinigame.OnMinigameCompleted -= OnMinigameCompleted;
            }
        }
        
        // Métodos abstractos que deben ser implementados por clases derivadas
        public abstract void ShowRoundResults(Dictionary<int, int> roundResults = null, int winnerId = -1);
        public abstract void ShowFinalResults(Dictionary<int, int> finalScores, int winnerId);
    }
    
    // UI Item for minigame selection
    public class MinigameSelectionItem : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI nameText;
        [SerializeField] private TMPro.TextMeshProUGUI descriptionText;
        [SerializeField] private UnityEngine.UI.Button selectButton;
        
        private System.Action onSelect;
        
        public void Initialize(string minigameName, string description, System.Action selectAction)
        {
            nameText.text = minigameName;
            descriptionText.text = description;
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