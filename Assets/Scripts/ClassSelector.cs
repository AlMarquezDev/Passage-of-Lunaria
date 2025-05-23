using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ClassSelector : MonoBehaviour
{
    [Header("UI References")]
    public Button leftArrow;
    public Button rightArrow;
    public Image classImage;
    public TMP_Text classNameText;
    public TMP_Text classDescriptionText;
    public TMP_InputField nameInputField;

    [Header("Class Configuration")]
    public ClassData[] classes;

    [Header("Animation Settings")]
    public float transitionDuration = 0.3f;
    public float slideDistance = 200f;

    public System.Action<CharacterClassData> OnClassChanged;

    private int currentIndex = 0;
    private bool isTransitioning = false;

    private void Start()
    {
        leftArrow.onClick.AddListener(PreviousClass);
        rightArrow.onClick.AddListener(NextClass);
        UpdateClassDisplay();
    }

    private void UpdateClassDisplay()
    {
        var data = classes[currentIndex].classData;
        classNameText.text = data.className;
        classDescriptionText.text = data.classDescription;
        classImage.sprite = data.classSprite;

        OnClassChanged?.Invoke(data);
    }

    public void NextClass()
    {
        if (isTransitioning) return;
        int newIndex = (currentIndex + 1) % classes.Length;
        StartCoroutine(TransitionClass(newIndex, 1));
    }

    public void PreviousClass()
    {
        if (isTransitioning) return;
        int newIndex = (currentIndex - 1 + classes.Length) % classes.Length;
        StartCoroutine(TransitionClass(newIndex, -1));
    }

    private IEnumerator TransitionClass(int newIndex, int direction)
    {
        isTransitioning = true;

        var oldImage = classImage;
        var newImage = Instantiate(classImage, classImage.transform.parent);
        newImage.sprite = classes[newIndex].classData.classSprite;
        newImage.rectTransform.anchoredPosition = new Vector2(direction * slideDistance, 0);
        newImage.color = new Color(1, 1, 1, 0);
        newImage.transform.SetAsFirstSibling();

        float elapsed = 0;
        Vector2 oldStart = Vector2.zero;
        Vector2 newStart = newImage.rectTransform.anchoredPosition;

        while (elapsed < transitionDuration)
        {
            float t = elapsed / transitionDuration;
            oldImage.rectTransform.anchoredPosition = Vector2.Lerp(oldStart, -newStart, t);
            oldImage.color = new Color(1, 1, 1, 1 - t);

            newImage.rectTransform.anchoredPosition = Vector2.Lerp(newStart, Vector2.zero, t);
            newImage.color = new Color(1, 1, 1, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(oldImage.gameObject);
        classImage = newImage;
        currentIndex = newIndex;
        UpdateClassDisplay();
        isTransitioning = false;
    }

    // Accesores públicos
    public string GetCharacterName() => nameInputField.text.Trim();
    public int GetSelectedIndex() => currentIndex;
    public CharacterClassData GetSelectedClassData() => classes[currentIndex].classData;
}
