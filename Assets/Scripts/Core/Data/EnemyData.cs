public class EnemyData
{
    public string Type;
    public string DisplayName;
    public int EnemyID;
    public int Health;
    public int Damage;
    public int BaseResistance;
    public int BonusResistance;
    
    // World 2 modifiers parsed from CSV formulas
    // Health/Damage use multipliers (e.g., 1.7 means Health * 1.7)
    // BaseResistance uses addend (e.g., 10 means BaseResistance + 10)
    public float World2HealthMultiplier = 1f;
    public float World2DamageMultiplier = 1f;
    public int World2BaseResistanceAddend = 0;
    
    public bool IsElite => Type == "Elite Enemy";
    public bool IsBoss => Type == "Boss Enemy";
    public bool IsRegular => Type == "Enemy";
    
    // Get stats for a specific world
    public int GetHealth(int world) => world >= 2 ? UnityEngine.Mathf.RoundToInt(Health * World2HealthMultiplier) : Health;
    public int GetDamage(int world) => world >= 2 ? UnityEngine.Mathf.RoundToInt(Damage * World2DamageMultiplier) : Damage;
    public int GetBaseResistance(int world) => world >= 2 ? BaseResistance + World2BaseResistanceAddend : BaseResistance;
}
