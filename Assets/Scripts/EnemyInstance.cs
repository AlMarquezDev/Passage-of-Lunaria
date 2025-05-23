using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CombatSystem; // Aseg�rate que este namespace exista y contenga lo necesario

public class EnemyInstance
{
    public EnemyData enemyData;

    public int currentHP;
    public int currentMP;

    // Propiedades de acceso a stats
    public int Attack => enemyData?.attack ?? 0;
    public int Defense => enemyData?.defense ?? 0;
    public int MagicDefense => enemyData?.magicDefense ?? 0;
    public int Agility => enemyData?.agility ?? 0;
    public bool IsAlive => currentHP > 0;

    // Referencias asignadas externamente
    public Transform worldTransform;
    public Transform targetAnchor; // Punto donde aparecen VFX/SFX al ser golpeado ESTE enemigo

    public List<StatusEffectInstance> activeStatusEffects = new List<StatusEffectInstance>();

    // Constructor
    public EnemyInstance(EnemyData data)
    {
        enemyData = data;
        if (enemyData != null)
        {
            currentHP = enemyData.maxHP;
            currentMP = enemyData.maxMP;
        }
        else { Debug.LogError("EnemyInstance creado con EnemyData null!"); }
    }

    // TakeDamage - Modificado para llamar efecto de desintegraci�n
    // Firma MODIFICADA para aceptar afinidad (isHealing no aplica aqu�)
    public void TakeDamage(int damage, ElementalAffinity affinity = ElementalAffinity.Neutral)
    {
        int hpBeforeDamage = currentHP;
        int damageReceived = Mathf.Max(0, damage); // Da�o que se intenta aplicar (no negativo)
        currentHP = Mathf.Max(0, currentHP - damageReceived); // Aplica el da�o a la vida

        // Calcula el da�o REAL que se quit� de la vida (para mostrar el n�mero correcto)
        int damageTaken = hpBeforeDamage - currentHP;

        // Log opcional
        // Debug.Log($"EnemyInstance.TakeDamage: {enemyData?.enemyName} received {damage}, took {damageTaken}. TargetAnchor Null? {targetAnchor == null}");

        // Determinar el transform donde mostrar el efecto
        Transform anchorToShow = targetAnchor;
        if (anchorToShow == null)
        {
            // Debug.LogWarning($"TakeDamage for {enemyData?.enemyName}: targetAnchor was null, using worldTransform as fallback.");
            anchorToShow = this.worldTransform; // Fallback
        }

        // Mostrar el efecto visual usando DamageEffectsManager
        if (anchorToShow != null)
        {
            // Pasamos el da�o REAL aplicado (damageTaken) y la afinidad.
            // El �ltimo par�metro 'isHealing' es false para da�o.
            DamageEffectsManager.Instance?.ShowDamage(damageTaken, anchorToShow, affinity, false); // <--- PASAR PAR�METROS
        }
        // else { Debug.LogWarning($"TakeDamage for {enemyData?.enemyName}: No valid transform..."); } // Opcional

        // L�gica de despertar del sue�o (solo si hubo da�o efectivo)
        if (damageTaken > 0)
        {
            var sleep = activeStatusEffects.Find(s => s.type == StatusAilment.Sleep && !s.IsExpired);
            if (sleep != null)
            {
                // Debug.Log($"{enemyData?.enemyName ?? "Enemigo"} fue golpeado y se despert�."); // Opcional
                RemoveAilment(StatusAilment.Sleep);
                // Marcar golpe para todos los estados (incluso si se despert�)
                foreach (var s in activeStatusEffects) s.receivedHitThisTurn = true;
            }
            else
            {
                // Marcar golpe aunque no estuviera dormido
                foreach (var s in activeStatusEffects) s.receivedHitThisTurn = true;
            }
        }

        // Comprobar si muri�
        if (hpBeforeDamage > 0 && currentHP <= 0)
        {
            TriggerDeathSequence();
        }
    }

    // --- NUEVO: M�todo para iniciar la secuencia de muerte ---
    private void TriggerDeathSequence()
    {

        Debug.Log($"{enemyData?.enemyName ?? "Enemigo"} ha llegado a 0 HP. Iniciando desintegraci�n.");
        // Buscar el componente EnemyWorldAnchor en el objeto visual asociado
        EnemyWorldAnchor visualAnchor = worldTransform.GetComponent<EnemyWorldAnchor>();
        if (visualAnchor != null)
        {
            // Llamar al m�todo que iniciar� la corutina de desintegraci�n (debe existir en EnemyWorldAnchor)
            visualAnchor.StartDisintegrationEffect();
        }
        else
        {
            Debug.LogError($"No se encontr� EnemyWorldAnchor en {worldTransform.name} para iniciar efecto de muerte.", worldTransform);
            // Fallback: Podr�as destruir el objeto directamente si no hay anchor
            // Object.Destroy(worldTransform.gameObject);
        }
        // Aqu� podr�as a�adir l�gica adicional como marcar al enemigo como "muriendo"
        // para evitar que act�e o sea seleccionado como objetivo.
    }
    // --- FIN NUEVO ---

