using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMonsterAbility", menuName = "RPG/Abilities/MonsterAbility")]
public class MonsterAbilityData : ScriptableObject
{
    public string abilityName;

    [Header("Cost & Targeting")]
    public int mpCost;
    public AbilityTargetType targetType;

    [Header("Damage Formula")]
    public StatScaling damageFormula;
    public bool isHealing;

    [Header("Status Effects")]
    [Range(0f, 1f)]
    public float statusChance = 0f; public List<StatusAilment> inflictsStatusAilments;

    [Header("Visuals")]
    [Tooltip("Prefab del efecto visual que se instanciará sobre el objetivo al recibir la habilidad.")]
    public GameObject targetVFXPrefab;
    [Tooltip("Duración en segundos del VFX sobre el objetivo antes de destruirse.")]
    public float targetVFXDuration = 1.5f;
    [Tooltip("Sonido que se reproduce al ejecutar la habilidad.")]
    public AudioClip executionSound;


    public int CalculateDamage(EnemyInstance enemy)
    {
        if (damageFormula == null || enemy == null) return 0;
        return damageFormula.CalculateDamage(enemy);
    }
}