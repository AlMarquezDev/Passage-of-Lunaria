using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "RPG/Items/Consumable")]
public class ConsumableItem : ItemBase
{
    [Header("Efectos del ítem")]
    public int restoreHP;
    public int restoreMP;
    public bool fullRestore;

    public bool revives;
    [Range(0, 100)] public int reviveHPPercent;

    public StatusAilment[] curesAilments;

    [Header("Targeting")]
    public ItemTargetType targetType;

    [Header("Uso")]
    [TextArea] public string useDescription;
    [Tooltip("Sonido que se reproduce al usar este objeto consumible.")]
    public AudioClip useSound;

    public override string GetTooltipDescription()
    {
        StringBuilder sb = new StringBuilder();

        if (!string.IsNullOrEmpty(useDescription))
            sb.AppendLine(useDescription);

        if (fullRestore)
            sb.AppendLine("Fully restores HP and MP.");
        else
        {
            if (restoreHP > 0) sb.AppendLine($"Restores {restoreHP} HP.");
            if (restoreMP > 0) sb.AppendLine($"Restores {restoreMP} MP.");
        }

        if (revives) sb.AppendLine($"Revives a fallen ally with {reviveHPPercent}% HP.");

        if (curesAilments != null && curesAilments.Length > 0)
            sb.AppendLine("Cures: " + string.Join(", ", curesAilments));

        return sb.ToString();
    }

    public void ApplyEffect(CharacterStats target)
    {
        if (target == null) return;

        string targetName = target.characterName ?? "Target";
        string itemName = this.itemName ?? "Item";

        bool didRestoreHPorMP = false; bool didRevive = false;
        if (revives && target.currentHP <= 0)
        {
            int hpBeforeRevive = target.currentHP;
            target.currentHP = Mathf.Max(1, (int)(target.maxHP * (reviveHPPercent / 100f)));
            Debug.Log($"{itemName} revived {targetName} with {target.currentHP} HP!");
            didRevive = true;

            int healedAmount = target.currentHP - hpBeforeRevive;
            if (healedAmount > 0 && target.targetAnchor != null)
            {
                DamageEffectsManager.Instance?.ShowDamage(healedAmount, target.targetAnchor, ElementalAffinity.Neutral, true, false);
            }
        }

        if (target.currentHP > 0)
        {
            int hpBefore = target.currentHP;
            int mpBefore = target.currentMP;

            if (fullRestore)
            {
                target.currentHP = target.maxHP;
                target.currentMP = target.maxMP;
            }
            else
            {
                if (restoreHP > 0)
                    target.currentHP = Mathf.Min(target.maxHP, target.currentHP + restoreHP);
                if (restoreMP > 0)
                    target.currentMP = Mathf.Min(target.maxMP, target.currentMP + restoreMP);
            }

            int hpRestored = target.currentHP - hpBefore;
            int mpRestored = target.currentMP - mpBefore;

            if (hpRestored > 0)
            {
                Debug.Log($"{targetName} recuperó {hpRestored} HP.");
                if (target.targetAnchor != null)
                {
                    DamageEffectsManager.Instance?.ShowDamage(hpRestored, target.targetAnchor, ElementalAffinity.Neutral, true, false);
                    didRestoreHPorMP = true;
                }
            }
            if (mpRestored > 0)
            {
                Debug.Log($"{targetName} recuperó {mpRestored} MP.");
                if (target.targetAnchor != null)
                {
                    DamageEffectsManager.Instance?.ShowDamage(mpRestored, target.targetAnchor, ElementalAffinity.Neutral, true, true);
                    didRestoreHPorMP = true;
                }
            }

            if (curesAilments != null && curesAilments.Length > 0)
            {
                bool curedSomething = false;
                foreach (var ailment in curesAilments)
                {
                    if (target.activeStatusEffects.Any(se => se.type == ailment && !se.IsExpired))
                    {
                        target.RemoveAilment(ailment);
                        Debug.Log($"{itemName} curó {ailment} de {targetName}.");
                        curedSomething = true;
                    }
                }
            }
        }
        else if (!revives)
        {
            Debug.Log($"{itemName} no tuvo efecto en {targetName} porque está K.O.");
        }

        if (didRestoreHPorMP || didRevive)
        {
            if (target.targetAnchor != null)
            {
                DamageEffectsManager.Instance?.InstantiateHealingVFX(target.targetAnchor);
            }
            else
            {
                Debug.LogWarning($"ConsumableItem: No targetAnchor found for {targetName} to show healing VFX.");
            }
        }
    }

    public void Use(CharacterStats user, object target)
    {
        bool itemConsumed = false;

        if (target is CharacterStats characterTarget)
        {
            int hpBeforeApply = characterTarget.currentHP;
            int mpBeforeApply = characterTarget.currentMP;

            ApplyEffect(characterTarget);
            if (characterTarget.currentHP != hpBeforeApply || characterTarget.currentMP != mpBeforeApply)
            {
                itemConsumed = true;
            }
        }
        else if (target is List<CharacterStats> characterList)
        {
            Debug.Log($"Aplicando {itemName} a {characterList.Count} objetivo(s)...");
            bool appliedToAnyone = false;
            foreach (CharacterStats listTarget in characterList)
            {
                if (listTarget != null)
                {
                    int hpBeforeApply = listTarget.currentHP;
                    int mpBeforeApply = listTarget.currentMP;

                    ApplyEffect(listTarget);
                    if (listTarget.currentHP != hpBeforeApply || listTarget.currentMP != mpBeforeApply)
                    {
                        appliedToAnyone = true;
                    }
                }
            }
            if (appliedToAnyone)
            {
                itemConsumed = true;
            }
            else
            {
                Debug.Log($"{itemName} no tuvo efecto en ningún objetivo de la lista.");
            }
        }
        else
        {
            Debug.LogWarning($"[ConsumableItem] '{this.itemName}' intentó usarse en un objetivo de tipo inesperado: {target?.GetType().Name ?? "null"}");
        }

        if (itemConsumed && InventorySystem.Instance != null)
        {
            InventorySystem.Instance.RemoveItem(this, 1);
            Debug.Log($"Item '{itemName}' consumido.");
        }
        else if (itemConsumed && InventorySystem.Instance == null)
        {
            Debug.LogError("¡InventorySystem.Instance es null! No se pudo consumir el item.");
        }
    }
}