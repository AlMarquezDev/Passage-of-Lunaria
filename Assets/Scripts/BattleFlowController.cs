using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CombatSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BattleUIFocusManager))]
public class BattleFlowController : MonoBehaviour
{
    public static BattleFlowController Instance { get; private set; }

    [Header("UI References (Assigned in Editor - mainly for debug visibility)")]
    [SerializeField] private BattleCommandUI _editorCommandUIRef;
    [SerializeField] private AbilitySelectorUI _editorAbilitySelectorUIRef;
    [SerializeField] private ItemSelectorUI _editorItemSelectorUIRef;
    [SerializeField] private TargetSelector _editorTargetSelectorRef;

    private List<CharacterStats> currentParty;
    private List<EnemyInstance> currentEnemies;
    private List<BattleAction> plannedActions = new List<BattleAction>();
    private Dictionary<CharacterStats, AllyWorldAnchor> visualAnchorMap = new Dictionary<CharacterStats, AllyWorldAnchor>();

    private int currentCommandCharacterIndex = 0;
    private AllyWorldAnchor currentlySteppedForwardAnchor = null;
    private bool isCommandPhaseActive = false;
    private bool isBfcAwakeComplete = false;

    private void Awake()
    {
        if (isBfcAwakeComplete && Instance == this)
        {
            return;
        }

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            isBfcAwakeComplete = true;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            isBfcAwakeComplete = true;
        }
    }

    private void OnEnable()
    {
        TurnManager.OnTurnExecutionComplete += HandleTurnExecutionComplete;
    }

    private void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.OnTurnExecutionComplete -= HandleTurnExecutionComplete;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void BeginBattle(List<CharacterStats> party, List<EnemyInstance> enemies)
    {
        Debug.Log("<<<< BattleFlowController: BeginBattle >>>>");
        currentParty = party?.ToList() ?? new List<CharacterStats>();
        currentEnemies = enemies?.ToList() ?? new List<EnemyInstance>();
        Debug.Log($"[BFC] BeginBattle: Party Count = {currentParty.Count}, Enemy Count = {currentEnemies.Count}");

        InitializeVisualAnchors();

        plannedActions.Clear();
        currentCommandCharacterIndex = 0;
        currentlySteppedForwardAnchor = null;
        isCommandPhaseActive = false;

        Action startBattleSequenceAction = () =>
        {
            Debug.Log("[BFC] Transición de entrada completa. Iniciando música y fase de comandos.");
            if (MusicManager.Instance != null)
            {
                AudioClip themeToPlay = null;
                if (CombatSessionData.Instance != null)
                {
                    themeToPlay = CombatSessionData.Instance.customBattleMusic;
                }
                MusicManager.Instance.PlayBattleTheme(themeToPlay);
            }
            else
            {
                Debug.LogWarning("[BFC] MusicManager.Instance es null.");
            }
            StartCommandPhase();
        };

        if (SceneTransition.Instance != null)
        {
            Debug.Log("[BFC] Solicitando a SceneTransition que revele la escena de batalla.");
            SceneTransition.Instance.RevealSceneAfterBattleLoad(startBattleSequenceAction);
        }
        else
        {
            Debug.LogWarning("[BFC] SceneTransition.Instance es null. Iniciando batalla directamente.");
            startBattleSequenceAction();
        }
    }

    public void StartCommandPhase()
    {
        Debug.Log("<<<< BattleFlowController: StartCommandPhase >>>>");
        ReturnPreviouslySteppedCharacter(null);
        plannedActions.Clear();
        currentCommandCharacterIndex = 0;
        isCommandPhaseActive = true;
        AskCommandForNextCharacter();
    }

    private void HandleTurnExecutionComplete()
    {
        Debug.LogWarning("<<<< BattleFlowController: Evento OnTurnExecutionComplete RECIBIDO >>>>");
        var freshParty = GetParty()?.Where(p => p != null).ToList() ?? new List<CharacterStats>();
        var freshEnemies = GetEnemies()?.Where(e => e != null).ToList() ?? new List<EnemyInstance>();

        currentParty = freshParty;
        currentEnemies = freshEnemies;

        bool partyDefeated = !currentParty.Any(p => p.currentHP > 0);
        bool enemiesDefeated = !currentEnemies.Any(e => e.IsAlive);

        Debug.Log($"[BFC] HandleTurnExecutionComplete: Party Defeated={partyDefeated}, Enemies Defeated={enemiesDefeated}");

        if (CombatEndManager.Instance != null)
        {
            CombatEndManager.Instance.CheckForEndOfBattle(currentParty, currentEnemies);
        }
        else
        {
            Debug.LogError("CombatEndManager.Instance is NULL! Cannot check for end of battle.");
        }

        if (!partyDefeated && !enemiesDefeated)
        {
            Debug.Log("[BFC] HandleTurnExecutionComplete: Iniciando siguiente fase de comandos...");
            StartCommandPhase();
        }
        else
        {
            Debug.Log("[BFC] HandleTurnExecutionComplete: Fin de batalla detectado. No se inicia nueva fase de comandos.");
            isCommandPhaseActive = false;
        }
    }

    private void AskCommandForNextCharacter()
    {
        if (!isCommandPhaseActive)
        {
            return;
        }
        if (currentParty == null)
        {
            EndCommandPhaseAndBeginExecution();
            return;
        }

        int searchIndex = currentCommandCharacterIndex;
        CharacterStats nextCharacter = null;
        while (searchIndex < currentParty.Count)
        {
            if (currentParty[searchIndex] != null && currentParty[searchIndex].currentHP > 0)
            {
                nextCharacter = currentParty[searchIndex];
                currentCommandCharacterIndex = searchIndex;
                break;
            }
            searchIndex++;
        }

        if (nextCharacter == null)
        {
            EndCommandPhaseAndBeginExecution();
            return;
        }

        AllyWorldAnchor characterAnchor = GetVisualAnchorForCharacter(nextCharacter);
        if (characterAnchor != null)
        {
            if (currentlySteppedForwardAnchor != null && currentlySteppedForwardAnchor != characterAnchor)
            {
                ReturnPreviouslySteppedCharacter(null);
            }
            if (currentlySteppedForwardAnchor != characterAnchor)
            {
                characterAnchor.StepForwardForTurn(null);
                currentlySteppedForwardAnchor = characterAnchor;
            }
        }

        if (BattleCommandUI.Instance != null)
        {
            BattleCommandUI.Instance.Open(nextCharacter, command => HandleCommandSelection(nextCharacter, command));
        }
        else
        {
            isCommandPhaseActive = false;
        }
    }

    private void HandleCommandSelection(CharacterStats character, BattleCommand command)
    {
        if (!isCommandPhaseActive) return;
        BattleCommandUI.Instance?.Close();

        List<EnemyInstance> aliveEnemies = currentEnemies?.Where(e => e != null && e.IsAlive).ToList() ?? new List<EnemyInstance>();
        List<CharacterStats> aliveAllies = currentParty?.Where(p => p != null && p.currentHP > 0).ToList() ?? new List<CharacterStats>();

        switch (command)
        {
            case BattleCommand.Attack:
                if (!aliveEnemies.Any())
                {
                    if (TargetSelector.Instance?.audioSource != null && BattleCommandUI.Instance?.errorSound != null)
                    {
                        TargetSelector.Instance.audioSource.PlayOneShot(BattleCommandUI.Instance.errorSound);
                    }
                    ReturnToCommandSelection(character);
                    return;
                }
                TargetSelector.Instance?.OpenEnemyTargets(character, aliveEnemies,
                    target => RecordActionAndAskNext(new BattleAction(character, command, target))
                );
                break;
            case BattleCommand.Defend:
            case BattleCommand.Flee:
                RecordActionAndAskNext(new BattleAction(character, command, null));
                break;
            case BattleCommand.Special:
                AbilitySelectorUI.Instance?.Open(character);
                break;
            case BattleCommand.Item:
                ItemSelectorUI.Instance?.Open(character);
                break;
            default:
                RecordActionAndAskNext(new BattleAction(character, command, null));
                break;
        }
    }

    public void ReceiveAbilitySelection(CharacterStats character, AbilityData ability, object target)
    {
        if (!isCommandPhaseActive) return;
        if (character == null || ability == null)
        {
            ReturnToCommandSelection(character);
            return;
        }
        RecordActionAndAskNext(new BattleAction(character, BattleCommand.Special, target, ability, null));
    }

    public void ReceiveItemSelection(CharacterStats character, ConsumableItem item, object target)
    {
        if (!isCommandPhaseActive) return;
        if (character == null || item == null)
        {
            ReturnToCommandSelection(character);
            return;
        }
        RecordActionAndAskNext(new BattleAction(character, BattleCommand.Item, target, null, item));
    }

    private void RecordActionAndAskNext(BattleAction action)
    {
        if (!isCommandPhaseActive) return;
        if (action == null || action.actor == null)
        {
            currentCommandCharacterIndex++;
            AskCommandForNextCharacter();
            return;
        }
        plannedActions.Add(action);

        if (this == null || gameObject == null)
        {
            return;
        }
        StartCoroutine(StepBackAndAskNext());
    }

    private IEnumerator StepBackAndAskNext()
    {
        if (currentlySteppedForwardAnchor != null)
        {
            AllyWorldAnchor anchorToReturn = currentlySteppedForwardAnchor;
            currentlySteppedForwardAnchor = null;
            yield return StartCoroutine(anchorToReturn.ReturnToOriginalPositionAndWait(null));
        }
        currentCommandCharacterIndex++;
        AskCommandForNextCharacter();
    }

    public void ReturnToCommandSelection(CharacterStats character)
    {
        if (!isCommandPhaseActive)
        {
            return;
        }
        if (character == null)
        {
            StartCommandPhase();
            return;
        }
        CloseSubUIs();
        int characterIndex = currentParty?.FindIndex(p => p == character) ?? -1;
        if (characterIndex >= 0)
        {
            currentCommandCharacterIndex = characterIndex;
            AskCommandForNextCharacter();
        }
        else
        {
            currentCommandCharacterIndex = 0;
            AskCommandForNextCharacter();
        }
    }

    public void GoToPreviousCommandCharacter()
    {
        if (!isCommandPhaseActive || currentCommandCharacterIndex <= 0 || currentParty == null)
        {
            if (BattleCommandUI.Instance != null && BattleCommandUI.Instance.errorSound != null && BattleCommandUI.Instance.audioSource != null)
            {
                BattleCommandUI.Instance.audioSource.PlayOneShot(BattleCommandUI.Instance.errorSound);
            }
            return;
        }
        int previousValidIndex = -1;
        for (int i = currentCommandCharacterIndex - 1; i >= 0; i--)
        {
            if (i < currentParty.Count && currentParty[i] != null && currentParty[i].currentHP > 0)
            {
                previousValidIndex = i;
                break;
            }
        }
        if (previousValidIndex != -1)
        {
            ReturnPreviouslySteppedCharacter(null);
            CharacterStats charToReselect = currentParty[previousValidIndex];
            int actionToRemoveIndex = plannedActions.FindLastIndex(a => a.character == charToReselect);
            if (actionToRemoveIndex >= 0)
            {
                plannedActions.RemoveAt(actionToRemoveIndex);
            }
            currentCommandCharacterIndex = previousValidIndex;
            AskCommandForNextCharacter();
        }
        else
        {
            if (BattleCommandUI.Instance != null && BattleCommandUI.Instance.errorSound != null && BattleCommandUI.Instance.audioSource != null)
            {
                BattleCommandUI.Instance.audioSource.PlayOneShot(BattleCommandUI.Instance.errorSound);
            }
        }
    }

    private void EndCommandPhaseAndBeginExecution()
    {
        if (!isCommandPhaseActive && plannedActions.Count == 0)
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.BeginTurnExecution(new List<BattleAction>(plannedActions));
            }
            isCommandPhaseActive = false;
            CloseAllActionUIs();
            return;
        }
        if (!isCommandPhaseActive) return;

        isCommandPhaseActive = false;
        CloseAllActionUIs();

        ReturnPreviouslySteppedCharacter(() =>
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.BeginTurnExecution(new List<BattleAction>(plannedActions));
            }
        });
    }

    private void InitializeVisualAnchors()
    {
        visualAnchorMap.Clear();
        var anchorsInScene = FindObjectsOfType<AllyWorldAnchor>(true);

        if (currentParty == null)
        {
            return;
        }

        foreach (var anchor in anchorsInScene)
        {
            if (anchor == null) continue;
            if (anchor.owner == null)
            {
                anchor.gameObject.SetActive(false);
                continue;
            }

            if (currentParty.Contains(anchor.owner))
            {
                if (!visualAnchorMap.ContainsKey(anchor.owner))
                {
                    visualAnchorMap.Add(anchor.owner, anchor);
                    if (anchor.owner.targetAnchor == null)
                    {
                        Transform cursorAnchorPoint = anchor.transform.Find("TargetCursorAnchor");
                        anchor.owner.targetAnchor = cursorAnchorPoint ?? anchor.transform;
                    }
                    anchor.gameObject.SetActive(true);
                }
            }
            else
            {
                anchor.gameObject.SetActive(false);
            }
        }
        foreach (var member in currentParty)
        {
            if (member == null) continue;
            if (!visualAnchorMap.ContainsKey(member) || visualAnchorMap[member] == null)
            {
                Debug.LogError($"[BFC] ¡¡ERROR CRÍTICO!! No se encontró o mapeó un AllyWorldAnchor válido para {member.characterName}.");
            }
        }
    }

    public AllyWorldAnchor GetVisualAnchorForCharacter(CharacterStats character)
    {
        if (character == null) return null;
        if (visualAnchorMap.TryGetValue(character, out AllyWorldAnchor anchor))
        {
            if (anchor != null) return anchor;
            else
            {
                visualAnchorMap.Remove(character);
            }
        }
        return null;
    }

    private void ReturnPreviouslySteppedCharacter(Action onComplete)
    {
        if (currentlySteppedForwardAnchor != null)
        {
            AllyWorldAnchor anchorToReturn = currentlySteppedForwardAnchor;
            currentlySteppedForwardAnchor = null;
            StartCoroutine(anchorToReturn.ReturnToOriginalPositionAndWait(onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    private void CloseAllActionUIs()
    {
        BattleCommandUI.Instance?.Close();
        AbilitySelectorUI.Instance?.Close();
        ItemSelectorUI.Instance?.Close();
        TargetSelector.Instance?.Close();
        BattleUIFocusManager.Instance?.ClearAll();
    }

    private void CloseSubUIs()
    {
        AbilitySelectorUI.Instance?.Close();
        ItemSelectorUI.Instance?.Close();
        TargetSelector.Instance?.Close();
    }

    public bool CanGoToPreviousCharacter()
    {
        return isCommandPhaseActive && currentCommandCharacterIndex > 0 && currentParty != null && currentParty.Count > 0;
    }

    public IReadOnlyList<CharacterStats> GetParty() => currentParty?.AsReadOnly();
    public IReadOnlyList<EnemyInstance> GetEnemies() => currentEnemies?.AsReadOnly();
}