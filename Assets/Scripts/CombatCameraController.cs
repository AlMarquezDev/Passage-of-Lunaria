using UnityEngine;
using Cinemachine;
public class CombatCameraController : MonoBehaviour
{
    public static CombatCameraController Instance { get; private set; }

    [Header("Camera Control References")]
    [Tooltip("Transform que la Cinemachine Virtual Camera (VCam) debe seguir. Este objeto ser� movido por el script.")]
    public Transform cameraTarget;
    [Tooltip("Transform que representa el enfoque por defecto de la c�mara en combate (ej. un punto central entre los grupos).")]
    public Transform defaultFocus;
    [Header("Movement Settings")]
    [Tooltip("Velocidad de movimiento general de la c�mara (no usada directamente si Lerp es el principal motor).")]
    public float moveSpeed = 5f; [Tooltip("Velocidad a la que la c�mara se mueve hacia un nuevo objetivo de enfoque.")]
    public float focusSpeed = 8f;
    [Tooltip("Velocidad a la que la c�mara regresa a su posici�n por defecto.")]
    public float returnSpeed = 3f;
    [Tooltip("Umbral de distancia para considerar que la c�mara ha alcanzado su objetivo por defecto.")]
    public float defaultReachedThreshold = 0.1f;

    [Header("Camera Shake (Cinemachine Impulse)")]
    [Tooltip("Referencia al CinemachineImpulseSource para el efecto de vibraci�n.")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    private Transform currentTarget; private float currentSpeed;
    private Vector3 baseCameraTargetPosition;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[CombatCameraController] Destruyendo instancia anterior de CombatCameraController: {Instance.gameObject.name}. La nueva instancia ser�: {this.gameObject.name}");
            Destroy(Instance.gameObject);
        }
        Instance = this;

        if (cameraTarget == null)
        {
            Debug.LogError("[CombatCameraController] �ERROR CR�TICO! 'Camera Target' no est� asignado en el Inspector.", this);
            enabled = false; return;
        }
        if (defaultFocus == null)
        {
            Debug.LogError("[CombatCameraController] �ERROR CR�TICO! 'Default Focus' no est� asignado en el Inspector.", this);
            enabled = false; return;
        }

        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null)
            {
                Debug.LogWarning("[CombatCameraController] CinemachineImpulseSource no asignado y no encontrado en este GameObject. El efecto de vibraci�n no funcionar�.");
            }
        }
    }

    private void Start()
    {
        if (defaultFocus != null)
        {
            currentTarget = defaultFocus;
            currentSpeed = returnSpeed; if (cameraTarget != null)
            {
                cameraTarget.position = defaultFocus.position; baseCameraTargetPosition = cameraTarget.position;
            }
        }
        else if (cameraTarget != null)
        {
            currentTarget = cameraTarget; baseCameraTargetPosition = cameraTarget.position;
            Debug.LogWarning("[CombatCameraController] 'Default Focus' es null en Start. La c�mara podr�a no comportarse como se espera hasta que se llame a FocusOn().");
        }
    }

    private void Update()
    {
        if (cameraTarget == null || currentTarget == null) return;

        baseCameraTargetPosition = Vector3.Lerp(
   baseCameraTargetPosition,
   currentTarget.position,
   Time.deltaTime * currentSpeed
);
        cameraTarget.position = baseCameraTargetPosition;
    }

    public void FocusOn(Transform focusTransform)
    {
        if (focusTransform == null)
        {
            Debug.LogWarning("[CombatCameraController] FocusOn llamado con un focusTransform nulo. Volviendo al enfoque por defecto.");
            ReturnToDefault(); return;
        }
        Debug.Log($"[CombatCameraController] FocusOn: {focusTransform.name}");
        currentTarget = focusTransform;
        currentSpeed = focusSpeed;
    }

    public void ReturnToDefault()
    {
        Debug.Log("[CombatCameraController] ReturnToDefault");
        if (defaultFocus != null)
        {
            currentTarget = defaultFocus;
        }
        else if (cameraTarget != null)
        {
            Debug.LogWarning("[CombatCameraController] ReturnToDefault: 'Default Focus' es nulo. currentTarget se establecer� a 'Camera Target'.");
            currentTarget = cameraTarget;
        }
        currentSpeed = returnSpeed;
    }

    public bool IsNearDefaultPosition()
    {
        if (cameraTarget == null || defaultFocus == null)
        {
            return true;
        }
        return Vector3.Distance(baseCameraTargetPosition, defaultFocus.position) < defaultReachedThreshold;
    }

    public void TriggerShake()
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
    }
}