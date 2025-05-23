using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
public class UIButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Optional Sounds")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public AudioSource audioSource;

    [Header("Hover Scale Settings")]
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);
    public float scaleSpeed = 0.1f;

    [Header("Hover Cooldown")]
    public float hoverCooldownDuration = 0.5f; private float lastHoverActivationTime = -1f;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private bool isPointerCurrentlyInside = false;

    private Button buttonComponent;

    private void Awake()
    {
        originalScale = transform.localScale;
        buttonComponent = GetComponent<Button>();

        if (audioSource == null)
        {
            audioSource = GetComponentInParent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null && (hoverSound != null || clickSound != null))
                {
                    Debug.LogWarning($"UIButtonEffect en {gameObject.name}: No se encontró AudioSource. Se añadirá uno al propio botón. Es recomendable asignar uno explícitamente en el panel o un padre.", this);
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0;
                }
            }
        }
        lastHoverActivationTime = -hoverCooldownDuration;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!buttonComponent.interactable || !this.enabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (Time.unscaledTime < lastHoverActivationTime + hoverCooldownDuration)
        {
            if (!isPointerCurrentlyInside)
            {
                isPointerCurrentlyInside = true;
            }
            return;
        }

        if (!isPointerCurrentlyInside)
        {
            isPointerCurrentlyInside = true;
            lastHoverActivationTime = Time.unscaledTime;
            if (hoverSound != null && audioSource != null && audioSource.isActiveAndEnabled)
            {
                audioSource.PlayOneShot(hoverSound);
            }
            StartAnimatedScaling(hoverScale);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isPointerCurrentlyInside)
        {
            isPointerCurrentlyInside = false;
            StartAnimatedScaling(originalScale);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!buttonComponent.interactable || !this.enabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (clickSound != null && audioSource != null && audioSource.isActiveAndEnabled)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    private void StartAnimatedScaling(Vector3 targetScale)
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }

        if (gameObject.activeInHierarchy && this.enabled && Vector3.Distance(transform.localScale, targetScale) > 0.001f)
        {
            scaleCoroutine = StartCoroutine(ScaleToTargetCoroutine(targetScale));
        }
        else if (gameObject.activeInHierarchy && this.enabled)
        {
            transform.localScale = targetScale;
            scaleCoroutine = null;
        }
    }

    private IEnumerator ScaleToTargetCoroutine(Vector3 targetScale)
    {
        Vector3 currentScale = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < scaleSpeed)
        {
            if (!this.enabled || !gameObject.activeInHierarchy)
            {
                transform.localScale = originalScale;
                scaleCoroutine = null;
                yield break;
            }

            if (targetScale == hoverScale && !isPointerCurrentlyInside)
            {
                scaleCoroutine = null;
                yield break;
            }
            if (targetScale == originalScale && isPointerCurrentlyInside)
            {
                scaleCoroutine = null;
                yield break;
            }

            transform.localScale = Vector3.Lerp(currentScale, targetScale, elapsedTime / scaleSpeed);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
        scaleCoroutine = null;
    }

    private void OnDisable()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        transform.localScale = originalScale;
        isPointerCurrentlyInside = false;
    }

    private void Update()
    {
        if (isPointerCurrentlyInside && (!buttonComponent.interactable || !this.enabled || !gameObject.activeInHierarchy))
        {
            bool wasInside = isPointerCurrentlyInside;
            isPointerCurrentlyInside = false;

            if (wasInside)
            {
                StartAnimatedScaling(originalScale);
            }
        }
    }
}