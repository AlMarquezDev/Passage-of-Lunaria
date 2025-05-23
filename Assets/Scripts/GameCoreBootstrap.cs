using UnityEngine;
using UnityEngine.SceneManagement;

public class GameCoreBootstrap : MonoBehaviour
{
    [Header("Prefabs a invocar si faltan")]
    public GameObject gameManagerPrefab;
    public GameObject inventorySystemPrefab;
    public GameObject itemDatabasePrefab;
    public GameObject abilityDatabasePrefab;
    public GameObject classDatabasePrefab;
    public GameObject encounterManagerPrefab;
    public GameObject musicManagerPrefab;
    public GameObject combatTransitionFXPrefab;
    public GameObject damageEffectsManagerPrefab;
    public GameObject sceneTransitionPrefab;
    public GameObject battleFlowControllerPrefab;
    public GameObject sessionManagerForAuthPrefab;
    public GameObject combatSessionDataPrefab;

    private void Awake()
    {
        Debug.LogError($"GAME CORE BOOTSTRAP: AWAKE STARTING NOW! Escena: {SceneManager.GetActiveScene().name}, Tiempo: {Time.time}, Frame: {Time.frameCount}");
        TrySpawnSingleton<GameManager>(gameManagerPrefab);
        TrySpawnSingleton<InventorySystem>(inventorySystemPrefab);
        TrySpawnSingleton<ItemDatabase>(itemDatabasePrefab);
        TrySpawnSingleton<AbilityDatabase>(abilityDatabasePrefab);
        TrySpawnSingleton<ClassDatabase>(classDatabasePrefab);
        TrySpawnSingleton<EncounterManager>(encounterManagerPrefab);
        TrySpawnSingleton<MusicManager>(musicManagerPrefab);


        TrySpawnSingleton<CombatTransitionFX>(combatTransitionFXPrefab);
        TrySpawnSingleton<DamageEffectsManager>(damageEffectsManagerPrefab);
        TrySpawnSingleton<SceneTransition>(sceneTransitionPrefab);

        Debug.LogError($"GAME CORE BOOTSTRAP: Intentando instanciar BattleFlowController... Prefab asignado: {(battleFlowControllerPrefab == null ? "NO" : "SI - " + battleFlowControllerPrefab.name)}");
        TrySpawnSingleton<BattleFlowController>(battleFlowControllerPrefab); Debug.LogError("GAME CORE BOOTSTRAP: Intento de instanciar BattleFlowController COMPLETO.");

        TrySpawnSingleton<SessionManager>(sessionManagerForAuthPrefab);
        TrySpawnSingleton<CombatSessionData>(combatSessionDataPrefab);

        Debug.LogError($"GAME CORE BOOTSTRAP: AWAKE FINALIZADO! Escena: {SceneManager.GetActiveScene().name}, Tiempo: {Time.time}, Frame: {Time.frameCount}");
    }

    private void TrySpawnSingleton<T>(GameObject prefab) where T : MonoBehaviour
    {
        T existingInstance = FindObjectOfType<T>();
        if (existingInstance == null)
        {
            if (prefab != null)
            {
                string msg = $"[GameCoreBootstrap] No se encontró {typeof(T).Name}. INSTANCIANDO desde prefab: {prefab.name}. Frame: {Time.frameCount}";
                if (typeof(T) == typeof(BattleFlowController) || typeof(T) == typeof(BattleUIFocusManager)) Debug.LogError(msg); else Debug.Log(msg);

                GameObject instanceGO = Instantiate(prefab);
                T componentOnNewInstance = instanceGO.GetComponent<T>();

                if (componentOnNewInstance == null)
                {
                    string errorMsg = $"[GameCoreBootstrap] ¡ERROR GRAVE! El prefab {prefab.name} instanciado ({instanceGO.name}) NO contiene el componente {typeof(T).Name}. Frame: {Time.frameCount}";
                    if (typeof(T) == typeof(BattleFlowController) || typeof(T) == typeof(BattleUIFocusManager)) Debug.LogError(errorMsg); else Debug.LogError(errorMsg);
                }
                else
                {
                    string successMsg = $"[GameCoreBootstrap] Instanciado {typeof(T).Name} ({instanceGO.name}) desde prefab. Su propio Awake() debería manejar DontDestroyOnLoad y la asignación de Instance. Frame: {Time.frameCount}";
                    if (typeof(T) == typeof(BattleFlowController) || typeof(T) == typeof(BattleUIFocusManager)) Debug.LogError(successMsg); else Debug.Log(successMsg);
                }
            }
            else
            {
                string errorMsgNullPrefab = $"[GameCoreBootstrap] No se encontró {typeof(T).Name} y el prefab para él es NULL en GameCoreBootstrap. No se puede instanciar. Frame: {Time.frameCount}";
                if (typeof(T) == typeof(BattleFlowController) || typeof(T) == typeof(BattleUIFocusManager)) Debug.LogError(errorMsgNullPrefab); else Debug.LogError(errorMsgNullPrefab);
            }
        }
        else
        {
            string msgExists = $"[GameCoreBootstrap] {typeof(T).Name} YA EXISTE en la escena (GameObject: {existingInstance.gameObject.name}, ID: {existingInstance.GetInstanceID()}). No se instancia desde prefab. Frame: {Time.frameCount}";
            if (typeof(T) == typeof(BattleFlowController) || typeof(T) == typeof(BattleUIFocusManager)) Debug.LogError(msgExists); else Debug.Log(msgExists);
        }
    }
}