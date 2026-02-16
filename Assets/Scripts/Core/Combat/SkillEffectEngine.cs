using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data-driven effect execution engine. Processes EffectEntry lists from skills, potions, relics, rest, and QTEs.
/// Replaces all hardcoded ApplySkillEffects / HandleKillEffects / UsePotion logic.
/// </summary>
public static class SkillEffectEngine
{
    /// <summary>
    /// Context passed to effect execution so effects can reference combat state.
    /// </summary>
    public struct EffectContext
    {
        public Player Player;
        public CombatEnemy Target;
        public List<CombatEnemy> AllEnemies;
        public int DamageDealt;          // total damage dealt this action (for lifesteal, shield-from-damage, etc.)
        public Element AttackElement;
        public string SkillName;         // for logging
        public int SkillNumber;          // 1-indexed skill slot (for cooldown overrides)
    }

    /// <summary>
    /// Result of processing an effect list. Callers use this to drive UI feedback.
    /// </summary>
    public struct EffectResult
    {
        public int TotalDamageDealt;
        public int HealAmount;
        public int ShieldGained;
        public int EnergyDelta;
        public bool AppliedStun;
        public bool AppliedDoT;
        public bool TargetKilled;
        public bool HasOnKillBonus;
        public float OnKillBonusDamageMultiplier;
        public int OnKillCooldownOverride;
        public int OnKillEnergyRefundPercent;
    }

    /// <summary>
    /// Execute all effects in a list. This is the main entry point.
    /// damageDealt should be set BEFORE calling this if effects need it (lifesteal, shield-from-damage).
    /// For skills: call after damage is dealt to get post-hit effects.
    /// </summary>
    public static EffectResult Execute(List<EffectEntry> effects, EffectContext ctx)
    {
        var result = new EffectResult();
        if (effects == null) return result;

        foreach (var eff in effects)
        {
            ProcessEffect(eff, ref ctx, ref result);
        }

        return result;
    }

    /// <summary>
    /// Execute effects specifically for potions (simpler context, no combat target required for self-effects).
    /// </summary>
    public static EffectResult ExecutePotion(List<EffectEntry> effects, Player player, CombatEnemy target = null, Element elementOverride = Element.None)
    {
        var ctx = new EffectContext
        {
            Player = player,
            Target = target,
            AttackElement = elementOverride != Element.None ? elementOverride : Element.None,
            SkillName = "Potion"
        };
        return Execute(effects, ctx);
    }

    /// <summary>
    /// Execute effects for rest options (no combat context).
    /// </summary>
    public static EffectResult ExecuteRest(List<EffectEntry> effects, Player player)
    {
        var ctx = new EffectContext
        {
            Player = player,
            SkillName = "Rest"
        };
        return Execute(effects, ctx);
    }

    private static void ProcessEffect(EffectEntry eff, ref EffectContext ctx, ref EffectResult result)
    {
        if (eff == null || string.IsNullOrEmpty(eff.effectId)) return;

        switch (eff.effectId)
        {
            case "eff_deal_damage":
                ProcessDealDamage(eff, ref ctx, ref result);
                break;

            case "eff_energy_delta":
                ProcessEnergyDelta(eff, ref ctx, ref result);
                break;

            case "eff_apply_status":
                ProcessApplyStatus(eff, ref ctx, ref result);
                break;

            case "eff_dot":
                ProcessDoT(eff, ref ctx, ref result);
                break;

            case "eff_shield_gain":
                ProcessShieldGain(eff, ref ctx, ref result);
                break;

            case "eff_heal":
                ProcessHeal(eff, ref ctx, ref result);
                break;

            case "eff_lifesteal":
                ProcessLifesteal(eff, ref ctx, ref result);
                break;

            case "eff_temp_crit_bonus":
                ProcessTempCritBonus(eff, ref ctx, ref result);
                break;

            case "eff_multi_hit":
                // Multi-hit is handled by the caller (CombatManager) since it needs special damage loop
                break;

            case "eff_damage_multiplier":
                // Damage multiplier is consumed by the caller before damage calc (QTE offensive)
                break;

            case "eff_on_kill_bonus":
                ProcessOnKillBonus(eff, ref ctx, ref result);
                break;

            case "eff_stat_bonus":
                ProcessStatBonus(eff, ref ctx, ref result);
                break;

            // Relic-specific effects — handled by Player relic trigger system, not here
            case "eff_status_immunity":
            case "eff_ap_delta":
            case "eff_extra_marks":
            case "eff_missing_hp_damage":
            case "eff_ap_cost_reduction":
            case "eff_refresh_random_skill":
            case "eff_energy_cost_multiplier":
            case "eff_disable_system":
            case "eff_ap_banking":
            case "eff_post_combat_heal":
            case "eff_first_mark_bonus":
            case "eff_combat_shield":
            case "eff_perfect_qte_ap":
            case "eff_reaction_cost_reduction":
            case "eff_reaction_ap_refund":
            case "eff_end_turn_mark":
            case "eff_mark_transfer":
            case "eff_perfect_reaction_save_mark":
            case "eff_apply_equipped_mark":
            case "eff_reaction_double":
            case "eff_dual_reaction_shield":
            case "eff_reaction_weaken":
            case "eff_hide_marks":
            case "eff_reaction_extra_mark_cost":
                // These are processed by Player.AddRelic / OnRelicTurnStart / OnRelicTurnEnd / etc.
                break;

            default:
                GameLog.Warn(GameLogCategory.System, "[SkillEffectEngine]",
                    GameLog.Join("UnknownEffect", GameLog.KV("effectId", eff.effectId)));
                break;
        }
    }

