using System.Collections.Generic;
using UnityEngine;
using CombatSystem;
using System;
using System.Linq;

[RequireComponent(typeof(Animator), typeof(AudioSource), typeof(SpriteRenderer))]
public class AllyWorldAnchor : MonoBehaviour
{
    public CharacterStats owner;
    private Animator animator;
    private AudioSource audioSource;
    private SpriteRenderer mainSpriteRenderer;

    [Header("Visual Components")]
    public GameObject cursor;

    private Vector3 originalPosition;
    private Transform pendingDamageTarget;
    private System.Action pendingOnHitAction;

    private Coroutine stepMovementCoroutine;
    [SerializeField] private Transform stepForwardTargetTransform;
    [SerializeField] private float stepMoveSpeed = 10f;

    [Header("Cursor Bobbing")]
    [SerializeField] private float cursorBobDistance = 0.05f;
    [SerializeField] private float cursorBobSpeed = 3f;
    private Coroutine cursorBobCoroutine;
    private Transform cursorTransform;
    private Vector3 cursorOriginalLocalPos;

    [Header("Combat Effects")]
    [SerializeField] private GameObject allyHitVFXPrefab;
    [SerializeField] private float hitVFXDuration = 1.5f;

    [Header("Travel VFX (Instantiated Prefab)")]
    private ParticleSystem currentTravelVFXInstance = null;

    private CharacterClassData _classData;
    private AnimatorOverrideController _overrideController;

    private static readonly int IsDefeatedAnimParam = Animator.StringToHash("IsDefeated");
    private static readonly int TakeDamageAnimParam = Animator.StringToHash("TakeDamage");
    private static readonly int IdleAnimParam = Animator.StringToHash("Idle");
    private static readonly int TravelAnimParam = Animator.StringToHash("Travel");
    private static readonly int AttackAnimParam = Animator.StringToHash("Attack");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        mainSpriteRenderer = GetComponent<SpriteRenderer>();

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;

        if (cursor == null) cursor = transform.Find("Cursor")?.gameObject;
        if (cursor != null)
        {
            cursorTransform = cursor.transform;
            cursorOriginalLocalPos = cursorTransform.localPosition;
            cursor.SetActive(false);
        }

