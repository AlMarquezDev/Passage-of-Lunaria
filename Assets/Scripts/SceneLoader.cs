using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class SceneLoader : MonoBehaviour
{
    public static Action OnSceneLoaded;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        OnSceneLoaded?.Invoke();
        OnSceneLoaded = null; // Limpiar después de usarlo (importante para evitar ejecuciones múltiples)
    }
}
