using System.Collections.Generic;
using UnityEngine;

public struct ReactionEffectContext
{
    public string ReactionId;
    public Player Attacker;
    public CombatEnemy Target;
    public int FinalDamageDealt;
    public Element AttackElement;
    
    // Hit-only modifiers (applied before damage calculation, reset after)
    public int BonusCritChance;
    public float BonusCritDamage;
}

public static class ReactionEffectEngine
{
    public static ReactionEffectContext ApplyEffects(string reactionId, Player attacker, CombatEnemy target, int finalDamageDealt, Element attackElement)
    {
        var context = new ReactionEffectContext
        {
            ReactionId = reactionId,
            Attacker = attacker,
            Target = target,
            FinalDamageDealt = finalDamageDealt,
            AttackElement = attackElement,
            BonusCritChance = 0,
            BonusCritDamage = 0f
        };
        
        if (string.IsNullOrEmpty(reactionId)) return context;
        
        var effects = DataCache.GetReactionEffects(reactionId);
        if (effects == null || effects.Count == 0) return context;
        
        foreach (var effect in effects)
        {
            // Check chance
            if (effect.ChancePct < 100)
            {
                int roll = Random.Range(0, 100);
                if (roll >= effect.ChancePct)
                {
                    GameLog.Reaction(GameLog.Join(
                        "Effect",
                        GameLog.KV("id", reactionId),
                        GameLog.KV("type", effect.EffectType),
                        GameLog.KV("target", effect.Target),
                        GameLog.KV("value", effect.Value),
                        GameLog.KV("dur", effect.DurationTurns),
                        GameLog.KV("chance", effect.ChancePct),
                        GameLog.KV("applied", false),
                        GameLog.KV("roll", roll)
                    ), GameLogVerbosity.Verbose);
                    continue;
                }
            }
            
            ApplyEffect(effect, ref context);
        }
        
        return context;
    }
    
    // Apply pre-hit effects (CritChanceBonus, CritDamageBonus) and return modifiers
    public static ReactionEffectContext GetPreHitModifiers(string reactionId, Player attacker, CombatEnemy target, Element attackElement)
    {
        var context = new ReactionEffectContext
        {
            ReactionId = reactionId,
            Attacker = attacker,
            Target = target,
            FinalDamageDealt = 0, // Not known yet
            AttackElement = attackElement,
            BonusCritChance = 0,
            BonusCritDamage = 0f
        };
        
        if (string.IsNullOrEmpty(reactionId)) return context;
        
        var effects = DataCache.GetReactionEffects(reactionId);
        if (effects == null || effects.Count == 0) return context;
        
        foreach (var effect in effects)
        {
            // Only apply pre-hit effects here
            if (effect.EffectType == "CritChanceBonus" || effect.EffectType == "CritDamageBonus")
            {
                // Check chance
                if (effect.ChancePct < 100)
                {
                    int roll = Random.Range(0, 100);
                    if (roll >= effect.ChancePct) continue;
                }
                
                ApplyPreHitEffect(effect, ref context);
            }
        }
        
        return context;
    }
    
    // Apply post-hit effects (everything except crit bonuses)
    public static void ApplyPostHitEffects(string reactionId, Player attacker, CombatEnemy target, int finalDamageDealt, Element attackElement)
    {
        if (string.IsNullOrEmpty(reactionId)) return;
        
        var effects = DataCache.GetReactionEffects(reactionId);
        if (effects == null || effects.Count == 0) return;
        
        var context = new ReactionEffectContext
        {
            ReactionId = reactionId,
            Attacker = attacker,
            Target = target,
            FinalDamageDealt = finalDamageDealt,
            AttackElement = attackElement
        };
        
        foreach (var effect in effects)
        {
            // Skip pre-hit effects
            if (effect.EffectType == "CritChanceBonus" || effect.EffectType == "CritDamageBonus")
                continue;
            
            // Check chance
            if (effect.ChancePct < 100)
            {
                int roll = Random.Range(0, 100);
                if (roll >= effect.ChancePct)
                {
                    GameLog.Reaction(GameLog.Join(
                        "Effect",
                        GameLog.KV("id", reactionId),
                        GameLog.KV("type", effect.EffectType),
                        GameLog.KV("target", effect.Target),
                        GameLog.KV("value", effect.Value),
                        GameLog.KV("dur", effect.DurationTurns),
                        GameLog.KV("chance", effect.ChancePct),
                        GameLog.KV("applied", false),
                        GameLog.KV("roll", roll)
                    ), GameLogVerbosity.Verbose);
                    continue;
                }
            }
            
            ApplyEffect(effect, ref context);
        }
    }
    