        if (mainSpriteRenderer == null) Debug.LogError("AllyWorldAnchor requiere un SpriteRenderer!", this);
    }

    public void Initialize(CharacterStats character)
    {
        if (character == null) { Debug.LogError("Initialize: character null", this); return; }
        owner = character;

        if (owner.targetAnchor == null)
        {
            Transform specificAnchor = transform.Find("TargetCursorAnchor");
            owner.targetAnchor = specificAnchor ?? this.transform;
        }

        originalPosition = transform.position;

        if (GameManager.Instance != null)
        {
            _classData = GameManager.Instance.GetClassData(owner.characterJob);
        }
        if (_classData == null)
        {
            Debug.LogError($"AllyWorldAnchor para {owner.characterName}: No se pudo cargar ClassData para el job {owner.characterJob}.", this);
            enabled = false;
            return;
        }

        ConfigureAnimator(_classData);
        InitializeTravelVFX(_classData);

        SetCursorVisible(false);
        StopStepMovement();
        UpdateDefeatedStatusVisuals();
    }

    private void InitializeTravelVFX(CharacterClassData classDataRef)
    {
        if (currentTravelVFXInstance != null)
        {
            Destroy(currentTravelVFXInstance.gameObject);
            currentTravelVFXInstance = null;
        }

        if (classDataRef?.travelVFXPrefab != null)
        {
            try
            {
                GameObject vfxGO = Instantiate(classDataRef.travelVFXPrefab, transform.position, transform.rotation, transform);
                vfxGO.name = $"{owner.characterName}_TravelVFX_Instance";
                currentTravelVFXInstance = vfxGO.GetComponent<ParticleSystem>();

                if (currentTravelVFXInstance != null)
                {
                    currentTravelVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                else
                {
                    Destroy(vfxGO);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error instanciando Travel VFX para {owner.characterName}: {ex.Message}");
            }
        }
    }

    private void ConfigureAnimator(CharacterClassData classDataRef)
    {
        if (animator == null || classDataRef == null) return;

        RuntimeAnimatorController baseControllerToUse = animator.runtimeAnimatorController;
        if (Resources.Load<RuntimeAnimatorController>("BaseAllyAnimator") != null)
        {
            baseControllerToUse = Resources.Load<RuntimeAnimatorController>("BaseAllyAnimator");
        }

        if (baseControllerToUse == null) { Debug.LogError("No se encontró BaseAllyAnimator en Resources ni uno asignado por defecto."); return; }

        _overrideController = new AnimatorOverrideController(baseControllerToUse);
        animator.runtimeAnimatorController = _overrideController;

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        AnimationClip[] originalClips = _overrideController.animationClips;

        foreach (var originalClip in originalClips)
        {
            if (originalClip == null) continue;
            AnimationClip overrideClipToSet = null;

            if (originalClip.name.Contains("Idle") && classDataRef.idleAnimation != null) overrideClipToSet = classDataRef.idleAnimation;
            else if (originalClip.name.Contains("Travel") && classDataRef.travelAnimation != null) overrideClipToSet = classDataRef.travelAnimation;
            else if (originalClip.name.Contains("Attack") && classDataRef.attackAnimation != null) overrideClipToSet = classDataRef.attackAnimation;
            else if (originalClip.name.Contains("DamageTaken") && classDataRef.receivedDamageAnimation != null) overrideClipToSet = classDataRef.receivedDamageAnimation;
            else if (originalClip.name.Contains("Defeated") && classDataRef.deadAnimation != null) overrideClipToSet = classDataRef.deadAnimation;

            if (overrideClipToSet != null)
            {
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, overrideClipToSet));
            }
        }
        _overrideController.ApplyOverrides(overrides);
        SetAnimatorTrigger(IdleAnimParam);
    }

    public void UpdateDefeatedStatusVisuals()
    {
        if (owner == null || animator == null || _overrideController == null) return;
        bool isDefeated = owner.currentHP <= 0;
        animator.SetBool(IsDefeatedAnimParam, isDefeated);
    }

    public void PlayDamageTakenAnimation()
    {
        if (owner == null || animator == null || owner.currentHP <= 0 || _overrideController == null) return;
        SetAnimatorTrigger(TakeDamageAnimParam);
    }

    public void SetCursorVisible(bool visible)
    {
        if (cursor == null || cursorTransform == null) return;
        if (visible)
        {
            cursor.SetActive(true);
            if (cursorBobCoroutine == null) { cursorBobCoroutine = StartCoroutine(CursorBobCoroutine()); }
        }
        else
        {
            if (cursorBobCoroutine != null) { StopCoroutine(cursorBobCoroutine); cursorBobCoroutine = null; }
            if (cursorTransform != null) cursorTransform.localPosition = cursorOriginalLocalPos;
            if (cursor != null) cursor.SetActive(false);
        }
    }

    private System.Collections.IEnumerator CursorBobCoroutine()
    {
        if (cursorTransform == null) yield break;
        Vector3 startLocalPos = cursorOriginalLocalPos;
        while (true)
        {
            if (cursorTransform != null)
            {
                float yOffset = Mathf.Sin(Time.time * cursorBobSpeed) * cursorBobDistance;
                cursorTransform.localPosition = startLocalPos + new Vector3(0, yOffset, 0);
            }
            else { yield break; }
            yield return null;
        }
    }

    public void StepForwardForTurn(System.Action onComplete = null)
    {
        if (stepForwardTargetTransform != null)
        {
            StartMovement(stepForwardTargetTransform.position, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    public void ReturnToOriginalPosition(System.Action onComplete = null)
    {
        StartMovement(originalPosition, onComplete);
    }

    private void StartMovement(Vector3 targetPosition, System.Action onComplete)
    {
        if (!this.enabled || !gameObject.activeInHierarchy)
        {
            transform.position = targetPosition;
            onComplete?.Invoke();
            return;
        }
        if (stepMovementCoroutine != null)
        {
            StopCoroutine(stepMovementCoroutine);
        }
        stepMovementCoroutine = StartCoroutine(MoveToPositionCoroutine(targetPosition, onComplete));
    }

    private System.Collections.IEnumerator MoveToPositionCoroutine(Vector3 targetPosition, System.Action onComplete)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, stepMoveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
        stepMovementCoroutine = null;
        onComplete?.Invoke();
    }

    public void StopStepMovement()
    {
        if (stepMovementCoroutine != null)
        {
            StopCoroutine(stepMovementCoroutine);
            stepMovementCoroutine = null;
        }
    }

    public System.Collections.IEnumerator PerformAttack(Transform targetTransform, System.Action onHit, System.Action onComplete)
    {
        if (owner == null || owner.currentHP <= 0) { onComplete?.Invoke(); yield break; }
        if (targetTransform == null) { onComplete?.Invoke(); yield break; }
        if (_classData == null) { onComplete?.Invoke(); yield break; }

        if (Vector3.Distance(transform.position, originalPosition) > 0.01f)
        {
            yield return StartCoroutine(MoveToPositionCoroutine(originalPosition, null));
        }

        float speed = 20f;
        Vector3 attackPosition = Vector3.Lerp(originalPosition, targetTransform.position, 0.8f);

        CombatCameraController.Instance?.FocusOn(transform);
        currentTravelVFXInstance?.Play();
        SetAnimatorTrigger(TravelAnimParam);

        while (Vector3.Distance(transform.position, attackPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, attackPosition, Time.deltaTime * speed);
            yield return null;
        }

        currentTravelVFXInstance?.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        SetAnimatorTrigger(AttackAnimParam);
        pendingOnHitAction = onHit;
        pendingDamageTarget = targetTransform;
        if (_classData.attackSFX != null) PlaySound(_classData.attackSFX);

        float waitTime = _classData.attackAnimation != null ? _classData.attackAnimation.length : 0.5f;
        yield return new WaitForSeconds(waitTime);

        SetAnimatorTrigger(TravelAnimParam);
        while (Vector3.Distance(transform.position, originalPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, Time.deltaTime * speed);
            yield return null;
        }
        transform.position = originalPosition;

        SetAnimatorTrigger(IdleAnimParam);
        CombatCameraController.Instance?.ReturnToDefault();
        onComplete?.Invoke();
    }

    private string GetTargetName(GameObject target)
    {
        if (target == null) return "NULL Target GO";
        var ewa = target.GetComponent<EnemyWorldAnchor>();
        if (ewa != null && ewa.owner != null) return ewa.owner.enemyData?.enemyName ?? "Enemy?";
        var awa = target.GetComponent<AllyWorldAnchor>();
        if (awa != null && awa.owner != null) return awa.owner.characterName ?? "Ally?";
        return target.name;
    }

    public void TriggerDamageEffect()
    {
        if (pendingOnHitAction != null)
        {
            try
            {
                pendingOnHitAction.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error ejecutando pendingOnHitAction para {owner?.characterName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        if (allyHitVFXPrefab != null && pendingDamageTarget != null)
        {
            try
            {
                GameObject vfx = Instantiate(allyHitVFXPrefab, pendingDamageTarget.position, Quaternion.identity);
                if (vfx != null) { Destroy(vfx, hitVFXDuration); }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error instanciando allyHitVFXPrefab para {owner?.characterName}: {ex.Message}");
            }
        }
        pendingDamageTarget = null;
        pendingOnHitAction = null;
    }

    public System.Collections.IEnumerator ReturnToOriginalPositionAndWait(Action onComplete)
    {
        bool movementFinished = false;
        ReturnToOriginalPosition(() => movementFinished = true);
        yield return new WaitUntil(() => movementFinished);
        onComplete?.Invoke();
    }

    private void OnDestroy()
    {
        if (stepMovementCoroutine != null) StopCoroutine(stepMovementCoroutine);
        if (cursorBobCoroutine != null) StopCoroutine(cursorBobCoroutine);
        if (currentTravelVFXInstance != null)
        {
            currentTravelVFXInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null && audioSource.isActiveAndEnabled)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void ResetAllAnimatorTriggers()
    {
        if (animator == null || animator.runtimeAnimatorController == null || animator.parameters == null) return;
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(param.nameHash);
            }
        }
    }

    private void SetAnimatorTrigger(int triggerHash)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        ResetAllAnimatorTriggers();
        animator.SetTrigger(triggerHash);
    }

    private void SetAnimatorTrigger(string triggerName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(triggerName)) return;
        ResetAllAnimatorTriggers();
        animator.SetTrigger(triggerName);
    }
}