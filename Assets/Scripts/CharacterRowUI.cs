using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CharacterRowUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text jobText;
    public TMP_Text levelText;
    public TMP_Text hpText;
    public TMP_Text mpText;
    public Image spriteImage;

    public void SetData(CharacterStats stats, CharacterClassData classData)
    {
        nameText.text = stats.characterName;
        jobText.text = classData.className;
        levelText.text = $"Lvl. {stats.level}";
        hpText.text = $"HP: {stats.currentHP}/{stats.maxHP}";
        mpText.text = $"MP: {stats.currentMP}/{stats.maxMP}";
        spriteImage.sprite = classData.classSprite;
    }
}
