using UnityEngine;

[System.Serializable]
public class StatScaling
{
    public bool useSTR;
    [Range(0f, 1f)] public float strMultiplier;

    public bool useINT;
    [Range(0f, 1f)] public float intMultiplier;

    public bool useAGI;
    [Range(0f, 1f)] public float agiMultiplier;

    public int FlatBonusDamage;

    public int CalculateDamage(CharacterStats source)
    {
        float damage = 0;
        if (useSTR) damage += source.strength * strMultiplier;
        if (useINT) damage += source.intelligence * intMultiplier;
        if (useAGI) damage += source.agility * agiMultiplier;
        damage += FlatBonusDamage;
        return Mathf.RoundToInt(damage);
    }

    public int CalculateDamage(EnemyInstance enemy)
    {
        float damage = 0;
        if (useSTR) damage += enemy.Attack * strMultiplier;
        if (useINT) damage += 0; // puedes usar otro campo si añades INT al enemigo
        if (useAGI) damage += enemy.Agility * agiMultiplier;
        damage += FlatBonusDamage;
        return Mathf.RoundToInt(damage);
    }
}
