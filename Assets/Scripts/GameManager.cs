using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Party Data")]
    public List<CharacterStats> partyMembers = new();

    [Header("Clases disponibles")]
    public CharacterClassData[] allClassData;

    [Header("Curva de experiencia")]
    public ExpCurve expCurve;

    [Header("Flags")]
    public bool isFromSave = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (expCurve == null)
        {
            Debug.LogWarning("GameManager: No se ha asignado la curva de experiencia (ExpCurve).");
        }
    }

    public CharacterClassData GetClassData(CharacterJob job)
    {
        foreach (var data in allClassData)
        {
            if (data.characterJob == job)
                return data;
        }

        Debug.LogWarning($"GameManager: No se encontró clase para el job: {job}");
        return null;
    }

    public void ClearParty()
    {
        partyMembers.Clear();
    }

    public IReadOnlyList<CharacterStats> Party => partyMembers;
}
