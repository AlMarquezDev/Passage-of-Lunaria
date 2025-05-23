using UnityEngine;

public abstract class EquipmentItem : ItemBase
{
    public EquipmentSlot slotType;
    public StatBonus bonusStats;
    public CharacterJob[] allowedJobs;

    public abstract int GetPower(); // Puede ser ataque o defensa
}
