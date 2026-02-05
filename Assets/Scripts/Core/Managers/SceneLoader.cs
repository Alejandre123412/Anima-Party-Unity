using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using DG.Tweening;
using System;

namespace AnimaParty.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }
        
        [Header("UI References")]
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private UnityEngine.UI.Slider progressBar;
        [SerializeField] private TextMeshProUGUI tipText;
        [SerializeField] private CanvasGroup loadingCanvasGroup;
        
        [Header("Settings")]
        [SerializeField] private float minimumLoadTime = 1f;
        [SerializeField] private string[] loadingTips;
        [SerializeField] private float fadeDuration = 0.3f;
        
        // Evento para notificar cuando se carga una escena
        public event Action<string> OnSceneLoadedEvent;
        
        private AsyncOperation currentLoadingOperation;
        private bool isLoading = false;
        private Coroutine currentLoadCoroutine;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Configurar canvas group si existe
                if (loadingCanvasGroup == null && loadingScreen != null)
                {
                    loadingCanvasGroup = loadingScreen.GetComponent<CanvasGroup>();
                    if (loadingCanvasGroup == null)
                        loadingCanvasGroup = loadingScreen.AddComponent<CanvasGroup>();
                }
                
                // Inicializar con alpha 0
                if (loadingCanvasGroup != null)
                    loadingCanvasGroup.alpha = 0f;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            // Suscribirse al evento de SceneManager para detectar cambios de escena
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }
        
        public void LoadScene(string sceneName, bool showLoadingScreen = true)
        {
            if (isLoading) 
            {
                Debug.LogWarning("Already loading a scene!");
                return;
            }
            
            if (currentLoadCoroutine != null)
                StopCoroutine(currentLoadCoroutine);
            
            currentLoadCoroutine = StartCoroutine(LoadSceneAsync(sceneName, showLoadingScreen));
        }
        
        public void LoadScene(int sceneIndex, bool showLoadingScreen = true)
        {
            if (isLoading) 
            {
                Debug.LogWarning("Already loading a scene!");
                return;
            }
            
            if (currentLoadCoroutine != null)
                StopCoroutine(currentLoadCoroutine);
            
            currentLoadCoroutine = StartCoroutine(LoadSceneAsync(sceneIndex, showLoadingScreen));
        }
        
        private IEnumerator LoadSceneAsync(string sceneName, bool showLoadingScreen)
        {
            isLoading = true;
            
            // Notificar que la escena está empezando a cargar
            OnSceneLoadingStarted?.Invoke(sceneName);
            
            if (showLoadingScreen)
            {
                yield return StartCoroutine(ShowLoadingScreen());
                yield return new WaitForSeconds(0.3f);
            }
            
            float startTime = Time.time;
            currentLoadingOperation = SceneManager.LoadSceneAsync(sceneName);
            currentLoadingOperation.allowSceneActivation = false;
            
            // Show initial progress
            if (loadingText != null)
                loadingText.text = "Preparando... 0%";
            
            if (progressBar != null)
                progressBar.value = 0f;
            
            while (!currentLoadingOperation.isDone)
            {
                float realProgress = Mathf.Clamp01(currentLoadingOperation.progress / 0.9f);
                float elapsedTime = Time.time - startTime;
                
                // Use fake progress during minimum load time
                float displayProgress = realProgress;
                if (elapsedTime < minimumLoadTime)
                {
                    float fakeProgress = elapsedTime / minimumLoadTime;
                    displayProgress = Mathf.Min(realProgress, fakeProgress * 0.9f);
                }
                
                // Update UI
                if (progressBar != null)
                    progressBar.value = displayProgress;
                    
                if (loadingText != null)
                    loadingText.text = $"Cargando... {Mathf.Round(displayProgress * 100)}%";
                
                // Check if we can activate the scene
                bool canActivate = currentLoadingOperation.progress >= 0.9f;
                bool minTimePassed = elapsedTime >= minimumLoadTime;
                
                if (canActivate && minTimePassed)
                {
                    // Actualizar a 100% antes de activar
                    if (progressBar != null)
                        progressBar.value = 1f;
                    
                    if (loadingText != null)
                        loadingText.text = "¡Listo!";
                    
                    yield return new WaitForSeconds(0.2f);
                    currentLoadingOperation.allowSceneActivation = true;
                }
                else if (canActivate && !minTimePassed)
                {
                    if (loadingText != null)
                        loadingText.text = $"Listo! {Mathf.Round((minimumLoadTime - elapsedTime) * 10) / 10f}s";
                }
                
                yield return null;
            }
            
            if (showLoadingScreen)
            {
                yield return StartCoroutine(HideLoadingScreen());
            }
            
            isLoading = false;
            currentLoadCoroutine = null;
            
            Debug.Log($"Scene loaded successfully: {sceneName}");
        }
        
        private IEnumerator LoadSceneAsync(int sceneIndex, bool showLoadingScreen)
        {
            isLoading = true;
            
            string sceneName = SceneManager.GetSceneByBuildIndex(sceneIndex).name;
            OnSceneLoadingStarted?.Invoke(sceneName);
            
            if (showLoadingScreen)
            {
                yield return StartCoroutine(ShowLoadingScreen());
                yield return new WaitForSeconds(0.3f);
            }
            
            float startTime = Time.time;
            currentLoadingOperation = SceneManager.LoadSceneAsync(sceneIndex);
            currentLoadingOperation.allowSceneActivation = false;
            
            if (loadingText != null)
                loadingText.text = "Preparando... 0%";
            
            if (progressBar != null)
                progressBar.value = 0f;
            
            while (!currentLoadingOperation.isDone)
            {
                float realProgress = Mathf.Clamp01(currentLoadingOperation.progress / 0.9f);
                float elapsedTime = Time.time - startTime;
                
                float displayProgress = realProgress;
                if (elapsedTime < minimumLoadTime)
                {
                    float fakeProgress = elapsedTime / minimumLoadTime;
                    displayProgress = Mathf.Min(realProgress, fakeProgress * 0.9f);
                }
                
                if (progressBar != null)
                    progressBar.value = displayProgress;
                    
                if (loadingText != null)
                    loadingText.text = $"Cargando... {Mathf.Round(displayProgress * 100)}%";
                
                bool canActivate = currentLoadingOperation.progress >= 0.9f;
                bool minTimePassed = elapsedTime >= minimumLoadTime;
                
                if (canActivate && minTimePassed)
                {
                    if (progressBar != null)
                        progressBar.value = 1f;
                    
                    if (loadingText != null)
                        loadingText.text = "¡Listo!";
                    
                    yield return new WaitForSeconds(0.2f);
                    currentLoadingOperation.allowSceneActivation = true;
                }
                else if (canActivate && !minTimePassed)
                {
                    if (loadingText != null)
                        loadingText.text = $"Listo! {Mathf.Round((minimumLoadTime - elapsedTime) * 10) / 10f}s";
                }
                
                yield return null;
            }
            
            if (showLoadingScreen)
            {
                yield return StartCoroutine(HideLoadingScreen());
            }
            
            isLoading = false;
            currentLoadCoroutine = null;
            
            Debug.Log($"Scene loaded successfully: {sceneName}");
        }
        
        private IEnumerator ShowLoadingScreen()
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
                
                // Fade in animation with DOTween
                if (loadingCanvasGroup != null)
                {
                    loadingCanvasGroup.alpha = 0f;
                    loadingCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad);
                    yield return new WaitForSeconds(fadeDuration);
                }
                
                // Show random tip
                if (tipText != null && loadingTips != null && loadingTips.Length > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, loadingTips.Length);
                    tipText.text = loadingTips[randomIndex];
                    
                    // Optional: Fade in tip text
                    Color originalColor = tipText.color;
                    tipText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
                    tipText.DOFade(1f, 0.5f);
                }
                
                // Reset progress bar
                if (progressBar != null)
                {
                    progressBar.value = 0f;
                    // Optional: Pulse animation
                    progressBar.transform.DOScale(1.05f, 0.5f)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                }
            }
        }
        
        private IEnumerator HideLoadingScreen()
        {
            if (loadingScreen != null)
            {
                // Fade out animation with DOTween
                if (loadingCanvasGroup != null)
                {
                    loadingCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad);
                    yield return new WaitForSeconds(fadeDuration);
                }
                
                // Stop any animations
                if (progressBar != null)
                    DOTween.Kill(progressBar.transform);
                
                if (tipText != null)
                    DOTween.Kill(tipText);
                
                loadingScreen.SetActive(false);
                
                // Reset alpha for next time
                if (loadingCanvasGroup != null)
                    loadingCanvasGroup.alpha = 0f;
            }
        }
        
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Scene loaded: {scene.name}");
            
            // Actualizar GameManager state basado en el nombre de la escena
            UpdateGameManagerState(scene.name);
            
            // Disparar evento personalizado
            OnSceneLoadedEvent?.Invoke(scene.name);
        }
        
        private void UpdateGameManagerState(string sceneName)
        {
            if (GameManager.Instance != null)
            {
                switch (sceneName)
                {
                    case "TitleScreen":
                        GameManager.Instance.ChangeState(GameManager.GameState.TitleScreen);
                        break;
                    case "CharacterSelect":
                        GameManager.Instance.ChangeState(GameManager.GameState.CharacterSelect);
                        break;
                    case "MainMenu":
                        GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);
                        break;
                    case "MinigamePartyLobby":
                        GameManager.Instance.ChangeState(GameManager.GameState.MinigamePartyLobby);
                        break;
                    case "MinigamePartyGame":
                        GameManager.Instance.ChangeState(GameManager.GameState.MinigamePartyGame);
                        break;
                    case "MinigamePlaying":
                        GameManager.Instance.ChangeState(GameManager.GameState.MinigamePlaying);
                        break;
                    case "Results":
                        GameManager.Instance.ChangeState(GameManager.GameState.Results);
                        break;
                    default:
                        Debug.Log($"Scene name '{sceneName}' not mapped to GameState");
                        break;
                }
            }
        }
        
        // Evento para notificar cuando empieza la carga
        public event Action<string> OnSceneLoadingStarted;
        
        public void ReloadCurrentScene(bool showLoadingScreen = true)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            LoadScene(currentScene, showLoadingScreen);
        }
        
        public void LoadNextScene(bool showLoadingScreen = true)
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                LoadScene(nextIndex, showLoadingScreen);
            }
            else
            {
                Debug.LogWarning("No more scenes in build settings");
                // Loop back to first scene
                LoadScene(0, showLoadingScreen);
            }
        }
        
        public bool IsLoading() => isLoading;
        
        public void CancelLoading()
        {
            if (currentLoadCoroutine != null)
            {
                StopCoroutine(currentLoadCoroutine);
                currentLoadCoroutine = null;
            }
            
            if (currentLoadingOperation != null && !currentLoadingOperation.isDone)
            {
                currentLoadingOperation.allowSceneActivation = true;
            }
            
            isLoading = false;
            
            // Hide loading screen if active
            if (loadingScreen != null && loadingScreen.activeSelf)
            {
                StartCoroutine(HideLoadingScreen());
            }
        }
        
        // Método para que otros scripts puedan suscribirse a la carga de escenas
        public void SubscribeToSceneLoad(Action<string> callback)
        {
            OnSceneLoadedEvent += callback;
        }
        
        public void UnsubscribeFromSceneLoad(Action<string> callback)
        {
            OnSceneLoadedEvent -= callback;
        }
        
        private void OnDestroy()
        {
            // Limpiar suscripciones
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            
            // Clean up DOTween animations
            if (loadingCanvasGroup != null)
                DOTween.Kill(loadingCanvasGroup);
            
            if (progressBar != null)
                DOTween.Kill(progressBar.transform);
            
            if (tipText != null)
                DOTween.Kill(tipText);
            
            // Limpiar eventos
            OnSceneLoadedEvent = null;
            OnSceneLoadingStarted = null;
        }
    }
}