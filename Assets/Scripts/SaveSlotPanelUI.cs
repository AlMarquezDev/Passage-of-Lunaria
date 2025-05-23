using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public enum SaveSlotMode { Load, Save }

public interface ISaveSlotHandler
{
    void OnSlotSelected(int slot);
}

public class SaveSlotPanelUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public TMP_Text detailText;
        public Button button;
        public GameObject charactersContainer;
        public List<Image> characterImages;
    }

    [Header("UI References")]
    public List<SlotUI> slotUIs;
    public GameObject root;
    public TMP_Text headerText;

    [Header("Audio Feedback")] // Nueva sección para sonidos
    public AudioSource uiAudioSource; // Asigna un AudioSource para los sonidos de este panel
    public AudioClip confirmSlotSound; // Sonido al confirmar un slot

    private ISaveSlotHandler handler;
    private SaveSlotMode mode;

    private void Awake()
    {
        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource == null)
            {
                Debug.LogWarning("SaveSlotPanelUI: No AudioSource assigned or found on this GameObject. Confirmation sounds will not play.");
            }
        }
    }

    private void Update()
    {
        if (root.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    public void Open(List<SaveStateDTOWrapper> slotsDataFromServer, SaveSlotMode currentMode, ISaveSlotHandler callbackHandler)
    {
        this.mode = currentMode;
        this.handler = callbackHandler;
        if (root != null) root.SetActive(true); else { Debug.LogError("SaveSlotPanelUI: Root GameObject no asignado!"); return; }

        if (headerText != null)
        {
            headerText.text = mode == SaveSlotMode.Load ? "Choose a slot to LOAD" : "Choose a slot to SAVE";
        }

        if (slotUIs == null || slotUIs.Count == 0)
        {
            Debug.LogError("SaveSlotPanelUI: slotUIs list is not assigned or empty in the Inspector!");
            return;
        }

        for (int i = 0; i < slotUIs.Count; i++)
        {
            var slotUI = slotUIs[i];
            if (slotUI == null)
            {
                Debug.LogError($"SaveSlotPanelUI: SlotUI en el índice {i} de la lista slotUIs es nulo.");
                continue;
            }

            if (slotUI.characterImages != null)
            {
                foreach (Image charImg in slotUI.characterImages)
                {
                    if (charImg != null)
                    {
                        charImg.enabled = false;
                        charImg.sprite = null;
                    }
                }
            }
            if (slotUI.charactersContainer != null)
            {
                slotUI.charactersContainer.SetActive(false);
            }

            var dataWrapper = slotsDataFromServer.Find(s => s.slot == i + 1);

            if (dataWrapper != null && !string.IsNullOrEmpty(dataWrapper.saveData))
            {
                SaveStateData parsedSaveData = null;
                try
                {
                    parsedSaveData = JsonUtility.FromJson<SaveStateData>(dataWrapper.saveData);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"SaveSlotPanelUI: Error parseando saveData para slot {i + 1}. JSON: {dataWrapper.saveData}. Error: {e.Message}");
                    if (slotUI.detailText != null) slotUI.detailText.text = "Data Error";
                    if (slotUI.button != null) slotUI.button.interactable = false;
                    continue;
                }

                if (parsedSaveData == null)
                {
                    Debug.LogError($"SaveSlotPanelUI: parsedSaveData es null después de JsonUtility.FromJson para slot {i + 1}.");
                    if (slotUI.detailText != null) slotUI.detailText.text = "Data Error";
                    if (slotUI.button != null) slotUI.button.interactable = false;
                    continue;
                }

                if (slotUI.detailText != null)
                {
                    slotUI.detailText.text = parsedSaveData.sceneName ?? "Unknown Scene";
                }

                if (slotUI.button != null) slotUI.button.interactable = true;

                if (slotUI.characterImages != null && parsedSaveData.partyMembers != null)
                {
                    if (slotUI.charactersContainer != null) slotUI.charactersContainer.SetActive(parsedSaveData.partyMembers.Count > 0);

                    for (int charIdx = 0; charIdx < slotUI.characterImages.Count; charIdx++)
                    {
                        Image currentCharacterImage = slotUI.characterImages[charIdx];
                        if (currentCharacterImage == null) continue;

                        if (charIdx < parsedSaveData.partyMembers.Count && parsedSaveData.partyMembers[charIdx] != null)
                        {
                            SaveCharacterData savedChar = parsedSaveData.partyMembers[charIdx];
                            CharacterJob job = EnumUtility.Parse<CharacterJob>(savedChar.job);

                            CharacterClassData classData = null;
                            if (ClassDatabase.Instance != null) classData = ClassDatabase.Instance.GetByJob(job);
                            else if (GameManager.Instance != null) classData = GameManager.Instance.GetClassData(job);
                            else Debug.LogWarning("SaveSlotPanelUI: Ni ClassDatabase ni GameManager disponibles para ClassData.");

                            if (classData != null && classData.classSprite != null)
                            {
                                currentCharacterImage.sprite = classData.classSprite;
                                currentCharacterImage.enabled = true;
                            }
                            else
                            {
                                currentCharacterImage.enabled = false;
                                if (classData == null) Debug.LogWarning($"SaveSlotPanelUI: No se encontró ClassData para job {job} en slot {i + 1}, char {charIdx + 1}.");
                                else if (classData.classSprite == null) Debug.LogWarning($"SaveSlotPanelUI: ClassSprite es null para job {classData.className} en slot {i + 1}, char {charIdx + 1}.");
                            }
                        }
                        else
                        {
                            currentCharacterImage.enabled = false;
                        }
                    }
                }
            }
            else
            {
                if (slotUI.detailText != null) slotUI.detailText.text = "Empty data";
                if (slotUI.button != null) slotUI.button.interactable = (mode == SaveSlotMode.Save);

                if (slotUI.charactersContainer != null) slotUI.charactersContainer.SetActive(false);
                if (slotUI.characterImages != null)
                {
                    foreach (Image charImg in slotUI.characterImages)
                    {
                        if (charImg != null) charImg.enabled = false;
                    }
                }
            }

            int capturedSlotIndex = i + 1;
            if (slotUI.button != null)
            {
                slotUI.button.onClick.RemoveAllListeners();
                if (slotUI.button.interactable)
                {
                    slotUI.button.onClick.AddListener(() =>
                    {
                        if (uiAudioSource != null && confirmSlotSound != null)
                        {
                            uiAudioSource.PlayOneShot(confirmSlotSound);
                        }
                        handler?.OnSlotSelected(capturedSlotIndex);
                    });
                }
            }
        }
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
    }
}