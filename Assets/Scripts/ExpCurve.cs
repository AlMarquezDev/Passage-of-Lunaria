using UnityEngine;

[CreateAssetMenu(fileName = "ExpCurve", menuName = "RPG/Progression/Exp Curve")]
public class ExpCurve : ScriptableObject
{
    [Tooltip("Cantidad de experiencia necesaria para subir de nivel. Índice 0 = Nivel 1")]
    public int[] expToLevel = new int[99];

    [Header("Curva de experiencia")]
    public float baseExp = 20f;
    public float exponent = 1.5f;

    private void OnValidate()
    {
        if (expToLevel == null || expToLevel.Length != 99)
            expToLevel = new int[99];

        for (int i = 0; i < expToLevel.Length; i++)
        {
            int level = i + 1;
            // Updated formula for demo balance [cite: 1567]
            expToLevel[i] = Mathf.RoundToInt(5 + (level * 5));
        }
    }

    public int GetExpRequiredForLevel(int level)
    {
        if (level < 1 || level > expToLevel.Length)
        {
            Debug.LogWarning($"[ExpCurve] Nivel fuera de rango solicitado: {level}");
            return 999999;
        }

        return expToLevel[level - 1];
    }
}