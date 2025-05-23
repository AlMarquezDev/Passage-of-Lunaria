// VictoryScreenManager.cs
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text; // Necesario para StringBuilder

public class VictoryScreenManager : MonoBehaviour
{
    public static VictoryScreenManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject screenRoot;
    public TMP_Text totalExpText;
    public TMP_Text itemsGainedLabel;
    public TMP_Text itemsGainedText;
    public Transform characterProgressionLayoutGroup;
    public GameObject characterDisplayPrefab; // Prefab de CharacterVictoryDisplay
    public TMP_Text continuePromptText;

    private System.Action _onContinueCallback;
    private bool _canProceedWithEnter = false;
    private List<CharacterStatsSnapshot> _partyStateBeforeRewards;

    [System.Serializable]
    public class CharacterStatsSnapshot
    {
        public string characterName;
        public int level;
        public int currentExp;
        public int expToNextLevel;
        public List<AbilityData> knownAbilities;

        public CharacterStatsSnapshot(CharacterStats stats)
        {
            if (stats == null)
            {
                Debug.LogError("CharacterStatsSnapshot constructor received null stats!");
                characterName = "Unknown";
                level = 1;
                currentExp = 0;
                expToNextLevel = 100;
                knownAbilities = new List<AbilityData>();
                return;
            }

            characterName = stats.characterName;
            level = stats.level;
            currentExp = stats.currentExp;
            expToNextLevel = stats.expToNextLevel;

            if (expToNextLevel <= 0 && level < 99) // Max level 99
            {
                if (GameManager.Instance != null && GameManager.Instance.expCurve != null)
                {
                    expToNextLevel = GameManager.Instance.expCurve.GetExpRequiredForLevel(level);
                }
                else
                {
                    Debug.LogWarning($"GameManager.Instance or expCurve is null while creating snapshot for {characterName}. expToNextLevel might be incorrect. Defaulting to 100.");
                    expToNextLevel = 100; // Fallback
                }
            }
            knownAbilities = stats.knownAbilities != null ? new List<AbilityData>(stats.knownAbilities) : new List<AbilityData>();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        if (screenRoot != null)
        {
            screenRoot.SetActive(false);
        }
        else
        {
            Debug.LogError("VictoryScreenManager: screenRoot is not assigned in the Inspector!");
        }
    }

