using UnityEngine;
using DG.Tweening;

namespace AnimaParty.Core
{
    public class CameraShake : MonoBehaviour
    {
        [Header("Shake Settings")]
        [SerializeField] private float defaultDuration = 0.5f;
        [SerializeField] private float defaultStrength = 0.5f;
        [SerializeField] private int defaultVibrato = 10;
        [SerializeField] private float defaultRandomness = 90f;
        [SerializeField] private bool fadeOut = true;
        
        private Transform cameraTransform;
        private Vector3 originalPosition;
        private Tween currentShakeTween;
        
        private void Awake()
        {
            cameraTransform = GetComponent<Transform>();
            originalPosition = cameraTransform.localPosition;
        }
        
        public void Shake(float duration = -1f, float strength = -1f, int vibrato = -1, float randomness = -1f)
        {
            // Usar valores por defecto si no se especifican
            float shakeDuration = duration > 0 ? duration : defaultDuration;
            float shakeStrength = strength > 0 ? strength : defaultStrength;
            int shakeVibrato = vibrato > 0 ? vibrato : defaultVibrato;
            float shakeRandomness = randomness > 0 ? randomness : defaultRandomness;
            
            // Cancelar shake anterior si existe
            currentShakeTween?.Kill();
            
            // Aplicar shake usando DOTween
            currentShakeTween = cameraTransform.DOShakePosition(
                shakeDuration,
                shakeStrength,
                shakeVibrato,
                shakeRandomness,
                fadeOut
            ).OnComplete(() => {
                // Restaurar posición original
                cameraTransform.localPosition = originalPosition;
            });
        }
        
        public void StopShake()
        {
            currentShakeTween?.Kill();
            cameraTransform.localPosition = originalPosition;
        }
        
        private void OnDestroy()
        {
            currentShakeTween?.Kill();
        }
    }
}