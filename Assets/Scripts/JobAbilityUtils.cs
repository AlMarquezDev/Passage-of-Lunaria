public static class JobAbilityUtils
{
    public static AbilityType GetAbilityTypeForJob(CharacterJob job)
    {
        return job switch
        {
            CharacterJob.Warrior => AbilityType.BattleCry,
            CharacterJob.Thief => AbilityType.Trickster,
            CharacterJob.Monk => AbilityType.MartialArts,
            CharacterJob.RedMage => AbilityType.Spellstrike,
            CharacterJob.BlackMage => AbilityType.BlackMagic,
            CharacterJob.WhiteMage => AbilityType.WhiteMagic,
            _ => AbilityType.BattleCry
        };
    }
}
