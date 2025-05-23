using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "RPG/Items/Weapon")]
public class WeaponItem : EquipmentItem
{
    public WeaponType weaponType;
    public int attackPower;

#if UNITY_EDITOR
    private void Reset()
    {
        slotType = EquipmentSlot.RightHand;
    }
#endif

    public override int GetPower() => attackPower;

    public override string GetTooltipDescription()
    {
        var sb = new StringBuilder();
        sb.AppendLine(description);
        sb.AppendLine();
        sb.Append($"ATK: {attackPower}");
        sb.Append(FormatBonusStats(bonusStats));
        return sb.ToString();
    }
}
