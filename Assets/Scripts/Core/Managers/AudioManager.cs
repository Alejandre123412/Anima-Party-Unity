using UnityEngine;
using System.Collections.Generic;

namespace AnimaParty.Core
{
    public enum SoundType
    {
        Music,
        SFX,
        UI,
        Ambient
    }
    
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        public SoundType type = SoundType.SFX;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;
        public bool playOnAwake = false;
        
        [HideInInspector] public AudioSource source;
    }
    
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 1f;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float uiVolume = 1f;
        
        [Header("Sound Library")]
        [SerializeField] private List<Sound> sounds = new List<Sound>();
        
        [Header("Audio Sources")]
        [SerializeField] private GameObject audioSourcePrefab;
        
        private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();
        private List<AudioSource> activeSources = new List<AudioSource>();
        private AudioSource musicSource;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Initialize()
        {
            // Create music source
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            
            // Populate dictionary
            foreach (var sound in sounds)
            {
                if (!soundDictionary.ContainsKey(sound.name))
                {
                    soundDictionary[sound.name] = sound;
                }
                else
                {
                    Debug.LogWarning($"Duplicate sound name: {sound.name}");
                }
            }
            
            Debug.Log($"AudioManager initialized with {soundDictionary.Count} sounds");
        }
        
        public void Play(string soundName, Vector3 position = default)
        {
            if (soundDictionary.TryGetValue(soundName, out Sound sound))
            {
                AudioSource source = GetAvailableAudioSource();
                if (source == null) return;
                
                SetupAudioSource(source, sound, position);
                source.Play();
                
                // Return to pool when finished (for non-looping sounds)
                if (!sound.loop)
                {
                    StartCoroutine(ReturnToPoolAfterPlay(source, sound.clip.length));
                }
            }
            else
            {
                Debug.LogWarning($"Sound '{soundName}' not found!");
            }
        }
        
        public void PlayMusic(string musicName)
        {
            if (soundDictionary.TryGetValue(musicName, out Sound sound) && sound.type == SoundType.Music)
            {
                if (musicSource.isPlaying)
                    musicSource.Stop();
                
                SetupAudioSource(musicSource, sound, Vector3.zero);
                musicSource.Play();
            }
            else
            {
                Debug.LogWarning($"Music '{musicName}' not found!");
            }
        }
        
        public void StopMusic()
        {
            if (musicSource.isPlaying)
                musicSource.Stop();
        }
        
        public void Stop(string soundName)
        {
            foreach (var source in activeSources)
            {
                if (source != null && source.isPlaying && source.clip != null)
                {
                    var sound = FindSoundByClip(source.clip);
                    if (sound != null && sound.name == soundName)
                    {
                        source.Stop();
                    }
                }
            }
        }
        
        public void StopAllSFX()
        {
            foreach (var source in activeSources)
            {
                if (source != null && source.isPlaying)
                {
                    var sound = FindSoundByClip(source.clip);
                    if (sound != null && sound.type == SoundType.SFX)
                    {
                        source.Stop();
                    }
                }
            }
        }
        
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }
        
        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }
        
        private void UpdateAllVolumes()
        {
            // Update music source
            if (musicSource != null && musicSource.clip != null)
            {
                var sound = FindSoundByClip(musicSource.clip);
                if (sound != null)
                {
                    musicSource.volume = sound.volume * GetVolumeForType(sound.type) * masterVolume;
                }
            }
            
            // Update all active sources
            foreach (var source in activeSources)
            {
                if (source != null && source.clip != null)
                {
                    var sound = FindSoundByClip(source.clip);
                    if (sound != null)
                    {
                        source.volume = sound.volume * GetVolumeForType(sound.type) * masterVolume;
                    }
                }
            }
        }
        
        private float GetVolumeForType(SoundType type)
        {
            return type switch
            {
                SoundType.Music => musicVolume,
                SoundType.SFX => sfxVolume,
                SoundType.UI => uiVolume,
                SoundType.Ambient => musicVolume,
                _ => 1f
            };
        }
        
        private AudioSource GetAvailableAudioSource()
        {
            // Look for inactive source
            foreach (var source in activeSources)
            {
                if (source != null && !source.isPlaying)
                {
                    return source;
                }
            }
            
            // Create new source
            if (audioSourcePrefab != null)
            {
                GameObject sourceObj = Instantiate(audioSourcePrefab, transform);
                AudioSource newSource = sourceObj.GetComponent<AudioSource>();
                activeSources.Add(newSource);
                return newSource;
            }
            else
            {
                GameObject sourceObj = new GameObject("AudioSource");
                sourceObj.transform.SetParent(transform);
                AudioSource newSource = sourceObj.AddComponent<AudioSource>();
                activeSources.Add(newSource);
                return newSource;
            }
        }
        
        private void SetupAudioSource(AudioSource source, Sound sound, Vector3 position)
        {
            source.transform.position = position;
            source.clip = sound.clip;
            source.volume = sound.volume * GetVolumeForType(sound.type) * masterVolume;
            source.pitch = sound.pitch;
            source.loop = sound.loop;
            source.playOnAwake = false;
            source.spatialBlend = (position == Vector3.zero) ? 0f : 1f; // 2D or 3D
        }
        
        private Sound FindSoundByClip(AudioClip clip)
        {
            foreach (var sound in sounds)
            {
                if (sound.clip == clip)
                    return sound;
            }
            return null;
        }
        
        private System.Collections.IEnumerator ReturnToPoolAfterPlay(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay + 0.1f); // Small buffer
            if (source && !source.loop && !source.isPlaying)
            {
                // Just stop it, we'll reuse it later
                source.Stop();
                source.clip = null;
            }
        }
        
        public void AddSound(Sound newSound)
        {
            if (!soundDictionary.ContainsKey(newSound.name))
            {
                sounds.Add(newSound);
                soundDictionary[newSound.name] = newSound;
            }
        }
        
        public bool HasSound(string soundName) => soundDictionary.ContainsKey(soundName);
    }
}