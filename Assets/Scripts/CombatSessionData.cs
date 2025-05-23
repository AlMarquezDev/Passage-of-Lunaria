using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Cinemachine;

public class CombatSessionData : MonoBehaviour
{
    public static CombatSessionData Instance;

    public List<CharacterStats> partyMembers;
    public EnemyGroupData enemyGroup;
    public AudioClip customBattleMusic;

    private string previousScene;
    private Vector3 previousPlayerPosition;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

    public void SavePreCombatState(string sceneName, Vector3 playerPosition)
    {
        previousScene = sceneName;
        previousPlayerPosition = playerPosition;
        Debug.Log($"[CombatSessionData] PreCombatState Saved: Scene='{previousScene}', Pos={playerPosition}");
    }

    public string GetPreviousSceneName()
    {
        return previousScene;
    }

    public Vector3 GetPreviousPlayerPosition()
    {
        return previousPlayerPosition;
    }

    public void FinalizeReturnToMap()
    {
        Debug.Log($"[CombatSessionData] FinalizeReturnToMap: Reposicionando jugador en '{previousScene}' y restaurando música.");
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Vector3 targetPlayerPosition = GetPreviousPlayerPosition();
            Vector3 oldPlayerPosition = player.transform.position;

            player.transform.position = targetPlayerPosition;
            Debug.Log($"[CombatSessionData] Jugador reposicionado en: {targetPlayerPosition}");

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                CinemachineBrain cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
                if (cinemachineBrain != null && cinemachineBrain.ActiveVirtualCamera != null)
                {
                    Vector3 positionDelta = targetPlayerPosition - oldPlayerPosition;
                    cinemachineBrain.ActiveVirtualCamera.OnTargetObjectWarped(player.transform, positionDelta);
                    Debug.Log($"[CombatSessionData] CinemachineBrain notificado del warp del jugador. Delta: {positionDelta}");
                }
                else if (cinemachineBrain == null)
                {
                    Debug.LogWarning("[CombatSessionData] CinemachineBrain no encontrado en la cámara principal. No se puede notificar warp.");
                }
                else if (cinemachineBrain.ActiveVirtualCamera == null)
                {
                    Debug.LogWarning("[CombatSessionData] CinemachineBrain no tiene una VCam activa. No se puede notificar warp.");
                }
            }
            else
            {
                Debug.LogWarning("[CombatSessionData] Cámara principal no encontrada. No se puede notificar warp a Cinemachine.");
            }
        }
        else
        {
            Debug.LogWarning("[CombatSessionData] FinalizeReturnToMap: No se encontró 'Player' tag. No se pudo reposicionar ni notificar a Cinemachine.");
        }

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ReturnToMapMusic();
            Debug.Log($"[CombatSessionData] Llamada a MusicManager.ReturnToMapMusic() para '{previousScene}'.");
        }
        else
        {
            Debug.LogWarning("[CombatSessionData] FinalizeReturnToMap: MusicManager.Instance es null.");
        }
    }

    public void ReturnToPreviousScene()
    {
        if (string.IsNullOrEmpty(previousScene))
        {
            Debug.LogError("[CombatSessionData] ReturnToPreviousScene: previousScene name is null or empty! Cannot return.");
            return;
        }
        if (SceneTransition.Instance != null)
        {
            Debug.Log($"[CombatSessionData] ReturnToPreviousScene: Usando SceneTransition para volver a '{previousScene}'.");
            SceneTransition.Instance.LoadScene(previousScene, SceneTransition.TransitionContext.FromBattle);
        }
        else
        {
            Debug.LogError("[CombatSessionData] SceneTransition.Instance es null. Usando corutina de carga directa como fallback.");
            StartCoroutine(ReturnToPreviousSceneCoroutineDirect());
        }
    }

    private IEnumerator ReturnToPreviousSceneCoroutineDirect()
    {
        Debug.Log($"[CombatSessionData] ReturnToPreviousSceneCoroutineDirect: Iniciando carga de '{previousScene}'.");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(previousScene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        Debug.Log($"[CombatSessionData] Escena '{previousScene}' cargada asíncronamente (directo).");
        yield return null;

        FinalizeReturnToMap();

        Debug.LogWarning("[CombatSessionData] Escena revelada directamente sin transición de barras (fallback).");
    }
}