using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Collections.Generic;

public class PedestalSaveTrigger : MonoBehaviour, ISaveSlotHandler
{
    public Transform player;
    public float interactionDistance = 2f;
    public SaveSlotPanelUI slotPanel;

    private bool isInRange => Vector3.Distance(player.position, transform.position) <= interactionDistance;

    void Update()
    {
        if (isInRange && Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(ShowSaveSlots());
        }
    }

    IEnumerator ShowSaveSlots()
    {
        string url = "https://rpgapi-dgtn.onrender.com/game/save-states";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Instance.GetToken());

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch save states: " + request.error);
            yield break;
        }

        string json = request.downloadHandler.text;
        SaveStateDTOWrapperList wrapper = JsonUtility.FromJson<SaveStateDTOWrapperList>("{\"states\":" + json + "}");

        slotPanel.Open(wrapper.states, SaveSlotMode.Save, this);
    }

    public void OnSlotSelected(int slot)
    {
        SaveToSlot(slot);
        slotPanel.Close();
    }

    public void SaveToSlot(int slot)
    {
        var saveData = SaveStateBuilder.CreateSaveState(player.position);
        string json = JsonUtility.ToJson(saveData);
        Debug.Log("Sending save state JSON: " + json);

        string url = $"https://rpgapi-dgtn.onrender.com/game/save-states/{slot}";
        UnityWebRequest request = new UnityWebRequest(url, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Instance.GetToken());

        request.SendWebRequest().completed += _ =>
        {
            if (request.result == UnityWebRequest.Result.Success)
                Debug.Log("Game saved successfully in slot " + slot + "!");
            else
                Debug.LogError("Failed to save: " + request.error);
        };
    }
}
