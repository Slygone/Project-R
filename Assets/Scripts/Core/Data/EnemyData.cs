public class EnemyData
{
    public string Type;
    public string DisplayName;
    public int EnemyID;
    public int Health;
    public int Damage; // Base damage value
    public int BaseResistance;
    public int BonusResistance;
    
    // Skills (skill IDs referencing skills.json)
    public string Skill1Id;
    public string Skill2Id;
    public int Skill2Cooldown;
    public string Skill3Id;
    public int Skill3Cooldown;
    public string Skill4Id;
    
    // Attack pattern (1-indexed skill numbers, repeating cycle for bosses)
    public int[] AttackPattern;
    
    // Spawn control
    public bool SpawnOnly; // true = can only be spawned by other enemies (MadSlime, SadSlime)
    
    // Boss: Split mechanic (SlimeBoss)
    public float SplitThreshold;    // health ratio to trigger split (0.66 = 66%)
    public string[] SplitInto;      // enemy displayNames to spawn on split
    public int SplitHealthPercent;  // % of current health for each spawned enemy
    
    // Boss: Reactive pattern (MirrorBoss)
    public bool ReactivePattern;    // true = uses reactive skill selection instead of fixed pattern
    public int ReactiveOnAttack;    // skill number to use when player attacks
    public int ReactiveOnShield;    // skill number to use when player gains shield
    public int ReactiveOnReaction;  // skill number to use when player triggers reaction
    
    // Boss: Reborn mechanic (FallenChampion)
    public int RebornHealthPercent; // revive at this % of max health (0 = no reborn)
    public float RebornDamageBonus; // damage bonus after reborn (1.0 = +100%)
    public int[] RebornPattern;     // new attack pattern after reborn
    
    // Boss: Spawn requirement (FallenChampion)
    public string SpawnRequirement;     // "defeatBosses" etc.
    public int SpawnRequirementCount;   // number of bosses to defeat first
    
    // Rewards
    public int RewardXP;
    public int RewardGoldMin;
    public int RewardGoldMax;
    public int SigilChance; // 0-100 percent chance to drop sigil
    public int RelicChance; // 0-100 percent chance to drop relic
    
    // World modifiers parsed from JSON data
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
    
    public bool IsElite => Type == "Elite";
    public bool IsBoss => Type == "Boss";
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
