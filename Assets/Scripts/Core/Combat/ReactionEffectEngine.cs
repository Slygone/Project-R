using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Result of processing all reaction effects.
/// CombatManager uses this to show floating text, update UI, etc.
/// </summary>
public struct ReactionResult
{
    public int TotalDamageToTarget;
    public int TotalDamageToOthers;
    public int ShieldGained;
    public bool DidCrit;
    public List<DamageInstance> DamageInstances;
    
    public struct DamageInstance
    {
        public CombatEnemy Target;
        public int Damage;
        public bool IsCrit;
    }
}

/// <summary>
/// Data-driven reaction effect engine. Processes all effects from elementalReactions.json.
/// Reaction damage is its own separate source — not affected by skill multipliers.
/// Reactions cannot crit unless the reaction's canCrit flag is true.
/// </summary>
public static class ReactionEffectEngine
{
    /// <summary>
    /// Process all effects for a reaction. This is the main entry point.
    /// </summary>
    public static ReactionResult ProcessReaction(
        string reactionId,
        Player attacker,
        CombatEnemy target,
        List<CombatEnemy> allEnemies,
        float qteMultiplier,
        Element attackElement)
    {
        var result = new ReactionResult
        {
            DamageInstances = new List<ReactionResult.DamageInstance>()
        };
        
        if (string.IsNullOrEmpty(reactionId) || attacker == null) return result;
        
        var reactionDef = DataCache.GetReactionDef(reactionId);
        if (reactionDef == null || reactionDef.Effects == null || reactionDef.Effects.Count == 0)
        {
            GameLog.Warn(GameLogCategory.Reaction, "[ReactionEngine]", $"No effects for reaction {reactionId}");
            return result;
        }
        
        // Base damage range for this reaction (character damage + affinity bonus)
        int baseDamageRange = attacker.GetTotalDamage();
        
        foreach (var effect in reactionDef.Effects)
        {
            if (effect == null || string.IsNullOrEmpty(effect.effectId)) continue;
            
            GameLog.Reaction(GameLog.Join(
                "ProcessEffect",
                GameLog.KV("reaction", reactionId),
                GameLog.KV("effectId", effect.effectId),
                GameLog.KV("target", effect.target ?? "none")
            ), GameLogVerbosity.Verbose);
            
            switch (effect.effectId)
            {
                case "rxn_deal_damage":
                    ProcessDealDamage(effect, reactionDef, attacker, target, allEnemies, baseDamageRange, qteMultiplier, attackElement, ref result);
                    break;
                    
                case "rxn_apply_dot":
                    ProcessApplyDot(effect, attacker, target, baseDamageRange);
                    break;
                    
                case "rxn_player_buff":
                    ProcessPlayerBuff(effect, attacker, baseDamageRange);
                    break;
                    
                case "rxn_enemy_debuff":
                    ProcessEnemyDebuff(effect, attacker, target, baseDamageRange);
                    break;
                    
                case "rxn_apply_shield":
                    ProcessApplyShield(effect, attacker, ref result);
                    break;
                    
                case "rxn_reduce_resist":
                    ProcessReduceResist(effect, target);
                    break;
                    
                default:
                    GameLog.Warn(GameLogCategory.Reaction, "[ReactionEngine]", $"Unknown effectId: {effect.effectId} in {reactionId}");
                    break;
            }
        }
        
        return result;
    }
    
    // ==================== EFFECT PROCESSORS ====================
    
