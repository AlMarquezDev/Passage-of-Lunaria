using UnityEngine;

[CreateAssetMenu(fileName = "NewStarterKit", menuName = "RPG/Characters/StarterKit")]
public class StarterKit : ScriptableObject
{
    public CharacterJob job;

    [Header("Initial Equipment")]
    public WeaponItem startingWeapon;
    public ArmorItem startingShield;
    public ArmorItem startingHead;
    public ArmorItem startingBody;
    public ArmorItem startingAccessory;

    [Header("Initial Abilities")]
    public AbilityData[] startingAbilities;
}
