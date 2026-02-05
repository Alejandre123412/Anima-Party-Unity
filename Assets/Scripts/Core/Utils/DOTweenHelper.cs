using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

namespace AnimaParty.Utils
{
    public static class DOTweenHelper
    {
        // === MOVIMIENTO ===
        public static Tween DOMovePunch(this Transform transform, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            return transform.DOPunchPosition(punch, duration, vibrato, elasticity);
        }
        
        public static Tween DOMoveShake(this Transform transform, float strength, float duration, int vibrato = 10, float randomness = 90f)
        {
            return transform.DOShakePosition(duration, strength, vibrato, randomness);
        }
        
        // === ESCALA ===
        public static Tween DOScalePunch(this Transform transform, float punchScale, float duration)
        {
            return transform.DOPunchScale(Vector3.one * punchScale, duration, 10, 1f);
        }
        
        public static Tween DOScaleBounce(this GameObject obj, float targetScale, float duration)
        {
            return obj.transform.DOScale(targetScale, duration)
                .SetEase(Ease.OutBounce);
        }
        
        // === UI ===
        public static Tween DOFadeText(this TextMeshProUGUI text, float targetAlpha, float duration)
        {
            return text.DOFade(targetAlpha, duration);
        }
        
        public static Tween DOColorPulse(this Image image, Color targetColor, float duration)
        {
            return image.DOColor(targetColor, duration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        
        public static Tween DOFillAmount(this Image image, float targetFill, float duration)
        {
            return image.DOFillAmount(targetFill, duration)
                .SetEase(Ease.OutQuad);
        }
        
        // === VALORES NUMÉRICOS ===
        public static Tween DOValue(this System.Action<float> setter, float startValue, float endValue, float duration)
        {
            return DOTween.To(() => startValue, x => setter(x), endValue, duration);
        }
        
        // === EFECTOS ESPECIALES PARA DICTATOR SWITCH ===
        public static Tween DOReactionEffect(this Transform playerTransform, float reactionTime)
        {
            Sequence seq = DOTween.Sequence();
            
            // 1. Escala rápida
            seq.Append(playerTransform.DOScale(1.2f, 0.1f));
            
            // 2. Color verde (reacción buena)
            var renderer = playerTransform.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color originalColor = renderer.material.color;
                seq.Join(renderer.material.DOColor(Color.green, 0.1f));
                
                // 3. Volver al color original
                seq.AppendInterval(0.3f);
                seq.Append(renderer.material.DOColor(originalColor, 0.2f));
            }
            
            // 4. Volver a escala normal
            seq.Join(playerTransform.DOScale(1f, 0.2f));
            
            return seq;
        }
        
        public static Tween DOEarlyPressEffect(this Transform playerTransform)
        {
            Sequence seq = DOTween.Sequence();
            
            // 1. Temblar (shake)
            seq.Append(playerTransform.DOShakePosition(0.5f, 0.3f, 20, 90f));
            
            // 2. Color rojo
            var renderer = playerTransform.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color originalColor = renderer.material.color;
                seq.Join(renderer.material.DOColor(Color.red, 0.2f));
                
                // 3. Volver al color original
                seq.AppendInterval(0.3f);
                seq.Append(renderer.material.DOColor(originalColor, 0.3f));
            }
            
            return seq;
        }
        
        public static Tween DOEliminationEffect(this Transform playerTransform)
        {
            Sequence seq = DOTween.Sequence();
            
            // 1. Escalar a 0
            seq.Append(playerTransform.DOScale(0f, 0.8f)
                .SetEase(Ease.InBack));
            
            // 2. Rotar mientras desaparece
            seq.Join(playerTransform.DORotate(new Vector3(0, 360, 0), 0.8f, RotateMode.FastBeyond360)
                .SetEase(Ease.InOutCubic));
            
            // 3. Fade out si tiene renderer
            var renderer = playerTransform.GetComponent<Renderer>();
            if (renderer != null)
            {
                seq.Join(renderer.material.DOFade(0f, 0.8f));
            }
            
            return seq;
        }
        
        public static Tween DOPromptAppear(this Transform promptTransform)
        {
            Sequence seq = DOTween.Sequence();
            
            // Aparecer desde escala 0
            promptTransform.localScale = Vector3.zero;
            
            seq.Append(promptTransform.DOScale(1.2f, 0.3f)
                .SetEase(Ease.OutBack));
            
            seq.Append(promptTransform.DOScale(1f, 0.1f));
            
            // Efecto de pulso continuo
            seq.Append(promptTransform.DOScale(1.1f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo));
            
            return seq;
        }
        
        public static Tween DOWinnerEffect(this Transform winnerTransform)
        {
            Sequence seq = DOTween.Sequence();
            
            // 1. Salto
            seq.Append(winnerTransform.DOJump(
                winnerTransform.position + Vector3.up * 2f,
                2f, // altura
                3,  // número de saltos
                2f  // duración
            ).SetEase(Ease.OutQuad));
            
            // 2. Rotación continua
            seq.Join(winnerTransform.DORotate(
                new Vector3(0, 360, 0),
                2f,
                RotateMode.FastBeyond360
            ).SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental));
            
            // 3. Cambio de color arcoíris
            var renderer = winnerTransform.GetComponent<Renderer>();
            if (renderer != null)
            {
                seq.Join(renderer.material.DOColor(
                    Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f),
                    0.5f
                ).SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine));
            }
            
            return seq;
        }
    }
}