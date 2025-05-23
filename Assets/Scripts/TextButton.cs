using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TextButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private AudioSource audioSource;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color pressedTextColor = new Color(0.8f, 0.8f, 0.8f);

    [Header("Sounds")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private bool enableHoverSound = true;
    [SerializeField] private float clickSoundDelay = 0.05f;

    [Header("Icon Settings")]
    [SerializeField] private GameObject iconPrefab;
    [SerializeField] private bool showHoverIcon = true;
    [SerializeField] private float iconXOffset = -100f;
    [SerializeField] private float iconYPosition = 0f;

    private Button button;
    private MenuManager menuManager;
    private GameObject iconInstance;
    private Vector2 originalIconPosition;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (buttonText == null)
            Debug.LogError("Missing buttonText reference.", this);

        menuManager = FindObjectOfType<MenuManager>();
        ConfigureAudioSource();
        ConfigureButton();
        InitializeIcon();
    }

    private void OnEnable()
    {
        if (buttonText != null)
            buttonText.color = normalColor;

        HideIcon();
    }

    private void InitializeIcon()
    {
        if (showHoverIcon && iconPrefab != null)
        {
            iconInstance = Instantiate(iconPrefab, transform);
            iconInstance.SetActive(false);

            RectTransform iconRect = iconInstance.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(0, iconYPosition);
            originalIconPosition = iconRect.anchoredPosition;
        }
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null && menuManager != null)
            audioSource = menuManager.GetComponent<AudioSource>();

        if (audioSource == null)
            Debug.LogError("Missing AudioSource reference.", this);
    }

    private void ConfigureButton()
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.1f);
        colors.pressedColor = Color.clear; button.colors = colors;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;
        buttonText.color = hoverColor;
        PlayHoverSound();
        ShowIcon();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonText.color = normalColor;
        HideIcon();
    }

    private void ShowIcon()
    {
        if (!showHoverIcon || iconInstance == null) return;

        iconInstance.SetActive(true);
        RectTransform iconRect = iconInstance.GetComponent<RectTransform>();
        iconRect.anchoredPosition = new Vector2(
            originalIconPosition.x + iconXOffset,
            originalIconPosition.y
        );
    }

    private void HideIcon()
    {
        if (!showHoverIcon || iconInstance == null) return;

        iconInstance.SetActive(false);
        iconInstance.GetComponent<RectTransform>().anchoredPosition = originalIconPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!button.interactable) return;
        HandleClickWithSound(() => button.onClick.Invoke());
    }

    public void HandleClickWithSound(System.Action action)
    {
        if (!button.interactable) return;
        StartCoroutine(ClickSequence(action));
    }

    private IEnumerator ClickSequence(System.Action action)
    {
        PlayClickSound();
        buttonText.color = pressedTextColor;
        yield return new WaitForSecondsRealtime(clickSoundDelay);
        buttonText.color = normalColor;
        action?.Invoke();
    }

    private void PlayHoverSound()
    {
        if (enableHoverSound && hoverSound != null && audioSource != null)
            audioSource.PlayOneShot(hoverSound);
    }

    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
            audioSource.PlayOneShot(clickSound);
    }
}