using UnityEngine;
using System.Collections.Generic;

namespace AnimaParty.Core
{
    [System.Serializable]
    public class PlayerInfo
    {
        public int playerId;
        public string playerName;
        public int controllerId;
        public int characterIndex;
        public bool isAI;
        public int totalPoints;
        public int wins;
        public bool isEliminated;
        
        public PlayerInfo(int id, string name, int controllerId, bool ai = false)
        {
            playerId = id;
            playerName = name;
            this.controllerId = controllerId;
            isAI = ai;
            characterIndex = 0;
            totalPoints = 0;
            wins = 0;
            isEliminated = false;
        }
    }
    
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }
        
        [Header("Player Settings")]
        public List<PlayerInfo> players = new List<PlayerInfo>();
        public List<GameObject> characterPrefabs;
        
        [Header("Current Players")]
        public List<GameObject> activePlayerObjects = new List<GameObject>();
        
        private void Awake()
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
        
        public void AddPlayer(int controllerId, string name = "")
        {
            if (players.Count >= GameManager.Instance.maxPlayers)
            {
                Debug.LogWarning("Maximum players reached");
                return;
            }
            
            if (string.IsNullOrEmpty(name))
                name = $"Player {players.Count + 1}";
            
            var newPlayer = new PlayerInfo(players.Count, name, controllerId);
            players.Add(newPlayer);
            
            Debug.Log($"Player added: {name} (Controller: {controllerId})");
        }
        
        public void AddAIPlayer()
        {
            if (players.Count >= GameManager.Instance.maxPlayers)
            {
                Debug.LogWarning("Maximum players reached");
                return;
            }
            
            var aiPlayer = new PlayerInfo(players.Count, $"AI {players.Count + 1}", -1, true);
            players.Add(aiPlayer);
            
            Debug.Log($"AI Player added: {aiPlayer.playerName}");
        }
        
        public void ClearPlayers()
        {
            players.Clear();
            foreach (var playerObj in activePlayerObjects)
            {
                if (playerObj != null)
                    Destroy(playerObj);
            }
            activePlayerObjects.Clear();
            
            Debug.Log("All players cleared");
        }
        
        public PlayerInfo GetPlayerById(int id)
        {
            return players.Find(p => p.playerId == id);
        }
        
        public void SpawnPlayersInScene(Transform spawnPoint, float spacing = 2f)
        {
            if (characterPrefabs.Count == 0)
            {
                Debug.LogError("No character prefabs assigned!");
                return;
            }
            
            ClearActivePlayers();
            
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                
                // Get character prefab
                int charIndex = Mathf.Clamp(player.characterIndex, 0, characterPrefabs.Count - 1);
                GameObject characterPrefab = characterPrefabs[charIndex];
                
                // Calculate spawn position
                Vector3 spawnPos = spawnPoint.position + new Vector3(i * spacing - (players.Count - 1) * spacing / 2f, 0, 0);
                
                // Instantiate player
                GameObject playerObj = Instantiate(characterPrefab, spawnPos, Quaternion.identity);
                playerObj.name = $"Player_{player.playerId}_{player.playerName}";
                
                // Setup player controller
                var playerController = playerObj.AddComponent<PlayerController>();
                playerController.Initialize(player);
                
                activePlayerObjects.Add(playerObj);
                
                Debug.Log($"Spawned {player.playerName} at position {spawnPos}");
            }
        }
        
        private void ClearActivePlayers()
        {
            foreach (var playerObj in activePlayerObjects)
            {
                if (playerObj != null)
                    Destroy(playerObj);
            }
            activePlayerObjects.Clear();
        }
        
        public void UpdatePlayerScore(int playerId, int points)
        {
            var player = GetPlayerById(playerId);
            if (player != null)
            {
                player.totalPoints += points;
                Debug.Log($"Player {player.playerName} now has {player.totalPoints} points");
            }
        }
    }
}