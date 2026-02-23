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
    public string target;               // "Self", "SelectedEnemy", "AllEnemies", "Player", "AllAllies"

    // eff_deal_damage
    public float multiplier;            // damage multiplier (e.g., 1.25 = 125% of base)
    public bool ignoreArmor;            // true = bypass resistance
    public bool ignoreShield;           // true = bypass shields (Piercing Arrow)
    public float shieldMultiplier;      // bonus multiplier to shield damage (Spell Blade: 1.5)
    public bool useElemental;           // true = use random element for resistance calc (potions)
    public int value;                   // flat value (heal amount, shield flat, stat bonus amount, flat damage)

    // eff_energy_delta
    public int amount;                  // energy gain (positive) or cost (negative)

    // eff_apply_status
    public string status;               // status id (e.g., "status_stun")
    public int duration;                // turns the status lasts
    public float magnitude;             // strength of the status (e.g., 50 = 50% block)
    public int maxStacks;               // max stackable magnitude (Vulnerable: 50)
    public int breakThreshold;          // % of max health to break shield (Frost Shield: 20)
    public float breakDamageMultiplier; // damage multiplier on shield break (Frost Shield: 1.5)

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

    // eff_syphon_shield
    public float damagePerPoint;        // damage per shield point stolen (Syphon Magic: 1.0)

    // eff_mirror_copy
    public string copyType;             // "reaction" for Mirror Reaction

    // eff_combo_attack (uses hitCount and multiplier above)
    public bool perfectNegate;          // true = perfect defensive QTE negates all damage

    // eff_reborn
    public int healthPercent;           // revive at this % of max health
    public float damageBonus;           // bonus damage multiplier after reborn (1.0 = +100%)

    // eff_status_immunity (relics)
    public string immuneStatus;         // status id to become immune to (e.g., "status_sunder")

    // eff_ap_delta (relics — modify AP at trigger time)
    // uses 'amount' field above

    // eff_extra_marks (relics — apply extra elemental marks per skill)
    public int extraMarks;              // number of extra marks to apply

    // eff_missing_hp_damage (relics — bonus damage based on missing HP)
    public int maxBonusPercent;         // maximum bonus damage % (e.g., 30)

    // eff_ap_cost_reduction (relics — reduce AP cost of skills)
    public int apReduction;             // amount to reduce AP cost by
    public int skillCount;              // number of skills affected (0 = all skills this turn)

    // eff_refresh_random_skill (relics — refresh a random skill cooldown)
    public int apSpentThreshold;        // AP spent before triggering (e.g., 20)

    // eff_energy_cost_multiplier (relics — modify ultimate energy cost)
    // uses 'multiplier' field above (e.g., 2.0 = 100% increase)

    // eff_disable_system (relics — disable game systems)
    public string disableTarget;        // "defensiveQTE", "reactionQTE", "sigils", "enemyMarks"
    public int disableDuration;         // turns to disable (0 = permanent)

    // eff_ap_banking (relics — keep unspent AP between turns)
    // no extra fields needed, flag-based

    // eff_post_combat_heal (relics — heal after combat)
    // uses 'percentOfMaxHealth' field above

    // eff_first_mark_bonus (relics — first mark each turn applies extra)
    // uses 'extraMarks' field above

    // eff_combat_shield (relics — gain shield at combat start)
    // uses 'percentOfMaxHealth' field above

    // eff_perfect_qte_ap (relics — perfect QTE grants AP)
    // uses 'amount' field above

    // eff_reaction_cost_reduction (relics — reduce dual reaction mark cost)
    // uses 'value' field above (marks reduced per element)

    // eff_reaction_ap_refund (relics — first reaction refunds AP)
    // uses 'amount' field above

    // eff_end_turn_mark (relics — apply mark at end of turn)
    // uses 'extraMarks' field above

    // eff_mark_transfer (relics — transfer marks on enemy death)
    // uses 'value' field above (number of marks to transfer)

    // eff_perfect_reaction_save_mark (relics — perfect reaction saves marks)
    // uses 'value' field above (number of marks saved)

    // eff_apply_equipped_mark (relics — apply equipped element mark to all enemies)
    // uses 'extraMarks' field above

    // eff_reaction_double (relics — first reaction triggers twice)
    // uses 'multiplier' field above (effect multiplier for second trigger)

    // eff_dual_reaction_shield (relics — dual reaction grants shield)
    // uses 'percentOfMaxHealth' field above

    // eff_reaction_weaken (relics — reaction causes weaken)
    // uses 'duration' field above

    // eff_hide_marks (relics — hide enemy marks UI)
    // uses 'disableTarget' field above

    // eff_reaction_extra_mark_cost (relics — reactions cost extra marks)
    // uses 'value' field above

    // eff_temp_resist (relics — temporary resist bonus for N turns)
    // uses 'value' (resist amount) and 'duration' (turns) above

    // eff_hide_enemy_intentions (relics — hide enemy action previews)
    // no extra fields needed, flag-based

    // eff_ultimate_cooldown_bonus (relics — reduce ultimate cooldown by N turns)
    // uses 'value' field above

    // eff_randomize_ap_cost (relics — randomize skill AP costs periodically)
    public int interval;                // turns between randomizations (e.g., 3)

    // eff_gold_on_kill (relics — gain gold on enemy kill, breaks on gold spend)
    public int goldAmount;              // gold gained per enemy kill (e.g., 6)
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
