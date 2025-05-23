using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LogoAnimation : MonoBehaviour
{
    [Tooltip("Duration of the logo animation in seconds")]
    public float animationDuration = 2f;

    private Material logoMaterial;

    void Start()
    {
        // Get the Image component from this UI element.
        Image img = GetComponent<Image>();
        if (img != null)
        {
            // Create an instance of the material so we don't modify the shared material.
            logoMaterial = Instantiate(img.material);
            img.material = logoMaterial;

            // Start the animation coroutine.
            StartCoroutine(AnimateLogo());
        }
        else
        {
            Debug.LogError("No Image component found on this GameObject.");
        }
    }

    IEnumerator AnimateLogo()
    {
        float elapsedTime = 0f;
        // Animate _Progress from 0 to 1 over animationDuration seconds.
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / animationDuration);
            logoMaterial.SetFloat("_Progress", progress);
            yield return null;
        }
        // Ensure _Progress is set to 1 when finished.
        logoMaterial.SetFloat("_Progress", 1f);
    }
}
