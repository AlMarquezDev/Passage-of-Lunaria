using System.Collections.Generic;
using UnityEngine;

public enum AbilityTargetType
{
    Enemy, Ally, AllEnemies, AllAllies, Any
}


[CreateAssetMenu(fileName = "NewAbility", menuName = "RPG/Abilities/Ability")]
public class AbilityData : ScriptableObject
{
    public string abilityName;
    public string description;
    public int mpCost;
    public AbilityType abilityType;
    public Sprite icon;

    public bool isHealing; public List<StatusAilment> inflictsStatusAilments;
    public float statusChance;
    [Header("Damage Formula")]
    public StatScaling damageFormula;

    [Header("Ability Element")]
    public Element abilityElement = Element.None;

    [Header("Visuals & Execution")]
    public GameObject targetVFXPrefab;
    public float targetVFXDuration = 1.5f;
    public bool requiresTravel = true;
    public AudioClip executionSound;


    public AbilityTargetType targetType = AbilityTargetType.Enemy;

    public string GetTooltipDescription()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append(description);
        sb.Append($"\n\nMP: {mpCost}");
        return sb.ToString();
    }

    public int CalculateDamage(CharacterStats caster)
    {
        if (damageFormula == null) return 0;
        return damageFormula.CalculateDamage(caster);
    }
}