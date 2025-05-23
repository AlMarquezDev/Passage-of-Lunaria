using TMPro;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TMP_Text))]
public class BlinkingText : MonoBehaviour
{
    [Tooltip("Velocidad del parpadeo")]
    public float blinkSpeed = 1f;
    [Tooltip("Tiempo de retraso antes de iniciar el efecto (en segundos)")]
    public float startDelay = 3f;

    private TMP_Text textComponent;

    void Start()
    {
        textComponent = GetComponent<TMP_Text>();
                Color initialColor = textComponent.color;
        initialColor.a = 0f;
        textComponent.color = initialColor;

                StartCoroutine(Blink());
    }

    private IEnumerator Blink()
    {
                yield return new WaitForSeconds(startDelay);

        while (true)
        {
                        float alpha = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
            Color currentColor = textComponent.color;
            currentColor.a = alpha;
            textComponent.color = currentColor;
            yield return null;
        }
    }
}