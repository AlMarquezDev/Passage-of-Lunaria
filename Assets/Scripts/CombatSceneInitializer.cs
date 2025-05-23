using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class CombatSceneInitializer : MonoBehaviour
{
    [Header("Ally Setup")]
    [Tooltip("Prefab del aliado que se instanciará.")]
    public GameObject allyPrefab; [Tooltip("Lista de Transforms donde se posicionarán los aliados (máximo 4).")]
    public List<Transform> allySlots; [Header("Enemy Setup")]
    public List<Transform> enemySlots; [Header("Combat UI Canvas")]
    [Tooltip("Arrastra aquí el Transform raíz del Canvas UI principal de la escena de combate.")]
    public Transform combatUICanvasForEffects;
    private List<EnemyInstance> spawnedEnemies;
    private void Start()
    {
        Debug.LogError($"COMBAT SCENE INITIALIZER: START en GameObject '{gameObject.name}' en Escena '{SceneManager.GetActiveScene().name}', Tiempo: {Time.time}, Frame: {Time.frameCount}");

        if (allyPrefab == null) { Debug.LogError("[CombatSceneInitializer] Ally Prefab no asignado!", this); return; }
        if (allySlots == null || allySlots.Count == 0) { Debug.LogError("[CombatSceneInitializer] Ally Slots no asignados o lista vacía!", this); return; }
        if (enemySlots == null || enemySlots.Count == 0) { Debug.LogError("[CombatSceneInitializer] Enemy Slots no asignados o lista vacía!", this); return; }
        if (DamageEffectsManager.Instance != null)
        {
            if (combatUICanvasForEffects != null)
            {
                DamageEffectsManager.Instance.SetCurrentCombatCanvas(combatUICanvasForEffects);
            }
            else
            {
                Debug.LogError("[CombatSceneInitializer] 'Combat UI Canvas For Effects' no está asignado en el Inspector. DamageEffectsManager podría no funcionar correctamente.", this);
            }
        }
        else
        {
            Debug.LogWarning("[CombatSceneInitializer] DamageEffectsManager.Instance es null. No se pudo asignar el canvas de combate.");
        }

        if (CombatSessionData.Instance == null)
        {
            Debug.LogError("[CombatSceneInitializer] CombatSessionData.Instance es null. No se puede iniciar el combate.", this); return;
        }
        if (CombatSessionData.Instance.partyMembers == null)
        {
            Debug.LogError("[CombatSceneInitializer] CombatSessionData.Instance.partyMembers es null. No se puede iniciar el combate.", this); return;
        }
        if (CombatSessionData.Instance.enemyGroup == null)
        {
            Debug.LogError("[CombatSceneInitializer] CombatSessionData.Instance.enemyGroup es null. No se puede iniciar el combate.", this); return;
        }

        SpawnParty(); SpawnEnemies();
        Debug.LogError($"[CombatSceneInitializer] Justo ANTES de comprobar BattleFlowController.Instance. Es null? {(BattleFlowController.Instance == null)}. Frame: {Time.frameCount}");
        if (BattleFlowController.Instance != null)
        {
            Debug.LogError($"[CombatSceneInitializer] BattleFlowController.Instance ENCONTRADO: {BattleFlowController.Instance.gameObject.name} (ID: {BattleFlowController.Instance.GetInstanceID()}). Llamando a BeginBattle. Frame: {Time.frameCount}");
            BattleFlowController.Instance.BeginBattle(CombatSessionData.Instance.partyMembers,
                spawnedEnemies
            );
        }
        else
        {
            Debug.LogError("[CombatSceneInitializer] BattleFlowController.Instance SIGUE SIENDO NULL. No se puede llamar a BeginBattle. Frame: {Time.frameCount}", this);
        }
        Debug.LogError($"COMBAT SCENE INITIALIZER: START FINALIZADO. Frame: {Time.frameCount}");
    }

    private void SpawnParty()
    {
        var party = CombatSessionData.Instance.partyMembers; if (party == null)
        {
            Debug.LogError("[CombatSceneInitializer] SpawnParty: partyMembers en CombatSessionData es null!"); return;
        }

        Debug.Log($"[CombatSceneInitializer] Spawning party. Count: {party.Count}, Slot Count: {allySlots.Count}"); int count = Mathf.Min(party.Count, allySlots.Count); for (int i = 0; i < count; i++)
        {
            var character = party[i]; if (character == null)
            {
                Debug.LogWarning($"[CombatSceneInitializer] Personaje en party índice {i} es null."); continue;
            }
            if (allySlots[i] == null)
            {
                Debug.LogWarning($"[CombatSceneInitializer] Ally Slot en índice {i} es null."); continue;
            }

            Vector3 position = allySlots[i].position; Quaternion rotation = allySlots[i].rotation;
            Debug.Log($"[CombatSceneInitializer] Instanciando aliado '{character.characterName}' en slot {i} ({position})"); GameObject obj = Instantiate(allyPrefab, position, rotation); obj.name = $"Ally_{character.characterName}";
            AllyWorldAnchor anchor = obj.GetComponent<AllyWorldAnchor>(); if (anchor != null)
            {
                anchor.Initialize(character);
            }
            else
            {
                Debug.LogError($"[CombatSceneInitializer] El prefab de aliado '{allyPrefab.name}' no tiene el script AllyWorldAnchor.", obj);
            }
        }
    }

    private void SpawnEnemies()
    {
        if (CombatSessionData.Instance.enemyGroup == null)
        {
            Debug.LogError("[CombatSceneInitializer] SpawnEnemies: enemyGroup en CombatSessionData es null!"); spawnedEnemies = new List<EnemyInstance>(); return;
        }

        spawnedEnemies = CombatSessionData.Instance.enemyGroup.CreateEnemies(); if (spawnedEnemies == null)
        {
            Debug.LogError("[CombatSceneInitializer] SpawnEnemies: CreateEnemies() devolvió null!"); spawnedEnemies = new List<EnemyInstance>(); return;
        }

        Debug.Log($"[CombatSceneInitializer] Spawning enemies. Count: {spawnedEnemies.Count}, Slot Count: {enemySlots.Count}"); int count = Mathf.Min(spawnedEnemies.Count, enemySlots.Count); for (int i = 0; i < count; i++)
        {
            var enemyInstance = spawnedEnemies[i]; if (enemyInstance == null || enemyInstance.enemyData == null)
            {
                Debug.LogWarning($"[CombatSceneInitializer] EnemyInstance o su EnemyData en índice {i} es null."); continue;
            }
            if (enemyInstance.enemyData.worldPrefab == null)
            {
                Debug.LogError($"[CombatSceneInitializer] EnemyData '{enemyInstance.enemyData.enemyName}' no tiene worldPrefab asignado!", enemyInstance.enemyData); continue;
            }
            if (enemySlots[i] == null)
            {
                Debug.LogWarning($"[CombatSceneInitializer] Enemy Slot en índice {i} es null."); continue;
            }

            Transform slot = enemySlots[i]; Vector3 position = slot.position; Quaternion rotation = slot.rotation;
            Debug.Log($"[CombatSceneInitializer] Instanciando enemigo '{enemyInstance.enemyData.enemyName}' en slot {i} ({position}) usando prefab '{enemyInstance.enemyData.worldPrefab.name}'"); GameObject obj = Instantiate(enemyInstance.enemyData.worldPrefab, position, rotation); obj.name = $"Enemy_{enemyInstance.enemyData.enemyName}_{i}";
            enemyInstance.worldTransform = obj.transform; EnemyWorldAnchor anchor = obj.GetComponent<EnemyWorldAnchor>(); if (anchor != null)
            {
                anchor.Initialize(enemyInstance);
            }
            else
            {
                Debug.LogError($"[CombatSceneInitializer] El worldPrefab del enemigo '{enemyInstance.enemyData.enemyName}' no tiene el script EnemyWorldAnchor.", obj);
            }
        }
    }
    private void OnValidate()
    {
        if (allySlots != null && (allySlots.Count < 1 || allySlots.Count > 4)) Debug.LogWarning("Se recomienda entre 1 y 4 slots para aliados."); if (enemySlots != null && enemySlots.Count != 6) Debug.LogWarning("Se esperan 6 slots para enemigos si ese es tu diseño (ej. 2 columnas x 3 filas). Ajusta si es diferente.");
    }
}