using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CombatSystem;
using UnityEngine.UI;
using System.Linq;
using System.Collections;
public class AbilitySelectorUI : MonoBehaviour
{
    public static AbilitySelectorUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject rootPanel;
    public RectTransform commandCursor;
    public AudioSource audioSource;
    public ElementIconDatabase elementIconDatabase;
    public List<AbilitySlotUIElements> abilitySlots = new List<AbilitySlotUIElements>(3);

    [Header("Selection Feedback")]
    public Color normalColor = Color.white;
    public Color unavailableColor = Color.gray;
    public Color selectedColor = Color.yellow;
    public float cursorXOffset = -20f;

    [Header("Audio")]
    public AudioClip moveSound;
    public AudioClip confirmSound;
    public AudioClip errorSound;
    public AudioClip cancelSound;

    [Header("Tooltip")]
    public TMP_Text tooltipText;

    [System.Serializable]
    public class AbilitySlotUIElements
    {
        public GameObject slotContainer;
        public TMP_Text abilityNameText;
        public TMP_Text mpCostText;
        public Image elementIconImage;
        [HideInInspector] public AbilityData assignedAbility;
    }

    private int selectedIndex = 0;
    private CharacterStats currentCharacter;
    private List<AbilityData> knownAbilitiesFiltered = new List<AbilityData>();
    private Coroutine shakeCoroutine = null;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        SetupAudioSource();

        if (rootPanel == null) Debug.LogError("[AbilitySelUI] rootPanel no asignado!", this);
        if (commandCursor == null) Debug.LogError("[AbilitySelUI] commandCursor no asignado!", this);
        if (elementIconDatabase == null) Debug.LogError("[AbilitySelUI] elementIconDatabase no asignado!", this);
        if (abilitySlots == null || abilitySlots.Count != 3 || abilitySlots.Any(IsSlotInvalid))
        {
            Debug.LogError("[AbilitySelUI] La lista 'abilitySlots' no está configurada correctamente.", this);
        }

