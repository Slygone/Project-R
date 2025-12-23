using System.Collections.Generic;
using UnityEngine;

// Status effect types
public enum StatusEffectType
{
    None,
    DoT,        // Damage over time
    Stun,       // Skip turn
    Block,      // Reduce next incoming damage by %
    Shield,     // Absorb damage (persists over combats, not worlds)
    TempResist  // Temporary resistance modifier (can be negative)
}

// Individual status effect instance
public class StatusEffect
{
    public StatusEffectType Type;
    public int Duration;        // Turns remaining
    public float Value;         // Effect value (damage for DoT, % for Block, amount for Shield)
    public string Source;       // What caused this effect
    public int StackCount;      // For DoT stacking (max 3)
    public float BaseDamage;    // Original base damage for DoT (before stacking)
    
    public StatusEffect(StatusEffectType type, int duration, float value, string source = "")
    {
        Type = type;
        Duration = duration;
        Value = value;
        Source = source;
        StackCount = 1;
        BaseDamage = value;
    }
}

// Manages status effects on a combat entity
public class StatusEffectManager
{
    private List<StatusEffect> effects = new List<StatusEffect>();
    
    // Constants for DoT stacking
    private const int MAX_DOT_STACKS = 3;
    private const float DOT_CARRYOVER_PERCENT = 0.50f; // 50% of old DoT carries over
    
    // Constants for Stun
    private const int MAX_STUN_DURATION = 2;
    
    public void AddEffect(StatusEffect effect)
    {
        // For Block, replace existing block effect
        if (effect.Type == StatusEffectType.Block)
        {
            effects.RemoveAll(e => e.Type == StatusEffectType.Block);
        }
        
        // For Stun, add durations (capped at MAX_STUN_DURATION)
        if (effect.Type == StatusEffectType.Stun)
        {
            var existing = effects.Find(e => e.Type == StatusEffectType.Stun);
            if (existing != null)
            {
                int newDuration = existing.Duration + effect.Duration;
                existing.Duration = Mathf.Min(newDuration, MAX_STUN_DURATION);
                Debug.Log($"[StatusEffect] Stun stacked. New duration: {existing.Duration} turns (capped at {MAX_STUN_DURATION})");
                return;
            }
        }
        
        // For DoT, implement stacking with carryover
        if (effect.Type == StatusEffectType.DoT)
        {
            var existing = effects.Find(e => e.Type == StatusEffectType.DoT && e.Source == effect.Source);
            if (existing != null)
            {
                // Check stack cap
                if (existing.StackCount >= MAX_DOT_STACKS)
                {
                    // At max stacks, just refresh duration but don't increase damage further
                    existing.Duration = effect.Duration;
                    Debug.Log($"[StatusEffect] DoT at max stacks ({MAX_DOT_STACKS}). Duration refreshed, damage unchanged: {existing.Value}/turn");
                    return;
                }
                
                // Calculate carryover: 50% of old DoT's per-turn damage
                float carryover = existing.Value * DOT_CARRYOVER_PERCENT;
                float newDamage = effect.Value + carryover;
                
                existing.Value = newDamage;
                existing.Duration = effect.Duration; // Reset to full duration
                existing.StackCount++;
                
                Debug.Log($"[StatusEffect] DoT STACKED! Base: {effect.Value}, Carryover: {carryover:F1}, New total: {newDamage:F1}/turn, Stacks: {existing.StackCount}/{MAX_DOT_STACKS}, Duration: {existing.Duration}");
                return;
            }
        }
        
        effects.Add(effect);
        Debug.Log($"[StatusEffect] Added {effect.Type} (Value: {effect.Value}, Duration: {effect.Duration}, Source: {effect.Source})");
    }
    
    public void RemoveEffect(StatusEffectType type)
    {
        effects.RemoveAll(e => e.Type == type);
    }
    
    public void RemoveEffectBySource(StatusEffectType type, string source)
    {
        effects.RemoveAll(e => e.Type == type && e.Source == source);
    }
    
    public bool HasEffect(StatusEffectType type)
    {
        return effects.Exists(e => e.Type == type);
    }
    
    public StatusEffect GetEffect(StatusEffectType type)
    {
        return effects.Find(e => e.Type == type);
    }
    
    public List<StatusEffect> GetAllEffects()
    {
        return new List<StatusEffect>(effects);
    }
    
    // Check if stunned (call BEFORE TickEffects to properly skip turn)
    public bool CheckAndConsumeStun()
    {
        var stun = effects.Find(e => e.Type == StatusEffectType.Stun);
        if (stun != null)
        {
            Debug.Log($"[StatusEffect] Unit is STUNNED! Turns remaining before decrement: {stun.Duration}");
            stun.Duration--;
            if (stun.Duration <= 0)
            {
                effects.Remove(stun);
                Debug.Log("[StatusEffect] Stun expired after this turn skip");
            }
            else
            {
                Debug.Log($"[StatusEffect] Stun continues. Turns remaining: {stun.Duration}");
            }
            return true; // Was stunned, skip turn
        }
        return false; // Not stunned
    }
    
