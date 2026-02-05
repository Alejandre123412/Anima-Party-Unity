using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening; // Añadido

namespace AnimaParty.Minigames.DictatorSwitch
{
    public class DictatorSwitchUI : MonoBehaviour
    {
        [Header("Main UI")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI statusText;
        
        [Header("Player UI")]
        [SerializeField] private Transform playerStatusContainer;
        [SerializeField] private GameObject playerStatusPrefab;
        
        [Header("Effects")]
        [SerializeField] private Image promptBackground;
        [SerializeField] private ParticleSystem promptParticles;
        [SerializeField] private ParticleSystem eliminationParticles;
        
        [Header("Colors")]
        [SerializeField] private Color waitingColor = Color.red;
        [SerializeField] private Color readyColor = Color.green;
        [SerializeField] private Color[] playerColors;
        
        private DictatorSwitchController minigameController;
        private Sequence backgroundPulseSequence;
        private Sequence rainbowColorSequence;
        private Tween statusTextTween;
        private Tween playerUITween;
        
        public void Initialize(DictatorSwitchController controller)
        {
            minigameController = controller;
            SetupPlayerUI();
            HideAllUI();
        }
        
        private void SetupPlayerUI()
        {
            // Clear existing UI
            foreach (Transform child in playerStatusContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create UI for each player
            var players = minigameController.players;
            
            for (int i = 0; i < players.Count; i++)
            {
                GameObject statusObj = Instantiate(playerStatusPrefab, playerStatusContainer);
                // Resto del código...
            }
        }
        
        public void UpdateUI(int currentRound, int totalRounds, float timer, string phase)
        {
            // Update round text
            if (roundText != null)
                roundText.text = $"RONDA {currentRound}/{totalRounds}";
            
            // Update timer based on phase
            if (timerText != null)
            {
                switch (phase)
                {
                    case "Waiting":
                        timerText.text = $"Siguiente: {timer:F1}s";
                        timerText.color = Color.yellow;
                        break;
                    case "PromptVisible":
                        timerText.text = $"¡Reacciona! {timer:F2}s";
                        timerText.color = Color.green;
                        break;
                    case "Elimination":
                        timerText.text = "Eliminando...";
                        timerText.color = Color.red;
                        break;
                    default:
                        timerText.text = "";
                        break;
                }
            }
            
            // Update countdown
            if (countdownText != null && phase == "Waiting")
            {
                countdownText.text = Mathf.Ceil(timer).ToString();
                countdownText.gameObject.SetActive(true);
                
                // Pulse effect
                float scale = 1f + Mathf.Sin(Time.time * 5f) * 0.2f;
                countdownText.transform.localScale = Vector3.one * scale;
            }
            else if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
            }
        }
        
        public void ShowPrompt()
        {
            if (promptText != null)
            {
                promptText.text = "¡PRESIONA A!";
                promptText.gameObject.SetActive(true);
                
                // Animation - DOTween
                promptText.transform.localScale = Vector3.zero;
                promptText.transform.DOScale(Vector3.one, 0.3f)
                    .SetEase(Ease.OutBack);
            }
            
            if (promptBackground != null)
            {
                promptBackground.gameObject.SetActive(true);
                promptBackground.color = waitingColor;
                
                // Cancel existing pulse sequence
                backgroundPulseSequence?.Kill();
                
                // Pulse animation - DOTween
                backgroundPulseSequence = DOTween.Sequence();
                backgroundPulseSequence
                    .Append(promptBackground.DOColor(readyColor, 0.5f).SetEase(Ease.InOutSine))
                    .Append(promptBackground.DOColor(waitingColor, 0.5f).SetEase(Ease.InOutSine))
                    .SetLoops(-1, LoopType.Yoyo);
            }
            
            if (promptParticles != null)
                promptParticles.Play();
        }
        
        public void HidePrompt()
        {
            if (promptText != null)
            {
                promptText.gameObject.SetActive(false);
            }
            
            if (promptBackground != null)
            {
                promptBackground.gameObject.SetActive(false);
                backgroundPulseSequence?.Kill();
            }
            
            if (promptParticles != null)
                promptParticles.Stop();
        }
        
        public void UpdatePlayerStatus(GameObject playerUIObject, string status, Color color)
        {
            if (playerUIObject != null)
            {
                // Cancel existing tween
                playerUITween?.Kill();
                
                // Pulse effect for status change - DOTween
                playerUITween = playerUIObject.transform.DOScale(Vector3.one * 1.2f, 0.2f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        playerUIObject.transform.DOScale(Vector3.one, 0.1f);
                    });
            }
        }
        
