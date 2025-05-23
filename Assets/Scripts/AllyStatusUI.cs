using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CombatSystem;
using System.Linq;
public class AllyStatusUI : MonoBehaviour
{
    [System.Serializable]
    public class AllySlotUI
    {
        public TMP_Text nameText;
        public TMP_Text hpLabelText;
        public TMP_Text hpNumberText;
        public Slider hpSlider;
        public TMP_Text mpLabelText;
        public TMP_Text mpNumberText;
        public Slider mpSlider;
    }

    public List<AllySlotUI> slots = new List<AllySlotUI>(4);
    private List<CharacterStats> characters;
    void Start()
    {
        if (BattleFlowController.Instance != null)
        {
            characters = BattleFlowController.Instance.GetParty()?.ToList() ?? new List<CharacterStats>();
        }
        else if (CombatSessionData.Instance != null)
        {
            characters = CombatSessionData.Instance.partyMembers;
        }
        else
        {
            Debug.LogError("AllyStatusUI: BattleFlowController y CombatSessionData son null en Start.", this);
            characters = new List<CharacterStats>();
        }

        if (slots == null || slots.Count < characters.Count)
        {
            Debug.LogError("AllyStatusUI: No hay suficientes slots asignados en el Inspector para todos los miembros del party.", this);
        }

        for (int i = characters.Count; i < slots.Count; i++)
        {
            if (slots[i]?.nameText?.transform?.parent != null)
            {
                slots[i].nameText.transform.parent.gameObject.SetActive(false);
            }
        }

        UpdateUI();
    }

    private void OnEnable()
    {
        TurnManager.OnActionExecuted += UpdateUI;
    }

    private void OnDisable()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.OnActionExecuted -= UpdateUI;
        }
    }

    public void UpdateUI()
    {
        if (characters == null || BattleFlowController.Instance?.GetParty().Count != characters.Count)
        {
            if (BattleFlowController.Instance != null)
            {
                characters = BattleFlowController.Instance.GetParty()?.ToList() ?? new List<CharacterStats>();
            }
            else
            {
                return;
            }
        }


        for (int i = 0; i < slots.Count; i++)
        {
            AllySlotUI slot = slots[i];
            if (slot == null || slot.nameText == null || slot.hpNumberText == null || slot.mpNumberText == null || slot.hpSlider == null || slot.mpSlider == null)
            {
                continue;
            }


            if (i < characters.Count && characters[i] != null)
            {
                CharacterStats c = characters[i];

                if (slot.nameText?.transform?.parent != null && !slot.nameText.transform.parent.gameObject.activeSelf)
                {
                    slot.nameText.transform.parent.gameObject.SetActive(true);
                }

                slot.nameText.text = c.characterName;
                if (slot.hpLabelText != null) slot.hpLabelText.text = "HP"; slot.hpNumberText.text = $"{c.currentHP} / {c.maxHP}";
                if (slot.mpLabelText != null) slot.mpLabelText.text = "MP"; slot.mpNumberText.text = $"{c.currentMP} / {c.maxMP}";
                slot.hpSlider.maxValue = c.maxHP;
                slot.hpSlider.value = c.currentHP;

                slot.mpSlider.maxValue = Mathf.Max(1, c.maxMP); slot.mpSlider.value = c.currentMP;

                bool isDead = c.currentHP <= 0;
                Color statusColor = isDead ? Color.red : Color.white;
                slot.nameText.color = statusColor;
                if (slot.hpLabelText != null) slot.hpLabelText.color = statusColor;
                slot.hpNumberText.color = statusColor;
                if (slot.mpLabelText != null) slot.mpLabelText.color = statusColor;
                slot.mpNumberText.color = statusColor;

                slot.hpSlider.interactable = !isDead; slot.mpSlider.interactable = !isDead;
            }
            else
            {
                if (slot.nameText?.transform?.parent != null)
                {
                    slot.nameText.transform.parent.gameObject.SetActive(false);
                }
            }
        }
    }
}