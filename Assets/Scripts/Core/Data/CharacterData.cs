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
}
