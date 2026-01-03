public class EnemyData
{
    public string Type;
    public string DisplayName;
    public int EnemyID;
    public int Health;
    public int Damage; // Base damage value
    public int BaseResistance;
    public int BonusResistance;
    
    // World modifiers parsed from CSV formulas
    // Health/Damage use multipliers (e.g., 1.7 means Health * 1.7)
    // BaseResistance uses addend (e.g., 10 means BaseResistance + 10)
    public float World2HealthMultiplier = 1f;
    public float World2DamageMultiplier = 1f;
    public int World2BaseResistanceAddend = 0;
    
    public float World3HealthMultiplier = 1f;
    public float World3DamageMultiplier = 1f;
    public int World3BaseResistanceAddend = 0;
    
    public float World4HealthMultiplier = 1f;
    public float World4DamageMultiplier = 1f;
    public int World4BaseResistanceAddend = 0;
    
    public float World5HealthMultiplier = 1f;
    public float World5DamageMultiplier = 1f;
    public int World5BaseResistanceAddend = 0;
    
    public bool IsElite => Type == "Elite Enemy";
    public bool IsBoss => Type == "Boss Enemy";
    public bool IsRegular => Type == "Enemy";
    
    // Get stats for a specific world
    public int GetHealth(int world)
    {
        float multiplier = world switch
        {
            5 => World5HealthMultiplier,
            4 => World4HealthMultiplier,
            3 => World3HealthMultiplier,
            2 => World2HealthMultiplier,
            _ => 1f
        };
        return UnityEngine.Mathf.RoundToInt(Health * multiplier);
    }
    
    public int GetDamage(int world)
    {
        float multiplier = world switch
        {
            5 => World5DamageMultiplier,
            4 => World4DamageMultiplier,
            3 => World3DamageMultiplier,
            2 => World2DamageMultiplier,
            _ => 1f
        };
        return UnityEngine.Mathf.RoundToInt(Damage * multiplier);
    }
    
    public int GetBaseResistance(int world)
    {
        int addend = world switch
        {
            5 => World5BaseResistanceAddend,
            4 => World4BaseResistanceAddend,
            3 => World3BaseResistanceAddend,
            2 => World2BaseResistanceAddend,
            _ => 0
        };
        return BaseResistance + addend;
    }
}
