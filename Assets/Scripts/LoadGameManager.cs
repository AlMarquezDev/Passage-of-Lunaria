using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Cinemachine;

public class LoadGameManager : MonoBehaviour, ISaveSlotHandler
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void ShowLoadPanel(SaveSlotPanelUI panelToUse)
    {
        if (panelToUse == null)
        {
            Debug.LogError("LoadGameManager: ShowLoadPanel fue llamado con un panel NULO.");
            return;
        }
        StartCoroutine(FetchAndShowLoadPanel(panelToUse));
    }

    private IEnumerator FetchAndShowLoadPanel(SaveSlotPanelUI activeSlotPanel)
    {
        string url = "https://rpgapi-dgtn.onrender.com/game/save-states";
        UnityWebRequest request = UnityWebRequest.Get(url);

        if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.GetToken()))
        {
            request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Instance.GetToken());
        }
        else
        {
            Debug.LogError("LoadGameManager: No hay token de sesión disponible para FetchAndShowLoadPanel.");
            if (activeSlotPanel != null) activeSlotPanel.gameObject.SetActive(false);
            yield break;
        }

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch save states: " + request.error + " | Response: " + request.downloadHandler.text);
            if (activeSlotPanel != null) activeSlotPanel.gameObject.SetActive(false);
            yield break;
        }

        string json = request.downloadHandler.text;
        SaveStateDTOWrapperList wrapper = null;
        try
        {
            if (string.IsNullOrWhiteSpace(json) || json == "[]" || json == "null")
            {
                Debug.LogWarning("FetchAndShowLoadPanel: No save states data received from server or data is empty.");
                wrapper = new SaveStateDTOWrapperList { states = new List<SaveStateDTOWrapper>() };
            }
            else
            {
                wrapper = JsonUtility.FromJson<SaveStateDTOWrapperList>("{\"states\":" + json + "}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing save states JSON: " + e.Message + "\nJSON: " + json);
            if (activeSlotPanel != null) activeSlotPanel.gameObject.SetActive(false);
            yield break;
        }

        if (wrapper == null)
        {
            Debug.LogError("Parsed save states wrapper is null even after attempting to parse. JSON: " + json);
            wrapper = new SaveStateDTOWrapperList { states = new List<SaveStateDTOWrapper>() };
        }
        if (wrapper.states == null)
        {
            Debug.LogWarning("Parsed save states list (wrapper.states) is null. Initializing to empty list. JSON: " + json);
            wrapper.states = new List<SaveStateDTOWrapper>();
        }

        if (activeSlotPanel != null)
        {
            activeSlotPanel.Open(wrapper.states, SaveSlotMode.Load, this);
        }
        else
        {
            Debug.LogError("LoadGameManager (FetchAndShowLoadPanel): activeSlotPanel es null. Esto no debería ocurrir si ShowLoadPanel lo validó.");
        }
    }

    public void OnSlotSelected(int slot)
    {
        StartCoroutine(LoadGameFromSlot(slot));
    }

    private IEnumerator LoadGameFromSlot(int slot)
    {
        string url = $"https://rpgapi-dgtn.onrender.com/game/save-states/{slot}";
        UnityWebRequest request = UnityWebRequest.Get(url);
        if (SessionManager.Instance != null && !string.IsNullOrEmpty(SessionManager.Instance.GetToken()))
        {
            request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Instance.GetToken());
        }
        else
        {
            Debug.LogError("LoadGameManager: No hay token de sesión disponible para LoadGameFromSlot.");
            yield break;
        }

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load game: " + request.error + " | Response: " + request.downloadHandler.text);
            yield break;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log("Raw JSON response: " + responseText);

        SaveStateDTOWrapper wrapper = null;
        SaveStateData saveData = null;
        try
        {
            wrapper = JsonUtility.FromJson<SaveStateDTOWrapper>(responseText);
            if (wrapper != null && !string.IsNullOrEmpty(wrapper.saveData))
            {
                saveData = JsonUtility.FromJson<SaveStateData>(wrapper.saveData);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing loaded game JSON: " + e.Message + "\nJSON: " + responseText);
            yield break;
        }

        if (saveData == null)
        {
            Debug.LogError("Failed to parse SaveStateData from loaded game slot. saveData is null. Response was: " + responseText);
            yield break;
        }

        TemporarySaveDataBuffer.Data = saveData;

        SceneManager.sceneLoaded += OnSceneLoaded;
        GameLoadContext.IsLoadingFromSave = true;
        GameLoadContext.HasGameFinishedLoading = false;

        if (SceneTransition.Instance != null)
        {
            Debug.Log($"LoadGameManager: Iniciando transición a escena '{saveData.sceneName}' usando SceneTransition.Instance.");
            SceneTransition.Instance.LoadScene(saveData.sceneName, SceneTransition.TransitionContext.Generic);
        }
        else
        {
            Debug.LogError("LoadGameManager: SceneTransition.Instance es null. Cargando escena directamente.");
            SceneManager.LoadScene(saveData.sceneName);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(ApplySaveDataAfterSceneLoad());
    }

    private IEnumerator ApplySaveDataAfterSceneLoad()
    {
        yield return null;

        SaveStateData saveData = TemporarySaveDataBuffer.Data;
        if (saveData == null)
        {
            Debug.LogError("ApplySaveDataAfterSceneLoad: saveData en TemporarySaveDataBuffer es null!");
            yield break;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (saveData.playerPosition != null)
            {
                Vector3 targetPlayerPosition = saveData.playerPosition.ToVector3();
                Vector3 oldPlayerPosition = player.transform.position;
                player.transform.position = targetPlayerPosition;
                Debug.Log($"[LoadGameManager] Jugador reposicionado por carga de partida en: {targetPlayerPosition}");

                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    CinemachineBrain cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
                    if (cinemachineBrain != null && cinemachineBrain.ActiveVirtualCamera != null)
                    {
                        Vector3 positionDelta = targetPlayerPosition - oldPlayerPosition;
                        cinemachineBrain.ActiveVirtualCamera.OnTargetObjectWarped(player.transform, positionDelta);
                        Debug.Log($"[LoadGameManager] CinemachineBrain notificado del warp del jugador por carga. Delta: {positionDelta}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("ApplySaveDataAfterSceneLoad: saveData.playerPosition es null.");
            }
        }
        else
        {
            Debug.LogWarning("ApplySaveDataAfterSceneLoad: No se encontró GameObject con tag 'Player'.");
        }

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.partyMembers == null)
            {
                GameManager.Instance.partyMembers = new List<CharacterStats>();
            }
            else
            {
                GameManager.Instance.partyMembers.Clear();
            }

            if (saveData.partyMembers == null)
            {
                Debug.LogWarning("ApplySaveDataAfterSceneLoad: saveData.partyMembers es null!");
            }
            else
            {
                foreach (var savedChar in saveData.partyMembers)
                {
                    if (savedChar == null)
                    {
                        Debug.LogWarning("ApplySaveDataAfterSceneLoad: Encontrado savedChar nulo en la lista.");
                        continue;
                    }
                    CharacterStats stats = new CharacterStats
                    {
                        characterName = savedChar.name,
                        characterJob = EnumUtility.Parse<CharacterJob>(savedChar.job),
                        level = savedChar.level,
                        currentExp = savedChar.experience,
                        currentHP = savedChar.currentHP,
                        maxHP = savedChar.maxHP,
                        currentMP = savedChar.currentMP,
                        maxMP = savedChar.maxMP,
                        strength = savedChar.strength,
                        defense = savedChar.defense,
                        intelligence = savedChar.intelligence,
                        agility = savedChar.agility,
                        baseStrength = savedChar.baseStrength,
                        baseDefense = savedChar.baseDefense,
                        baseIntelligence = savedChar.baseIntelligence,
                        baseAgility = savedChar.baseAgility,
                        baseMaxHP = savedChar.baseMaxHP,
                        baseMaxMP = savedChar.baseMaxMP,
                        knownAbilities = new List<AbilityData>()
                    };
                    stats.expToNextLevel = StatFormulaUtility.GetExpForLevel(stats.level);
                    stats.rightHand = ItemDatabase.Instance?.GetWeaponByName(savedChar.rightHandID);
                    stats.leftHand = ItemDatabase.Instance?.GetArmorByName(savedChar.leftHandID);
                    stats.head = ItemDatabase.Instance?.GetArmorByName(savedChar.headID);
                    stats.body = ItemDatabase.Instance?.GetArmorByName(savedChar.bodyID);
                    stats.accessory = ItemDatabase.Instance?.GetArmorByName(savedChar.accessoryID);
                    if (savedChar.knownAbilitiesIDs != null)
                    {
                        foreach (var abilityId in savedChar.knownAbilitiesIDs)
                        {
                            if (string.IsNullOrEmpty(abilityId)) continue;
                            var ability = AbilityDatabase.Instance?.GetByName(abilityId);
                            if (ability != null) stats.knownAbilities.Add(ability);
                        }
                    }
                    GameManager.Instance.partyMembers.Add(stats);
                }
            }
        }
        else
        {
            Debug.LogError("ApplySaveDataAfterSceneLoad: GameManager.Instance es null!");
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ClearInventory();
            if (saveData.inventory != null)
            {
                foreach (var entry in saveData.inventory)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.itemID)) continue;
                    ItemBase item = ItemDatabase.Instance?.GetByName(entry.itemID);
                    if (item != null)
                    {
                        InventorySystem.Instance.AddItem(item, entry.quantity);
                    }
                    else
                    {
                        Debug.LogWarning($"ApplySaveDataAfterSceneLoad: Item con ID '{entry.itemID}' no encontrado en ItemDatabase.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("ApplySaveDataAfterSceneLoad: saveData.inventory es null!");
            }
        }
        else
        {
            Debug.LogError("ApplySaveDataAfterSceneLoad: InventorySystem.Instance es null!");
        }
        GameLoadContext.HasGameFinishedLoading = true;
        Debug.Log("ApplySaveDataAfterSceneLoad: Carga de datos aplicada.");
    }
}