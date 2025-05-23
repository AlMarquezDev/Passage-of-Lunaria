using UnityEngine;
using System.Collections.Generic;
public class CombatStartInventory : MonoBehaviour
{
    [System.Serializable]
    public struct ItemToAddEntry
    {
        [Tooltip("Arrastra aqu� el ScriptableObject del Item (ej. Potion.asset) desde la ventana Project.")]
        public ItemBase itemAsset; [Tooltip("Cantidad a a�adir de este objeto.")]
        public int quantity;
    }

    [Header("Configuraci�n de Items a A�adir Autom�ticamente")]
    [Tooltip("Configura aqu� la lista de objetos y cantidades que se a�adir�n al empezar el combate.")]
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
            Debug.LogError("[CombatStartInventory] InventorySystem.Instance no encontrado. No se pueden a�adir objetos.");
            return;
        }
        if (ItemDatabase.Instance == null)
        {
            Debug.LogError("[CombatStartInventory] ItemDatabase.Instance no encontrado. No se pueden a�adir objetos.");
            return;
        }

        if (itemsToAutoAdd == null || itemsToAutoAdd.Count == 0)
        {
            Debug.LogWarning("[CombatStartInventory] La lista 'Items To Auto Add' est� vac�a. No se a�adieron objetos.");
            return;
        }

        InventorySystem inv = InventorySystem.Instance;
        ItemDatabase db = ItemDatabase.Instance;
        Debug.Log("[CombatStartInventory] A�adiendo objetos de prueba autom�ticamente...");


        foreach (var entry in itemsToAutoAdd)
        {
            if (entry.itemAsset != null && entry.quantity > 0)
            {
                inv.AddItem(entry.itemAsset, entry.quantity);
                Debug.Log($"[CombatStartInventory] A�adido: {entry.itemAsset.itemName} x{entry.quantity}");
            }
            else if (entry.itemAsset == null)
            {
                Debug.LogWarning("[CombatStartInventory] Se encontr� una entrada en la lista sin un Item Asset asignado.");
            }
        }
        Debug.Log("[CombatStartInventory] Objetos de prueba a�adidos autom�ticamente.");

        itemsAdded = true;
    }

}