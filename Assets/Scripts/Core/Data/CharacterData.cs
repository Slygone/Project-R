public class CharacterData
{
    public string DisplayName;
    public int CharacterID;
    public int MaxHealth;
    public int Damage; // Base damage value
    public string DamageRangeLabel; // Raw DamageRange for UI display
    public int Gold;
    public int MaxEnergy;
    public float CritChance;
    public float CritDamage;
    public int BaseResistance;
    public int BonusResistance;
    public int MaxActionPoints;
    
    // Skill 1
    public string Skill1;
    public float Skill1DamagePercent; // e.g., 125 for 125%
    public string Skill1Effect;
    public int Skill1Cooldown;
    public int Skill1EnergyGain;
    public int Skill1APCost;
    public string Skill1Element;
    public int Skill1MarkChance;
    public int Skill1MarkCount;
    
    // Skill 2
    public string Skill2;
    public float Skill2DamagePercent;
    public string Skill2Effect;
    public int Skill2Cooldown;
    public int Skill2EnergyGain;
    public int Skill2APCost;
    public string Skill2Element;
    public int Skill2MarkChance;
    public int Skill2MarkCount;
    
    // Skill 3
    public string Skill3;
    public float Skill3DamagePercent;
    public string Skill3Effect;
    public int Skill3Cooldown;
    public int Skill3EnergyGain;
    public int Skill3APCost;
    public string Skill3Element;
    public int Skill3MarkChance;
    public int Skill3MarkCount;
    
    // Skill 4
    public string Skill4;
    public float Skill4DamagePercent;
    public string Skill4Effect;
    public int Skill4Cooldown;
    public int Skill4EnergyGain;
    public int Skill4APCost;
    public string Skill4Element;
    public int Skill4MarkChance;
    public int Skill4MarkCount;
    
    // Skill 5 (Ultimate - uses energy instead of gaining)
    public string Skill5;
    public float Skill5DamagePercent;
    public string Skill5Effect;
    public int Skill5Cooldown;
    public int Skill5EnergyCost;
    public int Skill5APCost;
    public string Skill5Element;
    public int Skill5MarkChance;
    public int Skill5MarkCount;
    
    // Talent Perks (4 tiers, A/B options each) - loaded from JSON
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
