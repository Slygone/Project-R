public class CharacterData
{
    public string DisplayName;
    public int CharacterID;
    public int MaxHealth;
    public int Damage; // Base damage value
    public string DamageRangeLabel; // Raw CSV DamageRange for UI display
    public int Gold;
    public int MaxEnergy;
    public float CritChance;
    public float CritDamage;
    public int BaseResistance;
    public int BonusResistance;
    
    // Skill 1
    public string Skill1;
    public float Skill1DamagePercent; // e.g., 125 for 125%
    public string Skill1Effect;
    public int Skill1Cooldown;
    public int Skill1EnergyGain;
    
    // Skill 2
    public string Skill2;
    public float Skill2DamagePercent;
    public string Skill2Effect;
    public int Skill2Cooldown;
    public int Skill2EnergyGain;
    
    // Skill 3
    public string Skill3;
    public float Skill3DamagePercent;
    public string Skill3Effect;
    public int Skill3Cooldown;
    public int Skill3EnergyCost;
    
    // Talent Perks (4 tiers, A/B options each) - loaded from CSV
    public string Perk1a;
    public string Perk1b;
    public string Perk2a;
    public string Perk2b;
    public string Perk3a;
    public string Perk3b;
    public string Perk4a;
    public string Perk4b;
    
    /// <summary>
    /// Returns the number of talent tiers available (derived from perk columns).
    /// Currently hardcoded to 4, but could be made dynamic if more tiers are added.
    /// </summary>
    public int GetTierCount()
    {
        int count = 0;
        if (!string.IsNullOrEmpty(Perk1a) || !string.IsNullOrEmpty(Perk1b)) count++;
        if (!string.IsNullOrEmpty(Perk2a) || !string.IsNullOrEmpty(Perk2b)) count++;
        if (!string.IsNullOrEmpty(Perk3a) || !string.IsNullOrEmpty(Perk3b)) count++;
        if (!string.IsNullOrEmpty(Perk4a) || !string.IsNullOrEmpty(Perk4b)) count++;
        return count;
    }
    
    /// <summary>
    /// Get perk option string for a specific tier (0-3) and side (A/B).
    /// </summary>
    public string GetPerkOption(int tier, bool isOptionA)
    {
        switch (tier)
        {
            case 0: return isOptionA ? Perk1a : Perk1b;
            case 1: return isOptionA ? Perk2a : Perk2b;
            case 2: return isOptionA ? Perk3a : Perk3b;
            case 3: return isOptionA ? Perk4a : Perk4b;
            default: return "";
        }
    }
}
