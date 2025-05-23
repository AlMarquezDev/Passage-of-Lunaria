using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Necesario para IEnumerator

[RequireComponent(typeof(SpriteRenderer))] // Añadido RequireComponent
[RequireComponent(typeof(AudioSource))] // Añadido RequireComponent para AudioSource
public class EnemyWorldAnchor : MonoBehaviour
{
    public EnemyInstance owner;

    [Header("Visual Components")]
    [Tooltip("Referencia al SpriteRenderer principal del enemigo.")]
    [SerializeField] private SpriteRenderer enemySpriteRenderer;
    [Tooltip("Referencia al GameObject del cursor hijo.")]
    public GameObject cursor;

    [Header("Flashing Effect")]
    [Tooltip("Color al que parpadea el sprite cuando está seleccionado.")]
    [SerializeField] private Color flashColor = new Color(1f, 0.5f, 0.5f, 1f);
    [Tooltip("Curva para suavizar el parpadeo.")]
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Velocidad del ciclo de parpadeo.")]
    [SerializeField] private float flashSpeed = 2f;

    [Header("Cursor Bobbing")]
    [Tooltip("Distancia vertical que sube y baja el cursor.")]
    [SerializeField] private float cursorBobDistance = 0.05f;
    [Tooltip("Velocidad del movimiento vertical del cursor.")]
    [SerializeField] private float cursorBobSpeed = 3f;

    [Header("Disintegration Effect")]
    [Tooltip("Material que usa el shader de desintegración. Asigna tu material aquí.")]
    [SerializeField] private Material disintegrationMaterial;
    [Tooltip("Nombre de la propiedad float (0 a 1) en el shader que controla la desintegración.")]
    [SerializeField] private string dissolveAmountShaderProperty = "_DissolveAmount";
    [Tooltip("Duración total del efecto de desintegración en segundos.")]
    [SerializeField] private float disintegrationDuration = 1.0f;
    [Tooltip("Dirección y fuerza del impulso aplicado durante la desintegración.")]
    [SerializeField] private Vector3 disintegrationImpulseDirection = new Vector3(-1, 0.5f, 0);
    [Tooltip("Magnitud de la fuerza del impulso.")]
    [SerializeField] private float disintegrationImpulseStrength = 3f;
    [Tooltip("Retraso adicional antes de destruir el GameObject después de que termine el efecto.")]
    [SerializeField] private float postDisintegrationDestroyDelay = 0.2f;

    [Header("Attack Flash")] // Nueva sección opcional para el Inspector
    [Tooltip("Color del parpadeo de ataque.")]
    [SerializeField] private Color attackFlashColor = Color.white;
    [SerializeField] private int numberOfBlinks = 3; // Ejemplo: 3 parpadeos
    [Tooltip("Duración en segundos que el color de parpadeo está visible.")]
    [SerializeField] private float blinkOnDuration = 0.08f; // Ejemplo: Muy corto
    [Tooltip("Duración en segundos que el color original está visible entre parpadeos.")]
    [SerializeField] private float blinkOffDuration = 0.05f;

    // Añade esto junto a las otras declaraciones de variables miembro al principio de la clase
    [Header("Poison Effect")]
    [Tooltip("Color al que se teñirá el sprite cuando esté envenenado.")]
    [SerializeField] private Color poisonTintColor = new Color(0.5f, 1f, 0.5f, 1f); // Un verde pálido
    [Tooltip("Prefab del VFX en bucle que se mostrará cuando esté envenenado.")]
    [SerializeField] private GameObject poisonVFXPrefab;

    private bool isVisuallyPoisoned = false; // Flag para saber si estamos mostrando los efectos
    private GameObject currentPoisonVFXInstance = null; // Referencia al VFX instanciado
                                                        // La variable originalSpriteColor que ya tenías guardada en Awake nos servirá para restaurar el color

