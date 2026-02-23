using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized manager for ALL relic logic. Owns the relic list, tracks all relic state,
/// and provides event methods called by CombatManager at key game moments.
/// Directly modifies Player stats where needed and accepts enemy lists for enemy-side effects.
/// No fallbacks — every relic is data-driven from relics.json.
/// </summary>
public class RelicManager
{
    // ════════════════════════════════════════════════════════════
    //  REFERENCES
    // ════════════════════════════════════════════════════════════
    private Player player;

    // ════════════════════════════════════════════════════════════
    //  RELIC LIST
    // ════════════════════════════════════════════════════════════
    private List<RelicData> relics = new List<RelicData>();
    private List<string> brokenRelicIds = new List<string>(); // Fracture Revival after use

    // ════════════════════════════════════════════════════════════
    //  PERSISTENT STATE (across combats, within a run)
    // ════════════════════════════════════════════════════════════
    private int skillUseCounter;              // Deadeye Counter (persists across combats)
    private int apSpentCounter;               // Cooldown Lottery (persists across combats)
    private bool fractureRevivalUsed;         // Fracture Revival (per-run, once)
    private List<string> statusImmunities = new List<string>();
    private float energyCostMultiplier = 1f;
    private int ultimateCooldownBonus;        // Overcharged Ultimate (+2 turns)
    private bool disableReactionQTE;          // Sigil Renounce
    private bool disableSigils;               // Sigil Renounce

    // Passive relic flags (set on acquire, persist across combats)
    private bool hasAPBanking;                // Ice Cream Core
    private bool hasPerfectQteAp;
    private int perfectQteApAmount;
    private bool hasMarkTransfer;
    private int markTransferCount;
    private bool hasPerfectReactionSaveMark;
    private int perfectReactionSaveMarkCount;
    private bool hasReactionDouble;           // Reactor Core
    private float reactionDoubleMultiplier;
    private bool hasDualReactionShield;       // Dual Specialist
    private int dualReactionShieldPercent;
    private bool hasReactionWeaken;           // Reaction Exhaustion
    private int reactionWeakenDuration;
    private bool hasReactionCostReduction;    // Catalyst Splinter
    private int reactionExtraMarkCost;        // Overconsumption
    private bool hasMissingHpDamage;          // Last Stand Blade
    private int missingHpDamageMaxPercent;
    private bool hideEnemyIntentions;         // Elemental Fog (reworked)
    private int randomizeAPInterval;            // Elemental Fog: turns between AP randomizations
    private bool hasPiggyBank;                  // Piggy Bank: +gold on kill
    private int piggyBankGoldPerKill;

    // ════════════════════════════════════════════════════════════
    //  PER-COMBAT STATE
    // ════════════════════════════════════════════════════════════
    private bool inCombat;
    private List<CombatEnemy> currentEnemies;  // cached for mid-combat relic adds
    private int turnCounter;
    private int bankedAP;                     // Ice Cream Core (cap 50, resets per combat)
    private bool reactionDoubleUsed;          // Reactor Core (first reaction per combat)
    private bool dualReactionShieldUsed;      // Dual Specialist (once per combat)
    private bool reactionCostReductionUsed;   // Catalyst Splinter (first dual per combat)
    private int crackedBatteryTurnsLeft;
    private int crackedBatteryApDelta;
    private int earlyGuardTurnsLeft;          // Early Guard Override temp resist
    private int earlyGuardResistAmount;       // +10% each
    private int disableDefensiveQTETurns;
    private int lastCombatStartHeal;
    private int cooldownLotteryFreeSkill = -1; // skill index 0-3 with free cast, -1 = none
    private int pendingCombatStartAPDelta;     // First Pulse: deferred until after RefreshAP
    private bool deadeyeCritReady;

    // ════════════════════════════════════════════════════════════
    //  PER-TURN STATE
    // ════════════════════════════════════════════════════════════
    private int extraMarksThisTurn;           // Mark Echo
    private int apReductionThisTurn;          // Rhythm Discount
    private int apReductionSkillsRemaining;   // Rhythm Discount (2 skills)
    private bool perfectQteApUsedThisTurn;
    private bool reactionWeakenUsedThisTurn;
    private int siphonApDelta;                // Siphoning Aura per-turn AP loss

    // Elemental Fog state
    private int[] randomizedAPCosts = null;   // null = not active, [4] for skills 1-4

    // UI tracking
    private HashSet<string> relicJustTriggered = new HashSet<string>();

    // ════════════════════════════════════════════════════════════
    //  CONSTRUCTOR & RESET
    // ════════════════════════════════════════════════════════════

    public RelicManager(Player owner)
    {
        player = owner;
    }

    public void ResetForNewRun()
    {
        relics.Clear();
        brokenRelicIds.Clear();

        skillUseCounter = 0;
        apSpentCounter = 0;
        fractureRevivalUsed = false;
        statusImmunities.Clear();
        energyCostMultiplier = 1f;
        ultimateCooldownBonus = 0;
        disableReactionQTE = false;
        disableSigils = false;

        hasAPBanking = false;
        hasPerfectQteAp = false;
        perfectQteApAmount = 0;
        hasMarkTransfer = false;
        markTransferCount = 0;
        hasPerfectReactionSaveMark = false;
        perfectReactionSaveMarkCount = 0;
        hasReactionDouble = false;
        reactionDoubleMultiplier = 0.5f;
        hasDualReactionShield = false;
        dualReactionShieldPercent = 0;
        hasReactionWeaken = false;
        reactionWeakenDuration = 0;
        hasReactionCostReduction = false;
        reactionExtraMarkCost = 0;
        hasMissingHpDamage = false;
        missingHpDamageMaxPercent = 0;
        hideEnemyIntentions = false;
        randomizeAPInterval = 0;
        hasPiggyBank = false;
        piggyBankGoldPerKill = 0;

        ResetCombatState();
        relicJustTriggered.Clear();
    }

