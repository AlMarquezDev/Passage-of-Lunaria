using UnityEngine;
using TMPro;
using System.Collections;

public class TMPFadeIn : MonoBehaviour
{
    [Tooltip("Duración de la transición de opacidad (de 0 a 1) en segundos")]
    public float fadeDuration = 2f;

    private TMP_Text tmpText;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        if (tmpText == null)
        {
            Debug.LogError("No se encontró un componente TMP_Text en este GameObject.");
            return;
        }
        // Inicializa el texto como completamente invisible
        Color initialColor = tmpText.color;
        initialColor.a = 0f;
        tmpText.color = initialColor;
    }

    void Start()
    {
        StartCoroutine(FadeInCoroutine());
    }

    IEnumerator FadeInCoroutine()
    {
        float elapsedTime = 0f;
        Color currentColor = tmpText.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeDuration);
            currentColor.a = alpha;
            tmpText.color = currentColor;
            yield return null;
        }

        // Aseguramos que el alpha final es 1
        currentColor.a = 1f;
        tmpText.color = currentColor;
    }
}