    private static void ProcessDealDamage(EffectEntry eff, ref EffectContext ctx, ref EffectResult result)
    {
        // For potions with flat damage value
        if (eff.value > 0 && ctx.Target != null && ctx.Target.IsAlive())
        {
            Element element = ctx.AttackElement;
            if (eff.useElemental && element == Element.None)
            {
                element = GetRandomElement();
            }
            int damage = ctx.Target.ApplyResistance(eff.value, element);
            ctx.Target.TakeDamage(damage);
            result.TotalDamageDealt += damage;

            GameLog.Combat(GameLog.Join("EffectDamage",
                GameLog.KV("source", ctx.SkillName),
                GameLog.KV("target", ctx.Target.Name),
                GameLog.KV("flat", eff.value),
                GameLog.KV("final", damage)
            ), GameLogVerbosity.Verbose);
        }
        // Multiplier-based damage is calculated by CombatManager's damage pipeline, not here
    }

    private static void ProcessEnergyDelta(EffectEntry eff, ref EffectContext ctx, ref EffectResult result)
    {
        if (ctx.Player == null) return;
        result.EnergyDelta += eff.amount;
        // Actual energy application is handled by Player.UseSkillAndApplyEffects for skills
        // For non-skill sources, caller applies directly
    }

    private static void ProcessApplyStatus(EffectEntry eff, ref EffectContext ctx, ref EffectResult result)
    {
        string target = eff.target ?? "SelectedEnemy";

        if (target == "Self" && ctx.Player != null)
        {
            ApplyStatusToPlayer(eff, ctx.Player);
        }
        else if (target == "SelectedEnemy" && ctx.Target != null && ctx.Target.IsAlive())
        {
            ApplyStatusToEnemy(eff, ctx.Target, ref result);
        }
        else if (target == "AllEnemies" && ctx.AllEnemies != null)
        {
            foreach (var enemy in ctx.AllEnemies)
            {
                if (enemy.IsAlive())
                {
                    ApplyStatusToEnemy(eff, enemy, ref result);
                }
            }
        }
    }

    private static void ApplyStatusToPlayer(EffectEntry eff, Player player)
    {
        switch (eff.status)
        {
            case "status_riposte_stance":
                player.ApplyBlock(eff.magnitude > 0 ? eff.magnitude : 50f);
                break;
            case "status_damage_up":
                player.ApplyDamageBuff(eff.magnitude > 0 ? eff.magnitude : 25f, eff.duration);
                break;
            case "status_evasion":
                player.ApplyEvasion(eff.magnitude > 0 ? eff.magnitude : 100f, eff.duration);
                break;
            default:
                GameLog.Status(GameLog.Join("ApplyStatus",
                    GameLog.KV("target", "Player"),
                    GameLog.KV("status", eff.status),
                    GameLog.KV("duration", eff.duration),
                    GameLog.KV("magnitude", eff.magnitude)
                ), GameLogVerbosity.Verbose);
                break;
        }
    }

    private static void ApplyStatusToEnemy(EffectEntry eff, CombatEnemy enemy, ref EffectResult result)
    {
        switch (eff.status)
        {
            case "status_stun":
                enemy.ApplyStun(eff.duration > 0 ? eff.duration : 1);
                result.AppliedStun = true;
                break;
            case "status_slow":
                enemy.ApplySlow(eff.duration > 0 ? eff.duration : 1);
                break;
            case "status_blind":
                // Blind reduces accuracy - store on enemy
                break;
            case "status_weak":
                enemy.ApplyWeak(eff.duration > 0 ? eff.duration : 2, eff.magnitude > 0 ? eff.magnitude : 20f);
                break;
            default:
                GameLog.Status(GameLog.Join("ApplyStatus",
                    GameLog.KV("target", enemy.Name),
                    GameLog.KV("status", eff.status),
                    GameLog.KV("duration", eff.duration),
                    GameLog.KV("magnitude", eff.magnitude)
                ), GameLogVerbosity.Verbose);
                break;
        }
    }