    private void ResetCombatState()
    {
        turnCounter = 0;
        bankedAP = 0;
        reactionDoubleUsed = false;
        dualReactionShieldUsed = false;
        reactionCostReductionUsed = false;
        crackedBatteryTurnsLeft = 0;
        crackedBatteryApDelta = 0;
        earlyGuardTurnsLeft = 0;
        earlyGuardResistAmount = 0;
        disableDefensiveQTETurns = 0;
        lastCombatStartHeal = 0;
        cooldownLotteryFreeSkill = -1;
        pendingCombatStartAPDelta = 0;
        deadeyeCritReady = false;
        extraMarksThisTurn = 0;
        apReductionThisTurn = 0;
        apReductionSkillsRemaining = 0;
        perfectQteApUsedThisTurn = false;
        reactionWeakenUsedThisTurn = false;
        siphonApDelta = 0;
        randomizedAPCosts = null;
    }

    // ════════════════════════════════════════════════════════════
    //  ADD / REMOVE RELIC
    // ════════════════════════════════════════════════════════════

    public void AddRelic(RelicData relic)
    {
        relics.Add(relic);
        string trigger = relic.Trigger ?? "onAcquire";

        // Apply immediate stat effects for onAcquire and permanent triggers
        if (trigger == "onAcquire" || trigger == "permanent")
        {
            ApplyRelicStatsImmediate(relic);
        }

        // Reset counters to 0 on acquire — counters start from the moment the relic is picked up
        if (relic.Trigger == "onSkillUse" && relic.TriggerInterval > 0)
        {
            skillUseCounter = 0;
            deadeyeCritReady = false;
        }
        if (relic.Effects != null)
        {
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_refresh_random_skill" && eff.apSpentThreshold > 0)
                    apSpentCounter = 0;
            }
        }

        // Set passive flags from effects
        if (relic.Effects != null)
        {
            foreach (var eff in relic.Effects)
            {
                SetPassiveFlag(eff, true, relic);
            }
        }

        // If we're in combat and this is a combatStart relic, immediately apply its effects
        if (inCombat && trigger == "combatStart")
        {
            ApplyCombatStartEffectsForRelic(relic, currentEnemies);
            Debug.Log($"[RelicManager] Mid-combat add: immediately applied combatStart effects for {relic.DisplayName}");
        }

