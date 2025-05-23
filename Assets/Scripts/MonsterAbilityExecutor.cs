using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public static class MonsterAbilityExecutor
{
    public static void Execute(EnemyInstance user, MonsterAbilityData ability, object target)
    {
        if (user == null || ability == null)
        {
            Debug.LogError("MonsterAbilityExecutor.Execute: User o Ability es null.");
            return;
        }

        int baseEffectAmount = ability.CalculateDamage(user);

        switch (ability.targetType)
        {
            case AbilityTargetType.Enemy:
            case AbilityTargetType.Ally:
            case AbilityTargetType.Any:
                ApplyToTarget(user, target, baseEffectAmount, ability);
                break;

            case AbilityTargetType.AllEnemies:
                if (target is List<CharacterStats> playerList)
                {
                    foreach (var player in playerList.Where(p => p != null && p.currentHP > 0).ToList())
                        ApplyToTarget(user, player, baseEffectAmount, ability);
                }
                else { Debug.LogError($"MonsterAbilityExecutor: {ability.abilityName} (AllEnemies) target inesperado: {target?.GetType().Name}"); }
                break;

            case AbilityTargetType.AllAllies:
                if (target is List<EnemyInstance> enemyList)
                {
                    foreach (var enemyAlly in enemyList.Where(e => e != null && e.IsAlive).ToList())
                        ApplyToTarget(user, enemyAlly, baseEffectAmount, ability);
                }
                else { Debug.LogError($"MonsterAbilityExecutor: {ability.abilityName} (AllAllies) target inesperado: {target?.GetType().Name}"); }
                break;

            default:
                Debug.LogWarning($"MonsterAbilityExecutor: Tipo de objetivo no manejado {ability.targetType} para {ability.abilityName}");
                break;
        }
    }

    private static void ApplyToTarget(EnemyInstance user, object target, int baseAmount, MonsterAbilityData ability)
    {
        if (target == null || user == null || ability == null)
        {
            Debug.LogWarning("ApplyToTarget: Argumento nulo recibido.");
            return;
        }

        string userName = user.enemyData?.enemyName ?? "Enemigo";

        if (ability.isHealing)
        {
            if (target is EnemyInstance ally && ally.enemyData != null)
            {
                int hpBefore = ally.currentHP;
                if (ally.IsAlive)
                {
                    ally.currentHP = Mathf.Min(ally.enemyData.maxHP, ally.currentHP + baseAmount);
                    int healedAmount = ally.currentHP - hpBefore;
                    if (healedAmount > 0)
                    {
                        Transform healAnchor = ally.targetAnchor ?? ally.worldTransform;
                        if (healAnchor != null) DamageEffectsManager.Instance?.ShowDamage(healedAmount, healAnchor, ElementalAffinity.Neutral, true);
                    }
                    TryApplyStatus(ally.activeStatusEffects, ability, ally.enemyData.enemyName);
                }
            }
            else { Debug.LogWarning($"{userName} ({ability.abilityName}) intentó curar un objetivo inválido: {target.GetType().Name}"); }
        }
        else
        {
            if (target is CharacterStats player)
            {
                if (player.currentHP > 0)
                {
                    int hpBefore = player.currentHP;
                    int defense = player.defense;
                    if (player.isDefendingThisTurn) { defense = (int)(defense * 1.5f); }
                    int damageDealt = Mathf.Max(1, baseAmount - defense);

                    player.TakeDamage(damageDealt);
                    int actualDamageTaken = hpBefore - player.currentHP;

                    TryApplyStatus(player.activeStatusEffects, ability, player.characterName);
                }
            }
            else if (target is EnemyInstance enemy)
            {
                if (enemy.IsAlive && enemy.enemyData != null)
                {
                    int hpBefore = enemy.currentHP;
                    int defense = enemy.Defense;
                    int damageDealt = Mathf.Max(1, baseAmount - defense);

                    enemy.TakeDamage(damageDealt);
                    int actualDamageTaken = hpBefore - enemy.currentHP;


                    TryApplyStatus(enemy.activeStatusEffects, ability, enemy.enemyData.enemyName);
                }
            }
            else { Debug.LogWarning($"{userName} ({ability.abilityName}) intentó dañar un objetivo inválido: {target.GetType().Name}"); }
        }
    }

    private static void TryApplyStatus(List<StatusEffectInstance> targetStatusList, MonsterAbilityData ability, string targetName)
    {
        if (ability.inflictsStatusAilments == null || ability.inflictsStatusAilments.Count == 0 || targetStatusList == null || ability.statusChance <= 0) return;

        foreach (var statusType in ability.inflictsStatusAilments)
        {
            if (Random.value <= ability.statusChance)
            {
                bool alreadyHasStatus = targetStatusList.Exists(effect => effect.type == statusType && !effect.IsExpired);
                if (!alreadyHasStatus)
                {
                    int duration = 3; targetStatusList.Add(new StatusEffectInstance(statusType, duration));
                    Debug.Log($"{targetName} sufre el estado alterado: {statusType} por {duration} turnos.");
                }
            }
        }
    }
}