        public void ShowElimination(GameObject playerUIObject, string reason)
        {
            if (playerUIObject != null)
            {
                // Get components
                var nameText = playerUIObject.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                var statusText = playerUIObject.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
                var background = playerUIObject.GetComponent<Image>();
                
                if (nameText != null)
                    nameText.fontStyle = TMPro.FontStyles.Strikethrough;
                
                if (statusText != null)
                    statusText.text = "ELIMINADO";
                
                if (background != null)
                    background.color = Color.gray;
                
                // Show elimination effect
                if (eliminationParticles != null)
                {
                    Instantiate(eliminationParticles, 
                        playerUIObject.transform.position, 
                        Quaternion.identity, 
                        playerUIObject.transform);
                }
            }
            
            // Show status message
            if (statusText != null)
            {
                statusText.text = reason;
                statusText.gameObject.SetActive(true);
                statusText.color = Color.red;
                
                // Cancel existing tween
                statusTextTween?.Kill();
                
                // Fade out - DOTween
                statusTextTween = statusText.DOFade(0f, 1f)
                    .SetDelay(1.5f)
                    .OnComplete(() => statusText.gameObject.SetActive(false));
            }
        }
        
        public void ShowWinner(int playerIndex)
        {
            if (promptText != null)
            {
                promptText.text = $"¡GANADOR!\nJugador {playerIndex + 1}";
                promptText.gameObject.SetActive(true);
                promptText.color = Color.yellow;
                
                // Celebration animation - DOTween
                promptText.transform.DOScale(Vector3.one * 1.5f, 0.5f)
                    .SetEase(Ease.OutElastic);
                
                // Cancel existing rainbow sequence
                rainbowColorSequence?.Kill();
                
                // Rainbow color effect - DOTween
                rainbowColorSequence = DOTween.Sequence();
                float durationPerColor = 0.33f; // 3 colores por segundo aprox
                
                // Añadir varios colores en secuencia
                rainbowColorSequence.Append(promptText.DOColor(Color.red, durationPerColor));
                rainbowColorSequence.Append(promptText.DOColor(Color.yellow, durationPerColor));
                rainbowColorSequence.Append(promptText.DOColor(Color.green, durationPerColor));
                rainbowColorSequence.Append(promptText.DOColor(Color.cyan, durationPerColor));
                rainbowColorSequence.Append(promptText.DOColor(Color.blue, durationPerColor));
                rainbowColorSequence.Append(promptText.DOColor(Color.magenta, durationPerColor));
                
                rainbowColorSequence.SetLoops(-1, LoopType.Restart);
            }
            
            // Highlight winner in player UI
            // (Nota: Necesitarías una referencia al GameObject del jugador UI)
            /*
            if (playerUIObject != null)
            {
                var statusText = playerUIObject.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
                var background = playerUIObject.GetComponent<Image>();
                
                if (statusText != null)
                    statusText.text = "¡GANADOR!";
                
                if (background != null)
                    background.color = Color.yellow;
                
                // Celebration effect
                playerUIObject.transform.DOScale(Vector3.one * 1.3f, 0.3f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
            */
        }
        
        private Color GetPlayerColor(int index)
        {
            if (index >= 0 && index < playerColors.Length)
                return playerColors[index];
            
            return index switch
            {
                0 => Color.red,
                1 => Color.blue,
                2 => Color.green,
                3 => Color.yellow,
                _ => Color.white
            };
        }
        
        private void HideAllUI()
        {
            if (promptText != null) promptText.gameObject.SetActive(false);
            if (promptBackground != null) promptBackground.gameObject.SetActive(false);
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (statusText != null) statusText.gameObject.SetActive(false);
        }
        
        public void SetTitle(string title)
        {
            if (titleText != null)
                titleText.text = title;
        }
        
        private void OnDestroy()
        {
            // Clean up sequences and tweens
            backgroundPulseSequence?.Kill();
            rainbowColorSequence?.Kill();
            statusTextTween?.Kill();
            playerUITween?.Kill();
            
            DOTween.Kill(promptText);
            DOTween.Kill(promptBackground);
            DOTween.Kill(statusText);
        }
    }
}