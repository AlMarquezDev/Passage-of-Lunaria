using System;
using UnityEngine;

[System.Serializable]
public class StatusEffectInstance
{
    public StatusAilment type;
    public int remainingTurns;

    [NonSerialized] public bool receivedHitThisTurn;

    public StatusEffectInstance(StatusAilment type, int duration)
    {
        this.type = type;
        this.remainingTurns = duration;
    }

    public void TickDown()
    {
        if (remainingTurns > 0)
            remainingTurns--;
    }

    public bool IsExpired => remainingTurns <= 0;
}
