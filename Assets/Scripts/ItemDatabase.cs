using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    public List<ItemBase> allItems = new();

    private Dictionary<string, ItemBase> itemLookup;
    private Dictionary<string, WeaponItem> weaponLookup;
    private Dictionary<string, ArmorItem> armorLookup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadItemsFromResources();
            BuildLookup();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadItemsFromResources()
    {
        ItemBase[] loaded = Resources.LoadAll<ItemBase>("Items");
        allItems = loaded.ToList();
        Debug.Log($"[ItemDatabase] Loaded {allItems.Count} items from Resources/Items");
    }

    private void BuildLookup()
    {
        itemLookup = allItems.ToDictionary(i => i.itemName);
        weaponLookup = allItems.OfType<WeaponItem>().ToDictionary(w => w.itemName);
        armorLookup = allItems.OfType<ArmorItem>().ToDictionary(a => a.itemName);
    }

    public ItemBase GetByName(string name)
    {
        return itemLookup.TryGetValue(name, out var item) ? item : null;
    }

    public WeaponItem GetWeaponByName(string name)
    {
        return weaponLookup.TryGetValue(name, out var weapon) ? weapon : null;
    }

    public ArmorItem GetArmorByName(string name)
    {
        return armorLookup.TryGetValue(name, out var armor) ? armor : null;
    }
}
