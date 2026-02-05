using System;
using UnityEngine;
using System.Collections.Generic;
using AnimaParty.Core;

namespace AnimaParty.Minigames
{
    public abstract class MinigameBase : MonoBehaviour
    {
        [Header("Minigame Settings")]
        public string Name = "Minigame";

        public string Description = "";
        public float minigameDuration = 60f;
        public int maxPlayers = 4;
        public int minPlayers = 2;
        
        [Header("References")]
        public List<PlayerController> players = new List<PlayerController>();
        public Camera minigameCamera;
        
        protected Dictionary<int, int> minigameResults = new Dictionary<int, int>();
        
        public Action<Dictionary<int, int>> OnMinigameCompleted;
        protected virtual void Start()
        {
            InitializeMinigame(players);
        }
        
        public virtual void InitializeMinigame(List<PlayerController> players)
        {
            this.players = players;
            Debug.Log($"Initializing {Name} with {players.Count} players");
        }
        
        public virtual void StartMinigame()
        {
            Debug.Log($"Starting {Name}");
        }
        
        public virtual void EndMinigame()
        {
            Debug.Log($"Ending {Name}");
        }

        protected void OnCompleted()
        {
            OnMinigameCompleted.Invoke(minigameResults);
        }
        
        public virtual Dictionary<int, int> GetResults()
        {
            return new Dictionary<int, int>(minigameResults);
        }
    }
}