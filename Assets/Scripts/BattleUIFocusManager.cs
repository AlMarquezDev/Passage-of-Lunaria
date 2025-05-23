using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BattleUIFocusManager : MonoBehaviour
{
    public static BattleUIFocusManager Instance { get; private set; }

    public readonly HashSet<object> activeFocusSources = new HashSet<object>();

    public bool IsBlocked => activeFocusSources.Count > 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); Debug.Log($"[BattleUIFocusManager] Instancia creada y marcada como DontDestroyOnLoad: {gameObject.name}");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[BattleUIFocusManager] Instancia duplicada de BattleUIFocusManager en '{gameObject.name}'. Destruyendo este duplicado. La activa es '{Instance.gameObject.name}'.");
            Destroy(gameObject);
        }
    }

    public void SetFocus(object source)
    {
        if (source != null)
        {
            activeFocusSources.Add(source);
        }
    }

    public void ClearFocus(object source)
    {
        if (source != null)
        {
            bool removed = activeFocusSources.Remove(source);
            MonoBehaviour mb = source as MonoBehaviour;
            string sourceName = mb != null ? mb.GetType().Name + $"({mb.gameObject.name})" : source.GetType().Name;
            Debug.Log($"<<<< FocusManager: ClearFocus llamado por '{sourceName}'. Eliminado: {removed}. Focos activos ahora: {activeFocusSources.Count} >>>>");
        }
    }

    public bool CanInteract(object requester)
    {
        if (requester == null) return false;
        if (activeFocusSources.Count == 0) return true;
        if (activeFocusSources.Count == 1 && activeFocusSources.Contains(requester)) return true;
        return false;
    }

    public void ClearAll()
    {
        Debug.Log("<<<< FocusManager: ClearAll llamado. >>>>");
        activeFocusSources.Clear();
    }
}