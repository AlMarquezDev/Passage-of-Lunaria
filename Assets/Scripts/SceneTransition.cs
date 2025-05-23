// SceneTransition.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;

    [Header("Transition Settings")]
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio Settings")]
    [SerializeField] private AudioSource transitionAudioSource;
    [SerializeField] private AudioClip genericBarCloseSound;
    [SerializeField] private AudioClip enterCombatBarCloseSound;
    [SerializeField] private AudioClip exitCombatBarCloseSound;

    private Vector2 topBarStartOffscreen;
    private Vector2 bottomBarStartOffscreen;
    private Vector2 barsCoverScreenPosition = Vector2.zero;
    private bool isTransitioning;

    private readonly string[] battleSceneIdentifiers = { "Battle", "Combat" };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
            InitializeBars();

            if (transitionAudioSource == null)
            {
                transitionAudioSource = GetComponent<AudioSource>();
                if (transitionAudioSource == null && (genericBarCloseSound != null || enterCombatBarCloseSound != null || exitCombatBarCloseSound != null))
                {
                    transitionAudioSource = gameObject.AddComponent<AudioSource>();
                    transitionAudioSource.playOnAwake = false;
                }
            }
        }
        else if (Instance != this)
        {
            Destroy(transform.root.gameObject);
        }
    }

    private void Start()
    {
        if (topBar != null && topBar.anchoredPosition != topBarStartOffscreen)
            topBar.anchoredPosition = topBarStartOffscreen;
        if (bottomBar != null && bottomBar.anchoredPosition != bottomBarStartOffscreen)
            bottomBar.anchoredPosition = bottomBarStartOffscreen;
    }

    private void InitializeBars()
    {
        if (topBar == null || bottomBar == null) { return; }
        topBarStartOffscreen = new Vector2(0, topBar.rect.height);
        bottomBarStartOffscreen = new Vector2(0, -bottomBar.rect.height);
        topBar.anchoredPosition = topBarStartOffscreen;
        bottomBar.anchoredPosition = bottomBarStartOffscreen;
    }

    private void PlaySound(AudioClip clip)
    {
        if (transitionAudioSource != null && clip != null && transitionAudioSource.isActiveAndEnabled)
        {
            transitionAudioSource.PlayOneShot(clip);
        }
    }

    public enum TransitionContext
    {
        Generic,
        ToBattle,
        FromBattle
    }

    public void LoadScene(string sceneName, TransitionContext context = TransitionContext.Generic, System.Action onMidpoint = null, System.Action onComplete = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"[SceneTransition] Ya está en transición, no se puede cargar: {sceneName}");
            onComplete?.Invoke();
            return;
        }
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneTransition] Nombre de escena vacío.");
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(TransitionCoroutine(sceneName, context, onMidpoint, onComplete));
    }

    private IEnumerator TransitionCoroutine(string sceneName, TransitionContext context, System.Action onMidpoint, System.Action onComplete)
    {
        isTransitioning = true;
        Debug.Log($"[SceneTransition] Iniciando TransitionCoroutine para: {sceneName}, Contexto: {context}");

        AudioClip soundForBarsClosing = genericBarCloseSound;
        if (context == TransitionContext.ToBattle && enterCombatBarCloseSound != null)
        {
            soundForBarsClosing = enterCombatBarCloseSound;
        }
        else if (context == TransitionContext.FromBattle && exitCombatBarCloseSound != null)
        {
            soundForBarsClosing = exitCombatBarCloseSound;
        }

        PlaySound(soundForBarsClosing);

        if (topBar != null && bottomBar != null)
        {
            yield return StartCoroutine(AnimateBars(barsCoverScreenPosition, transitionDuration));
        }
        else
        {
            Debug.LogWarning("[SceneTransition] Barras (topBar/bottomBar) no asignadas. Transición visual de barras omitida.");
        }

        // Add a small real-time delay to ensure bars are visually closed before scene load starts
        yield return new WaitForSecondsRealtime(0.1f);

        onMidpoint?.Invoke();

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        Debug.Log($"[SceneTransition] Escena '{sceneName}' cargada asíncronamente.");
        yield return null;

        if (context == TransitionContext.FromBattle)
        {
            CombatSessionData.Instance?.FinalizeReturnToMap();
        }

        if (context != TransitionContext.ToBattle)
        {
            if (topBar != null && bottomBar != null)
            {
                yield return StartCoroutine(AnimateBars(topBarStartOffscreen, transitionDuration));
            }
        }
        else
        {
            Debug.Log($"[SceneTransition] Barras cerradas para {sceneName}. Esperando llamada para revelar.");
        }

        if (context != TransitionContext.ToBattle)
        {
            isTransitioning = false; // Reset here for non-battle specific transitions
        }
        onComplete?.Invoke();
    }

    public void RevealSceneAfterBattleLoad(System.Action onRevealComplete = null)
    {
        if (topBar != null && bottomBar != null &&
            (topBar.anchoredPosition == barsCoverScreenPosition || Vector2.Distance(topBar.anchoredPosition, barsCoverScreenPosition) < 0.1f))
        {
            StartCoroutine(AnimateBarsAndFinalize(topBarStartOffscreen, null, onRevealComplete));
        }
        else
        {
            isTransitioning = false; // Reset if bars were not closed
            onRevealComplete?.Invoke();
        }
    }

    private IEnumerator AnimateBarsAndFinalize(Vector2 targetPosition, AudioClip soundToPlay, System.Action onComplete)
    {
        if (topBar == null || bottomBar == null)
        {
            Debug.LogError("[SceneTransition] AnimateBarsAndFinalize cannot run, topBar or bottomBar is null.");
            yield break;
        }

        float elapsed = 0f;
        Vector2 currentTopStart = topBar.anchoredPosition;
        Vector2 currentBottomStart = bottomBar.anchoredPosition;
        Vector2 targetBottomActualPosition = (targetPosition == barsCoverScreenPosition) ? barsCoverScreenPosition : bottomBarStartOffscreen;

        while (elapsed < transitionDuration) // Use transitionDuration here, not 'duration'
        {
            elapsed += Time.unscaledDeltaTime;
            float t = animationCurve.Evaluate(elapsed / transitionDuration); // Use transitionDuration here
            if (topBar) topBar.anchoredPosition = Vector2.Lerp(currentTopStart, targetPosition, t);
            if (bottomBar) bottomBar.anchoredPosition = Vector2.Lerp(currentBottomStart, targetBottomActualPosition, t);
            yield return null;
        }
        if (topBar) topBar.anchoredPosition = targetPosition;
        if (bottomBar) bottomBar.anchoredPosition = targetBottomActualPosition;

        // NEW: Ensure isTransitioning is correctly set when bars are fully opened
        isTransitioning = false;
        onComplete?.Invoke();
    }

    private IEnumerator AnimateBars(Vector2 targetPosition, float duration)
    {
        if (topBar == null || bottomBar == null)
        {
            Debug.LogError("[SceneTransition] AnimateBars cannot run, topBar or bottomBar is null.");
            yield break;
        }

        float elapsed = 0f;
        Vector2 currentTopStart = topBar.anchoredPosition;
        Vector2 currentBottomStart = bottomBar.anchoredPosition;
        Vector2 targetBottomActualPosition = (targetPosition == barsCoverScreenPosition) ? barsCoverScreenPosition : bottomBarStartOffscreen;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = animationCurve.Evaluate(elapsed / duration);
            if (topBar) topBar.anchoredPosition = Vector2.Lerp(currentTopStart, targetPosition, t);
            if (bottomBar) bottomBar.anchoredPosition = Vector2.Lerp(currentBottomStart, targetBottomActualPosition, t);
            yield return null;
        }
        if (topBar) topBar.anchoredPosition = targetPosition;
        if (bottomBar) bottomBar.anchoredPosition = targetBottomActualPosition;
    }

    private bool IsSceneBattle(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        foreach (string identifier in battleSceneIdentifiers)
        {
            if (sceneName.Contains(identifier))
            {
                return true;
            }
        }
        return false;
    }
}