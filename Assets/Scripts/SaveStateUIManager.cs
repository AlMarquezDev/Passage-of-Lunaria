using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
public class SaveStateUIManager : MonoBehaviour
{
    public static SaveStateUIManager Instance;

    [System.Serializable]
    public class SlotUI
    {
        public TMP_Text titleText;
        public TMP_Text detailText;
        public Button continueButton;
    }

    public List<SlotUI> slotUIs = new(); // 3 elementos
    public Button loadGameButton; // botón general "Load Game"

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateSaveSlotUI(List<SaveStateDTOWrapper> states)
    {
        int nonEmptyCount = 0;

        foreach (var slotUI in slotUIs)
        {
            slotUI.titleText.text = "Empty slot";
            slotUI.detailText.text = "";
            slotUI.continueButton.interactable = false;
        }

        foreach (var state in states)
        {
            if (state != null && !string.IsNullOrEmpty(state.saveData))
            {
                int index = state.slot - 1;
                SaveStateData data = JsonUtility.FromJson<SaveStateData>(state.saveData);

                string leaderName = data.partyMembers.Count > 0 ? data.partyMembers[0].name : "Unknown";
                int level = data.partyMembers.Count > 0 ? data.partyMembers[0].level : 0;
                string scene = data.sceneName;

                slotUIs[index].titleText.text = $"Slot {state.slot}";
                slotUIs[index].detailText.text = $"?? {leaderName} | ?? Lv.{level} | ?? {scene}";
                slotUIs[index].continueButton.interactable = true;

                nonEmptyCount++;
            }
        }

        loadGameButton.interactable = nonEmptyCount > 0;
    }
}
