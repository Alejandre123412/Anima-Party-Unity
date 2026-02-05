using UnityEngine;
using System.Collections.Generic;

namespace AnimaParty.Core
{
    [CreateAssetMenu(fileName = "CharacterData", menuName = "AnimaParty/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [System.Serializable]
        public class CharacterInfo
        {
            public string characterId;
            public string displayName;
            public GameObject prefab;
            public Sprite icon;
            public Color defaultColor = Color.white;
            
            [Header("Stats")]
            [Range(1, 10)] public int speed = 5;
            [Range(1, 10)] public int agility = 5;
            [Range(1, 10)] public int strength = 5;
            [Range(1, 10)] public int luck = 5;
            
            [Header("Unlock Requirements")]
            public bool unlockedByDefault = true;
            public int unlockCost = 1000; // Points needed to unlock
            public string unlockDescription = "";
            
            [Header("Skins")]
            public List<Material> skinMaterials = new List<Material>();
            public List<Color> colorOptions = new List<Color>();
            
            [TextArea(3, 5)]
            public string description = "";
            
            public bool IsUnlocked()
            {
                if (unlockedByDefault) return true;
                
                // Check player data for unlock status
                if (PlayerData.Instance != null)
                {
                    var profile = PlayerData.Instance.GetOrCreateProfile("default");
                    return profile.unlockedCharacters.Contains(characterId);
                }
                
                return false;
            }
            
            public int GetUsageCount()
            {
                if (PlayerData.Instance != null)
                {
                    var profile = PlayerData.Instance.GetOrCreateProfile("default");
                    if (profile.characterUsage.TryGetValue(characterId, out int count))
                        return count;
                }
                return 0;
            }
        }
        
        [Header("Character List")]
        public List<CharacterInfo> characters = new List<CharacterInfo>();
        
        [Header("Default Character")]
        public string defaultCharacterId = "mii_default";
        
        private Dictionary<string, CharacterInfo> characterDictionary = new Dictionary<string, CharacterInfo>();
        
        public void Initialize()
        {
            characterDictionary.Clear();
            foreach (var character in characters)
            {
                if (!characterDictionary.ContainsKey(character.characterId))
                {
                    characterDictionary[character.characterId] = character;
                }
                else
                {
                    Debug.LogWarning($"Duplicate character ID: {character.characterId}");
                }
            }
        }
        
        public CharacterInfo GetCharacter(string characterId)
        {
            if (characterDictionary.TryGetValue(characterId, out CharacterInfo character))
            {
                return character;
            }
            
            Debug.LogWarning($"Character '{characterId}' not found, returning default");
            return GetDefaultCharacter();
        }
        
        public CharacterInfo GetDefaultCharacter()
        {
            return GetCharacter(defaultCharacterId);
        }
        
        public List<CharacterInfo> GetAllCharacters()
        {
            return new List<CharacterInfo>(characters);
        }
        
        public List<CharacterInfo> GetUnlockedCharacters()
        {
            return characters.FindAll(c => c.IsUnlocked());
        }
        
        public GameObject GetCharacterPrefab(string characterId, int skinIndex = 0, Color? color = null)
        {
            var character = GetCharacter(characterId);
            if (character == null || character.prefab == null) return null;
            
            // Instantiate the prefab
            GameObject instance = Instantiate(character.prefab);
            
            // Apply skin material if available
            if (skinIndex >= 0 && skinIndex < character.skinMaterials.Count)
            {
                var renderers = instance.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.material.name.Contains("Skin") || 
                        renderer.gameObject.name.Contains("Body"))
                    {
                        renderer.material = character.skinMaterials[skinIndex];
                    }
                }
            }
            
            // Apply color if specified
            if (color.HasValue)
            {
                var colorRenderers = instance.GetComponentsInChildren<Renderer>();
                foreach (var renderer in colorRenderers)
                {
                    if (renderer.material.HasProperty("_Color"))
                    {
                        renderer.material.color = color.Value;
                    }
                }
            }
            
            // Add character component
            var characterComponent = instance.AddComponent<Character>();
            characterComponent.characterInfo = character;
            characterComponent.skinIndex = skinIndex;
            
            return instance;
        }
        
        public void UnlockCharacter(string characterId)
        {
            var character = GetCharacter(characterId);
            if (character != null && !character.unlockedByDefault)
            {
                if (PlayerData.Instance != null)
                {
                    var profile = PlayerData.Instance.GetOrCreateProfile("default");
                    profile.UnlockCharacter(characterId);
                    PlayerData.Instance.SaveToFile();
                    
                    Debug.Log($"Character '{characterId}' unlocked!");
                }
            }
        }
        
        public bool CanAffordCharacter(string characterId, int points)
        {
            var character = GetCharacter(characterId);
            if (character == null) return false;
            
            return points >= character.unlockCost && !character.IsUnlocked();
        }
        
        public int GetTotalCharacters() => characters.Count;
        public int GetUnlockedCount() => GetUnlockedCharacters().Count;
    }
    
    // Character MonoBehaviour component
    public class Character : MonoBehaviour
    {
        public CharacterData.CharacterInfo characterInfo;
        public int skinIndex = 0;
        public Color characterColor = Color.white;
        
        private void Start()
        {
            // Register with PlayerManager if needed
            if (PlayerManager.Instance != null)
            {
                // PlayerManager handles character registration
            }
        }
        
        public float GetSpeedMultiplier()
        {
            return characterInfo.speed / 5f; // Normalized to around 1.0
        }
        
        public float GetAgilityMultiplier()
        {
            return characterInfo.agility / 5f;
        }
        
        public float GetStrengthMultiplier()
        {
            return characterInfo.strength / 5f;
        }
        
        public float GetLuckMultiplier()
        {
            return characterInfo.luck / 5f;
        }
    }
}