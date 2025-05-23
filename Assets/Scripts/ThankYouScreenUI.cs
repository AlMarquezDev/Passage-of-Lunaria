using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class ThankYouScreenUI : MonoBehaviour
{
    public static ThankYouScreenUI Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The root GameObject of the 'Thank You' panel.")]
    public GameObject rootPanel;
    [Tooltip("TextMeshProUGUI component for the main scrolling lore text.")]
    public TMP_Text loreText;
    [Tooltip("The RectTransform of the loreText to control its scrolling position.")]
    public RectTransform loreTextRectTransform;
    [Tooltip("TextMeshProUGUI component for the final 'Thank You For Playing!' message.")]
    public TMP_Text thankYouMessageText;
    [Tooltip("CanvasGroup on the root panel for fade effects.")]
    public CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    public float fadeInDuration = 1.0f;
    [Tooltip("Duration of the final fade-out to black before returning to main menu.")]
    public float fadeOutDurationToMenu = 2.0f;

    [Header("Lore Text Settings")]
    [TextArea(5, 10)]
    [Tooltip("The full lore text that will scroll.")]
    public string fullLoreContent = "As the echoes of the final battle faded, a profound stillness settled upon the ravaged lands. The grotesque influence that had woven itself into the very fabric of existence, twisting creatures and corrupting ancient energies, was finally undone. The heroic deeds of the chosen few had pierced the veil of shadow, shattering the malevolent entity that sought to plunge the realm into eternal chaos.\n\n" + // Paragraph 1
                                   "The ancient prophecies spoke of a great awakening, a return to balance, and now, the weary but resolute heroes stood as testaments to courage and sacrifice. The world, once shrouded in despair, breathed anew, its destiny reclaimed from the clutches of oblivion.\n\n" + // Paragraph 2
                                   "A new dawn had truly arrived, promising an era of peace and rebirth for all living beings."; // Paragraph 3
    [Tooltip("Speed at which the lore text scrolls automatically (units per second).")]
    public float scrollSpeed = 50.0f;
    [Tooltip("Speed multiplier when Enter is pressed.")]
    public float fastScrollMultiplier = 3.0f;
    [Tooltip("Padding from the top of the screen where the text starts appearing.")]
    public float startScrollYOffset = 0f;
    [Tooltip("Padding from the bottom of the screen where the text should finish scrolling.")]
    public float endScrollYOffset = 0f;

    [Header("Final Message Settings")]
    [Tooltip("Time delay after lore text finishes before final 'Thank You' message appears.")]
    public float thankYouMessageDelay = 1.0f;
    [Tooltip("Duration for the final 'Thank You' message to fade in.")]
    public float thankYouMessageFadeInDuration = 1.0f;

    [Header("Scene Transition Settings")]
    [Tooltip("Delay after final message is fully visible before starting fade out to main menu.")]
    public float returnToMenuDelay = 3.0f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip scrollSound;
    public AudioClip finalMessageSound;
    [Tooltip("Specific music track to play during the 'Thank You' screen.")]
    public AudioClip thankYouMusic;

    private Action onContinueCallback;
    private bool isScrollingFast = false;
    private float initialLoreTextY;
    private float loreTextHeight;
    private RectTransform parentRectTransform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (rootPanel == null) Debug.LogError("ThankYouScreenUI: rootPanel not assigned!");
        if (loreText == null) Debug.LogError("ThankYouScreenUI: loreText not assigned!");
        if (loreTextRectTransform == null && loreText != null) loreTextRectTransform = loreText.GetComponent<RectTransform>();
        if (loreTextRectTransform == null) Debug.LogError("ThankYouScreenUI: loreTextRectTransform not assigned and could not be found!");
        if (thankYouMessageText == null) Debug.LogError("ThankYouScreenUI: thankYouMessageText not assigned!");
        if (canvasGroup == null) Debug.LogError("ThankYouScreenUI: CanvasGroup not assigned! Add a CanvasGroup component to the rootPanel.");

        if (rootPanel != null) rootPanel.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        if (loreText != null) loreText.text = "";
        if (thankYouMessageText != null) thankYouMessageText.alpha = 0f;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0;
            }
        }

        if (loreTextRectTransform != null && loreTextRectTransform.parent is RectTransform)
        {
            parentRectTransform = loreTextRectTransform.parent as RectTransform;
        }
        else
        {
            Debug.LogError("ThankYouScreenUI: LoreText's parent is not a RectTransform or is null, cannot calculate scrolling area!");
            if (rootPanel != null) parentRectTransform = rootPanel.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        if (rootPanel.activeSelf && loreText.gameObject.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                isScrollingFast = true;
            }
            else if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
            {
                isScrollingFast = false;
            }
        }
    }

    public void ShowScreen(Action onContinue)
    {
        if (rootPanel == null || loreText == null || loreTextRectTransform == null || thankYouMessageText == null || canvasGroup == null || parentRectTransform == null)
        {
            Debug.LogError("ThankYouScreenUI: Cannot show screen due to unassigned critical UI references or invalid parentRectTransform.");
            onContinue?.Invoke();
            return;
        }

        onContinueCallback = onContinue;
        isScrollingFast = false;

        loreText.text = fullLoreContent;
        loreText.ForceMeshUpdate();
        loreTextHeight = loreText.preferredHeight;

        initialLoreTextY = -parentRectTransform.rect.height / 2 - loreTextHeight / 2 - startScrollYOffset;
        loreTextRectTransform.anchoredPosition = new Vector2(0, initialLoreTextY);

        loreText.gameObject.SetActive(true);
        thankYouMessageText.alpha = 0f;

        rootPanel.SetActive(true);
        StartCoroutine(SceneFadeInAndContentScroll());
    }

    private IEnumerator SceneFadeInAndContentScroll()
    {
        canvasGroup.alpha = 0f;
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.StopMusicWithFade();
            if (thankYouMusic != null)
            {
                // NEW: Call StartFade directly on MusicManager for thankYouMusic
                // This ensures it replaces any currently playing track immediately.
                MusicManager.Instance.StartFade(thankYouMusic, true); // true for looping
            }
        }

        float currentY = initialLoreTextY;
        float targetY = parentRectTransform.rect.height / 2 + loreTextHeight / 2 + endScrollYOffset;

        while (currentY < targetY)
        {
            float speed = scrollSpeed;
            if (isScrollingFast)
            {
                speed *= fastScrollMultiplier;
            }
            currentY += speed * Time.unscaledDeltaTime;
            loreTextRectTransform.anchoredPosition = new Vector2(0, currentY);
            yield return null;
        }
        loreTextRectTransform.anchoredPosition = new Vector2(0, targetY);

        loreText.gameObject.SetActive(false);

        timer = 0f;
        while (timer < thankYouMessageFadeInDuration)
        {
            thankYouMessageText.alpha = Mathf.Lerp(0f, 1f, timer / thankYouMessageFadeInDuration);
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
        thankYouMessageText.alpha = 1f;

        yield return new WaitForSecondsRealtime(returnToMenuDelay);

        // --- Lógica de desvanecimiento a negro y retorno al menú principal con SceneTransition ---
        // Detiene la música de agradecimiento con fade out, si está sonando.
        if (MusicManager.Instance != null && MusicManager.Instance.GetCurrentClip() == thankYouMusic)
        {
            MusicManager.Instance.StopMusicWithFade(); // Detiene la música de agradecimiento con fade out.
        }

        // Primero, oculta este panel para que SceneTransition pueda tomar el control visual.
        // Asume que SceneTransition tiene una superposición negra o barras para cubrir la pantalla.
        rootPanel.SetActive(false);
        // Si el CanvasGroup tenía alpha < 1, asegúrate de que esté opaco para SceneTransition.
        canvasGroup.alpha = 1f;

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.Logout();
            Debug.Log("[ThankYouScreenUI] Sesión limpiada. Iniciando transición de escena con SceneTransition.");
        }
        else
        {
            Debug.LogWarning("[ThankYouScreenUI] SessionManager.Instance es NULL. No se pudo limpiar la sesión.");
        }

        // Llamamos a SceneTransition para que realice su fade a negro y la carga de escena.
        // El 'fadeOutDurationToMenu' aquí es solo informativo para SceneTransition.
        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene("1_MainMenu", SceneTransition.TransitionContext.Generic);
        }
        else
        {
            Debug.LogError("[ThankYouScreenUI] SceneTransition.Instance es NULL! Cargando escena directamente como fallback.");
            SceneManager.LoadScene("1_MainMenu");
        }
        // --- Fin de la lógica de desvanecimiento y retorno ---
    }

    private IEnumerator FadeOutAndReturnToMenu()
    {
        yield break;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null && audioSource.isActiveAndEnabled)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}