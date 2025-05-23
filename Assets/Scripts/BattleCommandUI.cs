using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CombatSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
public class BattleCommandUI : MonoBehaviour
{
    public static BattleCommandUI Instance;

    [System.Serializable]
    public class CommandUIElement
    {
        [Tooltip("El RectTransform del objeto padre (ej. Command_01)")]
        public RectTransform containerRect;
        [Tooltip("El componente TextMeshPro del nombre")]
        public TMP_Text nameText;
        [Tooltip("El componente Image del icono (opcional)")]
        public Image iconImage;
        [HideInInspector] public Vector2 originalPosition;
    }

    [Header("UI References")]
    [Tooltip("Array con los 5 elementos de comando, en orden")]
    public CommandUIElement[] commandElements;
    [Tooltip("Asigna aquí el RectTransform de la imagen del cursor")]
    public RectTransform commandCursor;
    [Tooltip("Asigna el AudioSource para los SFX (o se buscará/creará)")]
    public AudioSource audioSource;

    [Header("Selection Feedback")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    [Tooltip("Distancia (en píxeles UI) que se desliza el comando seleccionado")]
    public float slideDistance = 20f;
    [Tooltip("Duración de la animación de deslizamiento y del cursor (en segundos)")]
    public float slideDuration = 0.15f;
    [Tooltip("Offset horizontal del cursor respecto a la posición original del comando")]
    public float cursorXOffset = -30f;
    [Header("Audio")]
    public AudioClip moveSound;
    public AudioClip confirmSound;
    public AudioClip cancelSound;
    public AudioClip errorSound;
    private int selectedIndex = 0;
    private Action<BattleCommand> onCommandSelected;
    private CharacterStats currentCharacter;
    private Coroutine animationCoroutine;

    private static readonly BattleCommand[] commandOrder =
{
        BattleCommand.Attack,
        BattleCommand.Defend,
        BattleCommand.Special,
        BattleCommand.Item,
        BattleCommand.Flee
    };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;

        if (commandElements != null && commandElements.Length == commandOrder.Length)
        {
            foreach (var element in commandElements)
            {
                if (element == null)
                {
                    Debug.LogError("[BCommandUI] Elemento nulo en commandElements!", this); continue;
                }
                if (element.containerRect != null)
                {
                    element.originalPosition = element.containerRect.anchoredPosition;
                }
                else
                {
                    Debug.LogError($"[BCommandUI] Elemento '{element.nameText?.text ?? "UNKNOWN"}' no tiene containerRect.", this);
                }
                if (element.nameText == null)
                {
                    Debug.LogError($"[BCommandUI] Elemento en commandElements no tiene nameText asignado.", element?.containerRect);
                }
            }
        }
        else
        {
            Debug.LogError($"[BCommandUI] commandElements no asignado o tamaño incorrecto ({commandElements?.Length ?? -1}).", this);
        }

        if (commandCursor == null) Debug.LogError("[BCommandUI] commandCursor no asignado.", this);
        else commandCursor.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    public void Open(CharacterStats character, Action<BattleCommand> onSelected)
    {
        Debug.Log($"[BattleCommandUI] Open llamado para: {character?.characterName ?? "NULL Character"}");

        if (commandElements == null || commandElements.Length != commandOrder.Length || commandElements.Any(e => e == null || e.containerRect == null || e.nameText == null))
        {
            Debug.LogError("[BCommandUI] Open: commandElements mal configurado. No se puede abrir.", this); return;
        }
        if (character == null || onSelected == null)
        {
            Debug.LogError("[BCommandUI] Open: character u onSelected es null.", this); return;
        }

        this.currentCharacter = character;
        this.selectedIndex = 0; this.onCommandSelected = onSelected;

        try
        {
            int specialIndex = Array.FindIndex(commandOrder, cmd => cmd == BattleCommand.Special);
            if (specialIndex != -1 && commandElements[specialIndex]?.nameText != null)
            {
                string commandName = "Special"; if (character != null)
                {
                    AbilityType abilityType = JobAbilityUtils.GetAbilityTypeForJob(character.characterJob);
                    commandName = string.Concat(abilityType.ToString().Select(x => Char.IsUpper(x) ? " " + x : x.ToString())).TrimStart();
                }
                commandElements[specialIndex].nameText.text = commandName;
            }
        }
        catch (Exception ex) { Debug.LogError($"Error actualizando nombre Special: {ex.Message}"); /* Fallback implícito a "Special" */ }


        for (int i = 0; i < commandElements.Length; i++)
        {
            var element = commandElements[i];
            if (element?.containerRect != null) element.containerRect.anchoredPosition = element.originalPosition;
            if (element?.nameText != null) element.nameText.color = normalColor;
        }

        gameObject.SetActive(true);
        Debug.Log("[BattleCommandUI] Panel activado. Intentando establecer foco...");
        if (BattleUIFocusManager.Instance != null)
        {
            BattleUIFocusManager.Instance.SetFocus(this); bool canInteractNow = BattleUIFocusManager.Instance.CanInteract(this);
            if (!canInteractNow) Debug.LogError("[BattleCommandUI] ¡¡¡ERROR!!! No se pudo obtener el foco después de llamar a SetFocus.");
        }
        else { Debug.LogError("[BattleCommandUI] BattleUIFocusManager.Instance es NULL!"); }

        UpdateVisuals(selectedIndex, -1, false);
    }

    public void Close()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        BattleUIFocusManager.Instance?.ClearFocus(this);
        if (commandCursor != null) commandCursor.gameObject.SetActive(false);
        gameObject.SetActive(false);
        currentCharacter = null;
        onCommandSelected = null;
    }

