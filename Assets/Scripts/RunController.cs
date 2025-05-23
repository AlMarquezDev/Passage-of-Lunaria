using UnityEngine;

public class RunController : MonoBehaviour
{
    public float runMultiplier = 2.0f;
    private float defaultMoveSpeed;
    private PlayerController playerController;
    private Animator animator;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();

        if (playerController != null)
        {
            defaultMoveSpeed = playerController.moveSpeed;
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (playerController != null)
            {
                playerController.moveSpeed = defaultMoveSpeed * runMultiplier;
            }
            if (animator != null)
            {
                animator.speed = 3.0f;
            }
        }
        else
        {
            if (playerController != null)
            {
                playerController.moveSpeed = defaultMoveSpeed;
            }
            if (animator != null)
            {
                animator.speed = 1.0f;
            }
        }
    }
}
