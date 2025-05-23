using UnityEngine;
using System.Linq;

public class PartyPanelUI : MonoBehaviour
{
    public static PartyPanelUI Instance;

    public GameObject characterRowPrefab;
    public RectTransform rowParent;
    public CharacterClassData[] classDatabase;

    private void Awake()
    {
        Instance = this;
    }

    public void GenerateRows()
    {
        if (rowParent == null)
        {
            Debug.LogError("PartyPanelUI: rowParent no está asignado.");
            return;
        }

        if (classDatabase == null || classDatabase.Length == 0)
        {
            Debug.LogError("PartyPanelUI: classDatabase no está asignado o está vacío.");
            return;
        }

        foreach (Transform child in rowParent)
            Destroy(child.gameObject);

        float startY = 75f;
        float spacing = 260f;

        for (int i = 0; i < GameManager.Instance.partyMembers.Count; i++)
        {
            var stats = GameManager.Instance.partyMembers[i];
            var classData = classDatabase.FirstOrDefault(c => c.characterJob == stats.characterJob);
            if (classData == null)
            {
                Debug.LogWarning($"Clase no encontrada para {stats.characterJob}");
                continue;
            }

            GameObject row = Instantiate(characterRowPrefab, rowParent);
            row.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, startY - spacing * i);
            row.GetComponent<CharacterRowUI>()?.SetData(stats, classData);
        }
    }


}