    private void Update()
    {
        if (!gameObject.activeSelf || BattleUIFocusManager.Instance == null || !BattleUIFocusManager.Instance.CanInteract(this))
        {
            return;
        }

        int previousIndex = selectedIndex;
        bool selectionChanged = false;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            int nextIndex = selectedIndex - 1;
            if (nextIndex < 0) nextIndex = commandElements.Length - 1; selectedIndex = nextIndex;
            selectionChanged = selectedIndex != previousIndex;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            int nextIndex = selectedIndex + 1;
            if (nextIndex >= commandElements.Length) nextIndex = 0; selectedIndex = nextIndex;
            selectionChanged = selectedIndex != previousIndex;
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmSelection();
            return;
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (BattleFlowController.Instance != null && BattleFlowController.Instance.CanGoToPreviousCharacter())
            {
                PlaySound(cancelSound); CharacterStats characterBeingCancelled = currentCharacter; Close();
                BattleFlowController.Instance.GoToPreviousCommandCharacter();
                return;
            }
            else
            {
                PlaySound(errorSound ?? cancelSound);
            }
        }

        if (selectionChanged)
        {
            PlaySound(moveSound);
            UpdateVisuals(selectedIndex, previousIndex, true);
        }
    }

    private void UpdateVisuals(int newIndex, int oldIndex, bool animate)
    {
        if (animationCoroutine != null) { StopCoroutine(animationCoroutine); animationCoroutine = null; }

        if (animate && slideDuration > 0)
        {
            animationCoroutine = StartCoroutine(AnimateSelectionChange(newIndex, oldIndex));
        }
        else
        {
            for (int i = 0; i < commandElements.Length; i++)
            {
                CommandUIElement element = commandElements[i];
                if (element?.nameText == null || element.containerRect == null) continue;
                bool isSelected = (i == newIndex);
                element.nameText.color = isSelected ? selectedColor : normalColor;
                element.containerRect.anchoredPosition = isSelected ? (element.originalPosition + Vector2.right * slideDistance) : element.originalPosition;
            }
            PositionCursor(newIndex);
        }
    }

    private void PositionCursor(int index)
    {
        if (commandCursor == null) return;

        bool shouldBeActive = (index >= 0 && index < commandElements.Length && commandElements[index]?.containerRect != null);
        commandCursor.gameObject.SetActive(shouldBeActive);
        if (shouldBeActive)
        {
            CommandUIElement selectedElement = commandElements[index];
            Vector2 cursorTargetPos = new Vector2(
                selectedElement.originalPosition.x + cursorXOffset,
                selectedElement.originalPosition.y
            );
            commandCursor.anchoredPosition = cursorTargetPos;
        }
    }

    private IEnumerator AnimateSelectionChange(int newIndex, int oldIndex)
    {
        float elapsedTime = 0f;
        CommandUIElement newElement = (newIndex >= 0 && newIndex < commandElements.Length) ? commandElements[newIndex] : null;
        CommandUIElement oldElement = (oldIndex >= 0 && oldIndex < commandElements.Length) ? commandElements[oldIndex] : null;
        RectTransform cursorRect = commandCursor;
        RectTransform newRect = newElement?.containerRect;
        RectTransform oldRect = oldElement?.containerRect;
        TMP_Text newText = newElement?.nameText;
        TMP_Text oldText = oldElement?.nameText;

        Vector2 newStartPos = newRect ? newRect.anchoredPosition : Vector2.zero;
        Vector2 newEndPos = newElement != null ? newElement.originalPosition + Vector2.right * slideDistance : newStartPos;
        Vector2 oldStartPos = oldRect ? oldRect.anchoredPosition : Vector2.zero;
        Vector2 oldEndPos = oldElement != null ? oldElement.originalPosition : oldStartPos;
        Vector2 cursorStartPos = cursorRect ? cursorRect.anchoredPosition : Vector2.zero;
        Vector2 cursorEndPos = Vector2.zero;
        bool cursorShouldBeActive = (cursorRect != null && newElement != null);
        if (cursorShouldBeActive)
        {
            cursorEndPos = new Vector2(newElement.originalPosition.x + cursorXOffset, newElement.originalPosition.y);
        }

        if (newText) newText.color = selectedColor;
        if (oldText) oldText.color = normalColor;
        if (cursorRect) cursorRect.gameObject.SetActive(cursorShouldBeActive);
        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / slideDuration);
            if (newRect) newRect.anchoredPosition = Vector2.Lerp(newStartPos, newEndPos, t);
            if (oldRect) oldRect.anchoredPosition = Vector2.Lerp(oldStartPos, oldEndPos, t);
            if (cursorRect && cursorShouldBeActive) cursorRect.anchoredPosition = Vector2.Lerp(cursorStartPos, cursorEndPos, t);
            yield return null;
        }

        if (newRect) newRect.anchoredPosition = newEndPos;
        if (oldRect) oldRect.anchoredPosition = oldEndPos;
        if (cursorRect && cursorShouldBeActive) cursorRect.anchoredPosition = cursorEndPos;
        if (cursorRect) cursorRect.gameObject.SetActive(cursorShouldBeActive);

        animationCoroutine = null;
    }

    private void ConfirmSelection()
    {
        if (selectedIndex >= 0 && selectedIndex < commandOrder.Length)
        {
            PlaySound(confirmSound);
            var selectedCommand = commandOrder[selectedIndex];
            if (onCommandSelected != null && currentCharacter != null)
            {
                Action<BattleCommand> savedCallback = onCommandSelected;
                savedCallback.Invoke(selectedCommand);
            }
            else { Debug.LogError("[BCommandUI] ConfirmSelection: Callback o Character es null!"); }
        }
        else { Debug.LogWarning($"[BCommandUI] ConfirmSelection: Índice fuera de rango: {selectedIndex}"); }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null && audioSource.isActiveAndEnabled)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}