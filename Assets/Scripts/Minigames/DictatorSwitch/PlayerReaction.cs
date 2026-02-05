using AnimaParty.Core;
using UnityEngine;
using DG.Tweening; // Añadido

namespace AnimaParty.Minigames.DictatorSwitch
{
    public class PlayerReaction : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int playerId = 0;
        [SerializeField] private float reactionTime = 0f;
        [SerializeField] private bool hasReacted = false;
        [SerializeField] private bool pressedEarly = false;
        
        [Header("Visual Feedback")]
        [SerializeField] private Renderer playerRenderer;
        [SerializeField] private ParticleSystem reactionParticles;
        [SerializeField] private Light reactionLight;
        
        [Header("Colors")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color readyColor = Color.green;
        [SerializeField] private Color reactedColor = Color.blue;
        [SerializeField] private Color earlyColor = Color.red;
        [SerializeField] private Color eliminatedColor = Color.gray;
        
        private DictatorSwitchController minigameController;
        private Material playerMaterial;
        private bool isEliminated = false;
        private Tween colorTween;
        private Tween lightTween;
        private Tween eliminationTween;
        
        private void Start()
        {
            Initialize();
        }
        
        public void Initialize(DictatorSwitchController controller = null)
        {
            minigameController = controller;
            
            // Get or create material
            if (playerRenderer != null)
            {
                playerMaterial = playerRenderer.material;
                playerMaterial.color = defaultColor;
            }
            
            // Initialize visual effects
            if (reactionLight != null)
            {
                reactionLight.enabled = false;
            }
            
            ResetReaction();
        }
        
        public void SetPlayerId(int id)
        {
            playerId = id;
        }
        
        public void RecordReaction(float time)
        {
            if (isEliminated) return;
            
            reactionTime = time;
            hasReacted = true;
            
            UpdateVisualFeedback(reactedColor);
            PlayReactionEffect();
            
            Debug.Log($"Player {playerId} reacted in {time:F3}s");
        }
        
        public void RecordEarlyPress()
        {
            if (isEliminated) return;
            
            pressedEarly = true;
            hasReacted = true;
            
            UpdateVisualFeedback(earlyColor);
            PlayEarlyEffect();
            
            Debug.Log($"Player {playerId} pressed too early!");
        }
        
        public void ResetReaction()
        {
            reactionTime = 0f;
            hasReacted = false;
            pressedEarly = false;
            
            if (!isEliminated)
            {
                UpdateVisualFeedback(defaultColor);
            }
            
            if (reactionLight != null)
                reactionLight.enabled = false;
        }
        
        public void Eliminate()
        {
            isEliminated = true;
            UpdateVisualFeedback(eliminatedColor);
            
            // Disable movement/input
            var playerController = GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }
            
            // Play elimination effect
            PlayEliminationEffect();
            
            Debug.Log($"Player {playerId} eliminated");
        }
        
        public void Revive()
        {
            isEliminated = false;
            ResetReaction();
            
            // Enable movement/input
            var playerController = GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = true;
            }
        }
        
        private void UpdateVisualFeedback(Color color)
        {
            // Cancel existing tweens
            colorTween?.Kill();
            lightTween?.Kill();
            
            if (playerMaterial != null)
            {
                // Smooth transition - DOTween
                colorTween = playerMaterial.DOColor(color, 0.3f);
            }
            
            if (reactionLight != null)
            {
                reactionLight.color = color;
                reactionLight.enabled = true;
                
                // Fade out light - DOTween
                lightTween = DOTween.To(
                    () => reactionLight.intensity,
                    x => reactionLight.intensity = x,
                    0f, 
                    0.5f)
                    .SetDelay(0.3f)
                    .OnComplete(() => reactionLight.enabled = false);
            }
        }
        
        private void PlayReactionEffect()
        {
            if (reactionParticles != null)
            {
                var particles = Instantiate(reactionParticles, transform.position + Vector3.up, Quaternion.identity);
                particles.Play();
                
                // Auto-destroy
                Destroy(particles.gameObject, 2f);
            }
            
            // Play sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play("Reaction", transform.position);
            }
            
            // Camera shake (if available)
            var cameraShake = Camera.main?.GetComponent<CameraShake>();
            if (cameraShake != null)
            {
                cameraShake.Shake(0.1f, 0.1f);
            }
        }
        
        private void PlayEarlyEffect()
        {
            // Cancel existing tween
            colorTween?.Kill();
            
            // Visual feedback for early press - DOTween
            if (playerMaterial != null)
            {
                playerMaterial.color = earlyColor;
                colorTween = playerMaterial.DOColor(defaultColor, 0.5f)
                    .SetEase(Ease.OutExpo);
            }
            
            // Play sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play("EarlyPress", transform.position);
            }
        }
        
        private void PlayEliminationEffect()
        {
            // Cancel existing tween
            eliminationTween?.Kill();
            
            // Shrink and fade out - DOTween
            eliminationTween = transform.DOScale(Vector3.zero, 0.5f)
                .SetEase(Ease.InBack);
                
            // Fade out material - DOTween
            if (playerMaterial != null)
            {
                playerMaterial.DOFade(0f, 0.5f);
            }
            
            // Play elimination sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.Play("Elimination", transform.position);
            }
            
            // Screen shake
            var cameraShake = Camera.main?.GetComponent<CameraShake>();
            if (cameraShake != null)
            {
                cameraShake.Shake(0.3f, 0.2f);
            }
        }
        
        public float GetReactionTime() => reactionTime;
        public bool HasReacted() => hasReacted;
        public bool PressedEarly() => pressedEarly;
        public bool IsEliminated() => isEliminated;
        public int GetPlayerId() => playerId;
        
        private void OnDestroy()
        {
            // Clean up tweens
            colorTween?.Kill();
            lightTween?.Kill();
            eliminationTween?.Kill();
            
            if (playerMaterial != null)
            {
                Destroy(playerMaterial);
            }
        }
    }
}