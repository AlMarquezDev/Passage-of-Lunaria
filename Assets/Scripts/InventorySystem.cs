using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    [SerializeField] private List<ItemEntry> items = new();

    // InventorySystem.cs
    private void Awake()
    {
        Debug.Log($"InventorySystem AWAKE - GameObject: {gameObject.name}, InstanceID: {GetInstanceID()}");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"InventorySystem Instance ASSIGNED - GameObject: {gameObject.name}");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"InventorySystem: Otra instancia encontrada (Instance: {Instance.gameObject.name}, ID: {Instance.GetInstanceID()}). Destruyendo este duplicado (Name: {gameObject.name}, ID: {GetInstanceID()}).");
            Destroy(gameObject);
        }
        else // Instance == this
        {
            Debug.Log($"InventorySystem AWAKE - Esta instancia ya es el Singleton. GameObject: {gameObject.name}");
        }
    }

    void OnDestroy()
    {
        Debug.LogWarning($"InventorySystem OnDestroy - GameObject: {gameObject.name}, InstanceID: {GetInstanceID()}");
        if (Instance == this)
        {
            // Instance = null; // Opcional: Podrías ponerlo a null para saber que fue destruido, pero
            // esto puede causar problemas si otros scripts aún intentan accederlo
            // sin saber que ha sido destruido. Es mejor asegurarse que DontDestroyOnLoad funcione.
            Debug.LogError("¡El Singleton de InventorySystem está siendo destruido! Esto no debería suceder si DontDestroyOnLoad está activo y es la instancia correcta.");
        }
    }

    public void AddItem(ItemBase item, int quantity)
    {
        var entry = items.FirstOrDefault(e => e.item == item);
        if (entry != null)
        {
            entry.quantity = Mathf.Clamp(entry.quantity + quantity, 0, 99);
        }
        else
        {
            items.Add(new ItemEntry { item = item, quantity = Mathf.Clamp(quantity, 0, 99) });
        }
    }

    public void RemoveItem(ItemBase item, int quantity)
    {
        var entry = items.FirstOrDefault(e => e.item == item);
        if (entry != null)
        {
            entry.quantity -= quantity;
            if (entry.quantity <= 0)
            {
                items.Remove(entry);
            }
        }
    }

    public int GetItemCount(ItemBase item)
    {
        var entry = items.FirstOrDefault(e => e.item == item);
        return entry != null ? entry.quantity : 0;
    }

    public void ClearInventory()
    {
        items.Clear();
    }

    public List<ItemEntry> GetAllItems()
    {
        return items;
    }

    public int GetQuantity(ItemBase item)
    {
        var entry = items.FirstOrDefault(e => e.item == item);
        return entry != null ? entry.quantity : 0;
    }

    public List<ConsumableItem> GetUsableConsumables()
    {
        return items
            .Where(e => e.item is ConsumableItem && e.quantity > 0)
            .Select(e => e.item as ConsumableItem)
            .ToList();
    }

    [ContextMenu("Print Inventory")]
    public void PrintInventory()
    {
        Debug.Log("INVENTORY CONTENT:");
        foreach (var entry in items)
        {
            Debug.Log($"- {entry.item.itemName} x{entry.quantity}");
        }
    }
}

[System.Serializable]
public class ItemEntry
{
    public ItemBase item;
    public int quantity;
}