    private void Update()
    {
        if (screenRoot != null && screenRoot.activeSelf && _canProceedWithEnter)
        {
            bool canInteract = true;
            if (BattleUIFocusManager.Instance != null)
            {
                canInteract = BattleUIFocusManager.Instance.CanInteract(this);
            }

            if (canInteract)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    Proceed();
                }
            }
        }
    }

    private void Proceed()
    {
        _canProceedWithEnter = false;
        if (screenRoot != null)
        {
            screenRoot.SetActive(false);
        }

        if (BattleUIFocusManager.Instance != null)
        {
            BattleUIFocusManager.Instance.ClearFocus(this);
        }

        _onContinueCallback?.Invoke();
        _onContinueCallback = null;
    }

    public void ShowScreen(List<CharacterStats> victoriousCharacters,
                           List<CharacterStatsSnapshot> partyStateBeforeRewards,
                           int expGainedByPartyThisFight,
                           List<ItemBase> itemsDropped,
                           System.Action onContinueCallback)
    {
        if (screenRoot == null || characterDisplayPrefab == null || characterProgressionLayoutGroup == null)
        {
            Debug.LogError("VictoryScreenManager is missing critical UI references! Cannot show screen.");
            onContinueCallback?.Invoke();
            return;
        }

        _partyStateBeforeRewards = partyStateBeforeRewards ?? new List<CharacterStatsSnapshot>();
        _onContinueCallback = onContinueCallback;
        _canProceedWithEnter = false;

        if (continuePromptText != null)
        {
            continuePromptText.text = "Press Enter to Continue";
            continuePromptText.gameObject.SetActive(false);
        }

        screenRoot.SetActive(true);
        if (BattleUIFocusManager.Instance != null)
        {
            BattleUIFocusManager.Instance.SetFocus(this);
        }

        if (totalExpText != null)
        {
            if (expGainedByPartyThisFight > 0)
            {
                totalExpText.text = $"+{expGainedByPartyThisFight} EXP!";
            }
            else
            {
                totalExpText.text = "0 EXP";
            }
        }
        else Debug.LogWarning("VictoryScreenManager: totalExpText UI element not assigned.");


        bool hasItems = itemsDropped != null && itemsDropped.Count > 0;
        if (itemsGainedLabel != null)
        {
            itemsGainedLabel.gameObject.SetActive(hasItems);
            if (hasItems) itemsGainedLabel.text = "Items Obtained:";
        }
        else if (hasItems) Debug.LogWarning("VictoryScreenManager: itemsGainedLabel UI element not assigned, but items were dropped.");


        if (itemsGainedText != null)
        {
            itemsGainedText.gameObject.SetActive(true);
            if (hasItems)
            {
                var groupedItems = itemsDropped
                    .Where(item => item != null)
                    .GroupBy(item => item.itemName)
                    .Select(group => new { ItemName = group.Key, Quantity = group.Count() });

                StringBuilder itemBuilder = new StringBuilder();
                foreach (var groupedItem in groupedItems)
                {
                    itemBuilder.AppendLine($"- {groupedItem.ItemName} x{groupedItem.Quantity}");
                }
                itemsGainedText.text = itemBuilder.ToString();
            }
            else
            {
                itemsGainedText.text = "No items obtained.";
            }
        }
        else Debug.LogWarning("VictoryScreenManager: itemsGainedText UI element not assigned.");


        foreach (Transform child in characterProgressionLayoutGroup)
        {
            Destroy(child.gameObject);
        }

        StartCoroutine(AnimateAllCharacterProgressions(victoriousCharacters, expGainedByPartyThisFight));
    }

    private IEnumerator AnimateAllCharacterProgressions(List<CharacterStats> finalCharacterStates, int totalExpPool)
    {
        if (finalCharacterStates == null)
        {
            Debug.LogError("AnimateAllCharacterProgressions received null finalCharacterStates list!");
            _canProceedWithEnter = true;
            if (continuePromptText != null) continuePromptText.gameObject.SetActive(true);
            yield break;
        }

        List<CharacterStats> aliveCharactersForAnimation = finalCharacterStates
            .Where(cs => cs != null && cs.currentHP > 0 && _partyStateBeforeRewards.Any(snapshot => snapshot != null && snapshot.characterName == cs.characterName))
            .ToList();

        if (!aliveCharactersForAnimation.Any())
        {
            _canProceedWithEnter = true;
            if (continuePromptText != null) continuePromptText.gameObject.SetActive(true);
            yield break;
        }

        int expPerLivingMember = aliveCharactersForAnimation.Count > 0 ? totalExpPool / aliveCharactersForAnimation.Count : 0;

        List<Coroutine> characterAnimationCoroutines = new List<Coroutine>();

        for (int i = 0; i < aliveCharactersForAnimation.Count; i++)
        {
            CharacterStats finalCharStats = aliveCharactersForAnimation[i];
            if (finalCharStats == null)
            {
                Debug.LogWarning($"Null CharacterStats found at index {i} in aliveCharactersForAnimation.");
                continue;
            }

            CharacterStatsSnapshot startSnapshot = _partyStateBeforeRewards.FirstOrDefault(s => s != null && s.characterName == finalCharStats.characterName);
            if (startSnapshot == null)
            {
                Debug.LogWarning($"Snapshot not found for {finalCharStats.characterName}. Skipping animation for this character.");
                continue;
            }

            GameObject displayInstance = Instantiate(characterDisplayPrefab, characterProgressionLayoutGroup);
            CharacterVictoryDisplay charDisplayScript = displayInstance.GetComponent<CharacterVictoryDisplay>();

            if (charDisplayScript != null)
            {
                charDisplayScript.PrepareDisplay(finalCharStats);
                Coroutine currentCharacterCoroutine = StartCoroutine(charDisplayScript.AnimateProgression(startSnapshot, expPerLivingMember));
                characterAnimationCoroutines.Add(currentCharacterCoroutine);
            }
            else
            {
                Debug.LogError($"CharacterDisplayPrefab used for {finalCharStats.characterName} is missing the CharacterVictoryDisplay script!");
            }
        }

        foreach (Coroutine coroutine in characterAnimationCoroutines)
        {
            yield return coroutine;
        }

        _canProceedWithEnter = true;
        if (continuePromptText != null) continuePromptText.gameObject.SetActive(true);
    }
}