        // If we're in combat and this is a Siphoning Aura-style relic, update cached siphon delta
        if (inCombat && trigger == "onTurnStart" && relic.Effects != null)
        {
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_ap_delta" && relic.TriggerInterval <= 0)
                {
                    siphonApDelta += eff.amount;
                }
            }
        }

        GameLog.System(GameLog.Join(
            "RelicGain",
            GameLog.KV("relic", relic.DisplayName),
            GameLog.KV("trigger", trigger),
            GameLog.KV("inCombat", inCombat)
        ), GameLogVerbosity.Verbose);
    }

    public void RemoveRelic(RelicData relic)
    {
        relics.Remove(relic);
        string trigger = relic.Trigger ?? "onAcquire";

        if (trigger == "onAcquire" || trigger == "permanent")
        {
            ReverseRelicStatsImmediate(relic);
        }

        if (relic.Effects != null)
        {
            foreach (var eff in relic.Effects)
            {
                SetPassiveFlag(eff, false, relic);
            }
        }

        // If we're in combat and this is a Siphoning Aura-style relic, recalculate cached siphon delta
        if (inCombat && trigger == "onTurnStart" && relic.Effects != null)
        {
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_ap_delta" && relic.TriggerInterval <= 0)
                {
                    siphonApDelta -= eff.amount;
                }
            }
        }

        GameLog.System(GameLog.Join("RelicRemove", GameLog.KV("relic", relic.DisplayName)));
    }

    private void SetPassiveFlag(EffectEntry eff, bool adding, RelicData relic)
    {
        switch (eff.effectId)
        {
            case "eff_status_immunity":
                if (adding && !string.IsNullOrEmpty(eff.immuneStatus) && !statusImmunities.Contains(eff.immuneStatus))
                    statusImmunities.Add(eff.immuneStatus);
                else if (!adding && !string.IsNullOrEmpty(eff.immuneStatus))
                    statusImmunities.Remove(eff.immuneStatus);
                break;

            case "eff_energy_cost_multiplier":
                if (adding && eff.multiplier > 0) energyCostMultiplier *= eff.multiplier;
                else if (!adding && eff.multiplier > 0) energyCostMultiplier /= eff.multiplier;
                break;

            case "eff_ap_banking":
                hasAPBanking = adding;
                break;

            case "eff_perfect_qte_ap":
                hasPerfectQteAp = adding;
                perfectQteApAmount = adding ? eff.amount : 0;
                break;

            case "eff_reaction_cost_reduction":
                hasReactionCostReduction = adding;
                break;

            case "eff_mark_transfer":
                hasMarkTransfer = adding;
                markTransferCount = adding ? eff.value : 0;
                break;

            case "eff_perfect_reaction_save_mark":
                hasPerfectReactionSaveMark = adding;
                perfectReactionSaveMarkCount = adding ? eff.value : 0;
                break;

            case "eff_reaction_double":
                hasReactionDouble = adding;
                reactionDoubleMultiplier = adding ? eff.multiplier : 0.5f;
                break;

            case "eff_dual_reaction_shield":
                hasDualReactionShield = adding;
                dualReactionShieldPercent = adding ? eff.percentOfMaxHealth : 0;
                break;

            case "eff_reaction_weaken":
                hasReactionWeaken = adding;
                reactionWeakenDuration = adding ? eff.duration : 0;
                break;

            case "eff_reaction_extra_mark_cost":
                if (adding) reactionExtraMarkCost += eff.value;
                else reactionExtraMarkCost -= eff.value;
                break;

            case "eff_missing_hp_damage":
                hasMissingHpDamage = adding;
                missingHpDamageMaxPercent = adding ? eff.maxBonusPercent : 0;
                break;

            case "eff_hide_enemy_intentions":
                hideEnemyIntentions = adding;
                break;

            case "eff_randomize_ap_cost":
                randomizeAPInterval = adding ? (eff.interval > 0 ? eff.interval : 3) : 0;
                break;

            case "eff_gold_on_kill":
                hasPiggyBank = adding;
                piggyBankGoldPerKill = adding ? eff.goldAmount : 0;
                break;

            case "eff_ultimate_cooldown_bonus":
                if (adding) ultimateCooldownBonus += eff.value;
                else ultimateCooldownBonus -= eff.value;
                break;

            case "eff_disable_system":
                string dt = (eff.disableTarget ?? "").ToLower();
                if (dt == "reactionqte") disableReactionQTE = adding;
                else if (dt == "sigils")
                {
                    disableSigils = adding;
                    if (adding)
                    {
                        player.ClearSkillEnchantments();
                        player.SetAffinity(Element.None);
                        Debug.Log("[RelicManager] Sigil Renounce: cleared all skill enchantments and affinity");
                    }
                }
                break;

            case "eff_temp_crit_bonus":
                if (!adding) deadeyeCritReady = false;
                break;
        }
    }

    private void ApplyRelicStatsImmediate(RelicData relic)
    {
        if (relic.Effects == null || relic.Effects.Count == 0) return;

        // Effects handled as passive flags — skip them in stat application
        var flagEffects = new HashSet<string> {
            "eff_status_immunity", "eff_energy_cost_multiplier", "eff_ap_banking",
            "eff_perfect_qte_ap", "eff_reaction_cost_reduction",
            "eff_mark_transfer", "eff_perfect_reaction_save_mark",
            "eff_reaction_double", "eff_dual_reaction_shield", "eff_reaction_weaken",
            "eff_reaction_extra_mark_cost", "eff_disable_system",
            "eff_temp_crit_bonus", "eff_missing_hp_damage",
            "eff_hide_enemy_intentions", "eff_ultimate_cooldown_bonus",
            "eff_randomize_ap_cost", "eff_gold_on_kill"
        };

        var standardEffects = new List<EffectEntry>();

        foreach (var eff in relic.Effects)
        {
            if (flagEffects.Contains(eff.effectId)) continue;

            if (eff.effectId == "eff_stat_bonus")
            {
                string stat = (eff.stat ?? "").ToLower();
                if (stat == "maxhealthpercent")
                {
                    int bonus = Mathf.RoundToInt(player.GetMaxHealth() * (eff.value / 100f));
                    player.AddMaxHealth(bonus);
                }
                else if (stat == "damagemax")
                {
                    player.AddBaseDamage(eff.value);
                }
                else if (stat == "physicalresist")
                {
                    player.AddPhysicalResist(eff.value);
                }
                else if (stat == "elementalresist")
                {
                    player.AddElementalResist(eff.value);
                }
                else
                {
                    standardEffects.Add(eff);
                }
                continue;
            }

            standardEffects.Add(eff);
        }

        if (standardEffects.Count > 0)
        {
            SkillEffectEngine.ExecuteRest(standardEffects, player);
        }
    }

    private void ReverseRelicStatsImmediate(RelicData relic)
    {
        if (relic.Effects == null) return;
        foreach (var eff in relic.Effects)
        {
            if (eff.effectId != "eff_stat_bonus") continue;
            string stat = (eff.stat ?? "").ToLower();
            if (stat == "maxhealthpercent")
            {
                int bonus = Mathf.RoundToInt(player.GetMaxHealth() * (eff.value / (100f + eff.value)));
                player.AddMaxHealth(-bonus);
            }
            else if (stat == "damagemax") player.AddBaseDamage(-eff.value);
            else if (stat == "physicalresist") player.AddPhysicalResist(-eff.value);
            else if (stat == "elementalresist") player.AddElementalResist(-eff.value);
            else if (stat == "damage") player.AddBaseDamage(-eff.value);
            else if (stat == "critchance") player.AddPermanentCritChance(-eff.value);
            else if (stat == "critdamage") player.AddPermanentCritDamage(-eff.value);
        }
    }

    // ════════════════════════════════════════════════════════════
    //  COMBAT START
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Called at the very start of combat. Processes all combatStart relics.
    /// Directly modifies player and enemies. Returns heal amount for floating text.
    /// </summary>
    public int OnCombatStart(List<CombatEnemy> enemies)
    {
        ResetCombatState();
        inCombat = true;
        currentEnemies = enemies;
        int healAmount = 0;

        foreach (var relic in relics)
        {
            if (IsRelicBroken(relic.Id)) continue;
            if (relic.Trigger != "combatStart" || relic.Effects == null) continue;
            healAmount += ApplyCombatStartEffectsForRelic(relic, enemies);
        }

        // Process Siphoning Aura: cache the per-turn AP delta
        siphonApDelta = 0;
        foreach (var relic in relics)
        {
            if (IsRelicBroken(relic.Id)) continue;
            if (relic.Trigger != "onTurnStart" || relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_ap_delta" && relic.TriggerInterval <= 0)
                {
                    siphonApDelta += eff.amount;
                }
            }
        }

        return healAmount;
    }

    /// <summary>
    /// Apply combatStart effects for a single relic. Used both at combat start and when
    /// adding a relic mid-combat (e.g., via cheat panel). Returns heal amount.
    /// </summary>
    private int ApplyCombatStartEffectsForRelic(RelicData relic, List<CombatEnemy> enemies)
    {
        if (relic.Effects == null) return 0;
        int healAmount = 0;

        foreach (var eff in relic.Effects)
        {
            switch (eff.effectId)
            {
                case "eff_ap_delta":
                    if (eff.duration > 0)
                    {
                        // Cracked Battery: -2 AP for N turns (applied each turn in OnTurnStart)
                        crackedBatteryTurnsLeft = eff.duration;
                        crackedBatteryApDelta = eff.amount;
                    }
                    else
                    {
                        // First Pulse: +2 AP — deferred until OnTurnStart (after RefreshAP)
                        pendingCombatStartAPDelta += eff.amount;
                    }
                    Debug.Log($"[RelicManager] {relic.DisplayName}: AP delta {eff.amount} (pending)");
                    break;

                case "eff_post_combat_heal":
                    int heal = Mathf.RoundToInt(player.GetMaxHealth() * eff.percentOfMaxHealth / 100f);
                    if (heal > 0)
                    {
                        player.Heal(heal);
                        healAmount += heal;
                        lastCombatStartHeal = heal;
                        Debug.Log($"[RelicManager] {relic.DisplayName}: healed {heal} HP");
                    }
                    break;

                case "eff_combat_shield":
                    int shield = Mathf.RoundToInt(player.GetMaxHealth() * eff.percentOfMaxHealth / 100f);
                    player.AddShield(shield);
                    Debug.Log($"[RelicManager] {relic.DisplayName}: +{shield} shield");
                    break;

                case "eff_disable_system":
                    string dt = (eff.disableTarget ?? "").ToLower();
                    if (dt == "defensiveqte")
                    {
                        disableDefensiveQTETurns = eff.disableDuration;
                        Debug.Log($"[RelicManager] {relic.DisplayName}: disable defensive QTE for {eff.disableDuration} turns");
                    }
                    break;

                case "eff_temp_resist":
                    earlyGuardTurnsLeft = eff.duration;
                    earlyGuardResistAmount = eff.value;
                    player.AddPhysicalResist(eff.value);
                    player.AddElementalResist(eff.value);
                    Debug.Log($"[RelicManager] {relic.DisplayName}: +{eff.value}% resist for {eff.duration} turns");
                    break;

                case "eff_apply_status":
                    if (eff.target == "AllEnemies" && enemies != null)
                    {
                        foreach (var enemy in enemies)
                        {
                            if (!enemy.IsAlive()) continue;
                            string sid = eff.status ?? "";
                            if (sid == "status_weaken")
                            {
                                enemy.ApplyWeak(eff.duration, eff.magnitude);
                            }
                            else if (sid == "status_vulnerable")
                            {
                                enemy.ApplyTempResistAll(-Mathf.RoundToInt(eff.magnitude), eff.duration);
                            }
                        }
                        Debug.Log($"[RelicManager] {relic.DisplayName}: applied {eff.status} to all enemies");
                    }
                    else if (eff.target == "Self")
                    {
                        string sid = eff.status ?? "";
                        if (sid == "status_weaken") player.ApplyWeaken(eff.magnitude, eff.duration);
                        else if (sid == "status_sunder") player.ApplySunder(eff.magnitude, eff.duration);
                        Debug.Log($"[RelicManager] {relic.DisplayName}: applied {eff.status} to player");
                    }
                    break;

                case "eff_apply_equipped_mark":
                    if (enemies != null)
                    {
                        Element markElement = GetEquippedSigilElement();
                        if (markElement != Element.None)
                        {
                            foreach (var enemy in enemies)
                            {
                                if (enemy.IsAlive())
                                    enemy.AddMarks(markElement, eff.extraMarks);
                            }
                            Debug.Log($"[RelicManager] {relic.DisplayName}: applied {eff.extraMarks} {markElement} mark(s) to all enemies");
                        }
                    }
                    break;
            }
        }

        return healAmount;
    }

    // ════════════════════════════════════════════════════════════
    //  TURN START
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Called at the start of each player turn, after AP refresh.
    /// </summary>
    public void OnTurnStart(int currentTurn)
    {
        turnCounter++;

        // Reset per-turn state
        extraMarksThisTurn = 0;
        apReductionThisTurn = 0;
        apReductionSkillsRemaining = 0;
        perfectQteApUsedThisTurn = false;
        reactionWeakenUsedThisTurn = false;

        // First Pulse (and similar): apply deferred combat-start AP delta on first turn
        if (pendingCombatStartAPDelta != 0)
        {
            player.ModifyCurrentAP(pendingCombatStartAPDelta);
            Debug.Log($"[RelicManager] Combat-start AP delta applied: {pendingCombatStartAPDelta}");
            pendingCombatStartAPDelta = 0;
        }

        // Cracked Battery: AP penalty for remaining turns
        if (crackedBatteryTurnsLeft > 0)
        {
            player.ModifyCurrentAP(crackedBatteryApDelta);
            crackedBatteryTurnsLeft--;
            Debug.Log($"[RelicManager] Cracked Battery: {crackedBatteryApDelta} AP, {crackedBatteryTurnsLeft} turns left");
        }

        // Early Guard Override: expire temp resist
        if (earlyGuardTurnsLeft > 0)
        {
            earlyGuardTurnsLeft--;
            if (earlyGuardTurnsLeft <= 0 && earlyGuardResistAmount > 0)
            {
                player.AddPhysicalResist(-earlyGuardResistAmount);
                player.AddElementalResist(-earlyGuardResistAmount);
                Debug.Log($"[RelicManager] Early Guard Override: temp resist expired");
                earlyGuardResistAmount = 0;
            }
        }

        // Siphoning Aura: -1 AP each turn
        if (siphonApDelta != 0)
        {
            player.ModifyCurrentAP(siphonApDelta);
            Debug.Log($"[RelicManager] Siphoning Aura: {siphonApDelta} AP");
        }

        // Process interval-based onTurnStart relics
        foreach (var relic in relics)
        {
            if (IsRelicBroken(relic.Id)) continue;
            if (relic.Trigger != "onTurnStart") continue;
            if (relic.TriggerInterval > 0 && turnCounter % relic.TriggerInterval != 0) continue;

            if (relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                switch (eff.effectId)
                {
                    case "eff_extra_marks":
                        // Mark Echo: +1 extra mark for next skill
                        extraMarksThisTurn += eff.extraMarks;
                        relicJustTriggered.Add(relic.Id);
                        Debug.Log($"[RelicManager] {relic.DisplayName}: +{eff.extraMarks} extra marks this turn");
                        break;

                    case "eff_ap_cost_reduction":
                        // Rhythm Discount: -1 AP for next 2 skills
                        apReductionThisTurn += eff.apReduction;
                        apReductionSkillsRemaining = eff.skillCount > 0 ? eff.skillCount : 999;
                        relicJustTriggered.Add(relic.Id);
                        Debug.Log($"[RelicManager] {relic.DisplayName}: -{eff.apReduction} AP for {eff.skillCount} skills");
                        break;
                }
            }
        }

        // Ice Cream Core: add banked AP on top of refreshed AP
        if (hasAPBanking && bankedAP > 0)
        {
            player.ModifyCurrentAP(bankedAP);
            Debug.Log($"[RelicManager] Ice Cream Core: added {bankedAP} banked AP");
            bankedAP = 0;
        }

        // Elemental Fog: randomize AP costs every Nth turn
        if (randomizeAPInterval > 0 && turnCounter % randomizeAPInterval == 0)
        {
            RandomizeSkillAPCosts();
        }
    }

    // ════════════════════════════════════════════════════════════
    //  SKILL USED
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when a skill is used. Handles Deadeye Counter, AP reduction tracking,
    /// Cooldown Lottery free skill consumption, and Mark Echo consumption.
    /// </summary>
    public void OnSkillUsed(int skillNumber, int apCost)
    {
        int skillIndex = skillNumber - 1;

        // Consume Cooldown Lottery free skill
        if (cooldownLotteryFreeSkill == skillIndex)
        {
            cooldownLotteryFreeSkill = -1;
            Debug.Log($"[RelicManager] Cooldown Lottery: free cast consumed for skill {skillNumber}");
        }

        // Consume Rhythm Discount skill count
        if (apReductionSkillsRemaining > 0)
        {
            apReductionSkillsRemaining--;
            if (apReductionSkillsRemaining <= 0)
            {
                apReductionThisTurn = 0;
            }
        }

        // Deadeye Counter
        skillUseCounter++;

        foreach (var relic in relics)
        {
            if (IsRelicBroken(relic.Id)) continue;
            if (relic.Trigger != "onSkillUse") continue;
            if (relic.TriggerInterval <= 0) continue;
            if (skillUseCounter % relic.TriggerInterval != 0) continue;

            if (relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_temp_crit_bonus")
                {
                    deadeyeCritReady = true;
                    relicJustTriggered.Add(relic.Id);
                    Debug.Log($"[RelicManager] {relic.DisplayName}: guaranteed crit ready ({skillUseCounter}/{relic.TriggerInterval})");
                }
            }
        }

        // Extra marks consumed after first skill use this turn
        // (the actual mark application is handled by CombatManager)
    }

    /// <summary>
    /// Called when AP is spent on a skill. Tracks Cooldown Lottery.
    /// </summary>
    public void OnAPSpent(int amount)
    {
        apSpentCounter += amount;

        foreach (var relic in relics)
        {
            if (IsRelicBroken(relic.Id)) continue;
            if (relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_refresh_random_skill" && eff.apSpentThreshold > 0)
                {
                    if (apSpentCounter >= eff.apSpentThreshold)
                    {
                        apSpentCounter -= eff.apSpentThreshold;
                        TriggerCooldownLottery();
                        relicJustTriggered.Add(relic.Id);
                        Debug.Log($"[RelicManager] {relic.DisplayName}: triggered at {eff.apSpentThreshold} AP spent");
                    }
                }
            }
        }
    }

    private void TriggerCooldownLottery()
    {
        // Refresh a random non-ultimate skill cooldown and make it free
        var candidates = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            if (player.GetSkillCooldown(i) > 0)
                candidates.Add(i);
        }

        int chosen;
        if (candidates.Count > 0)
        {
            // Prefer a skill on cooldown
            chosen = candidates[Random.Range(0, candidates.Count)];
            player.SetSkillCooldown(chosen, 0);
        }
        else
        {
            // No skill on cooldown — pick random non-ultimate
            chosen = Random.Range(0, 4);
        }

        cooldownLotteryFreeSkill = chosen;
        Debug.Log($"[RelicManager] Cooldown Lottery: skill {chosen + 1} is free and off cooldown");
    }

    // ════════════════════════════════════════════════════════════
    //  TURN END
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Called at end of player turn. Handles AP banking, Elemental Drip, Blood Toll.
    /// </summary>
    public void OnTurnEnd(List<CombatEnemy> enemies)
    {
        // Ice Cream Core: bank unspent AP (cap 50)
        if (hasAPBanking)
        {
            int unspent = player.GetCurrentAP();
            if (unspent > 0)
            {
                bankedAP = Mathf.Min(bankedAP + unspent, 50);
                Debug.Log($"[RelicManager] Ice Cream Core: banked {unspent} AP (total banked: {bankedAP})");
            }
        }

        // Process onTurnEnd relics
        foreach (var relic in relics)
        {
            if (IsRelicBroken(relic.Id)) continue;
            if (relic.Trigger != "onTurnEnd" || relic.Effects == null) continue;

            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_end_turn_mark" && enemies != null)
                {
                    // Elemental Drip: apply mark of equipped element to random alive enemy
                    Element equipped = GetEquippedSigilElement();
                    if (equipped == Element.None) continue;

                    var alive = new List<CombatEnemy>();
                    foreach (var e in enemies) { if (e.IsAlive()) alive.Add(e); }

                    if (alive.Count > 0)
                    {
                        var target = alive[Random.Range(0, alive.Count)];
                        target.AddMarks(equipped, eff.extraMarks);
                        Debug.Log($"[RelicManager] {relic.DisplayName}: applied {eff.extraMarks} {equipped} mark to {target.Name}");
                    }
                }
                else if (eff.effectId == "eff_deal_damage" && eff.target == "Self")
                {
                    int dmg = eff.value > 0 ? eff.value : 1;
                    player.TakeDamage(dmg);
                    Debug.Log($"[RelicManager] {relic.DisplayName}: took {dmg} self-damage");
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    //  COMBAT END
    // ════════════════════════════════════════════════════════════

    public void OnCombatEnd()
    {
        // Process onCombatEnd relics (Blood Toll)
        foreach (var relic in relics)
        {
            if (IsRelicBroken(relic.Id)) continue;
            if (relic.Trigger != "onCombatEnd" || relic.Effects == null) continue;

            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_post_combat_heal")
                {
                    int heal = Mathf.RoundToInt(player.GetMaxHealth() * eff.percentOfMaxHealth / 100f);
                    if (heal > 0) player.Heal(heal);
                }
                else if (eff.effectId == "eff_deal_damage" && eff.target == "Self")
                {
                    int dmg = eff.value > 0 ? eff.value : 1;
                    player.TakeDamage(dmg);
                }
            }
        }

        // Ice Cream Core: reset banked AP at combat end
        bankedAP = 0;

        // Remove Early Guard Override temp resist if still active
        if (earlyGuardTurnsLeft > 0 && earlyGuardResistAmount > 0)
        {
            player.AddPhysicalResist(-earlyGuardResistAmount);
            player.AddElementalResist(-earlyGuardResistAmount);
            earlyGuardTurnsLeft = 0;
            earlyGuardResistAmount = 0;
        }

        // Clear Elemental Fog randomized costs
        randomizedAPCosts = null;

        inCombat = false;
        currentEnemies = null;
    }

    // ════════════════════════════════════════════════════════════
    //  ENEMY KILLED (Piggy Bank)
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Called when an enemy is killed during combat. Returns bonus gold from Piggy Bank.
    /// </summary>
    public int OnEnemyKilled()
    {
        int bonusGold = 0;
        if (hasPiggyBank && !IsRelicBroken("relic_piggy_bank"))
        {
            bonusGold = piggyBankGoldPerKill;
            Debug.Log($"[RelicManager] Piggy Bank: +{bonusGold} gold on enemy kill");
        }
        return bonusGold;
    }

    /// <summary>
    /// Called when the player spends gold. Breaks Piggy Bank if active.
    /// </summary>
    public void OnGoldSpent()
    {
        if (hasPiggyBank && !IsRelicBroken("relic_piggy_bank"))
        {
            brokenRelicIds.Add("relic_piggy_bank");
            Debug.Log("[RelicManager] Piggy Bank: BROKEN — player spent gold");
        }
    }

    // ════════════════════════════════════════════════════════════
    //  PLAYER DEATH (Fracture Revival)
    // ════════════════════════════════════════════════════════════

    public bool HasRevive()
    {
        if (fractureRevivalUsed) return false;
        foreach (var r in relics)
        {
            if (r.Trigger == "onDeath" && !IsRelicBroken(r.Id)) return true;
        }
        return false;
    }

    /// <summary>
    /// Attempt to revive the player via Fracture Revival.
    /// Returns true if revived. Breaks the relic and reduces max HP.
    /// </summary>
    public bool TryRevive()
    {
        if (fractureRevivalUsed) return false;

        foreach (var relic in relics)
        {
            if (relic.Trigger != "onDeath" || IsRelicBroken(relic.Id)) continue;

            fractureRevivalUsed = true;
            brokenRelicIds.Add(relic.Id);

            if (relic.Effects != null)
            {
                foreach (var eff in relic.Effects)
                {
                    if (eff.effectId == "eff_reborn")
                    {
                        int reviveHP = Mathf.Max(1, Mathf.RoundToInt(player.GetMaxHealth() * (eff.healthPercent / 100f)));
                        player.SetHealth(reviveHP);
                    }
                    else if (eff.effectId == "eff_stat_bonus" && eff.stat == "maxHealthPercent" && eff.value < 0)
                    {
                        int reduction = Mathf.RoundToInt(player.GetMaxHealth() * (Mathf.Abs(eff.value) / 100f));
                        player.ReduceMaxHealth(reduction);
                    }
                }
            }

            Debug.Log($"[RelicManager] Fracture Revival used! Revived at {player.GetHealth()}/{player.GetMaxHealth()}");
            return true;
        }
        return false;
    }

    // ════════════════════════════════════════════════════════════
    //  REACTION INTEGRATION
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Called after a reaction is triggered. Returns true if should double (Reactor Core).
    /// Handles Reaction Exhaustion (weaken self), Dual Specialist (shield),
    /// and Catalyst Splinter consumption.
    /// </summary>
    public bool OnReactionTriggered(bool isDual)
    {
        bool shouldDouble = false;

        // Reactor Core: first reaction per combat triggers twice
        if (hasReactionDouble && !reactionDoubleUsed)
        {
            reactionDoubleUsed = true;
            shouldDouble = true;
            Debug.Log("[RelicManager] Reactor Core: reaction will trigger twice");
        }

        // Dual Specialist: dual reaction grants shield (once per combat)
        if (isDual && hasDualReactionShield && !dualReactionShieldUsed)
        {
            dualReactionShieldUsed = true;
            int shield = Mathf.RoundToInt(player.GetMaxHealth() * dualReactionShieldPercent / 100f);
            player.AddShield(shield);
            Debug.Log($"[RelicManager] Dual Specialist: +{shield} shield");
        }

        return shouldDouble;
    }

    /// <summary>
    /// Called after all reaction processing is complete (including Reactor Core second trigger).
    /// Handles Reaction Exhaustion (once-per-turn weaken).
    /// </summary>
    public void OnReactionComplete()
    {
        // Reaction Exhaustion: gain Weaken for 1 turn (once per turn)
        if (hasReactionWeaken && !reactionWeakenUsedThisTurn)
        {
            reactionWeakenUsedThisTurn = true;
            player.ApplyWeaken(25f, reactionWeakenDuration);
            Debug.Log($"[RelicManager] Reaction Exhaustion: Weaken applied for {reactionWeakenDuration} turns");
        }
    }

    /// <summary>
    /// Called before checking if a reaction can trigger.
    /// Returns the extra mark cost modifier for the current reaction check.
    /// </summary>
    public int GetMonoReactionMarkCost()
    {
        // Base: 6. Overconsumption: +1 = 7
        return 6 + reactionExtraMarkCost;
    }

    public int GetDualPerElementThreshold() => GetDualReactionMarkCostPerElement();

    public bool HasCatalystSplinter() => hasReactionCostReduction && !reactionCostReductionUsed;

    public int GetDualReactionMarkCostPerElement()
    {
        // Base: 3. Catalyst Splinter first dual: 2 instead of 3
        if (hasReactionCostReduction && !reactionCostReductionUsed)
        {
            return 2;
        }
        return 3;
    }

    public int GetDualReactionExtraMarkCost()
    {
        // Overconsumption: +1 total for dual (one element needs +1)
        return reactionExtraMarkCost;
    }

    public void ConsumeCatalystSplinter()
    {
        reactionCostReductionUsed = true;
    }

    // ════════════════════════════════════════════════════════════
    //  DEADEYE COUNTER
    // ════════════════════════════════════════════════════════════

    public bool IsDeadeyeCritReady() => deadeyeCritReady;

    public bool ConsumeDeadeyeCrit()
    {
        if (!deadeyeCritReady) return false;
        deadeyeCritReady = false;
        Debug.Log("[RelicManager] Deadeye Counter: guaranteed crit consumed");
        return true;
    }

    // ════════════════════════════════════════════════════════════
    //  QUERY METHODS
    // ════════════════════════════════════════════════════════════

    public List<RelicData> GetRelics() => relics;
    public bool HasRelic(string relicId) => relics.Exists(r => r.Id == relicId);
    public bool IsRelicBroken(string relicId) => brokenRelicIds.Contains(relicId);

    public List<RelicData> GetRelicsByTrigger(string trigger)
    {
        var result = new List<RelicData>();
        foreach (var r in relics)
        {
            if (r.Trigger == trigger && !IsRelicBroken(r.Id)) result.Add(r);
        }
        return result;
    }

    public bool IsImmuneToStatus(string statusId) => statusImmunities.Contains(statusId);
    public float GetEnergyCostMultiplier() => energyCostMultiplier;
    public int GetUltimateCooldownBonus() => ultimateCooldownBonus;
    public bool IsDefensiveQTEDisabled(int currentTurn) => disableDefensiveQTETurns > 0 && currentTurn <= disableDefensiveQTETurns;
    public bool IsReactionQTEDisabled() => disableReactionQTE;
    public bool AreSigilsDisabled() => disableSigils;
    public bool ShouldHideEnemyIntentions() => hideEnemyIntentions;
    public bool HasAPBanking() => hasAPBanking;
    public bool HasPiggyBank() => hasPiggyBank && !IsRelicBroken("relic_piggy_bank");
    public int GetPiggyBankGoldPerKill() => piggyBankGoldPerKill;
    public int GetBankedAP() => bankedAP;

    public int GetExtraMarksThisTurn() => extraMarksThisTurn;
    public void ConsumeExtraMarks() { extraMarksThisTurn = 0; }

    public bool HasPerfectQteAp() => hasPerfectQteAp;
    public bool TryConsumePerfectQteAp()
    {
        if (!hasPerfectQteAp || perfectQteApUsedThisTurn) return false;
        perfectQteApUsedThisTurn = true;
        return true;
    }
    public int GetPerfectQteApAmount() => perfectQteApAmount;

    public bool HasMarkTransfer() => hasMarkTransfer;
    public int GetMarkTransferCount() => markTransferCount;

    public bool HasPerfectReactionSaveMark() => hasPerfectReactionSaveMark;
    public int GetPerfectReactionSaveMarkCount() => perfectReactionSaveMarkCount;

    public bool HasReactionDouble() => hasReactionDouble && !reactionDoubleUsed;
    public float GetReactionDoubleMultiplier() => reactionDoubleMultiplier;

    public bool HasDualReactionShield() => hasDualReactionShield && !dualReactionShieldUsed;

    public bool HasReactionWeaken() => hasReactionWeaken && !reactionWeakenUsedThisTurn;
    public int GetReactionWeakenDuration() => reactionWeakenDuration;

    public int GetReactionExtraMarkCost() => reactionExtraMarkCost;

    public int GetLastCombatStartHeal()
    {
        int h = lastCombatStartHeal;
        lastCombatStartHeal = 0;
        return h;
    }

    /// <summary>
    /// Get the AP cost modifier for a specific skill.
    /// Returns the amount to subtract from the base AP cost.
    /// Accounts for Rhythm Discount and Cooldown Lottery.
    /// </summary>
    public int GetSkillAPReduction(int skillIndex)
    {
        // Cooldown Lottery: free cast
        if (cooldownLotteryFreeSkill == skillIndex)
            return 999; // effectively free

        // Rhythm Discount: -1 AP if active and skills remaining
        if (apReductionThisTurn > 0 && apReductionSkillsRemaining > 0)
            return apReductionThisTurn;

        return 0;
    }

    /// <summary>
    /// Check if a skill has a yellow highlight (Rhythm Discount or Cooldown Lottery active).
    /// </summary>
    public bool IsSkillHighlighted(int skillIndex)
    {
        if (cooldownLotteryFreeSkill == skillIndex) return true;
        if (apReductionThisTurn > 0 && apReductionSkillsRemaining > 0) return true;
        return false;
    }

    /// <summary>
    /// Get the effective AP cost for a skill, accounting for relic discounts.
    /// Returns the modified cost (never below 0).
    /// </summary>
    public int GetEffectiveAPCost(int skillIndex, int baseCost)
    {
        // Elemental Fog: use randomized cost if active (skills 1-4 only)
        if (randomizedAPCosts != null && skillIndex >= 0 && skillIndex < 4)
        {
            baseCost = randomizedAPCosts[skillIndex];
        }

        // Cooldown Lottery: free
        if (cooldownLotteryFreeSkill == skillIndex)
            return 0;

        // Rhythm Discount
        if (apReductionThisTurn > 0 && apReductionSkillsRemaining > 0)
            return Mathf.Max(0, baseCost - apReductionThisTurn);

        return baseCost;
    }

    /// <summary>
    /// Get randomized AP costs for Elemental Fog. Returns null if not active.
    /// </summary>
    public int[] GetRandomizedAPCosts() => randomizedAPCosts;

    private void RandomizeSkillAPCosts()
    {
        randomizedAPCosts = new int[4];
        for (int i = 0; i < 4; i++)
        {
            randomizedAPCosts[i] = Random.Range(0, 4); // 0-3 inclusive
        }
        Debug.Log($"[RelicManager] Elemental Fog: randomized AP costs to [{randomizedAPCosts[0]}, {randomizedAPCosts[1]}, {randomizedAPCosts[2]}, {randomizedAPCosts[3]}]");
    }

    /// <summary>
    /// Get the missing HP bonus damage percent (Last Stand Blade).
    /// </summary>
    public float GetMissingHPBonusDamagePercent()
    {
        if (!hasMissingHpDamage || missingHpDamageMaxPercent <= 0) return 0f;
        float missingPercent = 1f - ((float)player.GetHealth() / player.GetMaxHealth());
        return Mathf.Min(missingPercent * 100f, missingHpDamageMaxPercent);
    }

    /// <summary>
    /// Get the equipped sigil element for Elemental Broadcast and Elemental Drip.
    /// Checks all skill elements — if multiple types, picks randomly. If none, returns Element.None.
    /// </summary>
    public Element GetEquippedSigilElement()
    {
        if (disableSigils) return Element.None;

        var elements = new HashSet<Element>();
        for (int i = 1; i <= 5; i++)
        {
            Element e = player.GetSkillElement(i);
            if (e != Element.None) elements.Add(e);
        }

        if (elements.Count == 0)
        {
            // Fallback to player affinity
            Element aff = player.GetAffinity();
            if (aff != Element.None) return aff;
            return Element.None;
        }

        if (elements.Count == 1)
        {
            foreach (var e in elements) return e;
        }

        // Multiple types: pick randomly
        var list = new List<Element>(elements);
        return list[Random.Range(0, list.Count)];
    }

    // ════════════════════════════════════════════════════════════
    //  UI STATE
    // ════════════════════════════════════════════════════════════

    public struct RelicStateInfo
    {
        public string RelicId;
        public int CurrentCount;
        public int MaxCount;
        public bool IsReady;
        public bool JustTriggered;
        public bool IsBroken;
    }

    public List<RelicStateInfo> GetRelicStates()
    {
        var states = new List<RelicStateInfo>();
        foreach (var relic in relics)
        {
            var s = new RelicStateInfo { RelicId = relic.Id, IsBroken = IsRelicBroken(relic.Id) };
            bool hasCounter = false;

            if (s.IsBroken)
            {
                states.Add(s);
                continue;
            }

            // Deadeye Counter
            if (relic.Trigger == "onSkillUse" && relic.TriggerInterval > 0)
            {
                s.MaxCount = relic.TriggerInterval;
                s.CurrentCount = skillUseCounter % relic.TriggerInterval;
                hasCounter = true;
                if (relic.Effects != null)
                    foreach (var eff in relic.Effects)
                        if (eff.effectId == "eff_temp_crit_bonus") s.IsReady = deadeyeCritReady;
            }
            // Mark Echo, Rhythm Discount
            else if (relic.Trigger == "onTurnStart" && relic.TriggerInterval > 0)
            {
                s.MaxCount = relic.TriggerInterval;
                s.CurrentCount = turnCounter > 0 ? turnCounter % relic.TriggerInterval : 0;
                hasCounter = true;
                if (relic.Effects != null)
                    foreach (var eff in relic.Effects)
                    {
                        if (eff.effectId == "eff_extra_marks") s.IsReady = extraMarksThisTurn > 0;
                        else if (eff.effectId == "eff_ap_cost_reduction") s.IsReady = apReductionThisTurn > 0 && apReductionSkillsRemaining > 0;
                    }
            }
            // Cooldown Lottery, Reactor Core, Dual Specialist, Catalyst Splinter, etc.
            else if (relic.Effects != null)
            {
                foreach (var eff in relic.Effects)
                {
                    if (eff.effectId == "eff_refresh_random_skill" && eff.apSpentThreshold > 0)
                    {
                        s.MaxCount = eff.apSpentThreshold;
                        s.CurrentCount = apSpentCounter;
                        hasCounter = true;
                    }
                    else if (eff.effectId == "eff_reaction_double") s.IsReady = hasReactionDouble && !reactionDoubleUsed;
                    else if (eff.effectId == "eff_dual_reaction_shield") s.IsReady = hasDualReactionShield && !dualReactionShieldUsed;
                    else if (eff.effectId == "eff_reaction_cost_reduction") s.IsReady = hasReactionCostReduction && !reactionCostReductionUsed;
                    else if (eff.effectId == "eff_perfect_qte_ap") s.IsReady = hasPerfectQteAp && !perfectQteApUsedThisTurn;
                    else if (eff.effectId == "eff_ap_banking") s.IsReady = hasAPBanking && bankedAP > 0;
                }
            }

            s.JustTriggered = relicJustTriggered.Contains(relic.Id);

            if (hasCounter || s.IsReady || s.JustTriggered)
                states.Add(s);
        }
        return states;
    }

    public List<ReactionChipInfo> GetRelicBuffChips()
    {
        var chips = new List<ReactionChipInfo>();
        foreach (var relic in relics)
        {
            if (IsRelicBroken(relic.Id) || relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_temp_crit_bonus" && deadeyeCritReady)
                    chips.Add(new ReactionChipInfo("Deadeye Ready", "Next skill is a guaranteed critical hit.", 99, true));
                else if (eff.effectId == "eff_extra_marks" && extraMarksThisTurn > 0 && relic.Trigger == "onTurnStart")
                    chips.Add(new ReactionChipInfo("Mark Echo", $"+{extraMarksThisTurn} extra mark(s) this turn.", 1, true));
                else if (eff.effectId == "eff_ap_cost_reduction" && apReductionThisTurn > 0 && apReductionSkillsRemaining > 0)
                    chips.Add(new ReactionChipInfo("Rhythm Discount", $"-{apReductionThisTurn} AP cost for {apReductionSkillsRemaining} skill(s).", 1, true));
                else if (eff.effectId == "eff_reaction_double" && hasReactionDouble && !reactionDoubleUsed)
                    chips.Add(new ReactionChipInfo("Reactor Core", "Next reaction triggers twice.", 99, true));
                else if (eff.effectId == "eff_dual_reaction_shield" && hasDualReactionShield && !dualReactionShieldUsed)
                    chips.Add(new ReactionChipInfo("Dual Specialist", $"Dual Reaction grants {dualReactionShieldPercent}% HP shield.", 99, true));
                else if (eff.effectId == "eff_reaction_cost_reduction" && hasReactionCostReduction && !reactionCostReductionUsed)
                    chips.Add(new ReactionChipInfo("Catalyst Splinter", "Next dual reaction costs 2+2 marks.", 99, true));
                else if (eff.effectId == "eff_perfect_qte_ap" && hasPerfectQteAp && !perfectQteApUsedThisTurn)
                    chips.Add(new ReactionChipInfo("Focus Lens", $"Perfect QTE grants +{perfectQteApAmount} AP.", 1, true));
                else if (eff.effectId == "eff_ap_banking" && hasAPBanking && bankedAP > 0)
                    chips.Add(new ReactionChipInfo("Ice Cream Core", $"{bankedAP} AP banked for next turn.", 99, true));
            }
        }
        return chips;
    }

    public void ClearRelicJustTriggered() { relicJustTriggered.Clear(); }

    // ════════════════════════════════════════════════════════════
    //  EARLY GUARD OVERRIDE - TOOLTIP INFO
    // ════════════════════════════════════════════════════════════

    public bool HasEarlyGuardActive() => earlyGuardTurnsLeft > 0 && earlyGuardResistAmount > 0;
    public int GetEarlyGuardResistAmount() => earlyGuardResistAmount;
    public int GetEarlyGuardTurnsLeft() => earlyGuardTurnsLeft;

    // ════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════

    public int GetSkillUseCounter() => skillUseCounter;
    public int GetAPSpentCounter() => apSpentCounter;
    public int GetTurnCounter() => turnCounter;
    public int GetCooldownLotteryFreeSkill() => cooldownLotteryFreeSkill;
}
