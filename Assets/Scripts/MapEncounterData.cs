// MapEncounterData.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MapEncounterData : MonoBehaviour
{
    [Header("Map Encounter Configuration")]
    [Tooltip("Lista de posibles grupos de enemigos (ScriptableObjects EnemyGroupData) para este mapa/zona.")]
    public List<EnemyGroupData> possibleEncountersForThisMap;

    [Tooltip("Mínimo de pasos para el próximo encuentro en este mapa/zona.")]
    public int minStepsPerEncounter = 15;

    [Tooltip("Máximo de pasos para el próximo encuentro en este mapa/zona.")]
    public int maxStepsPerEncounter = 30;

    [Tooltip("¿Están activados los encuentros aleatorios para esta zona?")]
    public bool randomEncountersEnabled = true;

    // *** NUEVA VARIABLE ***
    [Header("Battle Scene Settings")]
    [Tooltip("Nombre de la escena de combate a cargar para los encuentros en ESTE mapa/zona.")]
    public string specificBattleSceneName = "03_CombatScene"; // Default, pero editable por mapa

    void Start()
    {
        if (!randomEncountersEnabled)
        {
            Debug.Log($"Random encounters disabled for map/zone: {gameObject.scene.name} via {gameObject.name}");
            if (EncounterManager.Instance != null)
            {
                // Desactivar encuentros pasando una lista vacía y escena nula (o la default que no se usará)
                EncounterManager.Instance.InitializeForMap(SceneManager.GetActiveScene().name, new List<EnemyGroupData>(), 0, 0, null);
            }
            return;
        }

        if (string.IsNullOrEmpty(specificBattleSceneName)) // Validar que la escena de batalla esté asignada
        {
            Debug.LogError($"MapEncounterData on {gameObject.name}: 'Specific Battle Scene Name' is not set! Cannot initialize encounters for map {gameObject.scene.name}.");
            if (EncounterManager.Instance != null)
            {
                EncounterManager.Instance.InitializeForMap(SceneManager.GetActiveScene().name, new List<EnemyGroupData>(), 0, 0, null);
            }
            return;
        }


        if (possibleEncountersForThisMap == null || possibleEncountersForThisMap.Count == 0)
        {
            Debug.LogWarning($"MapEncounterData on {gameObject.name}: 'Possible Encounters For This Map' is empty or not assigned. No random encounters will occur on map {gameObject.scene.name}.");
            if (EncounterManager.Instance != null)
            {
                EncounterManager.Instance.InitializeForMap(SceneManager.GetActiveScene().name, new List<EnemyGroupData>(), 0, 0, specificBattleSceneName);
            }
            return;
        }

        if (EncounterManager.Instance != null)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            // *** PASAR specificBattleSceneName AL ENCOUNTERMANAGER ***
            EncounterManager.Instance.InitializeForMap(currentSceneName, possibleEncountersForThisMap, minStepsPerEncounter, maxStepsPerEncounter, specificBattleSceneName);
        }
        else
        {
            Debug.LogError($"MapEncounterData on {gameObject.name}: EncounterManager.Instance is null. Cannot initialize encounters for this map ({gameObject.scene.name}). Ensure EncounterManager is loaded (e.g., via GameCoreBootstrap) and persistent.");
        }
    }
}