    // Tick DoT effects only (call after stun check), returns total DoT damage
    public int TickDoTEffects()
    {
        int dotDamage = 0;
        
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            
            // Apply DoT damage and decrement duration
            if (effect.Type == StatusEffectType.DoT)
            {
                int dmg = Mathf.RoundToInt(effect.Value);
                dotDamage += dmg;
                Debug.Log($"[StatusEffect] DoT tick: {dmg} damage from {effect.Source} (Stacks: {effect.StackCount}, Duration left: {effect.Duration - 1})");
                
                effect.Duration--;
                if (effect.Duration <= 0)
                {
                    Debug.Log($"[StatusEffect] DoT from {effect.Source} expired");
                    effects.RemoveAt(i);
                }
            }
        }
        
        return dotDamage;
    }
    
    // Tick DoT effects instantly WITHOUT consuming duration (for reaction effects)
    public int TickDoTEffectsInstant()
    {
        int dotDamage = 0;
        
        foreach (var effect in effects)
        {
            if (effect.Type == StatusEffectType.DoT)
            {
                int dmg = Mathf.RoundToInt(effect.Value);
                dotDamage += dmg;
                Debug.Log($"[StatusEffect] DoT instant tick: {dmg} damage from {effect.Source} (duration NOT consumed)");
            }
        }
        
        return dotDamage;
    }
    
    // Tick temp resist durations (call at end of turn)
    public void TickTempResists()
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            if (effect.Type == StatusEffectType.TempResist)
            {
                effect.Duration--;
                if (effect.Duration <= 0)
                {
                    Debug.Log($"[StatusEffect] TempResist {effect.Source} expired");
                    effects.RemoveAt(i);
                }
            }
        }
    }
    
    // Get total temp resist for a specific element (Source = element name or "All")
    public int GetTempResist(string elementOrAll)
    {
        int total = 0;
        foreach (var effect in effects)
        {
            if (effect.Type == StatusEffectType.TempResist)
            {
                if (effect.Source == elementOrAll || effect.Source == "All")
                {
                    total += Mathf.RoundToInt(effect.Value);
                }
            }
        }
        return total;
    }
    
    // Add temp resist effect
    public void AddTempResist(string elementOrAll, int deltaPct, int duration)
    {
        var effect = new StatusEffect(StatusEffectType.TempResist, duration, deltaPct, elementOrAll);
        effects.Add(effect);
        Debug.Log($"[StatusEffect] Added TempResist: {deltaPct}% for {elementOrAll}, {duration} turns");
    }
    
    // Legacy method - kept for compatibility but separated stun/DoT logic
    public int TickEffects()
    {
        // This now only ticks DoT, stun is handled separately via CheckAndConsumeStun
        return TickDoTEffects();
    }
    
    // Apply block reduction to incoming damage, returns reduced damage
    public int ApplyBlock(int damage)
    {
        var block = GetEffect(StatusEffectType.Block);
        if (block != null)
        {
            float reduction = block.Value / 100f;
            int reducedDamage = Mathf.RoundToInt(damage * (1f - reduction));
            Debug.Log($"[StatusEffect] Block reduced damage from {damage} to {reducedDamage} ({block.Value}% reduction)");
            
            // Block is consumed after use
            RemoveEffect(StatusEffectType.Block);
            return reducedDamage;
        }
        return damage;
    }
    
    // Clear all effects (used when combat ends or world changes)
    public void ClearAll()
    {
        effects.Clear();
    }
    
    // Clear non-persistent effects (Shield persists over combats)
    public void ClearCombatEffects()
    {
        effects.RemoveAll(e => e.Type != StatusEffectType.Shield);
    }
    
    // Clear debuffs only (keep shield and positive effects)
    public void ClearDebuffs()
    {
        effects.RemoveAll(e => e.Type == StatusEffectType.DoT || 
                               e.Type == StatusEffectType.Stun || 
                               e.Type == StatusEffectType.Block);
    }
    
    // Clear everything including Shield (for world transitions)
    public void ClearAllIncludingShield()
    {
        effects.Clear();
    }
    
    public bool IsStunned => HasEffect(StatusEffectType.Stun);
    
    public int GetShieldValue()
    {
        var shield = GetEffect(StatusEffectType.Shield);
        return shield != null ? Mathf.RoundToInt(shield.Value) : 0;
    }
    
    public void SetShield(int value, int maxShield)
    {
        var shield = GetEffect(StatusEffectType.Shield);
        if (shield != null)
        {
            shield.Value = Mathf.Min(value, maxShield);
        }
        else if (value > 0)
        {
            AddEffect(new StatusEffect(StatusEffectType.Shield, -1, Mathf.Min(value, maxShield), "Shield"));
        }
    }
    
    public void AddShield(int amount, int maxShield)
    {
        int current = GetShieldValue();
        SetShield(current + amount, maxShield);
    }
    
    // Damage shield first, returns remaining damage to apply to health
    public int DamageShield(int damage)
    {
        var shield = GetEffect(StatusEffectType.Shield);
        if (shield == null || shield.Value <= 0)
            return damage;
        
        int shieldValue = Mathf.RoundToInt(shield.Value);
        if (damage >= shieldValue)
        {
            // Shield broken
            int remaining = damage - shieldValue;
            Debug.Log($"[StatusEffect] Shield absorbed {shieldValue} damage and broke. {remaining} damage passes through.");
            RemoveEffect(StatusEffectType.Shield);
            return remaining;
        }
        else
        {
            // Shield absorbs all damage
            shield.Value -= damage;
            Debug.Log($"[StatusEffect] Shield absorbed {damage} damage. {shield.Value} shield remaining.");
            return 0;
        }
    }
}
