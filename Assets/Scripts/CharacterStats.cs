using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    public string characterName;
    public CharacterJob characterJob;

    public int level = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;

    public int currentHP;
    public int maxHP;
    public int currentMP;
    public int maxMP;

    public int baseStrength;
    public int baseDefense;
    public int baseIntelligence;
    public int baseAgility;
    public int baseMaxHP;
    public int baseMaxMP;

    public int strength;
    public int defense;
    public int intelligence;
    public int agility;

    public WeaponItem rightHand;
    public ArmorItem leftHand;
    public ArmorItem head;
    public ArmorItem body;
    public ArmorItem accessory;

    public List<AbilityData> knownAbilities = new();
    public List<StatusEffectInstance> activeStatusEffects = new();

    public bool isDefendingThisTurn = false;

    // === Combate ===
    // Transform asociado al cursor de selecci�n
    [System.NonSerialized]
    public Transform targetAnchor;

    // === Inicializaci�n ===
    public void SetBaseStats(CharacterClassData classData)
    {
        baseStrength = classData.strength;
        baseDefense = classData.defense;
        baseIntelligence = classData.intelligence;
        baseAgility = classData.agility;
        baseMaxHP = classData.baseHP;
        baseMaxMP = classData.baseMP;

        UpdateExpToNextLevel();

        ApplyAllEquipmentBonuses();
        currentHP = maxHP;
        currentMP = maxMP;

        LearnAbilitiesForLevel();
    }

    public void LearnAbilitiesForLevel()
    {
        CharacterClassData classData = GameManager.Instance.GetClassData(characterJob);
        foreach (var learnable in classData.learnableAbilities)
        {
            if (level >= learnable.levelLearned && !knownAbilities.Contains(learnable.ability))
                knownAbilities.Add(learnable.ability);
        }
    }

    // === Equipamiento ===
    public void ApplyAllEquipmentBonuses()
    {
        ResetToBaseStats();

        ApplyBonus(rightHand?.bonusStats);
        ApplyBonus(leftHand?.bonusStats);
        ApplyBonus(head?.bonusStats);
        ApplyBonus(body?.bonusStats);
        ApplyBonus(accessory?.bonusStats);
        currentHP = Mathf.Min(currentHP, maxHP);
        currentMP = Mathf.Min(currentMP, maxMP);
        // Tambi�n asegurarse que no sean negativos si algo muy raro pasa
        currentHP = Mathf.Max(0, currentHP);
        currentMP = Mathf.Max(0, currentMP);
    }

    private void ResetToBaseStats()
    {
        // --- A�ADE ESTE LOG ---
        Debug.LogWarning($"ResetToBaseStats para {characterName} (Lvl {level}): " +
                         $"baseMaxHP={this.baseMaxHP}, baseDefense={this.baseDefense}, " + // Usa this. para acceder a los privados si est�s fuera
                         $"baseMaxMP={this.baseMaxMP}, baseIntelligence={this.baseIntelligence}");
        // --------------------
        strength = Mathf.Min(baseStrength, 99);
        defense = Mathf.Min(baseDefense, 99);
        intelligence = Mathf.Min(baseIntelligence, 99);
        agility = Mathf.Min(baseAgility, 99);

        maxHP = StatFormulaUtility.CalculateMaxHP(baseMaxHP, defense, level, 9999);
        maxMP = StatFormulaUtility.CalculateMaxMP(baseMaxMP, intelligence, level, 9999);

        Debug.LogWarning($" ---> ResetToBaseStats Calculated: maxHP={maxHP}, maxMP={maxMP}");
    }

    private void ApplyBonus(StatBonus bonus)
    {
        if (bonus == null) return;

        strength = Mathf.Min(strength + bonus.strength, 99);
        defense = Mathf.Min(defense + bonus.defense, 99);
        intelligence = Mathf.Min(intelligence + bonus.intelligence, 99);
        agility = Mathf.Min(agility + bonus.agility, 99);

        maxHP = Mathf.Min(maxHP + bonus.maxHP, 9999);
        maxMP = Mathf.Min(maxMP + bonus.maxMP, 9999);
    }

    public void EquipItem(EquipmentItem item)
    {
        if (item == null || !item.allowedJobs.Contains(characterJob)) return;

        switch (item.slotType)
        {
            case EquipmentSlot.RightHand: rightHand = item as WeaponItem; break;
            case EquipmentSlot.LeftHand: leftHand = item as ArmorItem; break;
            case EquipmentSlot.Head: head = item as ArmorItem; break;
            case EquipmentSlot.Body: body = item as ArmorItem; break;
            case EquipmentSlot.Accessory: accessory = item as ArmorItem; break;
        }

        ApplyAllEquipmentBonuses();
    }

    public EquipmentItem GetEquippedItem(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.RightHand => rightHand,
            EquipmentSlot.LeftHand => leftHand,
            EquipmentSlot.Head => head,
            EquipmentSlot.Body => body,
            EquipmentSlot.Accessory => accessory,
            _ => null
        };
    }

    public void UnequipSlot(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.RightHand: rightHand = null; break;
            case EquipmentSlot.LeftHand: leftHand = null; break;
            case EquipmentSlot.Head: head = null; break;
            case EquipmentSlot.Body: body = null; break;
            case EquipmentSlot.Accessory: accessory = null; break;
        }

        ApplyAllEquipmentBonuses();
    }

    public bool HasItemEquipped(EquipmentItem item)
    {
        return rightHand == item || leftHand == item || head == item || body == item || accessory == item;
    }

    // === Estados ===
    public void ApplyStatus(StatusAilment ailment, int duration)
    {
        var existing = activeStatusEffects.Find(s => s.type == ailment);
        if (existing != null)
            existing.remainingTurns = duration;
        else
            activeStatusEffects.Add(new StatusEffectInstance(ailment, duration));
    }

    public bool HasStatus(StatusAilment ailment)
    {
        return activeStatusEffects.Exists(s => s.type == ailment && !s.IsExpired);
    }

    public void TickStatusEffects()
    {
        foreach (var s in activeStatusEffects)
            s.TickDown();
        activeStatusEffects.RemoveAll(s => s.IsExpired);
    }

    public void RemoveAilment(StatusAilment ailment)
    {
        activeStatusEffects.RemoveAll(s => s.type == ailment);
    }

    // === EXP & Subida de Nivel ===
    // In CharacterStats.cs, inside GainExperience method
    public void GainExperience(int amount)
    {
        if (level >= 99 || amount <= 0) return; // Added amount <= 0 check
        currentExp += amount;

        while (level < 99 && currentExp >= expToNextLevel)
        {
            if (expToNextLevel <= 0 && level < 99) // Safety for invalid expToNextLevel
            {
                Debug.LogWarning($"ExpToNextLevel was {expToNextLevel} for {characterName} at Lvl {level}. Recalculating.");
                UpdateExpToNextLevel();
                if (expToNextLevel <= 0)
                { // Still bad? Break to avoid infinite loop
                    Debug.LogError($"Failed to get valid ExpToNextLevel for {characterName} at Lvl {level}. Aborting level up loop.");
                    currentExp = 0; // Prevent further processing with bad data
                    break;
                }
            }
            currentExp -= expToNextLevel;
            level++;
            Debug.Log($"{characterName} sube a nivel {level}!");

            LevelUp(); // This should increase base stats
            UpdateExpToNextLevel(); // This calculates based on new level
            LearnAbilitiesForLevel(); // This learns based on new level
        }

        if (level >= 99)
        {
            currentExp = 0;
            expToNextLevel = 0; // Or some very high number if you prefer
        }
    }

    private void LevelUp()
    {
        CharacterClassData classData = GameManager.Instance.GetClassData(characterJob);

        baseStrength = Mathf.Min(baseStrength + classData.strengthGrowth, 99);
        baseDefense = Mathf.Min(baseDefense + classData.defenseGrowth, 99);
        baseIntelligence = Mathf.Min(baseIntelligence + classData.intelligenceGrowth, 99);
        baseAgility = Mathf.Min(baseAgility + classData.agilityGrowth, 99);
        baseMaxHP = Mathf.Min(baseMaxHP + classData.hpGrowth, 9999);
        baseMaxMP = Mathf.Min(baseMaxMP + classData.mpGrowth, 9999);

        ApplyAllEquipmentBonuses();
        currentHP = maxHP;
        currentMP = maxMP;
    }

    public void UpdateExpToNextLevel()
    {
        expToNextLevel = GameManager.Instance.expCurve.GetExpRequiredForLevel(level);
    }

    // === Da�o ===
    // Firma MODIFICADA para aceptar afinidad y si es curaci�n
    public void TakeDamage(int amount, ElementalAffinity affinity = ElementalAffinity.Neutral, bool isHealing = false)
    {
        int hpBefore = currentHP;
        // amount puede ser positivo para da�o o curaci�n aqu�
        int appliedChange = amount; // Usamos amount directamente para el c�lculo de HP

        if (isHealing)
        {
            // Aplicar curaci�n
            currentHP = Mathf.Min(maxHP, currentHP + appliedChange);
        }
        else
        {
            // Aplicar da�o (asegurando que amount no sea negativo aqu� si se pasa por error)
            appliedChange = Mathf.Max(0, appliedChange); // El da�o aplicado no puede ser negativo
            currentHP = Mathf.Max(0, currentHP - appliedChange);
        }

        // Calcular el cambio real para mostrarlo y para l�gica posterior
        int changeInHP = currentHP - hpBefore; // Positivo para curar, negativo o cero para da�o

        // Determinar el transform donde mostrar el efecto
        Transform anchorToShow = targetAnchor;
        if (anchorToShow == null)
        {
            // --- FALLBACK CORREGIDO ---
            // Debug.LogWarning($"TakeDamage for {characterName}: targetAnchor was null..."); // Opcional
            if (BattleFlowController.Instance != null)
            {
                AllyWorldAnchor visualAnchor = BattleFlowController.Instance.GetVisualAnchorForCharacter(this);
                if (visualAnchor != null)
                {
                    anchorToShow = visualAnchor.transform;
                    // Debug.Log($"TakeDamage for {characterName}: Found visualAnchor transform as fallback."); // Opcional
                }
                // else { Debug.LogWarning($"TakeDamage for {characterName}: Could not find visualAnchor..."); } // Opcional
            }
            // else { Debug.LogWarning("TakeDamage for {characterName}: BattleFlowController.Instance is null...");} // Opcional
            // --- FIN FALLBACK ---
        }

        // Llamar a ShowDamage si el anchor existe
        if (anchorToShow != null)
        {
            // Pasamos la cantidad del cambio REAL (siempre positivo para mostrar)
            // y la informaci�n de afinidad/curaci�n
            int amountToShow = Mathf.Abs(changeInHP);
            // Llamamos a la NUEVA versi�n de ShowDamage
            DamageEffectsManager.Instance?.ShowDamage(amountToShow, anchorToShow, affinity, isHealing); // <--- PASAR PAR�METROS
        }
        else
        {
            Debug.LogError($"TakeDamage for {characterName}: No valid transform found to show damage effect!");
        }

        // L�gica de despertar del sue�o (solo si hubo da�o efectivo)
        if (!isHealing && changeInHP < 0) // changeInHP ser� negativo si hubo da�o
        {
            var sleep = activeStatusEffects.FirstOrDefault(e => e.type == StatusAilment.Sleep && !e.IsExpired);
            if (sleep != null)
            {
                // Debug.Log($"{characterName} fue golpeado y se despert�."); // Opcional
                RemoveAilment(StatusAilment.Sleep);
                foreach (var status in activeStatusEffects) status.receivedHitThisTurn = true;
            }
            else
            {
                // Marcar golpe aunque no hubiera sleep
                foreach (var status in activeStatusEffects) status.receivedHitThisTurn = true;
            }
        }
    }

}
