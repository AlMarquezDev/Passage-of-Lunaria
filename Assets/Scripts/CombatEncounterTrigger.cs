using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CombatEncounterTrigger : MonoBehaviour
{
    [Tooltip("El grupo de enemigos específico para este trigger de encuentro.")]
    public EnemyGroupData enemyGroupForThisTrigger;
    [Tooltip("Nombre de la escena de combate a cargar.")]
    public string battleSceneName = "03_CombatScene";
    private bool _triggeredOnce = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggeredOnce) return;

        if (other.CompareTag("Player"))
        {
            if (enemyGroupForThisTrigger == null)
            {
                Debug.LogError($"CombatEncounterTrigger en {gameObject.name}: 'Enemy Group For This Trigger' no está asignado.", this);
                return;
            }
            if (string.IsNullOrEmpty(battleSceneName))
            {
                Debug.LogError($"CombatEncounterTrigger en {gameObject.name}: 'Battle Scene Name' está vacío.", this);
                return;
            }

            Debug.Log($"CombatEncounterTrigger activado por {other.name} para el grupo: {enemyGroupForThisTrigger.name}");
            _triggeredOnce = true;

            if (CombatSessionData.Instance != null)
            {
                CombatSessionData.Instance.SavePreCombatState(
                    SceneManager.GetActiveScene().name,
                    other.transform.position
                );
                CombatSessionData.Instance.enemyGroup = enemyGroupForThisTrigger;
                if (GameManager.Instance != null && GameManager.Instance.partyMembers != null)
                {
                    CombatSessionData.Instance.partyMembers = new List<CharacterStats>(GameManager.Instance.partyMembers);
                }
                else { Debug.LogError("CombatEncounterTrigger: GameManager o party nulo."); _triggeredOnce = false; return; }
            }
            else { Debug.LogError("CombatEncounterTrigger: CombatSessionData es nulo."); _triggeredOnce = false; return; }

            CombatContext.EnemyGroupToLoad = enemyGroupForThisTrigger;

            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.LoadScene(battleSceneName, SceneTransition.TransitionContext.ToBattle);
            }
            else
            {
                Debug.LogError("CombatEncounterTrigger: SceneTransition.Instance is null! Loading scene directly.");
                SceneManager.LoadScene(battleSceneName);
                _triggeredOnce = false;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _triggeredOnce = false;
        }
    }
}