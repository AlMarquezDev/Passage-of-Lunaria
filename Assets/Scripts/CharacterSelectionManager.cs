using UnityEngine;
using UnityEngine.UI; // Necesario para Button
using System.Linq;
using TMPro; // Necesario para TMP_Text
using System.Collections.Generic;
using UnityEngine.SceneManagement; // Necesario para SceneManager
using CombatSystem; // Asumiendo que aqu� est�n CharacterStats, CharacterClassData, etc.

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("References")]
    public ClassSelector[] characterSelectors; // Asume que ClassSelector existe
    public Button confirmButton;
    public TMP_Text confirmButtonText;

    [Header("Starter Kits")]
    public StarterKit[] starterKits; // Asume que StarterKit existe

    [Header("Text Settings")]
    public string unavailableText = "Name every character before proceeding";
    public string availableText = "Start";

    [Header("Text Colors")]
    public Color unavailableTextColor = Color.gray;
    public Color availableTextColor = Color.white;

    private bool isButtonInteractable;
    // Flag est�tico para prevenir reinicializaci�n si se vuelve a esta escena
    public static bool alreadyInitialized = false;

    void Start()
    {
        // Limpiar flag al iniciar esta escena (siempre crear nuevo party)
        alreadyInitialized = false;

        // A�adir listeners si los componentes existen
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirm);
        }
        else { Debug.LogError("Confirm Button no asignado en CharacterSelectionManager.", this); }

        if (characterSelectors != null)
        {
            foreach (var selector in characterSelectors)
            {
                if (selector != null && selector.nameInputField != null)
                {
                    // Usamos una lambda para asegurar que siempre llame al m�todo actual
                    selector.nameInputField.onValueChanged.AddListener(_ => UpdateConfirmButton());
                }
                else { Debug.LogWarning("Uno de los characterSelectors o su nameInputField no est� asignado.", this); }
            }
        }
        else { Debug.LogError("Character Selectors no asignado en CharacterSelectionManager.", this); }

        UpdateConfirmButton(); // Estado inicial del bot�n
    }

    // Actualiza la interactividad y texto del bot�n de confirmar
    void UpdateConfirmButton()
    {
        if (confirmButton == null || confirmButtonText == null || characterSelectors == null) return;

        // Comprueba que todos los selectores tengan un nombre no vac�o y una clase seleccionada
        isButtonInteractable = characterSelectors.All(s =>
            s != null && // A�adir null check para el selector
            !string.IsNullOrWhiteSpace(s.GetCharacterName()) &&
            s.GetSelectedIndex() >= 0 // Asume que GetSelectedIndex devuelve -1 si no hay selecci�n
        );

        confirmButton.interactable = isButtonInteractable;
        confirmButtonText.text = isButtonInteractable ? availableText : unavailableText;
        // El color se actualiza en LateUpdate para evitar flickering
    }

    // LateUpdate para asegurar que el color del texto refleje el estado interactable
    void LateUpdate()
    {
        if (confirmButtonText != null)
        {
            confirmButtonText.color = isButtonInteractable ? availableTextColor : unavailableTextColor;
        }
    }

    // Acci�n al pulsar el bot�n de confirmar
    void OnConfirm()
    {
        // Prevenir doble inicializaci�n
        if (alreadyInitialized)
        {
            Debug.LogWarning("Party ya fue inicializada. Carga directa a Test Grounds.", this);
            // Considerar si realmente debe cargar escena aqu� o si es un estado de error
            SceneTransition.Instance?.LoadScene("999_Test Grounds"); // Asume SceneTransition existe
            return;
        }

        // Validar instancias Singleton necesarias
        if (InventorySystem.Instance == null) { Debug.LogError("InventorySystem.Instance es null en OnConfirm.", this); return; }
        if (GameManager.Instance == null) { Debug.LogError("GameManager.Instance es null en OnConfirm.", this); return; }

        Debug.Log("OnConfirm: Inicializando Party...");

        // Limpiar inventario y party existente en GameManager
        InventorySystem.Instance.ClearInventory();
        GameManager.Instance.partyMembers.Clear();

        // Crear cada personaje
        for (int i = 0; i < characterSelectors.Length; i++)
        {
            var selector = characterSelectors[i];
            if (selector == null) { Debug.LogError($"Character Selector en �ndice {i} es null."); continue; }

            var classData = selector.GetSelectedClassData(); // Asume que esto devuelve CharacterClassData v�lido
            if (classData == null) { Debug.LogError($"ClassData seleccionada en �ndice {i} es null."); continue; }

            var kit = starterKits?.FirstOrDefault(k => k.job == classData.characterJob); // A�adir null check para starterKits

            // --- INICIALIZACI�N CORREGIDA ---
            // 1. Crear instancia VAC�A (o con datos m�nimos no calculados)
            CharacterStats stats = new CharacterStats
            {
                characterName = selector.GetCharacterName(),
                characterJob = classData.characterJob,
                level = 1 // Empezar a nivel 1 por defecto
                // NO asignar maxHP, currentHP, strength, etc. aqu�
            };

            // 2. Asignar equipo inicial ANTES de calcular stats finales
            stats.rightHand = kit?.startingWeapon;
            stats.leftHand = kit?.startingShield;
            stats.head = kit?.startingHead;
            stats.body = kit?.startingBody;
            stats.accessory = kit?.startingAccessory;

            // 3. LLAMAR A SetBaseStats: Este m�todo interno debe hacer todo el c�lculo
            //    (poner stats base, calcular maxHP/MP con f�rmulas y nivel, aplicar equipo, restaurar HP/MP)
            stats.SetBaseStats(classData); // Asume que este m�todo existe y funciona como se dise��

            // 4. Asignar habilidades iniciales (despu�s de SetBaseStats que puede llamar a LearnAbilitiesForLevel)
            // Opcional: si SetBaseStats no llama a LearnAbilitiesForLevel, llamarlo aqu�.
            // stats.LearnAbilitiesForLevel();
            // Si el kit a�ade habilidades ADICIONALES a las de nivel 1:
            if (kit?.startingAbilities != null)
            {
                foreach (var ability in kit.startingAbilities)
                {
                    if (ability != null && !stats.knownAbilities.Contains(ability))
                    {
                        stats.knownAbilities.Add(ability);
                    }
                }
            }
            // --- FIN INICIALIZACI�N CORREGIDA ---

            // Log para verificar stats despu�s de la inicializaci�n completa
            Debug.Log($"Personaje Creado: {stats.characterName} ({stats.characterJob}), Lvl: {stats.level}, HP: {stats.currentHP}/{stats.maxHP}, MP: {stats.currentMP}/{stats.maxMP}, STR: {stats.strength}, DEF: {stats.defense}, INT: {stats.intelligence}, AGI: {stats.agility}");
            // *** Verifica este Log: �Muestra maxHP=5 aqu�? ***

            // A�adir personaje al GameManager
            GameManager.Instance.partyMembers.Add(stats);

            // A�adir items equipados al inventario (si es la l�gica deseada)
            AddEquippedItemsToInventory(stats);
        }

        alreadyInitialized = true; // Marcar como inicializado
        Debug.Log("OnConfirm: Party creada. Cargando escena Overworld..."); // Actualiza este mensaje tambi�n si quieres
        SceneTransition.Instance?.LoadScene("Overworld");
    }


    // A�ade los items equipados al inventario
    private void AddEquippedItemsToInventory(CharacterStats stats)
    {
        if (stats == null || InventorySystem.Instance == null) return;
        // Usar el m�todo GetEquippedItem para seguridad
        if (stats.GetEquippedItem(EquipmentSlot.RightHand) is EquipmentItem rh) InventorySystem.Instance.AddItem(rh, 1);
        if (stats.GetEquippedItem(EquipmentSlot.LeftHand) is EquipmentItem lh) InventorySystem.Instance.AddItem(lh, 1);
        if (stats.GetEquippedItem(EquipmentSlot.Head) is EquipmentItem hd) InventorySystem.Instance.AddItem(hd, 1);
        if (stats.GetEquippedItem(EquipmentSlot.Body) is EquipmentItem bd) InventorySystem.Instance.AddItem(bd, 1);
        if (stats.GetEquippedItem(EquipmentSlot.Accessory) is EquipmentItem ac) InventorySystem.Instance.AddItem(ac, 1);
    }
}