using UnityEngine;

public static class StatFormulaUtility
{
    private const int MaxLevel = 99;
    private const int MaxHP = 9999;
    private const int MaxMP = 9999;

    public static int CalculateMaxHP(int baseHP, int defense, int level, int maxCap = MaxHP)
    {
        return Mathf.Min(baseHP + (level * 5), maxCap);
    }

    public static int CalculateMaxMP(int baseMP, int intelligence, int level, int maxCap = MaxMP)
    {
        return Mathf.Min(baseMP + (level * 3), maxCap);
    }

    public static int GetExpForLevel(int level)
    {
        if (GameManager.Instance?.expCurve == null)
        {
            Debug.LogWarning("GameManager o su curva de experiencia no están inicializados. Devolviendo EXP por defecto.");
            return 999999;
        }

        return GameManager.Instance.expCurve.GetExpRequiredForLevel(Mathf.Clamp(level, 1, MaxLevel));
    }
}
