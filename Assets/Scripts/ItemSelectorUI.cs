using System;
using System.Collections; // Necesario para Coroutine
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Necesario
using CombatSystem;
using UnityEngine.UI; // Necesario para ScrollRect, Image etc.
using System.Linq;

// Script para controlar la interfaz de usuario del selector de objetos en combate.
public class ItemSelectorUI : MonoBehaviour
{
    public static ItemSelectorUI Instance { get; private set; }

    [Header("Core UI References")]
    [Tooltip("GameObject raíz del panel.")]
    public GameObject root;
    [Tooltip("Transform padre donde se instanciarán los ItemEntry.")]
    public Transform contentParent;
    [Tooltip("Prefab del ItemEntry (debe tener el script ItemEntryUI).")]
    public GameObject entryPrefab;
    [Tooltip("RectTransform de la imagen del cursor.")]
    public RectTransform cursorImage;
    [Tooltip("ScrollRect para el contenido (opcional pero recomendado).")]
    public ScrollRect scrollRect;

    [Header("Messages")]
    [Tooltip("Asigna aquí el TextMeshPro que muestra el mensaje cuando no hay objetos.")]
    public TMP_Text emptyListMessageText;

    // --- SONIDO (Mantenido de antes) ---
    [Header("Audio (Opcional)")]
    public AudioSource audioSource;
    public AudioClip moveSound;
    public AudioClip confirmSound;
    public AudioClip cancelSound;
    public AudioClip errorSound;
    // ------------------------------------

    // Variables internas
    private List<ItemEntryUI> entryUIs = new List<ItemEntryUI>(); // Lista de scripts de los prefabs instanciados
    private int currentIndex = -1; // Empezar en -1 para indicar "sin selección"
    private CharacterStats currentCharacter;

    // Variables para scroll
    private RectTransform contentRect;
    private float entryHeight = -1f; // Altura de una entrada (se calculará)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Validar referencias
        if (root == null) Debug.LogError("[ItemSelectorUI] 'root' no asignado!", this);
        if (contentParent == null) Debug.LogError("[ItemSelectorUI] 'contentParent' no asignado!", this);
        if (entryPrefab == null) Debug.LogError("[ItemSelectorUI] 'entryPrefab' no asignado!", this);
        else if (entryPrefab.GetComponent<ItemEntryUI>() == null) Debug.LogError("[ItemSelectorUI] 'entryPrefab' no tiene el script ItemEntryUI!", this);
        if (cursorImage == null) Debug.LogError("[ItemSelectorUI] 'cursorImage' no asignado!", this);
        if (scrollRect == null) Debug.LogWarning("[ItemSelectorUI] 'scrollRect' no asignado (opcional).", this);
        if (emptyListMessageText == null) Debug.LogError("[ItemSelectorUI] 'emptyListMessageText' no asignado!", this);


        if (root != null) root.SetActive(false);
        if (cursorImage != null) cursorImage.gameObject.SetActive(false);
        if (emptyListMessageText != null) emptyListMessageText.gameObject.SetActive(false); // Oculto por defecto

        if (contentParent != null) contentRect = contentParent.GetComponent<RectTransform>();