    private static void ApplyPreHitEffect(ReactionEffectData effect, ref ReactionEffectContext context)
    {
        string effectType = effect.EffectType;
        float value = effect.GetValueAsFloat();

        GameLog.Reaction(GameLog.Join(
            "Effect",
            GameLog.KV("id", context.ReactionId),
            GameLog.KV("type", effectType),
            GameLog.KV("target", effect.Target),
            GameLog.KV("value", effect.Value),
            GameLog.KV("dur", effect.DurationTurns),
            GameLog.KV("chance", effect.ChancePct),
            GameLog.KV("applied", true)
        ), GameLogVerbosity.Verbose);
        
        switch (effectType)
        {
            case "CritChanceBonus":
                // Value is percentage points to add (e.g., 0.1 = +10%)
                context.BonusCritChance = Mathf.RoundToInt(value * 100f);
                break;
                
            case "CritDamageBonus":
                // Value is multiplier to add (e.g., 0.2 = +0.2x)
                context.BonusCritDamage = value;
                break;
        }
    }
    
    private static void ApplyEffect(ReactionEffectData effect, ref ReactionEffectContext context)
    {
        string effectType = effect.EffectType;
        string target = effect.Target;
        float value = effect.GetValueAsFloat();
        int duration = effect.DurationTurns;

        GameLog.Reaction(GameLog.Join(
            "Effect",
            GameLog.KV("id", context.ReactionId),
            GameLog.KV("type", effectType),
            GameLog.KV("target", target),
            GameLog.KV("value", effect.Value),
            GameLog.KV("dur", duration),
            GameLog.KV("chance", effect.ChancePct),
            GameLog.KV("applied", true)
        ), GameLogVerbosity.Verbose);
        
        switch (effectType)
        {
            case "ApplyDotFromHitPct":
                ApplyDotFromHitPct(context, value, duration);
                break;
                
            case "TriggerExistingDotsInstant":
                TriggerExistingDotsInstant(context);
                break;
                
            case "AddShieldFromHitPct":
                AddShieldFromHitPct(context, value);
                break;
                
            case "AddShieldFlat":
                AddShieldFlat(context, effect.GetValueAsInt());
                break;
                
            case "ResistAllDeltaPct":
                ApplyResistAllDelta(context, value, duration, target);
                break;
                
            case "ResistDeltaPct_Fire":
                ApplyResistDelta(context, Element.Fire, value, duration, target);
                break;
                
            case "ResistDeltaPct_Ice":
                ApplyResistDelta(context, Element.Ice, value, duration, target);
                break;
                
            case "ResistDeltaPct_Water":
                ApplyResistDelta(context, Element.Water, value, duration, target);
                break;
                
            case "ResistDeltaPct_Wind":
                ApplyResistDelta(context, Element.Wind, value, duration, target);
                break;
                
            case "ResistDeltaPct_Rock":
                ApplyResistDelta(context, Element.Rock, value, duration, target);
                break;
                
            case "ApplyStatus":
                ApplyStatus(context, effect.Value, duration);
                break;
                
            case "CritChanceBonus":
            case "CritDamageBonus":
                // These are handled in pre-hit, skip here
                break;
                
            default:
                GameLog.Warn(GameLogCategory.Reaction, "[Reaction]", $"EffectUnknown | id={context.ReactionId} | type={effectType}");
                break;
        }
    }
    
