using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AbilityDatabase : MonoBehaviour
{
    public static AbilityDatabase Instance;

    public List<AbilityData> allAbilities = new();
    private Dictionary<string, AbilityData> abilityLookup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAbilitiesFromResources();
            BuildLookup();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadAbilitiesFromResources()
    {
        AbilityData[] loaded = Resources.LoadAll<AbilityData>("Abilities");
        allAbilities = loaded.ToList();
        Debug.Log($"[AbilityDatabase] Loaded {allAbilities.Count} abilities from Resources/Abilities");
    }

    private void BuildLookup()
    {
        abilityLookup = allAbilities.ToDictionary(a => a.abilityName);
    }

    public AbilityData GetByName(string name)
    {
        return abilityLookup.TryGetValue(name, out var ability) ? ability : null;
    }
}
