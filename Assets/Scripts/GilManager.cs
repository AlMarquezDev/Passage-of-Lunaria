using UnityEngine;

public class GilManager : MonoBehaviour
{
    public static GilManager Instance { get; private set; }

    [SerializeField] private int currentGil = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persistencia entre escenas si lo necesitas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int GetGil()
    {
        return currentGil;
    }

    public void AddGil(int amount)
    {
        currentGil += amount;
        Debug.Log($"Has recibido {amount} Gil. Total: {currentGil}");
    }

    public bool SpendGil(int amount)
    {
        if (currentGil >= amount)
        {
            currentGil -= amount;
            Debug.Log($"Has gastado {amount} Gil. Restante: {currentGil}");
            return true;
        }

        Debug.Log("No tienes suficiente Gil.");
        return false;
    }

    public void SetGil(int amount)
    {
        currentGil = Mathf.Max(0, amount);
    }
}