    private static void ProcessDealDamage(
        ReactionEffectEntry effect,
        ReactionData reactionDef,
        Player attacker,
        CombatEnemy primaryTarget,
        List<CombatEnemy> allEnemies,
        int baseDamageRange,
        float qteMultiplier,
        Element attackElement,
        ref ReactionResult result)
    {
        // Reaction damage = baseDamageRange * damageMultiplier * variance * qteMultiplier
        float rawDamage = baseDamageRange * (effect.damageMultiplier > 0f ? effect.damageMultiplier : 1f);
        int afterVariance = attacker.ApplyVariance(rawDamage);
        int afterQTE = Mathf.RoundToInt(afterVariance * qteMultiplier);
        
        // Check for Ignite bonus (Fire_Fire special: +10% if DoT already exists)
        if (effect.bonusDmgIfExists > 0f && primaryTarget != null)
        {
            if (primaryTarget.HasDoT("Ignite"))
            {
                afterQTE = Mathf.RoundToInt(afterQTE * (1f + effect.bonusDmgIfExists));
            }
        }
        
        // Crit only if reaction allows it
        bool isCrit = false;
        if (reactionDef.CanCrit)
        {
            isCrit = Random.Range(0, 100) < attacker.GetCritChance();
            if (isCrit)
            {
                afterQTE = Mathf.RoundToInt(afterQTE * attacker.GetCritDamage());
            }
        }
        
        string targetType = effect.target ?? "Enemy";
        
        if (targetType == "Enemy" && primaryTarget != null && primaryTarget.IsAlive())
        {
            // Apply resistance unless ignoreResist
            int finalDamage = effect.ignoreResist ? afterQTE : primaryTarget.ApplyResistance(afterQTE, attackElement);
            
            primaryTarget.TakeDamage(finalDamage);
            result.TotalDamageToTarget += finalDamage;
            result.DidCrit = result.DidCrit || isCrit;
            result.DamageInstances.Add(new ReactionResult.DamageInstance
            {
                Target = primaryTarget,
                Damage = finalDamage,
                IsCrit = isCrit
            });
            
            GameLog.Reaction(GameLog.Join(
                "DealDamage",
                GameLog.KV("target", primaryTarget.Name),
                GameLog.KV("base", baseDamageRange),
                GameLog.KV("mult", effect.damageMultiplier),
                GameLog.KV("qte", qteMultiplier),
                GameLog.KV("crit", isCrit),
                GameLog.KV("ignoreResist", effect.ignoreResist),
                GameLog.KV("final", finalDamage)
            ));
        }
        else if (targetType == "AllOtherEnemies" && allEnemies != null)
        {
            foreach (var enemy in allEnemies)
            {
                if (enemy == null || !enemy.IsAlive() || enemy == primaryTarget) continue;
                
                int finalDamage = effect.ignoreResist ? afterQTE : enemy.ApplyResistance(afterQTE, attackElement);
                enemy.TakeDamage(finalDamage);
                result.TotalDamageToOthers += finalDamage;
                result.DamageInstances.Add(new ReactionResult.DamageInstance
                {
                    Target = enemy,
                    Damage = finalDamage,
                    IsCrit = false
                });
                
                GameLog.Reaction(GameLog.Join(
                    "AoEDamage",
                    GameLog.KV("target", enemy.Name),
                    GameLog.KV("final", finalDamage)
                ));
            }
        }
    }
    
    private static void ProcessApplyDot(ReactionEffectEntry effect, Player attacker, CombatEnemy target, int baseDamageRange)
    {
        if (target == null || !target.IsAlive()) return;
        
        string dotName = !string.IsNullOrEmpty(effect.dotName) ? effect.dotName : "Reaction DoT";
        int dotDamagePerTurn = Mathf.RoundToInt(baseDamageRange * effect.damagePercent);
        if (dotDamagePerTurn <= 0) return;
        
        int duration = effect.duration > 0 ? effect.duration : 2;
        int maxStacks = effect.maxStacks > 0 ? effect.maxStacks : 1;
        
        // Check if DoT already exists for bonus damage
        if (effect.bonusDotIfExists > 0f && target.HasDoT(dotName))
        {
            dotDamagePerTurn = Mathf.RoundToInt(dotDamagePerTurn * (1f + effect.bonusDotIfExists));
        }
        
        target.ApplyNamedDoT(dotName, dotDamagePerTurn, duration, maxStacks, effect.refreshable);
        
        GameLog.Reaction(GameLog.Join(
            "ApplyDoT",
            GameLog.KV("target", target.Name),
            GameLog.KV("dot", dotName),
            GameLog.KV("dmg", dotDamagePerTurn),
            GameLog.KV("dur", duration),
            GameLog.KV("stacks", maxStacks),
            GameLog.KV("refresh", effect.refreshable)
        ));
    }
    
