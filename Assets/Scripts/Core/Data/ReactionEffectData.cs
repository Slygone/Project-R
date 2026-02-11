/// <summary>
/// Flat data class for a single reaction effect entry.
/// All possible fields are present; unused fields default to zero/null/false.
/// Unity's JsonUtility requires a single class with all fields.
/// </summary>
[System.Serializable]
public class ReactionEffectEntry
{
    // Common
    public string effectId;         // rxn_deal_damage, rxn_apply_dot, rxn_player_buff, rxn_enemy_debuff, rxn_apply_shield, rxn_reduce_resist
    public string target;           // "Enemy", "AllOtherEnemies", "Player"
    
    // rxn_deal_damage
    public float damageMultiplier;  // 1.0 = damage range, 2.0 = 2x, etc.
    public bool ignoreResist;       // skip resistance calculation
    
    // rxn_apply_dot
    public string dotName;          // "Ignite", "Magma Scorch"
    public float damagePercent;     // % of damage range per tick (0.15 = 15%)
    public float bonusDmgIfExists;  // extra reaction damage % if DoT already present
    public float bonusDotIfExists;  // extra DoT damage % if DoT already present
    
    // rxn_player_buff
    public string buffType;         // "CritDamage", "BonusAP", "ReflectiveArmor", "DamageReduction", "RockDamageWhileShielded"
    
    // rxn_enemy_debuff
    public string debuffType;       // "Weak", "Freeze", "Shatter", "HealOnHit", "Electrocute", "Mudslide"
    
    // rxn_reduce_resist
    public string elements;         // comma-separated: "Fire,Wind" or "All"
    
    // Shared
    public float value;             // generic value (buff %, shield amount, debuff magnitude, resist reduction %)
    public int duration;            // turns
    public int maxStacks;           // for stackable effects
    public bool refreshable;        // whether duration is refreshed on reapply
    public float[] stackValues;     // per-stack damage multipliers (e.g., [0.2, 0.25, 0.3])
}
