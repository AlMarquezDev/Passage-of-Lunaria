using UnityEngine;
using System.Collections.Generic;

// Enums (sin cambios)
public enum Element { None, Fire, Ice, Lightning, Wind, Spirit, Poison }
public enum ElementalAffinity { Immune, Resistant, Neutral, Weak }

[CreateAssetMenu(menuName = "RPG/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    public string enemyName;
    public Sprite sprite;

    [Header("Stats")]
    public int maxHP;
    public int maxMP;
    public int attack;
    public int defense;
    public int magicDefense;
    public int agility;

    [Header("Rewards")]
    public int goldReward;
    public int experienceReward;

    [Header("Drops")]
    public List<DropItemEntry> dropTable;

    [Header("Abilities")]
    public List<MonsterAbilityData> abilities;

    [Header("Elemental Affinities")]
    public List<ElementAffinityEntry> elementalAffinities;

    [Header("Visual")]
    public GameObject worldPrefab;
    // Campos de VFX/SFX eliminados de aquí

    // --- NUEVO CAMPO AÑADIDO ---
    [Header("Audio")] // Puedes añadirlo aquí o agruparlo con otros SFX si los añades después
    [Tooltip("Sonido que se reproduce cuando este enemigo muere (al iniciar desintegración).")]
    public AudioClip deathSFX;
    // --- FIN NUEVO CAMPO ---

    public ElementalAffinity GetAffinity(Element element)
    {
        var found = elementalAffinities?.Find(a => a.element == element);
        return found != null ? found.affinity : ElementalAffinity.Neutral;
    }
}

// Clases serializables (sin cambios)
[System.Serializable]
public class DropItemEntry { public ItemBase item; [Range(0f, 1f)] public float dropChance; }
[System.Serializable]
public class ElementAffinityEntry { public Element element; public ElementalAffinity affinity; }