    private static void ProcessDoT(EffectEntry eff, ref EffectContext ctx, ref EffectResult result)
    {
        if (ctx.Target == null || !ctx.Target.IsAlive()) return;

        int dotDamage = Mathf.RoundToInt(ctx.DamageDealt * (eff.damagePercent / 100f));
        int duration = eff.duration > 0 ? eff.duration : 2;
        ctx.Target.ApplyDoT(dotDamage, duration, ctx.SkillName ?? "DoT");
        result.AppliedDoT = true;

        GameLog.Status(GameLog.Join("ApplyDoT",
            GameLog.KV("target", ctx.Target.Name),
            GameLog.KV("damage", dotDamage),
            GameLog.KV("duration", duration),
            GameLog.KV("source", ctx.SkillName)
        ), GameLogVerbosity.Verbose);
    }

    private static void ProcessShieldGain(EffectEntry eff, ref EffectContext ctx, ref EffectResult result)
    {
        if (ctx.Player == null) return;

        int shieldAmount = 0;

        if (eff.percentOfDamage > 0 && ctx.DamageDealt > 0)
        {
            // Shield = % of damage dealt
            shieldAmount = Mathf.RoundToInt(ctx.DamageDealt * (eff.percentOfDamage / 100f));

            // Cap at % of max HP if specified
            if (eff.capPercent > 0)
            {
                int cap = Mathf.RoundToInt(ctx.Player.GetMaxHealth() * (eff.capPercent / 100f));
                shieldAmount = Mathf.Min(shieldAmount, cap);
            }
        }
        else if (eff.percentOfMaxHealth > 0)
        {
            // Shield = % of max health (used by defensive QTE, rest)
            shieldAmount = Mathf.RoundToInt(ctx.Player.GetMaxHealth() * (eff.percentOfMaxHealth / 100f));
        }
        else if (eff.value > 0)
        {
            // Flat shield value
            shieldAmount = eff.value;
        }

        if (shieldAmount > 0)
        {
            ctx.Player.AddShield(shieldAmount);
            result.ShieldGained += shieldAmount;

            GameLog.Combat(GameLog.Join("ShieldGain",
                GameLog.KV("who", "Player"),
                GameLog.KV("amount", shieldAmount),
                GameLog.KV("source", ctx.SkillName)
            ), GameLogVerbosity.Verbose);
        }
    }

    private static void ProcessHeal(EffectEntry eff, ref EffectContext ctx, ref EffectResult result)
    {
        if (ctx.Player == null) return;

        int healAmount = 0;

        if (eff.percentOfMaxHealth > 0)
        {
            healAmount = Mathf.RoundToInt(ctx.Player.GetMaxHealth() * (eff.percentOfMaxHealth / 100f));
        }
        else if (eff.value > 0)
        {
            healAmount = eff.value;
        }

        if (healAmount > 0)
        {
            ctx.Player.Heal(healAmount);
            result.HealAmount += healAmount;

            GameLog.Combat(GameLog.Join("Heal",
                GameLog.KV("who", "Player"),
                GameLog.KV("amount", healAmount),
                GameLog.KV("source", ctx.SkillName)
            ), GameLogVerbosity.Verbose);
        }
    }

    private static void ProcessLifesteal(EffectEntry eff, ref EffectContext ctx, ref EffectResult result)
    {
        if (ctx.Player == null || ctx.DamageDealt <= 0) return;

        int healAmount = Mathf.RoundToInt(ctx.DamageDealt * (eff.percent / 100f));
        if (healAmount > 0)
        {
            ctx.Player.Heal(healAmount);
            result.HealAmount += healAmount;

            GameLog.Combat(GameLog.Join("Heal",
                GameLog.KV("who", "Player"),
                GameLog.KV("amount", healAmount),
                GameLog.KV("source", $"Lifesteal:{ctx.SkillName}")
            ), GameLogVerbosity.Verbose);
        }
    }

    private static void ProcessTempCritBonus(EffectEntry eff, ref EffectContext ctx, ref EffectResult result)
    {
        if (ctx.Player == null) return;
        // Temp crit bonuses are consumed immediately by the damage calculation
        // The CombatManager reads these from the skill's effects before calculating damage
    }

    private static void ProcessOnKillBonus(EffectEntry eff, ref EffectContext ctx, ref EffectResult result)
    {
        result.HasOnKillBonus = true;
        result.OnKillBonusDamageMultiplier = eff.bonusDamageMultiplier;
        result.OnKillCooldownOverride = eff.cooldownOverride;
        result.OnKillEnergyRefundPercent = eff.energyRefund;
    }