        if (tooltipText == null) Debug.LogError("[AbilitySelUI] tooltipText no asignado!", this);
        else tooltipText.text = "";
        if (rootPanel != null) rootPanel.SetActive(false);
        if (commandCursor != null) commandCursor.gameObject.SetActive(false);
    }

    public void Open(CharacterStats character)
    {
        Debug.Log($"[AbilitySelectorUI] Open llamado para: {character?.characterName ?? "NULL Character"}");
        if (rootPanel == null || abilitySlots == null || abilitySlots.Count != 3 || elementIconDatabase == null || abilitySlots.Any(IsSlotInvalid))
        {
            Debug.LogError("[AbilitySelUI] Open: Configuración inválida o referencias faltantes.", this);
            Close(); BattleFlowController.Instance?.ReturnToCommandSelection(character); return;
        }
        if (character == null || character.knownAbilities == null)
        {
            Debug.LogError("[AbilitySelUI] Open: Character o knownAbilities es null.", this);
            Close(); if (character != null) BattleFlowController.Instance?.ReturnToCommandSelection(character); return;
        }

        this.currentCharacter = character;
        knownAbilitiesFiltered.Clear();
        knownAbilitiesFiltered = character.knownAbilities.Where(ab => ab != null).Take(3).ToList();

        for (int i = 0; i < abilitySlots.Count; i++)
        {
            AbilitySlotUIElements uiSlot = abilitySlots[i];
            if (IsSlotInvalid(uiSlot))
            {
                if (uiSlot?.slotContainer != null) uiSlot.slotContainer.SetActive(false); continue;
            }

            if (i < knownAbilitiesFiltered.Count)
            {
                AbilityData ability = knownAbilitiesFiltered[i];
                uiSlot.assignedAbility = ability;
                uiSlot.slotContainer.SetActive(true);
                uiSlot.abilityNameText.text = ability.abilityName;
                uiSlot.mpCostText.text = ability.mpCost.ToString();
                uiSlot.mpCostText.gameObject.SetActive(true);
                Sprite icon = elementIconDatabase.GetIconForElement(ability.abilityElement);
                if (icon != null)
                {
                    uiSlot.elementIconImage.sprite = icon;
                    uiSlot.elementIconImage.gameObject.SetActive(true);
                }
                else
                {
                    uiSlot.elementIconImage.gameObject.SetActive(false);
                }
                UpdateSlotColor(uiSlot, false);
            }
            else
            {
                uiSlot.assignedAbility = null;
                uiSlot.slotContainer.SetActive(true);
                uiSlot.abilityNameText.text = "???";
                uiSlot.mpCostText.gameObject.SetActive(false);
                uiSlot.elementIconImage.gameObject.SetActive(false);
                uiSlot.abilityNameText.color = unavailableColor;
            }
        }

        if (knownAbilitiesFiltered.Count == 0)
        {
            Debug.LogWarning($"[AbilitySelUI] {character.characterName} no tiene habilidades válidas.");
            Close(); BattleFlowController.Instance?.ReturnToCommandSelection(character); return;
        }

        selectedIndex = 0;
        if (rootPanel) rootPanel.SetActive(true);
        Debug.Log("[AbilitySelectorUI] Panel activado. Intentando establecer foco...");
        if (BattleUIFocusManager.Instance != null)
        {
            BattleUIFocusManager.Instance.SetFocus(this);
        }
        else { Debug.LogError("[AbilitySelectorUI] BattleUIFocusManager.Instance es NULL!"); }

        UpdateSelectionVisuals(selectedIndex, -1);
    }

    public void Close()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        if (commandCursor != null) commandCursor.gameObject.SetActive(false);
        BattleUIFocusManager.Instance?.ClearFocus(this); currentCharacter = null;
        if (tooltipText != null) tooltipText.text = "";
        knownAbilitiesFiltered.Clear();
        if (shakeCoroutine != null) { StopCoroutine(shakeCoroutine); shakeCoroutine = null; }
    }

    private void Update()
    {
        if (rootPanel == null || !rootPanel.activeSelf || BattleUIFocusManager.Instance == null || !BattleUIFocusManager.Instance.CanInteract(this)) return;
        if (shakeCoroutine != null) return;

        int previousIndex = selectedIndex;
        bool selectionChanged = false;
        int maxNavigableIndex = abilitySlots.Count - 1;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (selectedIndex > 0) { selectedIndex--; selectionChanged = true; }
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (selectedIndex < maxNavigableIndex) { selectedIndex++; selectionChanged = true; }
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (selectedIndex >= 0 && selectedIndex < abilitySlots.Count)
            {
                AbilitySlotUIElements selectedSlot = abilitySlots[selectedIndex];
                if (selectedSlot != null && selectedSlot.assignedAbility != null)
                {
                    SelectCurrentAbility();
                }
                else
                {
                    PlaySound(errorSound ?? confirmSound); if (selectedSlot?.slotContainer != null && shakeCoroutine == null)
                    {
                        RectTransform rt = selectedSlot.slotContainer.GetComponent<RectTransform>();
                        if (rt != null) shakeCoroutine = StartCoroutine(ShakeElementCoroutine(rt));
                    }
                }
            }
            else { Debug.LogWarning($"Update: selectedIndex {selectedIndex} fuera de rango."); }
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            CancelSelection();
        }

        if (selectionChanged)
        {
            PlaySound(moveSound);
            UpdateSelectionVisuals(selectedIndex, previousIndex);
        }
    }

    private void UpdateSelectionVisuals(int newIndex, int oldIndex)
    {
        if (newIndex < 0 || newIndex >= abilitySlots.Count)
        {
            if (commandCursor != null) commandCursor.gameObject.SetActive(false);
            if (tooltipText != null) tooltipText.text = ""; return;
        }

        if (oldIndex >= 0 && oldIndex < abilitySlots.Count && abilitySlots[oldIndex] != null)
        {
            UpdateSlotColor(abilitySlots[oldIndex], false);
        }

        AbilityData selectedAbility = null; if (newIndex >= 0 && newIndex < abilitySlots.Count && abilitySlots[newIndex] != null)
        {
            UpdateSlotColor(abilitySlots[newIndex], true);
            PositionCursor(abilitySlots[newIndex]);
            selectedAbility = abilitySlots[newIndex].assignedAbility;
        }
        else if (commandCursor != null) { commandCursor.gameObject.SetActive(false); }


        if (tooltipText != null)
        {
            if (selectedAbility != null)
            {
                tooltipText.text = selectedAbility.description;
            }
            else if (newIndex >= 0 && newIndex < abilitySlots.Count && abilitySlots[newIndex] != null)
            {
                tooltipText.text = "You haven't unlocked this ability yet.";
            }
            else
            {
                tooltipText.text = "";
            }
        }
    }

    private void UpdateSlotColor(AbilitySlotUIElements uiSlot, bool isSelected)
    {
        if (IsSlotInvalid(uiSlot)) return;
        Color targetColor;
        bool hasAbility = uiSlot.assignedAbility != null;
        bool hasEnoughMP = hasAbility && currentCharacter != null && currentCharacter.currentMP >= uiSlot.assignedAbility.mpCost;

        targetColor = isSelected ? selectedColor : (hasAbility ? (hasEnoughMP ? normalColor : unavailableColor) : unavailableColor);

        uiSlot.abilityNameText.color = targetColor;
        if (uiSlot.mpCostText.gameObject.activeSelf)
        {
            uiSlot.mpCostText.color = targetColor;
        }
    }

    private void PositionCursor(AbilitySlotUIElements targetSlot)
    {
        if (commandCursor == null || IsSlotInvalid(targetSlot))
        {
            if (commandCursor != null) commandCursor.gameObject.SetActive(false); return;
        }
        RectTransform slotRect = targetSlot.slotContainer.GetComponent<RectTransform>();
        if (slotRect != null)
        {
            commandCursor.gameObject.SetActive(true);
            commandCursor.anchoredPosition = new Vector2(slotRect.anchoredPosition.x + cursorXOffset, slotRect.anchoredPosition.y);
        }
        else
        {
            Debug.LogError($"PositionCursor: Slot Container '{targetSlot.slotContainer.name}' has not RectTransform!");
            commandCursor.gameObject.SetActive(false);
        }
    }

    private void SelectCurrentAbility()
    {
        if (selectedIndex < 0 || selectedIndex >= knownAbilitiesFiltered.Count || currentCharacter == null)
        {
            Debug.LogWarning("SelectCurrentAbility: null character.");
            PlaySound(errorSound ?? confirmSound);
            return;
        }
        AbilityData ability = knownAbilitiesFiltered[selectedIndex];
        if (ability == null) { Debug.LogError("SelectCurrentAbility: null ability?"); return; }

        if (currentCharacter.currentMP < ability.mpCost)
        {
            Debug.Log($"{currentCharacter.characterName} has no MP for {ability.abilityName}.");
            PlaySound(errorSound ?? confirmSound);
            if (shakeCoroutine == null && abilitySlots[selectedIndex]?.slotContainer != null)
            {
                RectTransform rt = abilitySlots[selectedIndex].slotContainer.GetComponent<RectTransform>();
                if (rt != null) shakeCoroutine = StartCoroutine(ShakeElementCoroutine(rt));
            }
            return;
        }

        CharacterStats capturedCharacter = this.currentCharacter;
        AbilityData capturedAbility = ability;
        Action<object> callback = target =>
        {
            if (BattleFlowController.Instance != null)
            {
                BattleFlowController.Instance.ReceiveAbilitySelection(capturedCharacter, capturedAbility, target);
            }
            else { Debug.LogError("AbilitySelector Callback: BattleFlowController NULL!"); }
        };

        Close();

        List<EnemyInstance> aliveEnemies = BattleFlowController.Instance?.GetEnemies()?.Where(e => e != null && e.IsAlive).ToList() ?? new List<EnemyInstance>();
        List<CharacterStats> aliveAllies = BattleFlowController.Instance?.GetParty()?.Where(p => p != null && p.currentHP > 0).ToList() ?? new List<CharacterStats>();

        switch (capturedAbility.targetType)
        {
            case AbilityTargetType.Enemy:
                if (!aliveEnemies.Any()) { Debug.Log("No alive enemies to select"); BattleFlowController.Instance?.ReturnToCommandSelection(capturedCharacter); return; }
                TargetSelector.Instance?.OpenEnemyTargets(capturedCharacter, aliveEnemies, callback);
                break;
            case AbilityTargetType.Ally:
                if (!aliveAllies.Any()) { Debug.Log("No alive allies to select"); BattleFlowController.Instance?.ReturnToCommandSelection(capturedCharacter); return; }
                TargetSelector.Instance?.OpenAllyTargets(capturedCharacter, aliveAllies, callback);
                break;
            case AbilityTargetType.AllEnemies:
                BattleFlowController.Instance?.ReceiveAbilitySelection(capturedCharacter, capturedAbility, aliveEnemies);
                break;
            case AbilityTargetType.AllAllies:
                BattleFlowController.Instance?.ReceiveAbilitySelection(capturedCharacter, capturedAbility, aliveAllies);
                break;
            case AbilityTargetType.Any:
                if (!aliveEnemies.Any() && !aliveAllies.Any()) { Debug.Log("Any alive target."); BattleFlowController.Instance?.ReturnToCommandSelection(capturedCharacter); return; }
                TargetSelector.Instance?.OpenAnyTargets(capturedCharacter, aliveEnemies, aliveAllies, callback);
                break;
            default:
                Debug.LogWarning($"Target type {capturedAbility.targetType} not working.");
                BattleFlowController.Instance?.ReturnToCommandSelection(capturedCharacter);
                break;
        }
    }

    private void CancelSelection()
    {
        PlaySound(cancelSound ?? confirmSound);
        CharacterStats characterToReturn = currentCharacter;
        Close();
        if (characterToReturn != null)
        {
            BattleFlowController.Instance?.ReturnToCommandSelection(characterToReturn);
        }
        else
        {
            Debug.LogWarning("[AbilitySelUI] CancelSelection: currentCharacter was null");
        }
    }
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null && audioSource.isActiveAndEnabled)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    private void SetupAudioSource()
    {
        if (audioSource == null && BattleCommandUI.Instance?.audioSource != null)
        {
            audioSource = BattleCommandUI.Instance.audioSource;
        }
        else if (audioSource == null && TurnManager.Instance != null && TurnManager.Instance.TryGetComponent<AudioSource>(out var tmSource))
        {
            audioSource = tmSource;
        }
        else if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0;
        }
        if (audioSource != null) audioSource.playOnAwake = false;
    }
    private IEnumerator ShakeElementCoroutine(RectTransform elementRect)
    {
        if (elementRect == null) { shakeCoroutine = null; yield break; }
        Vector2 originalPos = elementRect.anchoredPosition;
        float shakeDuration = 0.25f; float shakeAmount = 6f; int shakeFrequency = 3;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            if (elementRect == null) { shakeCoroutine = null; yield break; }
            elapsed += Time.unscaledDeltaTime;
            float percentComplete = elapsed / shakeDuration;
            float xOffset = Mathf.Sin(percentComplete * Mathf.PI * 2f * shakeFrequency) * shakeAmount * (1f - percentComplete);
            elementRect.anchoredPosition = new Vector2(originalPos.x + xOffset, originalPos.y);
            yield return null;
        }
        if (elementRect != null) elementRect.anchoredPosition = originalPos;
        shakeCoroutine = null;
    }
    private bool IsSlotInvalid(AbilitySlotUIElements slot)
    {
        return slot == null || slot.slotContainer == null || slot.abilityNameText == null || slot.mpCostText == null || slot.elementIconImage == null;
    }

}