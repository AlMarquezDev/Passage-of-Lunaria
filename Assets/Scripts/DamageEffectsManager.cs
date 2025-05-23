using UnityEngine;
using Cinemachine; // Necesario si usas ImpulseSource, aunque el shake lo llama desde CombatCameraController
using System.Collections;
using UnityEngine.SceneManagement; // Necesario para OnSceneLoaded

public class DamageEffectsManager : MonoBehaviour
{
    public static DamageEffectsManager Instance;

    [Header("Floating Text")]
    [Tooltip("Prefab del texto flotante que se instanciar�.")]
    public GameObject floatingTextPrefab;
    [Tooltip("Transform del Canvas UI donde se instanciar�n los textos flotantes. Asignar desde CombatSceneInitializer.")]
    public Transform uiCanvas;
    [Tooltip("Peque�o retraso en segundos antes de mostrar el texto/shake para mejor timing.")]
    public float feedbackDelay = 0.1f;

    // NUEVO: Campo para el Prefab VFX de curaci�n general
    [Header("Healing VFX")]
    [Tooltip("Prefab del efecto visual a instanciar sobre el objetivo cuando recibe curaci�n o restauraci�n de MP/HP de un �tem.")]
    public GameObject genericHealVFXPrefab;
    [Tooltip("Duraci�n en segundos del VFX de curaci�n antes de destruirse.")]
    public float genericHealVFXDuration = 1.0f;

    private Camera mainCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (floatingTextPrefab == null)
        {
            Debug.LogError("[DamageEffectsManager] ERROR CR�TICO: floatingTextPrefab no asignado en el Inspector!", this);
        }
        // NUEVO: Validar genericHealVFXPrefab
        if (genericHealVFXPrefab == null)
        {
            Debug.LogWarning("[DamageEffectsManager] genericHealVFXPrefab no asignado. No se mostrar�n VFX al curar con �tems.");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GetCurrentMainCamera();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GetCurrentMainCamera();

        if (uiCanvas == null)
        {
            Debug.LogWarning($"[DamageEffectsManager] OnSceneLoaded({scene.name}): uiCanvas es null. Esperando que CombatSceneInitializer lo asigne.");
        }
    }

    private void GetCurrentMainCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[DamageEffectsManager] No se encontr� Camera.main en la escena actual.");
        }
    }

    public void SetCurrentCombatCanvas(Transform canvasTransform)
    {
        uiCanvas = canvasTransform;
        if (uiCanvas == null)
        {
            Debug.LogError("[DamageEffectsManager] SetCurrentCombatCanvas recibi� un canvasTransform nulo.");
        }
        else
        {
            Debug.Log($"[DamageEffectsManager] uiCanvas asignado/actualizado a: {uiCanvas.name}");
        }
    }

    /// <summary>
    /// Muestra el efecto de da�o/curaci�n (n�mero flotante).
    /// </summary>
    public void ShowDamage(int amount, Transform worldTargetTransform, ElementalAffinity affinity, bool isHealing, bool isMPHealing = false)
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("[DamageEffectsManager] ShowDamage: floatingTextPrefab es nulo. No se puede mostrar.");
            return;
        }
        if (uiCanvas == null)
        {
            Debug.LogError("[DamageEffectsManager] ShowDamage: uiCanvas es nulo. No se puede mostrar. Aseg�rate de que CombatSceneInitializer lo asigne.");
            return;
        }
        if (mainCamera == null)
        {
            GetCurrentMainCamera();
            if (mainCamera == null)
            {
                Debug.LogError("[DamageEffectsManager] ShowDamage: mainCamera sigue siendo nula. No se puede mostrar.");
                return;
            }
        }
        if (worldTargetTransform == null)
        {
            Debug.LogWarning("[DamageEffectsManager] ShowDamage: worldTargetTransform es nulo.");
            return;
        }

        // CORRECTED: Pass worldTargetTransform as the 5th argument to the coroutine
        StartCoroutine(ShowDamageCoroutine(amount, worldTargetTransform, affinity, isHealing, isMPHealing, worldTargetTransform));
    }

    // CORRECTED: Added worldTargetTransform as the 5th parameter to the coroutine
    private IEnumerator ShowDamageCoroutine(int amount, Transform worldTargetTransform, ElementalAffinity affinity, bool isHealing, bool isMPHealing, Transform targetTransformForFloatingText)
    {
        if (feedbackDelay > 0)
        {
            if (Time.timeScale > 0)
            {
                yield return new WaitForSeconds(feedbackDelay);
            }
            else
            {
                float unscaledTimer = 0f;
                while (unscaledTimer < feedbackDelay)
                {
                    unscaledTimer += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }

        if (worldTargetTransform == null)
        {
            Debug.LogWarning("[DamageEffectsManager] worldTargetTransform se volvi� NULL despu�s del delay en ShowDamageCoroutine.");
            yield break;
        }

        try
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldTargetTransform.position);
            if (screenPos.z < 0)
            {
                yield break;
            }

            GameObject textObj = Instantiate(floatingTextPrefab, uiCanvas);
            if (textObj != null)
            {
                textObj.transform.position = screenPos;
                var floatingTextComponent = textObj.GetComponent<FloatingDamageText>();
                if (floatingTextComponent != null)
                {
                    // CORRECTED: Pass targetTransformForFloatingText to Initialize
                    floatingTextComponent.Initialize(amount, affinity, isHealing, isMPHealing, targetTransformForFloatingText);
                }
                else
                {
                    Debug.LogError("[DamageEffectsManager] El prefab 'floatingTextPrefab' no tiene el script FloatingDamageText adjunto!");
                    Destroy(textObj);
                }
            }
            else
            {
                Debug.LogError("[DamageEffectsManager] Fall� la instanciaci�n de 'floatingTextPrefab'.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DamageEffectsManager] Excepci�n en ShowDamageCoroutine (l�gica de texto flotante): {ex.Message}\n{ex.StackTrace}");
        }

        if (!isHealing && affinity != ElementalAffinity.Immune)
        {
            if (CombatCameraController.Instance != null)
            {
                CombatCameraController.Instance.TriggerShake();
            }
            else
            {
                Debug.LogWarning("[DamageEffectsManager] CombatCameraController.Instance es null. No se puede hacer camera shake.");
            }
        }
    }

    // NUEVO M�TODO: Para instanciar VFX de curaci�n
    public void InstantiateHealingVFX(Transform targetTransform)
    {
        if (genericHealVFXPrefab != null && targetTransform != null)
        {
            try
            {
                GameObject vfx = Instantiate(genericHealVFXPrefab, targetTransform.position, Quaternion.identity);
                if (vfx != null)
                {
                    Destroy(vfx, genericHealVFXDuration);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DamageEffectsManager] Excepci�n al instanciar VFX de curaci�n: {ex.Message}\n{ex.StackTrace}");
            }
        }
        else
        {
            if (genericHealVFXPrefab == null) Debug.LogWarning("[DamageEffectsManager] genericHealVFXPrefab es null, no se puede instanciar VFX de curaci�n.");
            if (targetTransform == null) Debug.LogWarning("[DamageEffectsManager] Target transform para VFX de curaci�n es null.");
        }
    }
}