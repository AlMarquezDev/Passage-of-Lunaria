using UnityEngine;
using System.Collections.Generic;
public class CombatStartInventory : MonoBehaviour
{
    [System.Serializable]
    public struct ItemToAddEntry
    {
        [Tooltip("Arrastra aquí el ScriptableObject del Item (ej. Potion.asset) desde la ventana Project.")]
        public ItemBase itemAsset; [Tooltip("Cantidad a añadir de este objeto.")]
        public int quantity;
    }

    [Header("Configuración de Items a Añadir Automáticamente")]
    [Tooltip("Configura aquí la lista de objetos y cantidades que se añadirán al empezar el combate.")]
    public List<ItemToAddEntry> itemsToAutoAdd;
    private bool itemsAdded = false;

    void Start()
    {
        if (itemsAdded)
        {
            return;
        }

        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[CombatStartInventory] InventorySystem.Instance no encontrado. No se pueden añadir objetos.");
            return;
        }
        if (ItemDatabase.Instance == null)
        {
            Debug.LogError("[CombatStartInventory] ItemDatabase.Instance no encontrado. No se pueden añadir objetos.");
            return;
        }

        if (itemsToAutoAdd == null || itemsToAutoAdd.Count == 0)
        {
            Debug.LogWarning("[CombatStartInventory] La lista 'Items To Auto Add' está vacía. No se añadieron objetos.");
            return;
        }

        InventorySystem inv = InventorySystem.Instance;
        ItemDatabase db = ItemDatabase.Instance;
        Debug.Log("[CombatStartInventory] Añadiendo objetos de prueba automáticamente...");


        foreach (var entry in itemsToAutoAdd)
        {
            if (entry.itemAsset != null && entry.quantity > 0)
            {
                inv.AddItem(entry.itemAsset, entry.quantity);
                Debug.Log($"[CombatStartInventory] Añadido: {entry.itemAsset.itemName} x{entry.quantity}");
            }
            else if (entry.itemAsset == null)
            {
                Debug.LogWarning("[CombatStartInventory] Se encontró una entrada en la lista sin un Item Asset asignado.");
            }
        }
        Debug.Log("[CombatStartInventory] Objetos de prueba añadidos automáticamente.");

        itemsAdded = true;
    }

}