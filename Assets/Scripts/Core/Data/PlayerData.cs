using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace AnimaParty.Core
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "AnimaParty/Player Data")]
    public class PlayerData : ScriptableObject
    {
        public static PlayerData Instance = new();
        [System.Serializable]
        public class CharacterSelection
        {
            public int playerId;
            public string characterId;
            public Color playerColor;
            public int skinIndex;
        }
        
        [Header("Current Session Data")]
        public List<PlayerInfo> currentPlayers = new List<PlayerInfo>();
        public List<CharacterSelection> characterSelections = new List<CharacterSelection>();
        
        [Header("Player Profiles")]
        public List<PlayerProfile> playerProfiles = new List<PlayerProfile>();
        
        [Header("Settings")]
        public bool teamsEnabled = false;
        public int gameTimeLimit = 300; // seconds
        public int selectedMap = 0;
        
        [System.Serializable]
        public class PlayerProfile
        {
            public string profileName;
            public int totalGamesPlayed;
            public int totalWins;
            public int totalPoints;
            public float playTime; // in hours
            public List<string> unlockedCharacters = new List<string>();
            public Dictionary<string, int> characterUsage = new Dictionary<string, int>();
            public Dictionary<string, int> minigameStats = new Dictionary<string, int>();
            
            public void AddGamePlayed(bool won, int points, float timePlayed)
            {
                totalGamesPlayed++;
                if (won) totalWins++;
                totalPoints += points;
                playTime += timePlayed;
            }
            
            public void UnlockCharacter(string characterId)
            {
                if (!unlockedCharacters.Contains(characterId))
                {
                    unlockedCharacters.Add(characterId);
                }
            }
            
            public void RecordCharacterUsage(string characterId)
            {
                if (characterUsage.ContainsKey(characterId))
                    characterUsage[characterId]++;
                else
                    characterUsage[characterId] = 1;
            }
        }
        
        public void ClearCurrentSession()
        {
            currentPlayers.Clear();
            characterSelections.Clear();
            teamsEnabled = false;
            gameTimeLimit = 300;
            selectedMap = 0;
        }
        
        public void AddPlayerToSession(PlayerInfo player)
        {
            currentPlayers.Add(player);
            
            // Create default character selection
            var selection = new CharacterSelection
            {
                playerId = player.playerId,
                characterId = "default",
                playerColor = GetDefaultPlayerColor(player.playerId),
                skinIndex = 0
            };
            characterSelections.Add(selection);
        }
        
        public void SetCharacterSelection(int playerId, string characterId, Color color, int skinIndex)
        {
            var selection = characterSelections.Find(s => s.playerId == playerId);
            if (selection != null)
            {
                selection.characterId = characterId;
                selection.playerColor = color;
                selection.skinIndex = skinIndex;
            }
            else
            {
                characterSelections.Add(new CharacterSelection
                {
                    playerId = playerId,
                    characterId = characterId,
                    playerColor = color,
                    skinIndex = skinIndex
                });
            }
        }
        
        public CharacterSelection GetCharacterSelection(int playerId)
        {
            return characterSelections.Find(s => s.playerId == playerId);
        }
        
        public PlayerProfile GetOrCreateProfile(string profileName)
        {
            var profile = playerProfiles.Find(p => p.profileName == profileName);
            if (profile == null)
            {
                profile = new PlayerProfile { profileName = profileName };
                playerProfiles.Add(profile);
            }
            return profile;
        }
        
        public void SaveSessionToProfile(string profileName, bool won, int points, float timePlayed)
        {
            var profile = GetOrCreateProfile(profileName);
            profile.AddGamePlayed(won, points, timePlayed);
            
            // Record character usage for current session
            foreach (var selection in characterSelections)
            {
                profile.RecordCharacterUsage(selection.characterId);
            }
        }
        
        private Color GetDefaultPlayerColor(int playerId)
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
        
        public void SaveToFile()
        {
            // Implementation for saving to JSON or binary file
            string json = JsonUtility.ToJson(this, true);
            System.IO.File.WriteAllText(Application.persistentDataPath + "/playerdata.json", json);
            Debug.Log("Player data saved to: " + Application.persistentDataPath + "/playerdata.json");
        }
        
        public void LoadFromFile()
        {
            string path = Application.persistentDataPath + "/playerdata.json";
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(json, this);
                Debug.Log("Player data loaded from: " + path);
            }
            else
            {
                Debug.Log("No saved player data found, using defaults");
            }
        }
        
        
    }
}