    private static void ApplyDotFromHitPct(ReactionEffectContext context, float pct, int duration)
    {
        if (context.Target == null || !context.Target.IsAlive()) return;
        
        int dotDamagePerTurn = Mathf.RoundToInt(context.FinalDamageDealt * pct);
        if (dotDamagePerTurn <= 0) return;
        
        context.Target.ApplyDoT(dotDamagePerTurn, duration, "Reaction");
    }
    
    private static void TriggerExistingDotsInstant(ReactionEffectContext context)
    {
        if (context.Target == null || !context.Target.IsAlive()) return;
        
        int instantDamage = context.Target.TickDoTEffectsInstant();
    }
    
    private static void AddShieldFromHitPct(ReactionEffectContext context, float pct)
    {
        if (context.Attacker == null) return;
        
        int shieldAmount = Mathf.RoundToInt(context.FinalDamageDealt * pct);
        if (shieldAmount <= 0) return;

        int before = context.Attacker.GetShield();
        context.Attacker.AddShield(shieldAmount);
        GameLog.Combat(GameLog.Join(
            "ShieldGain",
            GameLog.KV("who", "Player"),
            GameLog.KV("amount", shieldAmount),
            GameLog.KV("source", "Reaction"),
            GameLog.KV("shieldBefore", before),
            GameLog.KV("shieldAfter", context.Attacker.GetShield())
        ));
    }
    
    private static void AddShieldFlat(ReactionEffectContext context, int amount)
    {
        if (context.Attacker == null || amount <= 0) return;

        int before = context.Attacker.GetShield();
        context.Attacker.AddShield(amount);
        GameLog.Combat(GameLog.Join(
            "ShieldGain",
            GameLog.KV("who", "Player"),
            GameLog.KV("amount", amount),
            GameLog.KV("source", "Reaction"),
            GameLog.KV("shieldBefore", before),
            GameLog.KV("shieldAfter", context.Attacker.GetShield())
        ));
    }
    
    private static void ApplyResistAllDelta(ReactionEffectContext context, float deltaPct, int duration, string target)
    {
        int deltaInt = Mathf.RoundToInt(deltaPct * 100f);
        
        if (target == "Attacker" && context.Attacker != null)
        {
            context.Attacker.ApplyTempResistAll(deltaInt, duration);
        }
        else if (target == "Enemy" && context.Target != null)
        {
            context.Target.ApplyTempResistAll(deltaInt, duration);
        }
    }
    
    private static void ApplyResistDelta(ReactionEffectContext context, Element element, float deltaPct, int duration, string target)
    {
        int deltaInt = Mathf.RoundToInt(deltaPct * 100f);
        
        if (target == "Attacker" && context.Attacker != null)
        {
            context.Attacker.ApplyTempResist(element, deltaInt, duration);
        }
        else if (target == "Enemy" && context.Target != null)
        {
            context.Target.ApplyTempResist(element, deltaInt, duration);
        }
    }
    
    private static void ApplyStatus(ReactionEffectContext context, string statusName, int duration)
    {
        if (context.Target == null || !context.Target.IsAlive()) return;
        
        string statusLower = statusName?.ToLower() ?? "";
        
        if (statusLower == "burn")
        {
            // Burn is a DoT - use base damage percentage
            int burnDamage = Mathf.RoundToInt(context.FinalDamageDealt * 0.1f);
            context.Target.ApplyDoT(burnDamage, duration > 0 ? duration : 2, "Burn");
        }
        else if (statusLower == "stun")
        {
            context.Target.ApplyStun(duration > 0 ? duration : 1);
        }
        else if (statusLower == "freeze" || statusLower == "frozen")
        {
            // Freeze acts like a stun for 1 turn unless a longer duration is specified
            int freezeDur = duration > 0 ? duration : 1;
            context.Target.ApplyStun(freezeDur);
        }
        else
        {
            GameLog.Warn(GameLogCategory.Status, "[Status]", $"ApplyUnknown | target={(context.Target != null ? context.Target.Name : "null")} | status={statusName}");
        }
    }
}