    private Coroutine attackFlashCoroutine;
    // Variables internas existentes
    private Color originalSpriteColor;
    private Coroutine flashCoroutine;
    private Coroutine cursorBobCoroutine;
    private Transform cursorTransform;
    private Vector3 cursorOriginalLocalPos;
    private bool isFlashing = false;
    private AudioSource audioSource; // Añadido para cachear AudioSource

    // Variables internas para Desintegración
    private Material originalMaterial;
    private Coroutine disintegrationCoroutine = null;
    private bool isDisintegrating = false;


    private void Awake()
    {
        // Obtener componentes requeridos
        enemySpriteRenderer = GetComponentInChildren<SpriteRenderer>(true); // Buscar en hijos por si acaso
        audioSource = GetComponent<AudioSource>(); // Obtener AudioSource

        if (enemySpriteRenderer != null)
        {
            originalMaterial = enemySpriteRenderer.material;
            originalSpriteColor = enemySpriteRenderer.color;
        }
        else { Debug.LogError($"SpriteRenderer no encontrado en {gameObject.name}.", this); }

        if (cursor == null) { cursor = transform.Find("Cursor")?.gameObject; }
        if (cursor != null)
        {
            cursorTransform = cursor.transform;
            cursorOriginalLocalPos = cursorTransform.localPosition;
            cursor.SetActive(false);
        }
        else { Debug.LogWarning($"GameObject 'Cursor' no encontrado.", this); }

        // Asegurar que AudioSource exista (aunque RequireComponent debería hacerlo)
        if (audioSource == null)
        {
            Debug.LogWarning($"AudioSource no encontrado en {gameObject.name}, añadiendo uno.", this);
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        if (audioSource != null) audioSource.playOnAwake = false;
    }

    private void Update()
    {
        // Salir si no hay dueño o si se está desintegrando
        if (owner == null || isDisintegrating)
        {
            // Asegurarse de limpiar efectos si el owner desaparece inesperadamente
            if (isVisuallyPoisoned)
            {
                StopPoisonVisuals();
            }
            return;
        }

        // Comprobar si el estado de veneno actual coincide con el estado visual
        bool currentlyPoisoned = owner.HasStatus(StatusAilment.Poison); // HasStatus ya comprueba IsExpired

        if (currentlyPoisoned && !isVisuallyPoisoned)
        {
            // Acaba de ser envenenado (o los efectos no estaban activos) -> Activar efectos
            StartPoisonVisuals();
        }
        else if (!currentlyPoisoned && isVisuallyPoisoned)
        {
            // Ya no está envenenado (o dejó de estarlo) -> Desactivar efectos
            StopPoisonVisuals();
        }
    }

    public void Initialize(EnemyInstance enemy)
    {
        StopPoisonVisuals();
        owner = enemy;
        if (owner != null)
        {
            // Asignación del targetAnchor (código existente)
            if (owner.targetAnchor == null) owner.targetAnchor = this.transform;

            // --- SECCIÓN PARA ASIGNAR SPRITE ---
            if (enemySpriteRenderer != null && owner.enemyData != null && owner.enemyData.sprite != null)
            {
                enemySpriteRenderer.sprite = owner.enemyData.sprite; // Asigna el sprite del EnemyData
                                                                     // Debug.Log($"Sprite '{owner.enemyData.sprite.name}' asignado a {gameObject.name}"); // Log opcional
            }
            else
            {
                // Log de advertencia si falta algo para la asignación del sprite
                if (enemySpriteRenderer == null) Debug.LogWarning($"EnemySpriteRenderer es null en {gameObject.name}.", this);
                if (owner.enemyData == null) Debug.LogWarning($"EnemyData es null para el owner de {gameObject.name}.", this);
                else if (owner.enemyData.sprite == null) Debug.LogWarning($"EnemyData.sprite es null para {owner.enemyData.enemyName}.", owner.enemyData);
            }
            // --- FIN DE LA SECCIÓN ---
        }

        // Resto del código de Initialize existente...
        isDisintegrating = false;
        if (disintegrationCoroutine != null) { StopCoroutine(disintegrationCoroutine); disintegrationCoroutine = null; }

        StopFlashing();
        SetCursorVisible(false);
        if (enemySpriteRenderer != null && originalMaterial != null)
        {
            if (enemySpriteRenderer.material != originalMaterial && enemySpriteRenderer.sharedMaterial != originalMaterial)
            {
                if (enemySpriteRenderer.material.shader == disintegrationMaterial?.shader)
                { enemySpriteRenderer.material = originalMaterial; }
            }
            enemySpriteRenderer.color = originalSpriteColor;
        }
        // No resetear shader property del material asset aquí
    }

    // --- Lógica de Parpadeo ---
    public void StartFlashing()
    {
        if (isFlashing || isDisintegrating || enemySpriteRenderer == null) return;
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        isFlashing = true;
        originalSpriteColor = enemySpriteRenderer.color; // Guardar color actual
        flashCoroutine = StartCoroutine(FlashSpriteCoroutine());
    }
    public void StopFlashing()
    {
        if (!isFlashing || enemySpriteRenderer == null) return;
        if (flashCoroutine != null) { StopCoroutine(flashCoroutine); flashCoroutine = null; }
        if (!isDisintegrating) { enemySpriteRenderer.color = originalSpriteColor; }
        isFlashing = false;
    }
    private IEnumerator FlashSpriteCoroutine()
    {
        Color baseColorForFlash = enemySpriteRenderer.color;
        while (true) { if (enemySpriteRenderer == null) yield break; float p = Mathf.PingPong(Time.time * flashSpeed, 1f); float c = flashCurve.Evaluate(p); enemySpriteRenderer.color = Color.Lerp(baseColorForFlash, flashColor, c); yield return null; }
    }

    // --- Lógica del Cursor ---
    public void SetCursorVisible(bool visible)
    {
        if (cursor == null || cursorTransform == null || isDisintegrating) return;
        if (visible) { cursor.SetActive(true); if (cursorBobCoroutine == null) { cursorBobCoroutine = StartCoroutine(CursorBobCoroutine()); } }
        else { if (cursorBobCoroutine != null) { StopCoroutine(cursorBobCoroutine); cursorBobCoroutine = null; } if (cursorTransform != null) cursorTransform.localPosition = cursorOriginalLocalPos; if (cursor != null) cursor.SetActive(false); }
    }
    private IEnumerator CursorBobCoroutine()
    {
        Vector3 startLocalPos = cursorTransform != null ? cursorTransform.localPosition : Vector3.zero;
        while (true) { if (cursorTransform != null) { float yOffset = Mathf.Sin(Time.time * cursorBobSpeed) * cursorBobDistance; cursorTransform.localPosition = startLocalPos + new Vector3(0, yOffset, 0); } else yield break; yield return null; }
    }

    // --- Lógica de Desintegración (Modificado StartDisintegrationEffect) ---
    public void StartDisintegrationEffect()
    {
        if (isDisintegrating || disintegrationCoroutine != null) { Debug.LogWarning($"Desintegración ya en progreso.", this); return; }
        if (disintegrationMaterial == null) { Debug.LogError("Disintegration Material no asignado!", this); return; }
        if (enemySpriteRenderer == null) { Debug.LogError("SpriteRenderer no encontrado!", this); return; }
        if (owner?.enemyData == null) { Debug.LogError("Owner o EnemyData null, no se puede iniciar desintegración.", this); return; }

        isDisintegrating = true;
        Debug.Log($"StartDisintegrationEffect llamado en {gameObject.name}");

        StopFlashing();
        SetCursorVisible(false);

        // --- AÑADIDO: Reproducir Sonido de Muerte ---
        AudioClip deathClip = owner.enemyData.deathSFX; // Obtener clip desde EnemyData
        if (deathClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathClip);
            Debug.Log($"Reproduciendo deathSFX: {deathClip.name} para {owner.enemyData.enemyName}");
        }
        else
        {
            // Mostrar warnings específicos si algo falta
            if (deathClip == null) Debug.LogWarning($"deathSFX no asignado en EnemyData para {owner.enemyData.enemyName}.", this);
            if (audioSource == null) Debug.LogWarning($"AudioSource no encontrado en {gameObject.name} para reproducir deathSFX.", this);
        }
        // --- FIN AÑADIDO ---

        originalMaterial = enemySpriteRenderer.material; // Guardar material actual antes de cambiarlo

        // Crear instancia del material de desintegración
        Material matInstance = new Material(disintegrationMaterial);
        enemySpriteRenderer.material = matInstance; // Aplicar material

        // Iniciar corutina pasando la instancia del material
        disintegrationCoroutine = StartCoroutine(DisintegrationCoroutine(matInstance));
    }