    private static void ProcessPlayerBuff(ReactionEffectEntry effect, Player attacker, int baseDamageRange)
    {
        if (attacker == null) return;
        
        string buffType = effect.buffType ?? "";
        float value = effect.value;
        int duration = effect.duration > 0 ? effect.duration : 1;
        
        switch (buffType)
        {
            case "CritDamage":
                // value = bonus crit damage % (e.g., 20 = +20%)
                attacker.ApplyReactionBuff("CritDamage", value, duration, effect.refreshable);
                GameLog.Reaction(GameLog.Join("PlayerBuff", GameLog.KV("buff", "CritDamage"), GameLog.KV("value", value), GameLog.KV("dur", duration)));
                break;
                
            case "BonusAP":
                // value = extra AP (e.g., 2)
                attacker.ApplyReactionBuff("BonusAP", value, duration, effect.refreshable);
                GameLog.Reaction(GameLog.Join("PlayerBuff", GameLog.KV("buff", "BonusAP"), GameLog.KV("value", value), GameLog.KV("dur", duration)));
                break;
                
            case "ReflectiveArmor":
                // value = reflect % (e.g., 30 = 30% reflect)
                attacker.ApplyReactionBuff("ReflectiveArmor", value, duration, effect.refreshable);
                GameLog.Reaction(GameLog.Join("PlayerBuff", GameLog.KV("buff", "ReflectiveArmor"), GameLog.KV("value", value), GameLog.KV("dur", duration)));
                break;
                
            case "DamageReduction":
                // value = reduction % (e.g., 10 = 10% less damage taken)
                attacker.ApplyReactionBuff("DamageReduction", value, duration, effect.refreshable);
                GameLog.Reaction(GameLog.Join("PlayerBuff", GameLog.KV("buff", "DamageReduction"), GameLog.KV("value", value), GameLog.KV("dur", duration)));
                break;
                
            case "RockDamageWhileShielded":
                // value = bonus rock damage % while shielded (e.g., 10)
                attacker.ApplyReactionBuff("RockDamageWhileShielded", value, 0, false);
                GameLog.Reaction(GameLog.Join("PlayerBuff", GameLog.KV("buff", "RockDamageWhileShielded"), GameLog.KV("value", value)));
                break;
                
            default:
                GameLog.Warn(GameLogCategory.Reaction, "[ReactionEngine]", $"Unknown buffType: {buffType}");
                break;
        }
    }
    
