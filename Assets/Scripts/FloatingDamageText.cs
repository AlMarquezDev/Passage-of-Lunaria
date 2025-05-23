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

    [Tooltip("Escala base (escala máxima antes de desvanecerse).")]
    public float normalScale = 1.0f;
    [Tooltip("Multiplicador de escala para Debilidad.")]
    public float weaknessScaleMultiplier = 1.3f;
    [Tooltip("Multiplicador de escala para Resistencia.")]
    public float resistanceScaleMultiplier = 0.8f;
    [Tooltip("Multiplicador de escala para Inmunidad.")]
    public float immuneScaleMultiplier = 0.7f;

    // NUEVO: Escala inicial para la animación de 'pequeño a grande'
    [Header("Scaling Animation")]
    [Tooltip("Escala inicial desde la que el texto crecerá.")]
    public float initialScaleFactor = 0.5f;
    [Tooltip("Curva para controlar la animación de escala (ej. EaseIn para un crecimiento inicial rápido).")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Default to EaseInOut if not set

    private float timer;
    private RectTransform rectTransform;
    private Vector3 startPos; // Posición anclada inicial en el canvas
    private Vector3 baseScale; // Escala base original del prefab

    // NUEVO: Referencia al transform del objetivo en el mundo
    private Transform _worldTargetTransform;
    // NUEVO: Referencia a la cámara principal para WorldToScreenPoint en Update
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
        // Asegurarse de obtener la cámara principal cuando el objeto se activa.
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("FloatingDamageText: Camera.main no encontrada. Los números flotantes no se posicionarán correctamente.");
        }
    }


    public void Initialize(int amount, ElementalAffinity affinity, bool isHealing, bool isMPHealing, Transform worldTargetTransform) // Agregado worldTargetTransform
    {
        if (rectTransform == null) Awake(); // Re-initialize if awake wasn't called (e.g. from pooling)
        if (rectTransform == null) return;

        // Guardar el transform del objetivo del mundo para actualizar la posición en cada frame
        _worldTargetTransform = worldTargetTransform;

        // La posición inicial de 'startPos' ahora se calcula en el Update en función del worldTargetTransform
        // Se mantiene aquí la base, pero el posicionamiento activo lo hará Update.
        startPos = rectTransform.anchoredPosition; // Esto será un valor temporal, no final de posicionamiento

        timer = 0f;
        text.fontStyle = FontStyles.Normal;
        text.text = amount.ToString();

        Color targetColor = normalDamageColor;
        float finalScaleMultiplier = normalScale; // Escala final (la que se alcanza después de crecer)

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
        else // Daño
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
        // La escala inicial se establece aquí, la animación de escala ocurrirá en Update
        rectTransform.localScale = baseScale * initialScaleFactor; // Empieza pequeño
    }

    void Update()
    {
        if (rectTransform == null || _mainCamera == null || _worldTargetTransform == null)
        {
            // Si falta alguna referencia crítica, destruir para evitar errores.
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

        // 2. Animación de Salto (Vertical)
        float currentJumpOffset = jumpCurve.Evaluate(t) * jumpHeight;
        // Ajustamos la posición anclada (local) para el salto
        // El PosX se mantiene constante (0 si está centrado o -590 si se configuró así)
        // El PosY se calcula desde su posición original anclada + el offset de salto.
        // Asumiendo que el RectTransform tiene un pivot en el centro (0.5, 0.5) para PosY 0.
        // Ajusta esta base si tu pivote es diferente.
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, currentJumpOffset);


        // 3. Animación de Escala (Pequeño a Grande)
        // Calcula la escala final que debería alcanzar el texto (considerando normal, debil, resist, inmune)
        float targetScaleMultiplier = normalScale; // Por defecto
        // Recalculamos el final para asegurarnos de que la animación llegue al tamaño correcto
        // Esto es una simplificación, idealmente se guardaría 'finalScaleMultiplier' de Initialize
        // Para daño
        if (!text.text.Contains("+")) // Si no es curación, entonces es daño
        {
            if (text.text == "Immune") targetScaleMultiplier = immuneScaleMultiplier;
            else if (text.fontStyle == FontStyles.Bold) targetScaleMultiplier = weaknessScaleMultiplier; // Weakness (Bold)
            else if (text.color == resistanceColor) targetScaleMultiplier = resistanceScaleMultiplier; // Resistance
        }

        float currentScaleFactor = scaleCurve.Evaluate(t); // Evalúa la curva de escala
        rectTransform.localScale = baseScale * Mathf.Lerp(initialScaleFactor, targetScaleMultiplier, currentScaleFactor);


        // 4. Animación de Opacidad (Fade Out)
        // La opacidad va de 1 a 0 (1 - t)
        text.alpha = 1f - t;
    }
}