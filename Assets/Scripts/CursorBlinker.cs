using UnityEngine;

public class CursorBlinker : MonoBehaviour
{
    public float blinkRate = 0.5f;
    private CanvasGroup canvasGroup;
    private float timer;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= blinkRate)
        {
            canvasGroup.alpha = 1 - canvasGroup.alpha;
            timer = 0f;
        }
    }
}
