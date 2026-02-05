using UnityEngine;

namespace AnimaParty.Core
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "AnimaParty/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Graphics Settings")]
        public Resolution resolution = new Resolution { width = 1920, height = 1080 };
        public bool fullscreen = true;
        public int qualityLevel = 2; // 0=Low, 1=Medium, 2=High
        public float brightness = 1f;
        public bool vsync = true;
        
        [Header("Audio Settings")]
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public float uiVolume = 1f;
        
        [Header("Gameplay Settings")]
        public bool showTutorials = true;
        public bool rumbleEnabled = true;
        public float cameraSensitivity = 1f;
        public bool invertYAxis = false;
        
        [Header("Minigame Party Settings")]
        public int defaultRounds = 5;
        public string defaultMinigameType = "Todos";
        public bool randomMinigames = true;
        public bool eliminationMode = false;
        public bool allowPowerUps = true;
        public bool suddenDeath = true;
        
        [Header("Control Settings")]
        public KeyCode[] playerKeys = new KeyCode[]
        {
            KeyCode.Z, // Player 1 Action
            KeyCode.X, // Player 2 Action
            KeyCode.C, // Player 3 Action
            KeyCode.V  // Player 4 Action
        };
        
        public void ApplyGraphicsSettings()
        {
            Screen.SetResolution(resolution.width, resolution.height, fullscreen);
            QualitySettings.SetQualityLevel(qualityLevel, true);
            QualitySettings.vSyncCount = vsync ? 1 : 0;
            
            // Apply brightness if supported
            if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat)
            {
                RenderSettings.ambientLight = new Color(brightness, brightness, brightness, 1f);
            }
        }
        
        public void ApplyAudioSettings()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(masterVolume);
                AudioManager.Instance.SetMusicVolume(musicVolume);
                AudioManager.Instance.SetSFXVolume(sfxVolume);
                AudioManager.Instance.SetUIVolume(uiVolume);
            }
        }
        
        public void SaveToFile()
        {
            string json = JsonUtility.ToJson(this, true);
            System.IO.File.WriteAllText(Application.persistentDataPath + "/gamesettings.json", json);
            Debug.Log("Game settings saved");
        }
        
        public void LoadFromFile()
        {
            string path = Application.persistentDataPath + "/gamesettings.json";
            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                JsonUtility.FromJsonOverwrite(json, this);
                Debug.Log("Game settings loaded");
                
                // Apply loaded settings
                ApplyGraphicsSettings();
                ApplyAudioSettings();
            }
            else
            {
                Debug.Log("No saved settings found, using defaults");
                ApplyGraphicsSettings();
                ApplyAudioSettings();
            }
        }
        
        public Resolution[] GetAvailableResolutions()
        {
            return Screen.resolutions;
        }
        
        public void SetResolution(int width, int height)
        {
            resolution.width = width;
            resolution.height = height;
            Screen.SetResolution(width, height, fullscreen);
        }
        
        public void ToggleFullscreen()
        {
            fullscreen = !fullscreen;
            Screen.fullScreen = fullscreen;
        }
        
        public void SetQualityLevel(int level)
        {
            qualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(qualityLevel, true);
        }
    }
}