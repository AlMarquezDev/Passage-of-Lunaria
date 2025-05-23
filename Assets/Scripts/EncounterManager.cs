// EncounterManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EncounterManager : MonoBehaviour
{
    public static EncounterManager Instance { get; private set; }

    private List<EnemyGroupData> _possibleEncountersForCurrentMap;
    private int _stepsToTriggerEncounter;
    private int _minStepsForCurrentMap;
    private int _maxStepsForCurrentMap;
    private string _battleSceneForCurrentMap;
    private int _currentStepsTaken;
    private string _currentMapName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializeForMap(string mapName, List<EnemyGroupData> encountersForMap, int minSteps, int maxSteps, string battleSceneNameForThisMap)
    {
        _currentMapName = mapName;
        _possibleEncountersForCurrentMap = encountersForMap ?? new List<EnemyGroupData>();
        _battleSceneForCurrentMap = battleSceneNameForThisMap;
        _minStepsForCurrentMap = Mathf.Max(1, minSteps);
        _maxStepsForCurrentMap = Mathf.Max(_minStepsForCurrentMap, maxSteps);

        if (string.IsNullOrEmpty(_battleSceneForCurrentMap) && _possibleEncountersForCurrentMap.Count > 0)
        {
            Debug.LogError($"EncounterManager: battleSceneNameForThisMap is null or empty for map '{_currentMapName}' but encounters are defined. Random encounters will likely fail to load a battle scene.");
            _stepsToTriggerEncounter = int.MaxValue;
        }
        else if (_possibleEncountersForCurrentMap.Count > 0)
        {
            _stepsToTriggerEncounter = Random.Range(_minStepsForCurrentMap, _maxStepsForCurrentMap + 1);
        }
        else
        {
            _stepsToTriggerEncounter = int.MaxValue;
        }
        _currentStepsTaken = 0;
        Debug.Log($"EncounterManager Initialized for Map: '{_currentMapName}'. Battle Scene: '{_battleSceneForCurrentMap}'. Next encounter in approx. {_stepsToTriggerEncounter} steps. Possible groups: {_possibleEncountersForCurrentMap.Count}");
    }

    public void RegisterStep()
    {
        if (string.IsNullOrEmpty(_battleSceneForCurrentMap) || _possibleEncountersForCurrentMap == null || _possibleEncountersForCurrentMap.Count == 0 || _stepsToTriggerEncounter == int.MaxValue)
        {
            return;
        }
        _currentStepsTaken++;
        if (_currentStepsTaken >= _stepsToTriggerEncounter)
        {
            StartRandomEncounter();
            _currentStepsTaken = 0;
            _stepsToTriggerEncounter = Random.Range(_minStepsForCurrentMap, _maxStepsForCurrentMap + 1);
        }
    }

    private void StartRandomEncounter()
    {
        if (string.IsNullOrEmpty(_battleSceneForCurrentMap))
        {
            Debug.LogError("EncounterManager: _battleSceneForCurrentMap is not set. Cannot start encounter.");
            return;
        }
        if (_possibleEncountersForCurrentMap == null || _possibleEncountersForCurrentMap.Count == 0)
        {
            Debug.LogWarning("EncounterManager: Attempted to start random encounter, but no possible encounters are defined for the current map.");
            return;
        }

        EnemyGroupData selectedGroup = _possibleEncountersForCurrentMap[Random.Range(0, _possibleEncountersForCurrentMap.Count)];
        if (selectedGroup == null)
        {
            Debug.LogError($"EncounterManager: A null EnemyGroupData was selected randomly from the list for map '{_currentMapName}'. Check the 'Possible Encounters For This Map' list in MapEncounterData.");
            return;
        }

        Debug.Log($"EncounterManager: Starting random encounter on map '{_currentMapName}' with group: {selectedGroup.name}. Transitioning to battle scene: '{_battleSceneForCurrentMap}'");

        if (CombatSessionData.Instance != null)
        {
            CombatSessionData.Instance.enemyGroup = selectedGroup;
            CombatSessionData.Instance.customBattleMusic = MusicManager.Instance?.defaultBattleTheme; // Set custom battle music for random encounters
            if (GameManager.Instance != null && GameManager.Instance.partyMembers != null)
            {
                CombatSessionData.Instance.partyMembers = new List<CharacterStats>(GameManager.Instance.partyMembers);
            }
            else
            {
                Debug.LogError("EncounterManager: GameManager.Instance or its partyMembers is null. Cannot set party for CombatSessionData. Encounter aborted.");
                return;
            }
        }
        else
        {
            Debug.LogError("EncounterManager: CombatSessionData.Instance is null! Cannot set enemyGroup or partyMembers for combat. Encounter aborted.");
            return;
        }

        CombatContext.EnemyGroupToLoad = selectedGroup;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            CombatSessionData.Instance.SavePreCombatState(SceneManager.GetActiveScene().name, playerObject.transform.position);
        }
        else
        {
            Debug.LogError("EncounterManager: Player GameObject with tag 'Player' not found! Cannot save player position. Encounter aborted.");
            return;
        }

        if (SceneTransition.Instance != null)
        {
            SceneTransition.Instance.LoadScene(_battleSceneForCurrentMap, SceneTransition.TransitionContext.ToBattle);
        }
        else
        {
            Debug.LogError("EncounterManager: SceneTransition.Instance is null! Loading scene directly.");
            if (!string.IsNullOrEmpty(_battleSceneForCurrentMap))
                SceneManager.LoadScene(_battleSceneForCurrentMap);
        }
    }
}