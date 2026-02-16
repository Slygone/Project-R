public class EnemyData
{
    public string Type;
    public string DisplayName;
    public int EnemyID;
    public int Health;
    public int Damage; // Base damage value (average of min/max)
    public int DamageMin;
    public int DamageMax;
    public int PhysicalResist;
    public int ElementalResist;
    public string DamageElement; // "none" = physical, "fire"/"ice"/etc = elemental
    
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
    
    // Boss: World pool (which worlds this boss can appear in; empty = any world)
    public int[] Worlds;
    
    // Boss: Spawn requirement (FallenChampion)
    public string SpawnRequirement;     // "defeatBosses" etc.
    public int SpawnRequirementCount;   // number of bosses to defeat first
    
    // Rewards
    public int RewardXP;
    public int RewardGoldMin;
    public int RewardGoldMax;
    public int SigilChance; // 0-100 percent chance to drop sigil
    public int RelicChance; // 0-100 percent chance to drop relic
    public int RegularCoreChance; // 0-100 percent chance to drop Regular Essence Core
    public int AscendedCoreChance; // 0-100 percent chance to drop Ascended Essence Core
    
    // World modifiers parsed from JSON data
    // Health/Damage use multipliers (e.g., 1.7 means Health * 1.7)
    // Resistance uses addend (e.g., 10 means Resist + 10) — applied to BOTH physical and elemental
    public float World2HealthMultiplier = 1f;
    public float World2DamageMultiplier = 1f;
    public int World2ResistanceAddend = 0;
    
    public float World3HealthMultiplier = 1f;
    public float World3DamageMultiplier = 1f;
    public int World3ResistanceAddend = 0;
    
    public float World4HealthMultiplier = 1f;
    public float World4DamageMultiplier = 1f;
    public int World4ResistanceAddend = 0;
    
    public float World5HealthMultiplier = 1f;
    public float World5DamageMultiplier = 1f;
    public int World5ResistanceAddend = 0;
    
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
    
    public int GetResistanceAddend(int world)
    {
        return world switch
        {
            5 => World5ResistanceAddend,
            4 => World4ResistanceAddend,
            3 => World3ResistanceAddend,
            2 => World2ResistanceAddend,
            _ => 0
        };
    }
    
    public int GetPhysicalResist(int world) => PhysicalResist + GetResistanceAddend(world);
    public int GetElementalResist(int world) => ElementalResist + GetResistanceAddend(world);
}