        SetupAudioSource();
    }

    /// <summary>
    /// Abre el panel de selección de items para un personaje.
    /// </summary>
    public void Open(CharacterStats character)
    {
        // Debug.Log($"[ItemSelectorUI] Open llamado para: {character?.characterName ?? "NULL Character"}");
        if (character == null)
        {
            Debug.LogError("[ItemSelectorUI] Open: Se intentó abrir con un personaje null.");
            return;
        }
        // Re-validar refs críticas por si acaso
        if (root == null || contentParent == null || entryPrefab == null || cursorImage == null || emptyListMessageText == null)
        {
            Debug.LogError("[ItemSelectorUI] Open: Faltan referencias de UI críticas. No se puede abrir.");
            Close(); // Asegurar cierre limpio
            BattleFlowController.Instance?.ReturnToCommandSelection(character); // Devolver control
            return;
        }

        currentCharacter = character;
        if (root != null) root.SetActive(true);
        if (emptyListMessageText != null) emptyListMessageText.gameObject.SetActive(false); // Ocultar mensaje al abrir
        BattleUIFocusManager.Instance?.SetFocus(this); // Tomar foco

        GenerateList(); // Generar y mostrar lista (esto puede llamar a Cancel si está vacía)
    }

    /// <summary>
    /// Cierra el panel y limpia el estado.
    /// </summary>
    public void Close()
    {
        StopAllCoroutines(); // Detener corutina de CancelAfterDelay si estaba activa
        if (root != null) root.SetActive(false);
        if (cursorImage != null) cursorImage.gameObject.SetActive(false);
        if (emptyListMessageText != null) emptyListMessageText.gameObject.SetActive(false);
        ClearList();
        BattleUIFocusManager.Instance?.ClearFocus(this); // Liberar foco
        currentCharacter = null;
    }

    /// <summary>
    /// Genera las entradas de UI para los items consumibles usables.
    /// </summary>
    private void GenerateList()
    {
        ClearList(); // Limpia la lista y resetea currentIndex a -1
        if (InventorySystem.Instance == null) { Debug.LogError("[ItemSelUI] InventorySystem.Instance NULL"); Close(); return; }

        // Obtener solo consumibles usables
        var usableItems = InventorySystem.Instance.GetUsableConsumables();
        if (usableItems == null) usableItems = new List<ConsumableItem>();

        entryHeight = -1f; // Resetear altura

        // --- Crear entradas de UI ---
        foreach (var item in usableItems)
        {
            if (item == null) continue; // Saltar items nulos si los hubiera

            GameObject go = Instantiate(entryPrefab, contentParent);
            ItemEntryUI entryUI = go.GetComponent<ItemEntryUI>();

            if (entryUI != null)
            {
                int qty = InventorySystem.Instance.GetItemCount(item);
                entryUI.SetData(item, qty); // Llama al método del prefab
                entryUIs.Add(entryUI); // Añadir a la lista

                // Calcular altura de la primera entrada válida (asumiendo que todas son iguales)
                if (entryHeight < 0 && go.activeInHierarchy) // Solo calcular si está activo
                {
                    // Forzar actualización del layout para obtener altura correcta
                    Canvas.ForceUpdateCanvases();
                    RectTransform rt = go.GetComponent<RectTransform>();
                    if (rt != null) entryHeight = LayoutUtility.GetPreferredHeight(rt); // Usar altura preferida
                                                                                        // Considerar VerticalLayoutGroup spacing si existe
                    VerticalLayoutGroup layout = contentParent.GetComponent<VerticalLayoutGroup>();
                    if (layout != null) entryHeight += layout.spacing;
                    // Debug.Log($"[ItemSelectorUI] Calculated Entry Height: {entryHeight}"); // Log para depurar altura
                }
            }
            else { Debug.LogError("[ItemSelUI] Prefab entryPrefab sin ItemEntryUI.", entryPrefab); Destroy(go); }
        }
        // Log final de altura si aún no se calculó (lista vacía o error)
        // if(entryHeight < 0) Debug.LogWarning($"[ItemSelectorUI] Entry Height could not be calculated.");


        // --- Comprobación de lista vacía ---
        if (entryUIs.Count == 0)
        {
            Debug.LogWarning("[ItemSelUI] No hay items consumibles usables.");
            currentIndex = -1; // Marcar como sin selección válida

            // Mostrar mensaje de lista vacía
            if (emptyListMessageText != null)
            {
                emptyListMessageText.text = "No items available."; // Mensaje en inglés
                emptyListMessageText.gameObject.SetActive(true);
            }
            // Desactivar cursor
            if (cursorImage != null) cursorImage.gameObject.SetActive(false);

            // Iniciar corutina para cancelar y volver automáticamente
            StartCoroutine(CancelAfterDelay(1.5f)); // Espera 1.5 segundos antes de cerrar
        }
        else
        {
            // Si hay items, seleccionar el primero y actualizar visuales
            currentIndex = 0;
            if (emptyListMessageText != null) emptyListMessageText.gameObject.SetActive(false); // Asegurar que el mensaje esté oculto
            UpdateSelectionVisuals();
            ScrollToSelection();
        }
    }

    /// <summary>
    /// Limpia las listas internas y destruye los objetos de UI generados.
    /// </summary>
    private void ClearList()
    {
        // --- MÉTODO MODIFICADO ---
        // Destruye específicamente los GameObjects de las entradas de UI anteriores
        foreach (ItemEntryUI entryUI in entryUIs)
        {
            if (entryUI != null) // Comprobar por si acaso
            {
                Destroy(entryUI.gameObject);
            }
        }
        entryUIs.Clear(); // Limpiar la lista de referencias

        // Ya no usamos el bucle que borraba todos los hijos de contentParent
        // if (contentParent != null) { ... }

        currentIndex = -1; // Resetear índice
    }

    /// <summary>
    /// Maneja el input del jugador para navegar, confirmar o cancelar.
    /// </summary>
    private void Update()
    {
        // Salir si no está activo, no tiene foco, o no hay items seleccionables
        if (root == null || !root.activeSelf || BattleUIFocusManager.Instance == null || !BattleUIFocusManager.Instance.CanInteract(this) || currentIndex < 0 || entryUIs.Count == 0)
        {
            // Permitir cancelar con Backspace incluso si la lista está vacía (mientras el panel esté visible)
            if (root != null && root.activeSelf && Input.GetKeyDown(KeyCode.Backspace))
            {
                Cancel();
            }
            return;
        }


        // Navegación Vertical (W/S o Flechas Arriba/Abajo)
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) MoveSelection(-1);
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) MoveSelection(1);
        // Confirmación
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) SelectItem();
        // Cancelación (ya manejada por Cancel())
        else if (Input.GetKeyDown(KeyCode.Backspace)) Cancel();
    }

    /// <summary>
    /// Mueve la selección actual y actualiza la paginación/scroll.
    /// </summary>
    private void MoveSelection(int delta)
    {
        if (entryUIs.Count == 0 || currentIndex < 0) return; // No moverse si no hay items o índice inválido

        int previousIndex = currentIndex;
        int newIndex = currentIndex + delta;

        // Aplicar límites (sin wrap around)
        newIndex = Mathf.Clamp(newIndex, 0, entryUIs.Count - 1);

        if (newIndex != currentIndex)
        {
            currentIndex = newIndex;
            PlaySound(moveSound);
            UpdateSelectionVisuals();
            ScrollToSelection();
        }
    }

    /// <summary>
    /// Actualiza el resaltado de los slots y la posición del cursor.
    /// </summary>
    private void UpdateSelectionVisuals()
    {
        if (cursorImage == null) return;

        // Actualizar resaltado interno de los slots (opcional)
        for (int i = 0; i < entryUIs.Count; i++)
        {
            // entryUIs[i]?.SetHighlight(i == currentIndex); // Descomentar si ItemEntryUI tiene esta lógica
        }

        // Posicionar el cursor principal
        PositionCursor();
    }

    /// <summary>
    /// Posiciona el cursor sobre el slot seleccionado actualmente. (Incluye Logs de Debug)
    /// </summary>
