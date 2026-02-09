using System;

/// <summary>
/// Universal inline effect reference used across skills, potions, relics, QTEs, and rest.
/// All fields are optional — only the relevant ones are populated per effect type.
/// Matches the flat JSON format: { "effectId": "eff_deal_damage", "target": "SelectedEnemy", "multiplier": 1.2 }
/// </summary>
[Serializable]
public class EffectEntry
{
    public string effectId;
    public string target;               // "Self", "SelectedEnemy", "AllEnemies"

    // eff_deal_damage
    public float multiplier;            // damage multiplier (e.g., 1.25 = 125% of base)
    public bool ignoreArmor;            // true = bypass resistance
    public bool useElemental;           // true = use random element for resistance calc (potions)
    public int value;                   // flat value (heal amount, shield flat, stat bonus amount, flat damage)

    // eff_energy_delta
    public int amount;                  // energy gain (positive) or cost (negative)

    // eff_apply_status
    public string status;               // status id (e.g., "status_stun")
    public int duration;                // turns the status lasts
    public float magnitude;             // strength of the status (e.g., 50 = 50% block)

    // eff_dot
    public int damagePercent;           // % of damage dealt applied as DoT per tick

    // eff_shield_gain
    public int percentOfDamage;         // shield = % of damage dealt
    public int capPercent;              // cap as % of max HP
    public int percentOfMaxHealth;      // shield = % of max health (used by defensive QTE, rest heal)

    // eff_lifesteal
    public int percent;                 // % of damage dealt healed

    // eff_temp_crit_bonus
    public int critChance;              // temporary crit chance bonus
    public int critDamage;              // temporary crit damage bonus

    // eff_multi_hit
    public int hitCount;                // number of hits

    // eff_on_kill_bonus
    public float bonusDamageMultiplier; // bonus damage to next target on kill
    public int cooldownOverride;        // override cooldown on kill
    public int energyRefund;            // energy refund % on kill

    // eff_stat_bonus (relics, potions)
    public string stat;                 // "critChance", "critDamage", "maxHealth", "damage", "elementalDamage"
    public string element;              // for elementalDamage: "Fire", "Ice", etc., or "All"

    // eff_damage_multiplier (offensive QTE)
    // uses 'multiplier' field above
}

/// <summary>
/// Chain settings for skills like DirtyStab that can be used multiple times in a row.
/// </summary>
[Serializable]
public class ChainSettings
{
    public int maxChainUses;
    public float stackBonusPerUse;
    public int maxStacks;
    public int forceCooldownAfterMaxChain;
}
