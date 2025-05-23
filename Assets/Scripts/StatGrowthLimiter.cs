using UnityEngine;

[System.Serializable]
public class StatGrowthLimiter
{
    [Range(1, 9999)] public int maxHP = 9999;
    [Range(1, 9999)] public int maxMP = 9999;
    [Range(1, 99)] public int maxSTR = 99;
    [Range(1, 99)] public int maxINT = 99;
    [Range(1, 99)] public int maxDEF = 99;
    [Range(1, 99)] public int maxAGI = 99;
}