// En ItemSelectorUI.cs
    private void PositionCursor()
    {
        if (cursorImage == null) return;

        if (currentIndex >= 0 && currentIndex < entryUIs.Count && entryUIs[currentIndex] != null)
        {
            ItemEntryUI selectedEntryUI = entryUIs[currentIndex];
            RectTransform selectedRect = selectedEntryUI.GetComponent<RectTransform>();

            if (selectedRect != null)
            {
                cursorImage.gameObject.SetActive(true);

                // --- NUEVA LÓGICA DE POSICIÓN GLOBAL ---
                // Requerimientos:
                // - Pivot del Cursor = (1, 0.5) [Centro-Derecha]
                // - Pivot del ItemEntry = (0, 0.5) [Centro-Izquierda]
                // - Cursor NO es hijo de ContentParent

                float desiredSpacing = 5f; // Espacio horizontal entre cursor e item

                // 1. Obtener la posición MUNDIAL del pivote del ItemEntry (su borde izquierdo-central)
                Vector3 itemPivotWorldPos = selectedRect.position;

                // 2. Calcular la posición MUNDIAL donde debe estar el pivote del Cursor
                //    (su borde derecho-central). Queremos que esté 'desiredSpacing' a la
                //    izquierda del borde izquierdo del item.
                Vector3 cursorTargetWorldPos = new Vector3(
                    itemPivotWorldPos.x - desiredSpacing,  // X = Borde Izquierdo del Item - Espacio
                    itemPivotWorldPos.y,                   // Y = Misma altura del centro del Item
                    itemPivotWorldPos.z                    // Z = Misma profundidad
                );

                // 3. Establecer la posición MUNDIAL del cursor. Como el pivote del cursor
                //    está en su borde derecho, esto alineará ese borde derecho con el punto calculado.
                cursorImage.position = cursorTargetWorldPos;
                // --- FIN NUEVA LÓGICA ---

                // Log opcional para depurar
                // Debug.Log($"[PositionCursor] World Pos - Cursor Right Edge Target: {cursorTargetWorldPos} | Item Left Edge: {itemPivotWorldPos}");
            }
            else
            {
                // Debug.LogWarning($"[PositionCursor] Selected Entry UI {selectedEntryUI.gameObject.name} has no RectTransform!");
                cursorImage.gameObject.SetActive(false);
            }
        }
        else
        {
            // Debug.Log($"[PositionCursor] Invalid currentIndex ({currentIndex}) or entryUI is null. Hiding cursor.");
            cursorImage.gameObject.SetActive(false);
        }
    }


    /// <summary>
    /// Ajusta la posición del ScrollRect para mantener visible el elemento seleccionado. (Incluye Logs de Debug)
    /// </summary>
    private void ScrollToSelection()
    {
        if (scrollRect == null || contentRect == null || entryUIs.Count == 0 || entryHeight <= 0 || currentIndex < 0)
        {
            // Debug.Log($"[ScrollToSelection] Skipping scroll."); // Log opcional
            return;
        }

        // --- LOGS DE DEBUG ---
        float viewPortHeight = scrollRect.viewport.rect.height;
        float contentHeight = contentRect.rect.height;
        // selectedY: Posición Y del borde SUPERIOR del elemento seleccionado relativo al borde SUPERIOR del contentRect (negativa hacia abajo)
        float selectedY = -(currentIndex * entryHeight);
        float currentScrollY = contentRect.anchoredPosition.y; // Posición Y actual del Content (positiva cuando se baja)
        float visibleTopY = -currentScrollY; // Borde superior visible relativo al Content
        float visibleBottomY = visibleTopY - viewPortHeight; // Borde inferior visible relativo al Content

        Debug.Log($"[ScrollToSelection] Index:{currentIndex}, EntryH:{entryHeight:F1}, SelectedY:{selectedY:F1}");
        Debug.Log($"[ScrollToSelection] ViewportH:{viewPortHeight:F1}, ContentH:{contentHeight:F1}, CurrentScrollY:{currentScrollY:F1}");
        Debug.Log($"[ScrollToSelection] VisibleTopY:{visibleTopY:F1}, VisibleBottomY:{visibleBottomY:F1}");
        // --- FIN LOGS ---

        float newY = currentScrollY;

        // Si la selección está por encima del área visible
        if (selectedY > visibleTopY + 0.01f)
        {
            newY = -selectedY;
            Debug.Log($"[ScrollToSelection] Selection Above Viewport. Setting newY = {newY:F1}");
        }
        // Si la selección (su borde inferior) está por debajo del área visible
        else if (selectedY - entryHeight < visibleBottomY - 0.01f)
        {
            newY = -(selectedY - viewPortHeight + entryHeight);
            Debug.Log($"[ScrollToSelection] Selection Below Viewport. Setting newY = {newY:F1}");
        }

        // Aplicar y limitar
        float maxScroll = Mathf.Max(0, contentHeight - viewPortHeight);
        newY = Mathf.Clamp(newY, 0, maxScroll);

        if (Mathf.Abs(contentRect.anchoredPosition.y - newY) > 0.01f)
        {
            contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, newY);
            Debug.Log($"[ScrollToSelection] Applied new anchoredPosition.y = {newY:F1}");
        }

        // Actualizar scrollbar (opcional)
        // ... (código del scrollbar) ...
    }

    /// <summary>
    /// Intenta usar el objeto seleccionado actualmente.
    /// </summary>
    private void SelectItem()
    {
        // Validaciones iniciales
        if (currentIndex < 0 || currentIndex >= entryUIs.Count || entryUIs[currentIndex] == null || currentCharacter == null || InventorySystem.Instance == null)
        {
            Debug.LogWarning("SelectItem: Selección inválida o falta referencia.");
            PlaySound(errorSound);
            return;
        }

        ConsumableItem item = entryUIs[currentIndex].GetAssignedItem();
        if (item == null) { Debug.LogError("SelectItem: Assigned item is null!"); PlaySound(errorSound); return; }

        int qty = InventorySystem.Instance.GetItemCount(item);
        if (qty <= 0)
        {
            Debug.Log($"No quedan unidades de {item.itemName}.");
            PlaySound(errorSound);
            return;
        }

        PlaySound(confirmSound);

        // --- Captura y Callback ---
        CharacterStats capturedCharacter = this.currentCharacter;
        ConsumableItem capturedItem = item;
        Action<object> callback = target => {
            if (BattleFlowController.Instance != null)
            {
                BattleFlowController.Instance.ReceiveItemSelection(capturedCharacter, capturedItem, target);
            }
            else { Debug.LogError("ItemSelector Callback: BattleFlowController NULL!"); }
        };

        Close(); // Cerrar esta UI ANTES de abrir TargetSelector

        // --- Lógica de Selección de Objetivo ---
        List<EnemyInstance> aliveEnemies = BattleFlowController.Instance?.GetEnemies()?.Where(e => e != null && e.IsAlive).ToList() ?? new List<EnemyInstance>();
        List<CharacterStats> aliveAllies = BattleFlowController.Instance?.GetParty()?.Where(p => p != null && p.currentHP > 0).ToList() ?? new List<CharacterStats>();

        switch (capturedItem.targetType)
        {
            case ItemTargetType.Ally:
                if (!aliveAllies.Any()) { Debug.Log("No hay aliados vivos."); BattleFlowController.Instance?.ReturnToCommandSelection(capturedCharacter); return; }
                TargetSelector.Instance?.OpenAllyTargets(capturedCharacter, aliveAllies, callback);
                break;
            case ItemTargetType.AllAllies:
                if (!aliveAllies.Any()) { Debug.Log("No hay aliados vivos."); BattleFlowController.Instance?.ReturnToCommandSelection(capturedCharacter); return; }
                BattleFlowController.Instance?.ReceiveItemSelection(capturedCharacter, capturedItem, aliveAllies); // Pasa lista directamente
                break;
            case ItemTargetType.Any:
                if (!aliveEnemies.Any() && !aliveAllies.Any()) { Debug.Log("No hay objetivos vivos."); BattleFlowController.Instance?.ReturnToCommandSelection(capturedCharacter); return; }
                TargetSelector.Instance?.OpenAnyTargets(capturedCharacter, aliveEnemies, aliveAllies, callback);
                break;
            // Añadir casos para Enemy, AllEnemies si es necesario
            default:
                Debug.LogWarning($"Target type {capturedItem.targetType} for item {capturedItem.itemName} not handled.");
                BattleFlowController.Instance?.ReturnToCommandSelection(capturedCharacter);
                break;
        }
    }

    /// <summary>
    /// Cancela la selección actual y vuelve al menú anterior.
    /// </summary>
    private void Cancel()
    {
        PlaySound(cancelSound ?? confirmSound); // Usa cancel si existe, si no confirm
        CharacterStats characterToReturn = currentCharacter;
        Close(); // Cierra esta UI
        if (characterToReturn != null)
        {
            BattleFlowController.Instance?.ReturnToCommandSelection(characterToReturn); // Devuelve control a BFC
        }
        // else { Debug.LogWarning("[ItemSelectorUI] Cancel: currentCharacter era null."); }
    }

    // --- NUEVA CORUTINA ---
    /// <summary>
    /// Espera un tiempo y luego llama a Cancel() para cerrar el menú automáticamente.
    /// </summary>
    private IEnumerator CancelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // Comprobar si todavía estamos en el estado "sin items" y el panel está activo
        if (entryUIs.Count == 0 && root != null && root.activeSelf)
        {
            // Debug.Log("[ItemSelUI] No items found, cancelling automatically."); // Opcional
            Cancel(); // Llama al método Cancel existente
        }
    }

    // --- Helpers ---
    private void SetupAudioSource()
    {
        if (audioSource == null && BattleCommandUI.Instance?.audioSource != null) { audioSource = BattleCommandUI.Instance.audioSource; }
        else if (audioSource == null && TurnManager.Instance != null && TurnManager.Instance.TryGetComponent<AudioSource>(out var tmSource)) { audioSource = tmSource; }
        else if (audioSource == null) { audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.spatialBlend = 0; }
        if (audioSource != null) audioSource.playOnAwake = false;
    }
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null && audioSource.isActiveAndEnabled) { audioSource.PlayOneShot(clip); }
    }
}