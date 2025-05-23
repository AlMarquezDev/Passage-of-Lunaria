using System; // <--- AÑADE ESTA LÍNEA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CombatSystem;// Asegúrate que este namespace exista y contenga BattleCommand

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    // Evento se dispara cuando todas las acciones del turno terminan
    public static event System.Action OnTurnExecutionComplete;
    // Evento se dispara después de que cada acción individual termina
    public static event System.Action OnActionExecuted;

    [Header("Action Timing")]
    [Tooltip("Pausa corta después de ejecutar una acción antes de procesar la siguiente.")]
    [SerializeField] private float postActionDelay = 0.3f;
    [Tooltip("Pausa corta si un actor no puede actuar debido a un estado.")]
    [SerializeField] private float statusInactionDelay = 0.8f;
    [Tooltip("Tiempo máximo de espera para que la cámara vuelva a su posición por defecto.")]
    [SerializeField] private float cameraReturnTimeout = 4.0f;
    [Tooltip("Tiempo que la cámara permanece enfocada en el objetivo en habilidades sin viaje (antes de empezar a volver).")]
    [SerializeField] private float noTravelAbilityCameraFocusTime = 0.6f;

    [Header("Common Enemy Hit Effects")]
    [Tooltip("Prefab del VFX que se instancia cuando un enemigo golpea a un aliado.")]
    public GameObject commonEnemyHitVFX;
    [Tooltip("Sonido que se reproduce cuando un enemigo golpea a un aliado.")]
    public AudioClip commonEnemyHitSFX;
    [Tooltip("Duración del VFX de golpe enemigo.")]
    public float commonEnemyHitVFXDuration = 1.5f;

    // Cola de acciones para el turno actual
    private Queue<BattleAction> turnActionQueue = new Queue<BattleAction>();
    // Corutina activa de procesamiento
    private Coroutine processActionsCoroutine = null;
    // Flag para evitar ejecuciones concurrentes
    private bool isExecutingTurn = false;

    // Referencias cacheadas de los participantes al inicio de la ejecución del turno
    private List<CharacterStats> currentPartyMembers;
    private List<EnemyInstance> currentEnemyInstances;

    // AudioSource para SFX generales del TurnManager
    private AudioSource sfxAudioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Opcional: DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        // Obtener o añadir un AudioSource
        sfxAudioSource = GetComponent<AudioSource>();
        if (sfxAudioSource == null) {
             sfxAudioSource = gameObject.AddComponent<AudioSource>();
             sfxAudioSource.playOnAwake = false;
             sfxAudioSource.spatialBlend = 0; // Audio 2D
        }
    }

    /// <summary>
    /// Inicia la fase de ejecución del turno. Prepara y ordena todas las acciones.
    /// </summary>
    public void BeginTurnExecution(List<BattleAction> plannedPlayerActions)
    {
        if (isExecutingTurn)
        {
            Debug.LogWarning("BeginTurnExecution llamado mientras un turno ya estaba en progreso. Ignorando.");
            return;
        }
        Debug.Log("<<<< TurnManager: BeginTurnExecution INICIADO >>>>");
        isExecutingTurn = true;
        turnActionQueue.Clear();

        // --- Preparación de Actores y Acciones ---
        currentPartyMembers = BattleFlowController.Instance?.GetParty()?.Where(p => p != null && p.currentHP > 0).ToList() ?? new List<CharacterStats>();
        currentEnemyInstances = BattleFlowController.Instance?.GetEnemies()?.Where(e => e != null && e.IsAlive).ToList() ?? new List<EnemyInstance>();

        List<BattleAction> allTurnActions = new List<BattleAction>();

        var validPlayerActions = plannedPlayerActions?
            .Where(a => a != null && a.character != null && currentPartyMembers.Contains(a.character))
            .ToList() ?? new List<BattleAction>();
        allTurnActions.AddRange(validPlayerActions);

        var enemiesToAct = currentEnemyInstances.OrderByDescending(e => e.Agility);
        foreach (var enemy in enemiesToAct)
        {
            BattleAction enemyAction = enemy.DecideAction(currentPartyMembers, currentEnemyInstances);
            if (enemyAction != null)
            {
                if (enemyAction.enemyActor == enemy) allTurnActions.Add(enemyAction);
                else Debug.LogError($"Inconsistencia en DecideAction para {enemy.enemyData.enemyName}");
            }
        }

        var orderedActions = allTurnActions.OrderByDescending(a => GetActionAgility(a));
        foreach (var action in orderedActions) turnActionQueue.Enqueue(action);

        Debug.Log($"TurnManager: Cola preparada con {turnActionQueue.Count} acciones.");

        if (processActionsCoroutine != null) StopCoroutine(processActionsCoroutine);
        processActionsCoroutine = StartCoroutine(ProcessActionQueue());
    }

    /// <summary>
    /// Corutina principal que procesa la cola de acciones una por una.
    /// </summary>
    private IEnumerator ProcessActionQueue()
    {
        Debug.Log("--- Iniciando Procesamiento de Cola de Acciones ---");
        yield return null;

        while (turnActionQueue.Count > 0)
        {
            // Re-evaluar listas de vivos al inicio de cada acción por si alguien murió por DoT
            currentPartyMembers = BattleFlowController.Instance?.GetParty()?.Where(p => p != null && p.currentHP > 0).ToList() ?? currentPartyMembers;
            currentEnemyInstances = BattleFlowController.Instance?.GetEnemies()?.Where(e => e != null && e.IsAlive).ToList() ?? currentEnemyInstances;

            if (CheckBattleEnded()) { Debug.Log("ProcessActionQueue: Fin de batalla detectado."); FinalizeTurnExecution(); yield break; }

            BattleAction currentAction = turnActionQueue.Dequeue();
            object actor = currentAction?.actor;

            if (actor == null) { Debug.LogWarning("ProcessActionQueue: Acción/Actor nulo."); continue; }

            string actorName = GetActorName(actor);
            bool isActorAlive = IsActorAlive(actor); // Comprobar si está vivo *ahora*
            List<StatusEffectInstance> actorStatuses = GetActorStatuses(actor);

            // Debug.Log($"--- Procesando Acción ({turnActionQueue.Count} restantes): {currentAction.command} por {actorName} ---");

            if (!isActorAlive) { Debug.Log($"{actorName} está KO al inicio de su acción. Acción saltada."); continue; }

            if (actor is CharacterStats playerActor) playerActor.isDefendingThisTurn = false;

            StatusAilment blockingAilment = CheckBlockingStatus(actorStatuses);
            if (blockingAilment != StatusAilment.Poison)
            {
                Debug.Log($"{actorName} no puede actuar debido a {blockingAilment}.");
                yield return new WaitForSeconds(statusInactionDelay);
                if (IsActorAlive(actor)) ApplyEndOfActionEffects(actor);
                OnActionExecuted?.Invoke();
                yield return new WaitForSeconds(postActionDelay);
                continue;
            }

            bool actedConfused = false;
            if (actorStatuses != null && actorStatuses.Any(s => s.type == StatusAilment.Confuse && !s.IsExpired))
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    Debug.Log($"{actorName} está confundido y actúa erráticamente!");
                    yield return StartCoroutine(ExecuteConfusionAttack(actor));
                    actedConfused = true;
                }
                else { Debug.Log($"{actorName} está confundido pero actúa normalmente."); }
            }

            if (!actedConfused)
            {
                 yield return StartCoroutine(ExecuteSingleAction(currentAction));
            }

            if (IsActorAlive(actor)) ApplyEndOfActionEffects(actor);

            OnActionExecuted?.Invoke();
            yield return new WaitForSeconds(postActionDelay);

        } // Fin del while

        Debug.Log("--- Cola de Acciones Vacía ---");
        FinalizeTurnExecution();
    }

    /// <summary>
    /// Corutina que ejecuta una única acción de batalla.
    /// </summary>
    private IEnumerator ExecuteSingleAction(BattleAction action)
    {
        object actor = action.actor;
        string actorName = GetActorName(actor);
        // Debug.Log($"ExecuteSingleAction: Iniciando {action.command} por {actorName}");

        Transform actorVisualTransform = GetActorVisualTransform(actor);
        AllyWorldAnchor playerAnchor = (actor is CharacterStats) ? actorVisualTransform?.GetComponent<AllyWorldAnchor>() : null;
        EnemyWorldAnchor enemyAnchor = (actor is EnemyInstance) ? actorVisualTransform?.GetComponent<EnemyWorldAnchor>() : null;

        if (actor is CharacterStats character)
        {
            // ACCIONES DE JUGADOR
            switch (action.command)
            {
                case BattleCommand.Attack:
                    yield return StartCoroutine(PerformPlayerAttack(action, playerAnchor));
                    break;
                case BattleCommand.Defend:
                    character.isDefendingThisTurn = true;
                    Debug.Log($"{actorName} se defiende.");
                    yield return new WaitForSeconds(0.5f);
                    break;
                case BattleCommand.Special:
                    yield return StartCoroutine(PerformPlayerAbility(action, playerAnchor));
                    break;
                case BattleCommand.Item:
                    yield return StartCoroutine(PerformPlayerItemUse(action));
                    break;
                case BattleCommand.Flee:
                    Debug.Log($"{actorName} intenta huir.");
                    yield return new WaitForSeconds(1.0f);
                    break;
                default:
                    Debug.LogWarning($"Comando de jugador no manejado: {action.command}");
                    yield return new WaitForSeconds(0.2f);
                    break;
            }
        }
        else if (actor is EnemyInstance enemy)
        {
            // ACCIONES DE ENEMIGO
            switch (action.command)
            {
                case BattleCommand.Attack:
                    yield return StartCoroutine(PerformEnemyAttack(action, enemy));
                    break;
                case BattleCommand.Special:
                    yield return StartCoroutine(PerformEnemyAbility(action, enemy, enemyAnchor));
                    break;
                default:
                    Debug.LogWarning($"Comando de enemigo no manejado: {action.command}");
                    yield return new WaitForSeconds(0.2f);
                    break;
            }
        }

        // Esperar retorno de cámara (excepto para Defend/Flee)
        if(action.command != BattleCommand.Defend && action.command != BattleCommand.Flee)
        {
            yield return StartCoroutine(WaitForCameraReturn($"Post-{action.command} ({actorName})"));
        }
        // Debug.Log($"ExecuteSingleAction: Finalizado {action.command} por {actorName}");
    }

    // --- Sub-Corutinas de Ejecución de Acciones Específicas ---

    private IEnumerator PerformPlayerAttack(BattleAction action, AllyWorldAnchor anchor)
    {
        CharacterStats character = action.character;
        object initialTarget = action.target;

        if (anchor == null) {
             Debug.LogError($"PerformPlayerAttack: No Anchor for {character.characterName}. Logic only.");
             action.target = RedirectTargetIfNeeded(initialTarget, character.characterName);
             if (action.target != null) PerformPlayerAttackLogic(action);
             yield break;
        }

        object currentTarget = RedirectTargetIfNeeded(initialTarget, character.characterName);
        if (currentTarget == null) {
            Debug.Log($"{character.characterName} Attack: No valid targets remain after redirection.");
            StartCoroutine(anchor.ReturnToOriginalPositionAndWait(null));
            yield break;
        }
        action.target = currentTarget;

        Transform targetVisualTransform = GetTargetVisualTransform(action.target);
        if (targetVisualTransform == null) {
             Debug.LogError($"PerformPlayerAttack: No visual transform for final target {GetTargetName(action.target)}. Executing logic only.");
             PerformPlayerAttackLogic(action);
             StartCoroutine(anchor.ReturnToOriginalPositionAndWait(null));
             yield break;
        }

        System.Action onHit = () => PerformPlayerAttackLogic(action);
        yield return StartCoroutine(anchor.PerformAttack(targetVisualTransform, onHit, () => {}));
    }

    private void PerformPlayerAttackLogic(BattleAction action)
    {
        CharacterStats character = action.character;
        object target = action.target;

        if (target is EnemyInstance enemyTarget && enemyTarget.IsAlive)
        {
            int defense = enemyTarget.Defense;
            int damage = Mathf.Max(1, character.strength - defense);

            // --- LLAMAR A TakeDamage (NORMAL) ---
            enemyTarget.TakeDamage(damage, ElementalAffinity.Neutral); // <--- PASAR Neutral
                                                                       // ----------------------------------
        }
        else if (target is CharacterStats allyTarget && allyTarget.currentHP > 0)
        { // Atacar a aliado
            int defense = allyTarget.defense;
            if (allyTarget.isDefendingThisTurn) { defense = (int)(defense * 1.5f); }
            int damage = Mathf.Max(1, character.strength - defense);

            // --- LLAMAR A TakeDamage (NORMAL) ---
            allyTarget.TakeDamage(damage, ElementalAffinity.Neutral, false); // <--- PASAR Neutral, false
                                                                             // ----------------------------------
        }
        // else { /* Target inválido */ }
    }

    private IEnumerator PerformPlayerAbility(BattleAction action, AllyWorldAnchor anchor)
    {
        CharacterStats character = action.character;
        AbilityData ability = action.ability;
        object initialTarget = action.target;

        // Comprobación de MP
        if (character.currentMP < ability.mpCost)
        {
            Debug.Log($"{character.characterName} lacks MP for {ability.abilityName}.");
            yield break;
        }

        // Gasto de MP (Lo movemos aquí, antes de la lógica visual/yields)
        character.currentMP -= ability.mpCost;
        // Debug.Log($"{character.characterName} uses {ability.abilityName} (Cost: {ability.mpCost} MP)");

        // --- SONIDO MOVIDO --- (Ya no se reproduce aquí)
        // PlaySoundAtSource(ability.executionSound, anchor.GetComponent<AudioSource>() ?? sfxAudioSource);

        // Acción Lógica Centralizada (Ahora también reproduce el sonido)
        System.Action executeLogicAction = () => {
            try
            {
                // --- SONIDO Y VFX JUNTOS ---
                PlaySoundAtSource(ability.executionSound, anchor?.GetComponent<AudioSource>() ?? sfxAudioSource); // Reproducir sonido aquí
                InstantiateTargetVFX(ability.targetVFXPrefab, ability.targetVFXDuration, action.target); // Instanciar VFX
                                                                                                         // --------------------------

                AbilityExecutor.Execute(character, ability, action.target); // Aplicar lógica

            }
            catch (System.Exception ex) { Debug.LogError($"EXCEPTION during Ability Execution/VFX/Sound for {ability.abilityName}: {ex.Message}\n{ex.StackTrace}"); }
        };

        // Fallback sin Anchor (También necesita reproducir sonido dentro de executeLogicAction si quieres consistencia)
        if (anchor == null)
        {
            Debug.LogError($"PerformPlayerAbility: No Anchor for {character.characterName}. Logic only.");
            // PlaySound(ability?.executionSound); // Sonido movido a executeLogicAction
            if (ability.targetType == AbilityTargetType.Enemy || ability.targetType == AbilityTargetType.Ally || ability.targetType == AbilityTargetType.Any)
            {
                action.target = RedirectTargetIfNeeded(initialTarget, character.characterName);
                if (action.target == null) { yield break; }
            }
            executeLogicAction(); // Esto ahora incluye sonido y VFX
            yield return new WaitForSeconds(Mathf.Max(0.5f, ability?.targetVFXDuration ?? 1.5f));
            yield break;
        }


        // Redirección (Solo para single target)
        object currentTarget = initialTarget;
        if (ability.targetType == AbilityTargetType.Enemy || ability.targetType == AbilityTargetType.Ally || ability.targetType == AbilityTargetType.Any)
        {
            currentTarget = RedirectTargetIfNeeded(initialTarget, character.characterName);
            if (currentTarget == null)
            {
                Debug.Log($"{character.characterName} used {ability.abilityName} but no valid targets remain after redirection.");
                StartCoroutine(anchor.ReturnToOriginalPositionAndWait(null));
                yield break;
            }
            action.target = currentTarget; // Actualizar target en la acción
        }

        // Manejo Visual (Sin cambios aquí, sigue pasando executeLogicAction)
        if (ability.requiresTravel)
        {
            Transform targetVisualTransform = GetTargetVisualTransform(action.target);
            if (targetVisualTransform == null)
            {
                Debug.LogError($"PerformPlayerAbility (Travel): No visual transform for final target {GetTargetName(action.target)}.");
                executeLogicAction(); // Lógica sin visuales (incluye sonido/VFX ahora)
                StartCoroutine(anchor.ReturnToOriginalPositionAndWait(null));
                yield break;
            }
            yield return StartCoroutine(anchor.PerformAttack(targetVisualTransform, executeLogicAction, () => { })); // executeLogicAction se invoca en TriggerDamageEffect
        }
        else
        {
            // Cast
            Transform focusTargetTransform = GetPrimaryTargetTransform(action.target);
            Coroutine cameraReturnCoroutine = null;
            if (focusTargetTransform != null && CombatCameraController.Instance != null)
            {
                CombatCameraController.Instance.FocusOn(focusTargetTransform);
                cameraReturnCoroutine = StartCoroutine(ReturnCameraAfterDelay(noTravelAbilityCameraFocusTime, $"Cast ({ability.abilityName})"));
            }
            else if (CombatCameraController.Instance != null)
            {
                CombatCameraController.Instance.FocusOn(anchor.transform);
                cameraReturnCoroutine = StartCoroutine(ReturnCameraAfterDelay(noTravelAbilityCameraFocusTime, $"Cast Fallback ({ability.abilityName})"));
            }

            float castTime = 1.0f; // Placeholder
            yield return new WaitForSeconds(castTime);
            executeLogicAction(); // Lógica, Sonido y VFX aquí para Cast
            yield return new WaitForSeconds(Mathf.Max(0.2f, ability.targetVFXDuration * 0.5f));
            if (cameraReturnCoroutine != null) yield return cameraReturnCoroutine;
            CombatCameraController.Instance?.ReturnToDefault();
        }
    }

    private IEnumerator PerformPlayerItemUse(BattleAction action)
    {
        CharacterStats character = action.character;
        ConsumableItem item = action.item;
        object initialTarget = action.target;

        if (item == null) { Debug.LogWarning("PerformPlayerItemUse: Item is null."); yield break; }

        object currentTarget = initialTarget;
         if (item.targetType == ItemTargetType.Ally || item.targetType == ItemTargetType.Any) {
            currentTarget = RedirectTargetIfNeeded(initialTarget, character.characterName);
             if(currentTarget == null) {
                 Debug.Log($"{character.characterName} tried to use {item.itemName} but no valid targets remain.");
                 yield break;
             }
             action.target = currentTarget;
         }

        // Debug.Log($"{character.characterName} uses {item.itemName} on {GetTargetName(action.target)}.");
        PlaySound(item?.useSound);
        yield return new WaitForSeconds(0.5f);

        try { item.Use(character, action.target); }
        catch (System.Exception ex) { Debug.LogError($"EXCEPTION during Item.Use for {item.itemName}: {ex.Message}\n{ex.StackTrace}");}

        yield return new WaitForSeconds(0.8f);
    }

    private IEnumerator PerformEnemyAttack(BattleAction action, EnemyInstance enemy)
    {
        object initialTarget = action.target;
        object currentTarget = RedirectTargetIfNeeded(initialTarget, enemy.enemyData?.enemyName ?? "Enemy");

        if (currentTarget == null)
        {
            Debug.Log($"{enemy.enemyData?.enemyName ?? "Enemy"} tried to attack but no valid targets remain.");
            yield break;
        }
        action.target = currentTarget;

        // --- SECCIÓN MODIFICADA ---
        // Obtener el Anchor y llamar al parpadeo ANTES de la espera
        EnemyWorldAnchor enemyAnchorVisual = enemy.worldTransform?.GetComponent<EnemyWorldAnchor>();
        // Log para verificar el anchor antes de llamar al flash
        Debug.Log($"[PerformEnemyAttack] Check Anchor: enemyAnchorVisual is {(enemyAnchorVisual == null ? "NULL" : "VALID")}");
        enemyAnchorVisual?.FlashForAttack(); // Llama al parpadeo si el anchor no es nulo
                                             // --------------------------

        if (action.target is CharacterStats playerTarget && playerTarget.currentHP > 0)
        {
            //Debug.Log($"{enemy.enemyData.enemyName} attacks {playerTarget.characterName}.");
            yield return new WaitForSeconds(0.8f); // Simular tiempo de ataque (el parpadeo ocurre antes)

            int defense = playerTarget.defense;
            if (playerTarget.isDefendingThisTurn) { defense = (int)(defense * 1.5f); }
            int damage = Mathf.Max(1, enemy.Attack - defense);
            playerTarget.TakeDamage(damage); // TakeDamage muestra efectos

            InstantiateEnemyHitEffect(playerTarget.targetAnchor); // Efecto visual/sonoro genérico
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            // Debug.LogWarning($"PerformEnemyAttack: Target '{GetTargetName(action.target)}' invalid/dead for {enemy.enemyData?.enemyName}.");
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator PerformEnemyAbility(BattleAction action, EnemyInstance enemy, EnemyWorldAnchor enemyAnchor) // enemyAnchor ya viene como parámetro
    {
        MonsterAbilityData ability = action.monsterAbility;
        object initialTarget = action.target;

        if (ability == null) { Debug.LogWarning($"PerformEnemyAbility: Ability data is null for {enemy.enemyData?.enemyName}"); yield break; }
        if (enemy.currentMP < ability.mpCost) { Debug.Log($"{enemy.enemyData?.enemyName} lacks MP for {ability.abilityName}"); yield break; }

        object currentTarget = initialTarget;
        if (ability.targetType == AbilityTargetType.Ally || ability.targetType == AbilityTargetType.Enemy || ability.targetType == AbilityTargetType.Any)
        {
            currentTarget = RedirectTargetIfNeeded(initialTarget, enemy.enemyData?.enemyName ?? "Enemy");
            if (currentTarget == null) { Debug.Log($"{enemy.enemyData?.enemyName} has no valid targets for {ability.abilityName}"); yield break; }
            action.target = currentTarget; // Actualizar
        }

        enemy.currentMP -= ability.mpCost;

        // --- SECCIÓN MODIFICADA ---
        // Log para verificar el anchor antes de llamar al flash
        Debug.Log($"[PerformEnemyAbility] Check Anchor: enemyAnchor parameter is {(enemyAnchor == null ? "NULL" : "VALID")}");
        // Llamar al parpadeo usando el anchor pasado como parámetro ANTES de la espera
        enemyAnchor?.FlashForAttack();
        // --------------------------

        // Debug.Log($"{enemy.enemyData.enemyName} uses {ability.abilityName} on {GetTargetName(action.target)}.");

        yield return new WaitForSeconds(0.5f); // Simular casteo (reducido de 1.2 a 0.5 para que sea más rápido que el ataque)

        try
        {
            // --- SONIDO Y VFX JUNTOS ---
            PlaySoundAtSource(ability.executionSound, enemyAnchor?.GetComponent<AudioSource>() ?? sfxAudioSource);
            InstantiateTargetVFX(ability.targetVFXPrefab, ability.targetVFXDuration, action.target);
            // --------------------------

            MonsterAbilityExecutor.Execute(enemy, ability, action.target); // Lógica
        }
        catch (System.Exception ex) { Debug.LogError($"EXCEPTION during MonsterAbilityExecutor.Execute or VFX/Sound for {ability.abilityName}:\n{ex.Message}\n{ex.StackTrace}"); }

        yield return new WaitForSeconds(Mathf.Max(0.5f, ability.targetVFXDuration));
    }

    private IEnumerator ExecuteConfusionAttack(object confusedActor)
    {
        List<object> possibleTargets = new List<object>();
        possibleTargets.AddRange(currentPartyMembers.Where(p => p != null && p.currentHP > 0));
        possibleTargets.AddRange(currentEnemyInstances.Where(e => e != null && e.IsAlive));

        if (possibleTargets.Count == 0) { /* Log y salir */ yield break; }

        object randomTarget = possibleTargets[UnityEngine.Random.Range(0, possibleTargets.Count)];
        int damage = 0;
        string attackerName = GetActorName(confusedActor);
        string targetName = GetActorName(randomTarget);

        if (confusedActor is CharacterStats pc) damage = Mathf.Max(1, pc.strength / 2);
        else if (confusedActor is EnemyInstance en) damage = Mathf.Max(1, en.Attack / 2);

        Debug.Log($"{attackerName} (confused) attacks {targetName}!");
        yield return new WaitForSeconds(0.6f); // Simular ataque

        Transform hitAnchor = GetTargetVisualTransform(randomTarget);
        if (randomTarget is CharacterStats targetPc) targetPc.TakeDamage(damage);
        else if (randomTarget is EnemyInstance targetEn) targetEn.TakeDamage(damage);

        if(hitAnchor != null) InstantiateEnemyHitEffect(hitAnchor); // Efecto golpe genérico

         yield return new WaitForSeconds(0.4f);
    }

    // --- Funciones de Ayuda ---

    private void ApplyEndOfActionEffects(object actor) {
        if (actor == null) return;
        ApplyDamageOverTime(actor);
        TickStatusEffects(actor);
    }

    private void ApplyDamageOverTime(object actor) {
        List<StatusEffectInstance> statuses = GetActorStatuses(actor);
        if (statuses == null) return;

        var poison = statuses.FirstOrDefault(s => s.type == StatusAilment.Poison && !s.IsExpired);
        if (poison != null) {
            int baseStat = (actor is CharacterStats pc) ? pc.maxHP : ((actor is EnemyInstance en) ? en.enemyData.maxHP : 10);
            int poisonDamage = Mathf.Max(1, Mathf.CeilToInt(baseStat * 0.05f));
            Transform anchor = GetTargetVisualTransform(actor);
            int actualDamage = 0;

            if (actor is CharacterStats pcTarget) {
                int hpBefore = pcTarget.currentHP;
                pcTarget.currentHP = Mathf.Max(0, pcTarget.currentHP - poisonDamage);
                actualDamage = hpBefore - pcTarget.currentHP;
            } else if (actor is EnemyInstance enTarget) {
                int hpBefore = enTarget.currentHP;
                enTarget.currentHP = Mathf.Max(0, enTarget.currentHP - poisonDamage);
                actualDamage = hpBefore - enTarget.currentHP;
                 if (hpBefore > 0 && enTarget.currentHP <= 0 && enTarget.worldTransform != null) {
                      enTarget.worldTransform.GetComponent<EnemyWorldAnchor>()?.StartDisintegrationEffect();
                 }
            }
            if (actualDamage > 0 && anchor != null)
            {
                // Llamada CORREGIDA con la nueva firma (int, Transform, ElementalAffinity, bool)
                DamageEffectsManager.Instance?.ShowDamage(actualDamage, anchor, ElementalAffinity.Neutral, false); // <--- LÍNEA CORREGIDA
            }
        }
        // Añadir otros DoTs
    }

    private void TickStatusEffects(object actor) {
        if (actor is CharacterStats pc) pc.TickStatusEffects();
        else if (actor is EnemyInstance en) en.TickStatusEffects();
    }

    private StatusAilment CheckBlockingStatus(List<StatusEffectInstance> statuses) {
        if (statuses == null) return StatusAilment.Poison; // "None"
        if (statuses.Any(s => s.type == StatusAilment.Petrify && !s.IsExpired)) return StatusAilment.Petrify;
        if (statuses.Any(s => s.type == StatusAilment.Sleep && !s.IsExpired)) return StatusAilment.Sleep;
        if (statuses.Any(s => s.type == StatusAilment.Paralyze && !s.IsExpired)) return StatusAilment.Paralyze;
        return StatusAilment.Poison; // None blocking
    }

    private object RedirectTargetIfNeeded(object currentTarget, string attackerName) {
        bool targetIsInvalid = false;
        string originalTargetName = GetTargetName(currentTarget);

        if (currentTarget == null) targetIsInvalid = true;
        else if (currentTarget is EnemyInstance ei) targetIsInvalid = !ei.IsAlive;
        else if (currentTarget is CharacterStats cs) targetIsInvalid = cs.currentHP <= 0;
        else if (currentTarget is List<EnemyInstance> || currentTarget is List<CharacterStats>) targetIsInvalid = false; // Listas no se invalidan aquí
        else targetIsInvalid = true;

        if (targetIsInvalid) {
            //Debug.Log($"Target '{originalTargetName}' for {attackerName} is invalid/defeated. Redirecting...");
            List<EnemyInstance> aliveEnemies = currentEnemyInstances?.Where(e => e != null && e.IsAlive && e != currentTarget).ToList(); // Excluir target original
            if (aliveEnemies != null && aliveEnemies.Any()) {
                EnemyInstance newTarget = aliveEnemies[UnityEngine.Random.Range(0, aliveEnemies.Count)];
                // Debug.Log($"--> Redirecting to enemy: {newTarget.enemyData.enemyName}.");
                return newTarget;
            }

            CharacterStats attackerPC = (attackerName == GetActorName(currentTarget) ? currentTarget as CharacterStats : null);
            List<CharacterStats> aliveAllies = currentPartyMembers?.Where(p => p != null && p.currentHP > 0 && p != currentTarget && p != attackerPC).ToList(); // Excluir target y atacante
            if (aliveAllies != null && aliveAllies.Any()) {
                CharacterStats newTarget = aliveAllies[UnityEngine.Random.Range(0, aliveAllies.Count)];
                // Debug.Log($"--> Redirecting to ally: {newTarget.characterName}.");
                return newTarget;
            }

            Debug.LogWarning("--> No valid targets remain for redirection.");
            return null;
        }
        return currentTarget; // Target original es válido
    }

    private bool CheckBattleEnded() {
        bool partyStillAlive = currentPartyMembers?.Any(p => p != null && p.currentHP > 0) ?? false;
        bool enemiesStillAlive = currentEnemyInstances?.Any(e => e != null && e.IsAlive) ?? false;
        bool partyDefeated = !partyStillAlive;
        bool enemiesDefeated = !enemiesStillAlive;

        if (partyDefeated || enemiesDefeated) {
             // Debug.Log($"CheckBattleEnded: PartyDefeated={partyDefeated}, EnemiesDefeated={enemiesDefeated}");
            List<CharacterStats> originalParty = BattleFlowController.Instance?.GetParty()?.ToList() ?? new List<CharacterStats>();
            List<EnemyInstance> originalEnemies = BattleFlowController.Instance?.GetEnemies()?.ToList() ?? new List<EnemyInstance>();
            // Asegurar que CombatEndManager exista antes de llamarlo
            if (CombatEndManager.Instance != null) {
                 CombatEndManager.Instance.CheckForEndOfBattle(originalParty, originalEnemies);
            } else {
                Debug.LogError("CombatEndManager.Instance es null! No se puede finalizar la batalla.");
            }
            return true;
        }
        return false;
    }

    private void FinalizeTurnExecution() {
        // Debug.Log("<<<< TurnManager: FinalizeTurnExecution >>>>");
        isExecutingTurn = false;
        processActionsCoroutine = null;
        turnActionQueue.Clear();
        try {
            OnTurnExecutionComplete?.Invoke();
        } catch (System.Exception ex) {
             Debug.LogError($"Error al invocar OnTurnExecutionComplete: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private IEnumerator WaitForCameraReturn(string context) {
        if (CombatCameraController.Instance == null || CombatCameraController.Instance.IsNearDefaultPosition()) yield break;
        float timer = 0f;
        while (!CombatCameraController.Instance.IsNearDefaultPosition() && timer < cameraReturnTimeout) {
            timer += Time.deltaTime;
            yield return null;
        }
        if (timer >= cameraReturnTimeout) {
             Debug.LogWarning($"WaitForCameraReturn ({context}): TIMEOUT!");
             CombatCameraController.Instance.ReturnToDefault();
        }
    }

    private IEnumerator ReturnCameraAfterDelay(float delay, string context) {
        if (delay > 0) yield return new WaitForSeconds(delay);
        // Debug.Log($"ReturnCameraAfterDelay ({context}): Iniciando retorno de cámara.");
        CombatCameraController.Instance?.ReturnToDefault();
    }

    // --- Helpers para Sonido y Efectos ---

    private void PlaySoundAtSource(AudioClip clip, AudioSource source) {
        if (clip != null && source != null && source.isActiveAndEnabled) {
            source.PlayOneShot(clip);
        }
    }
    private void PlaySound(AudioClip clip) { PlaySoundAtSource(clip, sfxAudioSource); }
    private void InstantiateEnemyHitEffect(Transform targetAnchor) {
         if (targetAnchor == null) return;
         if (commonEnemyHitVFX != null) {
             GameObject vfx = Instantiate(commonEnemyHitVFX, targetAnchor.position, Quaternion.identity);
             if (vfx != null) Destroy(vfx, commonEnemyHitVFXDuration);
         }
         PlaySoundAtSource(commonEnemyHitSFX, sfxAudioSource);
     }
    private void InstantiateTargetVFX(GameObject vfxPrefab, float duration, object target)
    {
        // --- Log 1 ---
        Debug.Log($"[InstantiateTargetVFX] Llamado para target: {GetTargetName(target)} Prefab: {(vfxPrefab == null ? "NULL" : vfxPrefab.name)}");

        if (vfxPrefab == null)
        {
            Debug.LogWarning("[InstantiateTargetVFX] vfxPrefab es NULL, no se puede instanciar."); // --- Log 2 ---
            return;
        }
        List<Transform> anchors = GetTargetVisualTransforms(target);

        // --- Log 3 ---
        Debug.Log($"[InstantiateTargetVFX] Encontrados {anchors.Count} anchors para el target.");

        foreach (Transform anchor in anchors)
        {
            if (anchor != null)
            {
                // --- Log 4 ---
                Debug.Log($"[InstantiateTargetVFX] Instanciando {vfxPrefab.name} en anchor: {anchor.name} (Pos: {anchor.position})");
                try
                {
                    GameObject vfxInstance = Instantiate(vfxPrefab, anchor.position, anchor.rotation);
                    if (vfxInstance == null)
                    {
                        Debug.LogError("[InstantiateTargetVFX] ¡Instantiate devolvió NULL!"); // --- Log 5 ---
                    }
                    else if (duration > 0)
                    {
                        Destroy(vfxInstance, duration);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[InstantiateTargetVFX] EXCEPCIÓN durante Instantiate: {ex.Message}\n{ex.StackTrace}"); // --- Log 6 ---
                }
            }
            else
            {
                Debug.LogWarning("[InstantiateTargetVFX] Se encontró un anchor NULL en la lista."); // --- Log 7 ---
            }
        }
    }

    #region Funciones de Ayuda (Acceso a Datos y Transforms)
    private int GetActionAgility(BattleAction action) { return action?.actor switch { CharacterStats pc => pc.agility, EnemyInstance en => en.Agility, _ => -1 }; }
        private string GetActorName(object actor) { return actor switch { CharacterStats pc => pc?.characterName ?? "Ally?", EnemyInstance en => en?.enemyData?.enemyName ?? "Enemy?", _ => "Unknown Actor" }; }
        private string GetTargetName(object target) { return target switch { null => "Null Target", CharacterStats pc => pc?.characterName ?? "Ally?", EnemyInstance en => en?.enemyData?.enemyName ?? "Enemy?", List<CharacterStats> csl => $"All Allies ({csl.Count(p=>p!=null && p.currentHP>0)} alive)", List<EnemyInstance> eil => $"All Enemies ({eil.Count(e=>e!=null && e.IsAlive)} alive)", _ => target.GetType().Name }; }
        private bool IsActorAlive(object actor) { return actor switch { CharacterStats pc => pc.currentHP > 0, EnemyInstance en => en.IsAlive, _ => false }; }
        private List<StatusEffectInstance> GetActorStatuses(object actor) { return actor switch { CharacterStats pc => pc.activeStatusEffects, EnemyInstance en => en.activeStatusEffects, _ => null }; }
        private Transform GetActorVisualTransform(object actor) { return actor switch { CharacterStats pc => BattleFlowController.Instance?.GetVisualAnchorForCharacter(pc)?.transform, EnemyInstance en => en.worldTransform, _ => null }; }
        private Transform GetTargetVisualTransform(object target) { return target switch { CharacterStats pcTarget when pcTarget.currentHP > 0 => pcTarget.targetAnchor, EnemyInstance enTarget when enTarget.IsAlive => enTarget.targetAnchor ?? enTarget.worldTransform, _ => null }; }
        private List<Transform> GetTargetVisualTransforms(object target) {
            List<Transform> transforms = new List<Transform>();
             switch(target) {
                 case CharacterStats pcTarget when pcTarget.currentHP > 0: if(pcTarget.targetAnchor != null) transforms.Add(pcTarget.targetAnchor); break;
                 case EnemyInstance enTarget when enTarget.IsAlive: Transform anchorE = enTarget.targetAnchor ?? enTarget.worldTransform; if(anchorE != null) transforms.Add(anchorE); break;
                 case List<CharacterStats> pcList: foreach(var pc in pcList) if(pc != null && pc.currentHP > 0 && pc.targetAnchor != null) transforms.Add(pc.targetAnchor); break;
                 case List<EnemyInstance> enList: foreach(var en in enList) if(en != null && en.IsAlive) { Transform anchorL = en.targetAnchor ?? en.worldTransform; if(anchorL != null) transforms.Add(anchorL); } break;
             }
             return transforms;
         }
        private Transform GetPrimaryTargetTransform(object target) {
             switch(target) {
                 case CharacterStats pcTarget when pcTarget.currentHP > 0: return pcTarget.targetAnchor;
                 case EnemyInstance enTarget when enTarget.IsAlive: return enTarget.targetAnchor ?? enTarget.worldTransform;
                 case List<CharacterStats> pcList: return pcList.FirstOrDefault(p => p != null && p.currentHP > 0)?.targetAnchor;
                 case List<EnemyInstance> enList:
                      // Encuentra el primer enemigo vivo, selecciona su anchor (o worldTransform), y devuelve el primero no nulo.
                      return enList.Where(e => e != null && e.IsAlive)
                                   .Select(e => e.targetAnchor ?? e.worldTransform) // Selecciona el transform a usar
                                   .FirstOrDefault(t => t != null); // Devuelve el primer transform válido encontrado
                 default: return null;
             }
         }
     #endregion

} // Fin de la clase TurnManager