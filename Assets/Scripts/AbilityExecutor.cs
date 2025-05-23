using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class AbilityExecutor
{
    public static void Execute(CharacterStats user, AbilityData ability, object target)
    {

        if (user == null || ability == null)
        {
            Debug.LogError("AbilityExecutor.Execute: User or Ability is null ");
            return;
        }

        int effectAmount = ability.CalculateDamage(user);

        switch (ability.targetType)
        {
            case AbilityTargetType.Enemy:
            case AbilityTargetType.Ally:
            case AbilityTargetType.Any:
                ApplyToTarget(user, target, effectAmount, ability);
                break;

            case AbilityTargetType.AllEnemies:
                if (target is List<EnemyInstance> enemies)
                {
                    foreach (var enemy in enemies.Where(e => e != null && e.IsAlive).ToList()) ApplyToTarget(user, enemy, effectAmount, ability);
                }
                else
                {
                    Debug.LogError($"AbilityExecutor ({ability.abilityName} - AllEnemies): Target type mismatch. Expected List<EnemyInstance>, got {target?.GetType().Name}");
                }
                break;

            case AbilityTargetType.AllAllies:
                if (target is List<CharacterStats> allies)
                {
                    foreach (var ally in allies.Where(a => a != null && a.currentHP > 0).ToList()) ApplyToTarget(user, ally, effectAmount, ability);
                }
                else
                {
                    Debug.LogError($"AbilityExecutor ({ability.abilityName} - AllAllies): Target type mismatch. Expected List<CharacterStats>, got {target?.GetType().Name}");
                }
                break;

            default:
                Debug.LogWarning($"AbilityExecutor: {ability.targetType}");
                break;
        }
    }

    private static void ApplyToTarget(CharacterStats user, object target, int amount, AbilityData ability)
    {
        if (target == null || user == null || ability == null)
        {
            Debug.LogWarning($"ApplyToTarget ({ability?.abilityName ?? "Unknown Ability"}): Target, User o Ability es null.");
            return;
        }

        string userName = user.characterName ?? "User";

        if (ability.isHealing)
        {
            if (target is CharacterStats ally)
            {
                if (ally.currentHP > 0)
                {
                    int hpBefore = ally.currentHP;
                    ally.currentHP = Mathf.Min(ally.maxHP, ally.currentHP + amount);
                    int healedAmount = ally.currentHP - hpBefore;

                    if (healedAmount > 0 && ally.targetAnchor != null)
                    {
                        DamageEffectsManager.Instance?.ShowDamage(healedAmount, ally.targetAnchor, ElementalAffinity.Neutral, true);
                    }

                    TryApplyStatus(ally, ability);
                }
            }
            else { Debug.LogWarning($"{ability.abilityName} (Healing) {target.GetType().Name}"); }
        }
        else
        {
            if (target is EnemyInstance enemy)
            {
                if (enemy.IsAlive)
                {
                    int defense = enemy.Defense;
                    ElementalAffinity affinity = ElementalAffinity.Neutral; float damageMultiplier = 1.0f;

                    if (ability.abilityElement != Element.None)
                    {
                        affinity = enemy.GetAffinity(ability.abilityElement); damageMultiplier = GetElementMultiplier(affinity);
                    }

                    int damageDealt = Mathf.Max(1, Mathf.RoundToInt((amount - defense) * damageMultiplier));

                    enemy.TakeDamage(damageDealt, affinity);
                    TryApplyStatus(enemy, ability);
                }
            }
            else if (target is CharacterStats ally)
            {
                if (ally.currentHP > 0)
                {
                    int defense = ally.defense;
                    if (ally.isDefendingThisTurn) defense = (int)(defense * 1.5f);
                    int damageDealt = Mathf.Max(1, amount - defense);

                    ally.TakeDamage(damageDealt, ElementalAffinity.Neutral, false);
                    TryApplyStatus(ally, ability);
                }
            }
            else { Debug.LogWarning($"{ability.abilityName} (Damage) {target.GetType().Name}"); }
        }
    }

    private static float GetElementMultiplier(ElementalAffinity affinity)
    {
        switch (affinity)
        {
            case ElementalAffinity.Immune: return 0f;
            case ElementalAffinity.Resistant: return 0.5f;
            case ElementalAffinity.Weak: return 1.5f;
            case ElementalAffinity.Neutral: default: return 1.0f;
        }
    }

    private static void TryApplyStatus(object target, AbilityData ability)
    {
        if (target == null || ability == null || ability.inflictsStatusAilments == null || ability.inflictsStatusAilments.Count == 0 || ability.statusChance <= 0) return;

        List<StatusEffectInstance> targetStatusList = null;
        string targetName = "Target";
        bool isTargetAlive = false;

        if (target is CharacterStats player)
        {
            targetStatusList = player.activeStatusEffects;
            targetName = player.characterName;
            isTargetAlive = player.currentHP > 0;
        }
        else if (target is EnemyInstance enemy)
        {
            targetStatusList = enemy.activeStatusEffects;
            targetName = enemy.enemyData?.enemyName ?? "Enemy";
            isTargetAlive = enemy.IsAlive;
        }

        if (targetStatusList == null || !isTargetAlive) return;

        foreach (var statusType in ability.inflictsStatusAilments)
        {
            if (Random.value <= ability.statusChance)
            {
                bool alreadyHasStatus = targetStatusList.Exists(effect => effect.type == statusType && !effect.IsExpired);
                if (!alreadyHasStatus)
                {
                    int duration = 3; targetStatusList.Add(new StatusEffectInstance(statusType, duration));
                    Debug.Log($"{targetName} is suffering {statusType} for {duration} turns");
                }
            }
        }
    }
}