using UnityEngine;
using System.Text;

public abstract class ItemBase : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;

    public abstract string GetTooltipDescription();

    protected string FormatBonusStats(StatBonus bonus)
    {
        if (bonus == null) return "";

        StringBuilder sb = new StringBuilder();

        if (bonus.strength != 0) sb.Append($" | STR+{bonus.strength}");
        if (bonus.defense != 0) sb.Append($" | DEF+{bonus.defense}");
        if (bonus.intelligence != 0) sb.Append($" | INT+{bonus.intelligence}");
        if (bonus.agility != 0) sb.Append($" | AGI+{bonus.agility}");
        if (bonus.maxHP != 0) sb.Append($" | HP+{bonus.maxHP}");
        if (bonus.maxMP != 0) sb.Append($" | MP+{bonus.maxMP}");

        return sb.ToString();
    }
}