    // Corutina de Desintegración (Modificada para aceptar la instancia del material)
    private IEnumerator DisintegrationCoroutine(Material materialInstance)
    {
        Debug.Log($"Iniciando corutina de desintegración para {gameObject.name}");

        // Asegurar valor inicial del shader en la instancia
        // Verificar si la propiedad existe antes de intentar asignarla
        if (materialInstance.HasProperty(dissolveAmountShaderProperty))
        {
            materialInstance.SetFloat(dissolveAmountShaderProperty, 0f);
        }
        else { Debug.LogError($"La propiedad '{dissolveAmountShaderProperty}' no existe en el material de desintegración.", materialInstance); yield break; }


        float timer = 0f;
        Collider col = GetComponent<Collider>(); if (col != null) col.enabled = false;
        Collider2D col2D = GetComponent<Collider2D>(); if (col2D != null) col2D.enabled = false;

        while (timer < disintegrationDuration)
        {
            // Salir si el objeto se destruye prematuramente
            if (this == null || gameObject == null) yield break;
            // Salir si el material o renderer se pierden
            if (materialInstance == null || enemySpriteRenderer == null) yield break;

            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / disintegrationDuration);

            materialInstance.SetFloat(dissolveAmountShaderProperty, progress);
            transform.position += disintegrationImpulseDirection.normalized * disintegrationImpulseStrength * Time.deltaTime;

            yield return null;
        }