    private static void ProcessStatBonus(EffectEntry eff, ref EffectContext ctx, ref EffectResult result)
    {
        if (ctx.Player == null) return;

        string stat = eff.stat ?? "";
        int val = eff.value;
        bool isCombatTemp = eff.duration == -1; // duration -1 = lasts for current combat

        switch (stat)
        {
            case "critChance":
                if (isCombatTemp)
                    ctx.Player.AddTempCritChance(val);
                else
                    ctx.Player.AddPermanentCritChance(val);
                break;

            case "critDamage":
                if (isCombatTemp)
                    ctx.Player.AddTempCritDamage(val);
                else
                    ctx.Player.AddPermanentCritDamage(val);
                break;

            case "maxHealth":
                ctx.Player.AddMaxHealth(val);
                break;

            case "damage":
                ctx.Player.AddBaseDamage(val);
                break;

            case "elementalDamage":
                string element = eff.element ?? "All";
                ctx.Player.AddElementalDamageBonus(element, val);
                break;

            default:
                GameLog.Warn(GameLogCategory.System, "[SkillEffectEngine]",
                    GameLog.Join("UnknownStat", GameLog.KV("stat", stat), GameLog.KV("value", val)));
                break;
        }

        GameLog.Status(GameLog.Join("StatBonus",
            GameLog.KV("stat", stat),
            GameLog.KV("value", val),
            GameLog.KV("source", ctx.SkillName),
            GameLog.KV("temp", isCombatTemp)
        ), GameLogVerbosity.Verbose);
    }

    /// <summary>
    /// Extract temp crit bonus from a skill's effects list (consumed before damage calc).
    /// Returns (critChanceBonus, critDamageBonus).
    /// </summary>
    public static (int critChance, int critDamage) GetTempCritBonus(List<EffectEntry> effects)
    {
        if (effects == null) return (0, 0);

        foreach (var eff in effects)
        {
            if (eff.effectId == "eff_temp_crit_bonus")
            {
                return (eff.critChance, eff.critDamage);
            }
        }
        return (0, 0);
    }

    /// <summary>
    /// Check if a skill targets all enemies (has eff_deal_damage with target "AllEnemies").
    /// </summary>
    public static bool IsAoE(List<EffectEntry> effects)
    {
        if (effects == null) return false;

        foreach (var eff in effects)
        {
            if (eff.effectId == "eff_deal_damage" && eff.target == "AllEnemies")
                return true;
        }
        return false;
    }

    /// <summary>
    /// Get the damage multiplier from a skill's effects.
    /// </summary>
    public static float GetDamageMultiplier(List<EffectEntry> effects)
    {
        if (effects == null) return 1f;

        foreach (var eff in effects)
        {
            if (eff.effectId == "eff_deal_damage" && eff.multiplier != 0f)
                return eff.multiplier;
        }
        return 1f;
    }

    /// <summary>
    /// Get multi-hit count from a skill's effects.
    /// </summary>
    public static int GetMultiHitCount(List<EffectEntry> effects)
    {
        if (effects == null) return 1;

        foreach (var eff in effects)
        {
            if (eff.effectId == "eff_multi_hit" && eff.hitCount > 1)
                return eff.hitCount;
        }
        return 1;
    }

    /// <summary>
    /// Get energy gain from a skill's effects (positive eff_energy_delta).
    /// </summary>
    public static int GetEnergyGain(List<EffectEntry> effects)
    {
        if (effects == null) return 0;

        foreach (var eff in effects)
        {
            if (eff.effectId == "eff_energy_delta" && eff.amount > 0)
                return eff.amount;
        }
        return 0;
    }

    /// <summary>
    /// Get energy cost from a skill's effects (negative eff_energy_delta, returned as positive).
    /// </summary>
    public static int GetEnergyCost(List<EffectEntry> effects)
    {
        if (effects == null) return 0;

        foreach (var eff in effects)
        {
            if (eff.effectId == "eff_energy_delta" && eff.amount < 0)
                return -eff.amount;
        }
        return 0;
    }

    /// <summary>
    /// Check if a skill needs an enemy target (has any effect targeting enemies).
    /// </summary>
    public static bool NeedsEnemyTarget(List<EffectEntry> effects)
    {
        if (effects == null) return false;

        foreach (var eff in effects)
        {
            if (eff.effectId == "eff_deal_damage") return true;
            if (eff.effectId == "eff_dot") return true;
            if (eff.target == "SelectedEnemy" || eff.target == "AllEnemies") return true;
        }
        return false;
    }

    private static Element GetRandomElement()
    {
        var elements = new Element[] { Element.Fire, Element.Ice, Element.Water, Element.Wind, Element.Rock };
        return elements[Random.Range(0, elements.Length)];
    }
}
