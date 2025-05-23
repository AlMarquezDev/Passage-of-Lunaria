using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class CombatTransitionFX : MonoBehaviour
{
    public static CombatTransitionFX Instance { get; private set; }

    [Tooltip("Animator que controla las animaciones de transición (FadeIn, FadeOut).")]
    public Animator animator;
    [Tooltip("AudioSource para los sonidos de transición.")]
    public AudioSource audioSource;
    [Tooltip("Sonido que se reproduce al INICIAR una transición hacia el combate.")]
    public AudioClip transitionInitiateSound;

    private Action onCurrentTransitionVisualsComplete;
    private string _sceneToLoadAfterFadeOut = null;
    private bool _isTransitioningToNewSceneViaLoad = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else if (Instance != this)
        {
            Destroy(transform.root.gameObject); return;
        }

        if (animator == null) Debug.LogError("[CombatTransitionFX] Animator no está asignado!", this);
        if (audioSource == null) Debug.LogWarning("[CombatTransitionFX] AudioSource no está asignado (sonidos de transición no funcionarán).", this);
        if (transitionInitiateSound == null) Debug.LogWarning("[CombatTransitionFX] Transition Initiate Sound no asignado.", this);
    }

    public void TransitionToScene(string sceneName)
    {
        if (animator == null)
        {
            Debug.LogError("[CombatTransitionFX] Animator es null. Cargando escena directamente.");
            if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
            return;
        }
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[CombatTransitionFX] TransitionToScene llamado con sceneName nulo o vacío.");
            return;
        }

        Debug.Log($"[CombatTransitionFX] Iniciando FadeOut para cargar escena: {sceneName}");
        _sceneToLoadAfterFadeOut = sceneName;
        _isTransitioningToNewSceneViaLoad = true;
        onCurrentTransitionVisualsComplete = null;

        PlayTransitionSound();
        animator.SetFloat("SpeedMultiplier", 1f);
        animator.SetTrigger("FadeOut");
    }

    public void PlayEnterBattleAnimation(Action onVisualsComplete)
    {
        if (animator == null)
        {
            Debug.LogError("[CombatTransitionFX] Animator es null. Invocando callback de PlayEnterBattleAnimation inmediatamente.");
            onVisualsComplete?.Invoke();
            return;
        }
        Debug.Log("[CombatTransitionFX] Iniciando animación de entrada/revelación (FadeIn).");
        onCurrentTransitionVisualsComplete = onVisualsComplete;
        _isTransitioningToNewSceneViaLoad = false;

        animator.SetFloat("SpeedMultiplier", 1f);
        animator.SetTrigger("FadeIn");
    }

    public void PlayExitBattleAnimation(Action onReturnToMapLogicComplete, bool playSound = true)
    {
        if (animator == null)
        {
            Debug.LogError("[CombatTransitionFX] Animator es null. Invocando callback de PlayExitBattleAnimation inmediatamente.");
            onReturnToMapLogicComplete?.Invoke();
            return;
        }
        Debug.Log($"[CombatTransitionFX] Iniciando animación de salida de batalla (FadeOut). Sonido: {playSound}");
        onCurrentTransitionVisualsComplete = onReturnToMapLogicComplete;
        _isTransitioningToNewSceneViaLoad = false;

        if (playSound)
        {
            PlayTransitionSound();
        }

        animator.SetFloat("SpeedMultiplier", 2f);
        animator.SetTrigger("FadeOut");
    }

    public void OnTransitionAnimationComplete()
    {
        Debug.Log($"[CombatTransitionFX] OnTransitionAnimationComplete. _isTransitioningToNewSceneViaLoad: {_isTransitioningToNewSceneViaLoad}, _sceneToLoad: '{_sceneToLoadAfterFadeOut}'");

        if (_isTransitioningToNewSceneViaLoad && !string.IsNullOrEmpty(_sceneToLoadAfterFadeOut))
        {
            string sceneToLoad = _sceneToLoadAfterFadeOut;
            _sceneToLoadAfterFadeOut = null;
            _isTransitioningToNewSceneViaLoad = false;

            Debug.Log($"[CombatTransitionFX] FadeOut para carga completo. Cargando escena ahora: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.Log("[CombatTransitionFX] Animación de FadeIn o FadeOut para retorno/entrada completada. Invocando onCurrentTransitionVisualsComplete.");
            onCurrentTransitionVisualsComplete?.Invoke();
            onCurrentTransitionVisualsComplete = null;
        }
    }

    private void PlayTransitionSound()
    {
        if (audioSource != null && transitionInitiateSound != null && audioSource.isActiveAndEnabled)
        {
            Debug.Log($"[CombatTransitionFX] Reproduciendo transitionInitiateSound: {transitionInitiateSound.name}");
            audioSource.PlayOneShot(transitionInitiateSound);
        }
        else
        {
            if (audioSource == null) Debug.LogWarning("[CombatTransitionFX] AudioSource es null. No se puede reproducir sonido.");
            else if (transitionInitiateSound == null) Debug.LogWarning("[CombatTransitionFX] TransitionInitiateSound no asignado. No se puede reproducir sonido.");
            else if (!audioSource.isActiveAndEnabled) Debug.LogWarning("[CombatTransitionFX] AudioSource no está activo y habilitado. No se puede reproducir sonido.");
        }
    }
}