public enum CharacterJob
{
    Warrior,
    Thief,
    Monk,
    RedMage,
    BlackMage,
    WhiteMage
}

public enum WeaponType
{
    Fist,
    Nunchaku,
    Dagger,
    Sword,
    Katana,
    Axe,
    Hammer,
    Staff
}

public enum ArmorType
{
    Shield,
    LightHelmet,
    Hat,
    HeavyHelmet,
    LightArmor,
    Robe,
    HeavyArmor,
    Gloves,
    Ring
}

public enum AbilityType
{
    BattleCry,
    Trickster,
    MartialArts,
    Spellstrike,
    BlackMagic,
    WhiteMagic
}


public enum EquipmentSlot
{
    RightHand,
    LeftHand,
    Head,
    Body,
    Accessory
}

public static class EquipmentUtils
{
    public static EquipmentSlot GetSlotForEquipment(EquipmentItem item)
    {
        if (item is WeaponItem)
            return EquipmentSlot.RightHand;

        if (item is ArmorItem armor)
        {
            return armor.armorType switch
            {
                ArmorType.Shield => EquipmentSlot.LeftHand,
                ArmorType.LightHelmet => EquipmentSlot.Head,
                ArmorType.Hat => EquipmentSlot.Head,
                ArmorType.HeavyHelmet => EquipmentSlot.Head,
                ArmorType.LightArmor => EquipmentSlot.Body,
                ArmorType.Robe => EquipmentSlot.Body,
                ArmorType.HeavyArmor => EquipmentSlot.Body,
                ArmorType.Gloves => EquipmentSlot.Accessory,
                ArmorType.Ring => EquipmentSlot.Accessory,
                _ => EquipmentSlot.Accessory
            };
        }

        return EquipmentSlot.Accessory; // Fallback
    }
}

