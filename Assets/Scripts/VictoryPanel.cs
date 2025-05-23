using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class VictoryPanel : MonoBehaviour
{
    public static VictoryPanel Instance;

    [Header("UI References")]
    public GameObject root;
    public TMP_Text gilText;
    public TMP_Text expText;
    public TMP_Text itemsText;
    public Button continueButton;

    private System.Action onContinueCallback;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (root != null)
            root.SetActive(false);
    }

    public void Show(int gilEarned, int expEarned, List<ItemBase> droppedItems, System.Action onContinue = null)
    {
        if (root != null)
            root.SetActive(true);

        BattleUIFocusManager.Instance?.SetFocus(this);
        onContinueCallback = onContinue;

        gilText.text = $"Gil ganado: {gilEarned}";
        expText.text = $"EXP total: {expEarned}";

        if (droppedItems != null && droppedItems.Count > 0)
        {
            itemsText.text = "Objetos obtenidos:\n";
            foreach (var item in droppedItems)
            {
                itemsText.text += $"- {item.itemName}\n";
            }
        }
        else
        {
            itemsText.text = "No se obtuvo ningún objeto.";
        }

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() =>
        {
            if (root != null)
                root.SetActive(false);

            BattleUIFocusManager.Instance?.ClearFocus(this);

            Debug.Log("Continuar después de la victoria.");
            onContinueCallback?.Invoke();
        });
    }
}
