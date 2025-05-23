using TMPro;
using UnityEngine;
using System.Collections;
using System;

public class FloatingDamageText : MonoBehaviour
{
    public TextMeshProUGUI text;
    public AnimationCurve jumpCurve;
    public float lifetime = 1.5f;
    public float jumpHeight = 50f;

    [Header("Effect Styling")]
    public Color normalDamageColor = Color.white;
    public Color healColor = Color.green;
    public Color weaknessColor = Color.red;
    public Color resistanceColor = Color.cyan;
    public Color immuneColor = Color.grey;
    public Color mpHealColor = Color.blue;

    [Tooltip("Escala base (escala m�xima antes de desvanecerse).")]
    public float normalScale = 1.0f;
    [Tooltip("Multiplicador de escala para Debilidad.")]
    public float weaknessScaleMultiplier = 1.3f;
    [Tooltip("Multiplicador de escala para Resistencia.")]
    public float resistanceScaleMultiplier = 0.8f;
    [Tooltip("Multiplicador de escala para Inmunidad.")]
    public float immuneScaleMultiplier = 0.7f;

    // NUEVO: Escala inicial para la animaci�n de 'peque�o a grande'
    [Header("Scaling Animation")]
    [Tooltip("Escala inicial desde la que el texto crecer�.")]
    public float initialScaleFactor = 0.5f;
    [Tooltip("Curva para controlar la animaci�n de escala (ej. EaseIn para un crecimiento inicial r�pido).")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Default to EaseInOut if not set

    private float timer;
    private RectTransform rectTransform;
    private Vector3 startPos; // Posici�n anclada inicial en el canvas
    private Vector3 baseScale; // Escala base original del prefab

    // NUEVO: Referencia al transform del objetivo en el mundo
    private Transform _worldTargetTransform;
    // NUEVO: Referencia a la c�mara principal para WorldToScreenPoint en Update
    private Camera _mainCamera;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            baseScale = rectTransform.localScale;
        }
        else
        {
            Debug.LogError("FloatingDamageText: RectTransform no encontrado!", this);
            baseScale = Vector3.one;
        }
    }

    void OnEnable()
    {
        // Asegurarse de obtener la c�mara principal cuando el objeto se activa.
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("FloatingDamageText: Camera.main no encontrada. Los n�meros flotantes no se posicionar�n correctamente.");
        }
    }


    public void Initialize(int amount, ElementalAffinity affinity, bool isHealing, bool isMPHealing, Transform worldTargetTransform) // Agregado worldTargetTransform
    {
        if (rectTransform == null) Awake(); // Re-initialize if awake wasn't called (e.g. from pooling)
        if (rectTransform == null) return;

        // Guardar el transform del objetivo del mundo para actualizar la posici�n en cada frame
        _worldTargetTransform = worldTargetTransform;

        // La posici�n inicial de 'startPos' ahora se calcula en el Update en funci�n del worldTargetTransform
        // Se mantiene aqu� la base, pero el posicionamiento activo lo har� Update.
        startPos = rectTransform.anchoredPosition; // Esto ser� un valor temporal, no final de posicionamiento

        timer = 0f;
        text.fontStyle = FontStyles.Normal;
        text.text = amount.ToString();

        Color targetColor = normalDamageColor;
        float finalScaleMultiplier = normalScale; // Escala final (la que se alcanza despu�s de crecer)

        if (isHealing)
        {
            if (isMPHealing)
            {
                targetColor = mpHealColor;
                text.text = $"+{amount} MP";
            }
            else
            {
                targetColor = healColor;
                text.text = $"+{amount}";
            }
        }
        else // Da�o
        {
            switch (affinity)
            {
                case ElementalAffinity.Weak:
                    targetColor = weaknessColor;
                    finalScaleMultiplier = weaknessScaleMultiplier;
                    text.fontStyle = FontStyles.Bold;
                    break;
                case ElementalAffinity.Resistant:
                    targetColor = resistanceColor;
                    finalScaleMultiplier = resistanceScaleMultiplier;
                    break;
                case ElementalAffinity.Immune:
                    text.text = "Immune";
                    targetColor = immuneColor;
                    finalScaleMultiplier = immuneScaleMultiplier;
                    amount = 0;
                    break;
                case ElementalAffinity.Neutral:
                default:
                    targetColor = normalDamageColor;
                    finalScaleMultiplier = normalScale;
                    break;
            }

            if (amount == 0 && affinity != ElementalAffinity.Immune)
            {
                targetColor = Color.grey;
                finalScaleMultiplier = immuneScaleMultiplier;
                text.text = "0";
            }
        }

        text.color = targetColor;
        // La escala inicial se establece aqu�, la animaci�n de escala ocurrir� en Update
        rectTransform.localScale = baseScale * initialScaleFactor; // Empieza peque�o
    }

    void Update()
    {
        if (rectTransform == null || _mainCamera == null || _worldTargetTransform == null)
        {
            // Si falta alguna referencia cr�tica, destruir para evitar errores.
            Destroy(gameObject);
            return;
        }

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float t = timer / lifetime; // Progreso de 0 a 1 a lo largo de la vida del objeto

        // 1. Posicionamiento fijo al objetivo del mundo
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(_worldTargetTransform.position);
        rectTransform.position = screenPos; // Usar position para Screen Space Overlay/Camera

        // 2. Animaci�n de Salto (Vertical)
        float currentJumpOffset = jumpCurve.Evaluate(t) * jumpHeight;
        // Ajustamos la posici�n anclada (local) para el salto
        // El PosX se mantiene constante (0 si est� centrado o -590 si se configur� as�)
        // El PosY se calcula desde su posici�n original anclada + el offset de salto.
        // Asumiendo que el RectTransform tiene un pivot en el centro (0.5, 0.5) para PosY 0.
        // Ajusta esta base si tu pivote es diferente.
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, currentJumpOffset);


        // 3. Animaci�n de Escala (Peque�o a Grande)
        // Calcula la escala final que deber�a alcanzar el texto (considerando normal, debil, resist, inmune)
        float targetScaleMultiplier = normalScale; // Por defecto
        // Recalculamos el final para asegurarnos de que la animaci�n llegue al tama�o correcto
        // Esto es una simplificaci�n, idealmente se guardar�a 'finalScaleMultiplier' de Initialize
        // Para da�o
        if (!text.text.Contains("+")) // Si no es curaci�n, entonces es da�o
        {
            if (text.text == "Immune") targetScaleMultiplier = immuneScaleMultiplier;
            else if (text.fontStyle == FontStyles.Bold) targetScaleMultiplier = weaknessScaleMultiplier; // Weakness (Bold)
            else if (text.color == resistanceColor) targetScaleMultiplier = resistanceScaleMultiplier; // Resistance
        }

        float currentScaleFactor = scaleCurve.Evaluate(t); // Eval�a la curva de escala
        rectTransform.localScale = baseScale * Mathf.Lerp(initialScaleFactor, targetScaleMultiplier, currentScaleFactor);


        // 4. Animaci�n de Opacidad (Fade Out)
        // La opacidad va de 1 a 0 (1 - t)
        text.alpha = 1f - t;
    }
}