    // ApplyStatus, HasStatus, TickStatusEffects, GetAffinity, RemoveAilment (sin cambios)
    public void ApplyStatus(StatusAilment ailment, int duration)
    {
        var existing = activeStatusEffects.Find(s => s.type == ailment);
        if (existing != null) existing.remainingTurns = duration;
        else activeStatusEffects.Add(new StatusEffectInstance(ailment, duration));
    }
    public bool HasStatus(StatusAilment ailment) { return activeStatusEffects.Exists(s => s.type == ailment && !s.IsExpired); }
    public void TickStatusEffects()
    {
        foreach (var s in activeStatusEffects.ToList())
        {
            if (s.type == StatusAilment.Sleep && !s.receivedHitThisTurn) { if (Random.value <= 0.3f) { Debug.Log($"{enemyData?.enemyName} se despierta."); RemoveAilment(StatusAilment.Sleep); continue; } }
            s.TickDown();
        }
        activeStatusEffects.RemoveAll(s => s.IsExpired);
        foreach (var s in activeStatusEffects) s.receivedHitThisTurn = false;
    }
    public ElementalAffinity GetAffinity(Element element)
    {
        if (enemyData?.elementalAffinities == null) return ElementalAffinity.Neutral;
        var found = enemyData.elementalAffinities.Find(a => a.element == element);
        return found != null ? found.affinity : ElementalAffinity.Neutral;
    }
    public void RemoveAilment(StatusAilment ailment) { activeStatusEffects.RemoveAll(s => s.type == ailment); }


    // Decide la acci�n a tomar (llamado por TurnManager)
    public BattleAction DecideAction(List<CharacterStats> availableParty, List<EnemyInstance> availableEnemies)
    {
        // C�digo DecideAction existente...
        if (!IsAlive) return null;
        var aliveParty = availableParty?.Where(p => p != null && p.currentHP > 0).ToList() ?? new List<CharacterStats>();
        if (!aliveParty.Any()) { Debug.LogWarning($"{enemyData?.enemyName} no pudo decidir acci�n (no party targets)."); return null; }
        bool canUseAbility = enemyData?.abilities?.Count > 0 && currentMP >= (enemyData.abilities.FirstOrDefault(a => a != null)?.mpCost ?? int.MaxValue);
        float chanceToUseAbility = 0.6f;
        if (canUseAbility && Random.value < chanceToUseAbility)
        {
            var usableAbilities = enemyData.abilities.Where(a => a != null && currentMP >= a.mpCost).ToList();
            if (usableAbilities.Count > 0) { var selectedAbility = usableAbilities[Random.Range(0, usableAbilities.Count)]; object abilityTarget = null; switch (selectedAbility.targetType) { case AbilityTargetType.Enemy: abilityTarget = GetRandomAliveCharacter(aliveParty); break; case AbilityTargetType.AllEnemies: abilityTarget = aliveParty; break; case AbilityTargetType.Ally: abilityTarget = GetRandomAliveEnemy(availableEnemies); break; case AbilityTargetType.AllAllies: abilityTarget = availableEnemies?.Where(a => a != null && a.IsAlive).ToList(); break; } if (abilityTarget != null) { return new BattleAction(this, BattleCommand.Special, abilityTarget, selectedAbility); } }
        }
        CharacterStats attackTarget = GetRandomAliveCharacter(aliveParty);
        if (attackTarget != null) { return new BattleAction(this, BattleCommand.Attack, attackTarget); }
        Debug.LogWarning($"{enemyData?.enemyName} - Fallo DecideAction final.");
        return null;
    }


    // Helpers GetRandom... (sin cambios)
    private CharacterStats GetRandomAliveCharacter(List<CharacterStats> party)
    {
        if (party == null) return null;
        var alive = party.Where(c => c != null && c.currentHP > 0).ToList();
        return alive.Count == 0 ? null : alive[Random.Range(0, alive.Count)];
    }
    private EnemyInstance GetRandomAliveEnemy(List<EnemyInstance> enemies)
    {
        if (enemies == null) return null;
        var alive = enemies.Where(e => e != null && e.IsAlive && e != this).ToList();
        return alive.Count == 0 ? null : alive[Random.Range(0, alive.Count)];
    }
}