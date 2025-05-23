using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class DialogueBubbleUI : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Text dialogueText;
    public GameObject bubbleRoot;

    [Header("Typewriter Effect Settings")]
    public float typewriterSpeed = 0.05f;

    private Coroutine currentTypewriterCoroutine;
    private string fullTextToDisplay;
    private Action onTypewriterCompleteCallback;
    private bool _isTyping = false;

    void Awake()
    {
        if (bubbleRoot == null && transform.childCount > 0)
        {
            bubbleRoot = transform.GetChild(0).gameObject;
        }
        else if (bubbleRoot == null)
        {
            Debug.LogError($"DialogueBubbleUI en {gameObject.name}: 'bubbleRoot' no asignado.", this);
        }

        if (dialogueText == null && bubbleRoot != null)
        {
            dialogueText = bubbleRoot.GetComponentInChildren<TMP_Text>();
            if (dialogueText == null) Debug.LogError($"DialogueBubbleUI en {gameObject.name}: No se encontró 'dialogueText' (TMP_Text) como hijo de bubbleRoot.", this);
        }
        else if (dialogueText == null)
        {
            Debug.LogError($"DialogueBubbleUI en {gameObject.name}: 'dialogueText' (TMP_Text) no asignado.", this);
        }
        HideBubble();
    }

    public void ShowBubble()
    {
        if (bubbleRoot != null) bubbleRoot.SetActive(true);
    }

    public void HideBubble()
    {
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
        StopTypewriter();
    }

    public bool IsTyping()
    {
        return _isTyping;
    }

    public void StartTypewriterEffect(string text, Action onComplete = null)
    {
        if (dialogueText == null)
        {
            Debug.LogError("DialogueBubbleUI: dialogueText no está asignado. No se puede iniciar el efecto máquina de escribir.");
            onComplete?.Invoke();
            return;
        }
        StopTypewriter();
        fullTextToDisplay = text;
        onTypewriterCompleteCallback = onComplete;
        dialogueText.text = "";
        _isTyping = true;
        currentTypewriterCoroutine = StartCoroutine(TypewriterCoroutine());
    }

    private IEnumerator TypewriterCoroutine()
    {
        foreach (char letter in fullTextToDisplay)
        {
            dialogueText.text += letter;
            if (typewriterSpeed > 0)
            {
                yield return new WaitForSeconds(typewriterSpeed);
            }
            else
            {
                yield return null;
            }
        }
        FinishTyping();
    }

    public void SkipTypewriter()
    {
        if (_isTyping)
        {
            StopTypewriter();
            if (dialogueText != null) dialogueText.text = fullTextToDisplay;
            FinishTyping(true);
        }
    }

    private void StopTypewriter()
    {
        if (currentTypewriterCoroutine != null)
        {
            StopCoroutine(currentTypewriterCoroutine);
            currentTypewriterCoroutine = null;
        }
        _isTyping = false;
    }

    private void FinishTyping(bool skipped = false)
    {
        _isTyping = false;
        currentTypewriterCoroutine = null;
        onTypewriterCompleteCallback?.Invoke();
    }
}