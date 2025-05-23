using System;
using UnityEngine;

[Serializable]
public class StatBonus
{
    public int strength;
    public int defense;
    public int intelligence;
    public int agility;
    public int maxHP;
    public int maxMP;

    public StatBonus() { }

    public StatBonus(int str, int def, int intl, int agi, int hp, int mp)
    {
        strength = str;
        defense = def;
        intelligence = intl;
        agility = agi;
        maxHP = hp;
        maxMP = mp;
    }

                public static StatBonus GetDifference(StatBonus newBonus, StatBonus currentBonus)
    {
        return new StatBonus
        {
            strength = (newBonus?.strength ?? 0) - (currentBonus?.strength ?? 0),
            defense = (newBonus?.defense ?? 0) - (currentBonus?.defense ?? 0),
            intelligence = (newBonus?.intelligence ?? 0) - (currentBonus?.intelligence ?? 0),
            agility = (newBonus?.agility ?? 0) - (currentBonus?.agility ?? 0),
            maxHP = (newBonus?.maxHP ?? 0) - (currentBonus?.maxHP ?? 0),
            maxMP = (newBonus?.maxMP ?? 0) - (currentBonus?.maxMP ?? 0),
        };
    }

                public static StatBonus Add(StatBonus a, StatBonus b)
    {
        return new StatBonus
        {
            strength = (a?.strength ?? 0) + (b?.strength ?? 0),
            defense = (a?.defense ?? 0) + (b?.defense ?? 0),
            intelligence = (a?.intelligence ?? 0) + (b?.intelligence ?? 0),
            agility = (a?.agility ?? 0) + (b?.agility ?? 0),
            maxHP = (a?.maxHP ?? 0) + (b?.maxHP ?? 0),
            maxMP = (a?.maxMP ?? 0) + (b?.maxMP ?? 0),
        };
    }

                public StatBonus Clone()
    {
        return new StatBonus
        {
            strength = strength,
            defense = defense,
            intelligence = intelligence,
            agility = agility,
            maxHP = maxHP,
            maxMP = maxMP
        };
    }

                public bool IsEmpty()
    {
        return strength == 0 && defense == 0 && intelligence == 0 &&
               agility == 0 && maxHP == 0 && maxMP == 0;
    }
}