    private static void ProcessEnemyDebuff(ReactionEffectEntry effect, Player attacker, CombatEnemy target, int baseDamageRange)
    {
        if (target == null || !target.IsAlive()) return;
        
        string debuffType = effect.debuffType ?? "";
        int duration = effect.duration > 0 ? effect.duration : 2;
        
        switch (debuffType)
        {
            case "Weak":
                // value = damage reduction % (e.g., 50 = enemy deals 50% less)
                target.ApplyWeak(duration, effect.value);
                GameLog.Reaction(GameLog.Join("EnemyDebuff", GameLog.KV("debuff", "Weak"), GameLog.KV("value", effect.value), GameLog.KV("dur", duration)));
                break;
                
            case "Freeze":
                // Freeze = skip turn, separate from Stun
                target.ApplyFreeze(duration);
                GameLog.Reaction(GameLog.Join("EnemyDebuff", GameLog.KV("debuff", "Freeze"), GameLog.KV("dur", duration)));
                break;
                
            case "Shatter":
                // Threshold and pop damage both = baseDamageRange
                target.ApplyShatter(baseDamageRange, baseDamageRange, duration, effect.refreshable);
                GameLog.Reaction(GameLog.Join("EnemyDebuff", GameLog.KV("debuff", "Shatter"), GameLog.KV("threshold", baseDamageRange), GameLog.KV("dur", duration)));
                break;
                
            case "HealOnHit":
                // value = heal % of player max HP when enemy attacks (e.g., 1 = 1%)
                target.ApplyReactionDebuff("HealOnHit", effect.value, duration, effect.refreshable);
                GameLog.Reaction(GameLog.Join("EnemyDebuff", GameLog.KV("debuff", "HealOnHit"), GameLog.KV("value", effect.value), GameLog.KV("dur", duration)));
                break;
                
            case "Electrocute":
                // Damage-on-hit mark, stacks up to maxStacks, per-stack damage multipliers
                target.ApplyReactionDebuff("Electrocute", 0, duration, effect.refreshable, effect.maxStacks, effect.stackValues, baseDamageRange);
                GameLog.Reaction(GameLog.Join("EnemyDebuff", GameLog.KV("debuff", "Electrocute"), GameLog.KV("stacks", effect.maxStacks), GameLog.KV("dur", duration)));
                break;
                
            case "Mudslide":
                // Stackable mark, at max stacks consume for damage + stun
                target.ApplyReactionDebuff("Mudslide", 0, duration, effect.refreshable, effect.maxStacks, effect.stackValues, baseDamageRange);
                GameLog.Reaction(GameLog.Join("EnemyDebuff", GameLog.KV("debuff", "Mudslide"), GameLog.KV("stacks", effect.maxStacks), GameLog.KV("dur", duration)));
                break;
                
            default:
                GameLog.Warn(GameLogCategory.Reaction, "[ReactionEngine]", $"Unknown debuffType: {debuffType}");
                break;
        }
    }
    
    private static void ProcessApplyShield(ReactionEffectEntry effect, Player attacker, ref ReactionResult result)
    {
        if (attacker == null) return;
        
        int amount = Mathf.RoundToInt(effect.value);
        if (amount <= 0) return;
        
        int before = attacker.GetShield();
        attacker.AddShield(amount);
        int gained = attacker.GetShield() - before;
        result.ShieldGained += gained;
        
        GameLog.Reaction(GameLog.Join(
            "ShieldGain",
            GameLog.KV("amount", amount),
            GameLog.KV("gained", gained),
            GameLog.KV("shieldAfter", attacker.GetShield())
        ));
    }
    
    private static void ProcessReduceResist(ReactionEffectEntry effect, CombatEnemy target)
    {
        if (target == null || !target.IsAlive()) return;
        
        int reductionPct = Mathf.RoundToInt(effect.value);
        int duration = effect.duration > 0 ? effect.duration : 2;
        string elements = effect.elements ?? "All";
        
        if (elements == "All")
        {
            target.ApplyTempResistAll(-reductionPct, duration);
            GameLog.Reaction(GameLog.Join("ReduceResist", GameLog.KV("elements", "All"), GameLog.KV("reduction", reductionPct), GameLog.KV("dur", duration)));
        }
        else
        {
            // Parse comma-separated elements
            string[] elementNames = elements.Split(',');
            foreach (string elemName in elementNames)
            {
                string trimmed = elemName.Trim();
                if (System.Enum.TryParse<Element>(trimmed, true, out Element elem) && elem != Element.None)
                {
                    target.ApplyTempResist(elem, -reductionPct, duration);
                    GameLog.Reaction(GameLog.Join("ReduceResist", GameLog.KV("element", elem), GameLog.KV("reduction", reductionPct), GameLog.KV("dur", duration)));
                }
            }
        }
    }
}