        if (materialInstance != null) materialInstance.SetFloat(dissolveAmountShaderProperty, 1f);
        Debug.Log($"Desintegración (shader) completada para {gameObject.name}");

        if (postDisintegrationDestroyDelay > 0) { yield return new WaitForSeconds(postDisintegrationDestroyDelay); }

        // Comprobar si aún existe antes de destruir
        if (this != null && gameObject != null)
        {
            Debug.Log($"Destruyendo GameObject {gameObject.name} después de desintegración.");
            Destroy(gameObject);
        }

        disintegrationCoroutine = null;
        isDisintegrating = false;
    }

    // Limpieza al destruir
    private void OnDestroy()
    {
        if (currentPoisonVFXInstance != null)
        {
            Destroy(currentPoisonVFXInstance);
            currentPoisonVFXInstance = null;
        }
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        if (cursorBobCoroutine != null) StopCoroutine(cursorBobCoroutine);
        if (disintegrationCoroutine != null) StopCoroutine(disintegrationCoroutine);
    }

    public void FlashForAttack()
    {
        // Detener cualquier parpadeo anterior (el de selección o uno de ataque previo)
        StopFlashing(); // Detiene el parpadeo de selección si estaba activo
        if (attackFlashCoroutine != null)
        {
            StopCoroutine(attackFlashCoroutine);
            // Asegurarse de restaurar el color original si se interrumpe
            if (enemySpriteRenderer != null)
            {
                enemySpriteRenderer.color = originalSpriteColor;
            }
        }

        // Iniciar la nueva corutina de parpadeo de ataque
        if (gameObject.activeInHierarchy && this.enabled) // Solo iniciar si está activo
        {
            attackFlashCoroutine = StartCoroutine(FlashAttackCoroutine());
        }
    }

    private IEnumerator FlashAttackCoroutine()
    {
        // Log inicial (opcional, puedes quitar los logs si ya funciona)
        Debug.Log($"[FlashAttackCoroutine] Started. Renderer valid: {enemySpriteRenderer != null}. Blinks: {numberOfBlinks}");

        if (enemySpriteRenderer == null) yield break; // Salir si no hay renderer

        // Guardar color original
        Color actualOriginalColor = enemySpriteRenderer.color;

        // Bucle para los parpadeos
        for (int i = 0; i < numberOfBlinks; i++)
        {
            // Comprobar si el renderer sigue siendo válido en cada iteración
            if (enemySpriteRenderer == null) yield break;

            // Poner color de parpadeo (ON)
            enemySpriteRenderer.color = attackFlashColor;
            // Esperar duración ON
            yield return new WaitForSeconds(blinkOnDuration);

            // Comprobar de nuevo antes de restaurar
            if (enemySpriteRenderer == null) yield break;

            // Poner color original (OFF)
            enemySpriteRenderer.color = actualOriginalColor;
            // Esperar duración OFF (excepto en el último parpadeo para que termine en original)
            if (i < numberOfBlinks - 1)
            {
                yield return new WaitForSeconds(blinkOffDuration);
            }
        }

        // Log final (opcional)
        Debug.Log($"[FlashAttackCoroutine] Finished blinking. Restored color to {actualOriginalColor}.");

        // Asegurarse de que el color final sea el original (por si acaso)
        if (enemySpriteRenderer != null)
        {
            enemySpriteRenderer.color = actualOriginalColor;
        }

        // Limpiar referencia a la corutina
        attackFlashCoroutine = null;
    }

    /// <summary>
    /// Activa el tinte verde y el VFX de veneno.
    /// </summary>
    private void StartPoisonVisuals()
    {
        isVisuallyPoisoned = true;

        // Aplicar tinte (solo si el renderer existe)
        if (enemySpriteRenderer != null)
        {
            // Podrías guardar el color ANTES de aplicar el tinte si otros efectos
            // pudieran modificarlo, pero por simplicidad usamos el guardado en Awake.
            // ¡Cuidado! Si el parpadeo de selección está activo, esto lo sobrescribirá.
            // Se podría añadir lógica para combinar colores si fuera necesario.
            enemySpriteRenderer.color = poisonTintColor;
        }

        // Instanciar y activar VFX (si no existe ya y el prefab está asignado)
        if (poisonVFXPrefab != null && currentPoisonVFXInstance == null)
        {
            try
            {
                // Instanciar como hijo para que se mueva con el enemigo
                currentPoisonVFXInstance = Instantiate(poisonVFXPrefab, this.transform.position, Quaternion.identity, this.transform);
                currentPoisonVFXInstance.name = $"{gameObject.name}_PoisonVFX"; // Nombre útil
                                                                                // Asegurarse de que esté activo (si el prefab no lo está por defecto)
                                                                                // currentPoisonVFXInstance.SetActive(true); // Descomentar si es necesario
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error instanciando poisonVFXPrefab para {gameObject.name}: {ex.Message}", this);
                currentPoisonVFXInstance = null; // Asegurar que sea nulo si falla
            }
        }
    }

    /// <summary>
    /// Desactiva el tinte verde y destruye el VFX de veneno.
    /// </summary>
    private void StopPoisonVisuals()
    {
        isVisuallyPoisoned = false;

        // Restaurar color original (solo si no está siendo afectado por otro flash/efecto)
        // Restauramos al color original guardado en Awake.
        // Si otros efectos como el flash de ataque están activos, este color podría
        // sobrescribirse temporalmente por ellos después.
        if (enemySpriteRenderer != null && attackFlashCoroutine == null && !isFlashing) // Solo restaurar si no hay otro flash activo
        {
            enemySpriteRenderer.color = originalSpriteColor;
        }

        // Destruir instancia del VFX si existe
        if (currentPoisonVFXInstance != null)
        {
            Destroy(currentPoisonVFXInstance);
            currentPoisonVFXInstance = null;
        }
    }
}