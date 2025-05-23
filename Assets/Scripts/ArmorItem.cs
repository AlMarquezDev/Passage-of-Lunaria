using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "NewArmor", menuName = "RPG/Items/Armor")]
public class ArmorItem : EquipmentItem
{
    public ArmorType armorType;
    public int defensePower;

    public override int GetPower() => defensePower;

    public override string GetTooltipDescription()
    {
        var sb = new StringBuilder();
        sb.AppendLine(description);
        sb.AppendLine();
        sb.Append($"DEF: {defensePower}");
        sb.Append(FormatBonusStats(bonusStats));
        return sb.ToString();
    }
}
