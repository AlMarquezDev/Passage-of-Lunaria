using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveStateData
{
    public string sceneName;
    public Vector3Serializable playerPosition;
    public List<SaveCharacterData> partyMembers;
    public List<SaveItemEntry> inventory;
}

[Serializable]
public class Vector3Serializable
{
    public float x, y, z;
    public Vector3Serializable(Vector3 v) { x = v.x; y = v.y; z = v.z; }
    public Vector3 ToVector3() => new Vector3(x, y, z);
}

[Serializable]
public class SaveCharacterData
{
    public string name;
    public string job;
    public int level;
    public int currentHP, maxHP;
    public int currentMP, maxMP;
    public int strength, defense, intelligence, agility;
    public int experience;

    public string rightHandID;
    public string leftHandID;
    public string headID;
    public string bodyID;
    public string accessoryID;

    public int baseStrength;
    public int baseDefense;
    public int baseIntelligence;
    public int baseAgility;
    public int baseMaxHP;
    public int baseMaxMP;

    public List<string> knownAbilitiesIDs = new();
}

[Serializable]
public class SaveItemEntry
{
    public string itemID;
    public int quantity;
}