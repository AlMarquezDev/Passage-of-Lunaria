using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ClassDatabase : MonoBehaviour
{
    public static ClassDatabase Instance;

    public List<CharacterClassData> allClasses = new();
    private Dictionary<CharacterJob, CharacterClassData> classLookup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadClassesFromResources();
            BuildLookup();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadClassesFromResources()
    {
        CharacterClassData[] loaded = Resources.LoadAll<CharacterClassData>("Classes");
        allClasses = loaded.ToList();
        Debug.Log($"[ClassDatabase] Loaded {allClasses.Count} classes from Resources/Classes");
    }

    private void BuildLookup()
    {
        classLookup = allClasses.ToDictionary(c => c.characterJob);
    }

    public CharacterClassData GetByJob(CharacterJob job)
    {
        return classLookup.TryGetValue(job, out var classData) ? classData : null;
    }
}
