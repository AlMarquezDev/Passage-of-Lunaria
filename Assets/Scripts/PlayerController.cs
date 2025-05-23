using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 3f;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private enum IdleDirection { Forward, Left, Back }
    private IdleDirection lastDirection = IdleDirection.Forward;
    private bool lastFacingRight = false;

    [Header("Encounter Settings")]
    [Tooltip("Distancia a recorrer para contar como un 'paso' para encuentros.")]
    [SerializeField] private float distancePerStepForEncounter = 2.0f;
    private float distanceCoveredSinceLastStep = 0f;

    private bool isMovementLocked = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isMovementLocked)
        {
            if (animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash("Idle_" + lastDirection.ToString())))
            {
                PlayIdleAnimation();
            }
            else if (animator != null)
            {
                animator.Play("Idle_Forward");
            }
            return;
        }

        Vector3 positionBeforeMove = transform.position;

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector3 moveInput = new Vector3(moveX, 0, moveY).normalized;

        transform.position += moveInput * moveSpeed * Time.deltaTime;

        float distanceMovedThisFrame = Vector3.Distance(positionBeforeMove, transform.position);

        if (moveInput.magnitude > 0.01f && distanceMovedThisFrame > 0.001f)
        {
            distanceCoveredSinceLastStep += distanceMovedThisFrame;

            if (distanceCoveredSinceLastStep >= distancePerStepForEncounter)
            {
                int stepsTakenNow = Mathf.FloorToInt(distanceCoveredSinceLastStep / distancePerStepForEncounter);
                for (int i = 0; i < stepsTakenNow; i++)
                {
                    if (EncounterManager.Instance != null)
                    {
                        EncounterManager.Instance.RegisterStep();
                    }
                }
                distanceCoveredSinceLastStep -= stepsTakenNow * distancePerStepForEncounter;
            }
        }

        if (animator == null || spriteRenderer == null) return;

        if (moveInput.magnitude > 0)
        {
            if (Mathf.Abs(moveX) > Mathf.Abs(moveY))
            {
                lastDirection = IdleDirection.Left;
                lastFacingRight = moveX > 0;
                animator.Play("Walk_Left");
                spriteRenderer.flipX = lastFacingRight;
            }
            else
            {
                if (moveY > 0)
                {
                    lastDirection = IdleDirection.Back;
                    animator.Play("Walk_Back");
                    spriteRenderer.flipX = false;
                }
                else if (moveY < 0)
                {
                    lastDirection = IdleDirection.Forward;
                    animator.Play("Walk_Forward");
                    spriteRenderer.flipX = false;
                }
            }
        }
        else
        {
            PlayIdleAnimation();
        }
    }

    private void PlayIdleAnimation()
    {
        if (animator == null || spriteRenderer == null) return;

        switch (lastDirection)
        {
            case IdleDirection.Forward:
                animator.Play("Idle_Forward");
                spriteRenderer.flipX = false;
                break;
            case IdleDirection.Left:
                animator.Play("Idle_Left");
                spriteRenderer.flipX = lastFacingRight;
                break;
            case IdleDirection.Back:
                animator.Play("Idle_Back");
                spriteRenderer.flipX = false;
                break;
        }
    }

    public void SetMovementLock(bool locked)
    {
        isMovementLocked = locked;
        Debug.Log($"PlayerController: Movimiento {(locked ? "BLOQUEADO" : "DESBLOQUEADO")}");
        if (locked)
        {
            PlayIdleAnimation();
            Rigidbody rb = GetComponent<Rigidbody>(); if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public bool IsMovementLocked()
    {
        return isMovementLocked;
    }
}