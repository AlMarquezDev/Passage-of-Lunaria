using UnityEngine;
using UnityEngine.SceneManagement;
public class BuildingSceneTrigger : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    [Tooltip("El nombre exacto de la escena a la que se transitará.")]
    public string sceneNameToLoad = "ChaosShrine";
    [Header("Interaction Settings")]
    [Tooltip("Tag del objeto que debe colisionar para activar la transición (normalmente 'Player').")]
    public string playerTag = "Player";

    private bool hasBeenTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered)
        {
            return;
        }

        if (other.CompareTag(playerTag))
        {
            if (string.IsNullOrEmpty(sceneNameToLoad))
            {
                Debug.LogError($"BuildingSceneTrigger en {gameObject.name}: 'Scene Name To Load' no está configurado.", this);
                return;
            }

            Debug.Log($"El jugador ({other.name}) ha entrado en '{gameObject.name}'. Transicionando a escena: '{sceneNameToLoad}'.");
            hasBeenTriggered = true;

            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene(sceneNameToLoad, SceneTransition.TransitionContext.Generic);
            }
            else
            {
                Debug.LogError("BuildingSceneTrigger: SceneTransition.Instance no encontrado. Cargando escena directamente como fallback.", this);
                SceneManager.LoadScene(sceneNameToLoad);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            hasBeenTriggered = false;
        }
    }

}