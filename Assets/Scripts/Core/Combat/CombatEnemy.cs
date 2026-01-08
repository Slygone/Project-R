using System.Collections.Generic;
using System.Linq;

public class CombatEnemy
{
    public string Name;
    public int Health;
    public int MaxHealth;
    public int Damage; // Base damage value
    public int EnemyID;
    public int BaseResistance;
    public int BonusResistance;
    public Element Affinity;
    public bool IsBoss;
    public bool IsElite;
    
    // Rewards from JSON
    public int RewardXP;
    public int RewardGoldMin;
    public int RewardGoldMax;
    public int SigilChance;
    public int RelicChance;
    
    // Damage variance range (applied each attack)
    private const float VARIANCE_MIN = 0.90f;
    private const float VARIANCE_MAX = 1.10f;
    
    // Status effects (DoT, Stun)
    private StatusEffectManager statusEffects = new StatusEffectManager();
    
    // Elemental Mark System
    // Tracks marks by element: 6 of same OR 3+3 of different triggers reaction
    private Dictionary<Element, int> elementalMarks = new Dictionary<Element, int>();
    private const int MARKS_FOR_SINGLE_REACTION = 6;
    private const int MARKS_FOR_DUAL_REACTION = 3;

    public CombatEnemy(EnemyData data) : this(data, 1) { }
    
    public CombatEnemy(EnemyData data, int world)
    {
        Name = data.DisplayName;
        MaxHealth = data.GetHealth(world);
        Health = MaxHealth;
        Damage = data.GetDamage(world);
        EnemyID = data.EnemyID;
        BaseResistance = data.GetBaseResistance(world);
        BonusResistance = data.BonusResistance;
        IsBoss = data.IsBoss;
        IsElite = data.IsElite;
        
        // Load rewards from JSON
        RewardXP = data.RewardXP;
        RewardGoldMin = data.RewardGoldMin;
        RewardGoldMax = data.RewardGoldMax;
        SigilChance = data.SigilChance;
        RelicChance = data.RelicChance;
        
        if (data.IsBoss)
        {
            Affinity = Element.None;
        }
        else
        {
            Affinity = GetRandomElement();
        }
    }
    
    // Roll damage with variance (0.90-1.10) applied each attack
    public int RollDamageWithVariance()
    {
        float variance = UnityEngine.Random.Range(VARIANCE_MIN, VARIANCE_MAX);
        return UnityEngine.Mathf.RoundToInt(Damage * variance);
    }

    private Element GetRandomElement()
    {
        var elements = new Element[] { Element.Fire, Element.Ice, Element.Water, Element.Wind, Element.Rock };
        return elements[UnityEngine.Random.Range(0, elements.Length)];
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health < 0) Health = 0;
    }

    public int CalculateResistance(Element attackerAffinity)
    {
        int totalResistance = BaseResistance;
        
        if (Affinity != Element.None && attackerAffinity == Affinity)
        {
            totalResistance += BonusResistance;
        }
        
        return totalResistance;
    }

    public int ApplyResistance(int damage, Element attackerAffinity)
    {
        int resistance = CalculateResistance(attackerAffinity);
        float multiplier = 1f - (resistance / 100f);
        if (multiplier < 0f) multiplier = 0f;
        return UnityEngine.Mathf.RoundToInt(damage * multiplier);
    }

    public bool IsAlive() => Health > 0;
    
    // ========== STATUS EFFECTS ==========
    
    public bool IsStunned => statusEffects.IsStunned;
    
    public void ApplyStun(int duration)
    {
        statusEffects.AddEffect(new StatusEffect(StatusEffectType.Stun, duration, 0, "Stun"));
        GameLog.Status(GameLog.Join(
            "Apply",
            GameLog.KV("target", Name),
            GameLog.KV("type", "Stun"),
            GameLog.KV("value", 1),
            GameLog.KV("dur", duration)
        ), GameLogVerbosity.Minimal);
    }
    
    public void ApplyDoT(int damagePerTurn, int duration, string source)
    {
        statusEffects.AddEffect(new StatusEffect(StatusEffectType.DoT, duration, damagePerTurn, source));
        GameLog.Status(GameLog.Join(
            "Apply",
            GameLog.KV("target", Name),
            GameLog.KV("type", "DoT"),
            GameLog.KV("value", damagePerTurn),
            GameLog.KV("dur", duration),
            GameLog.KV("source", source)
        ), GameLogVerbosity.Minimal);
    }
    
    /// <summary>
    /// Check if enemy is stunned and consume the stun turn.
    /// Call at START of enemy turn BEFORE any actions.
    /// Returns true if stunned (should skip turn), false if not stunned.
    /// </summary>
    public bool CheckAndConsumeStun()
    {
        bool wasStunned = statusEffects.CheckAndConsumeStun();
        return wasStunned;
    }
    
    /// <summary>
    /// Tick DoT effects and apply damage.
    /// Call AFTER stun check - DoT still ticks even when stunned.
    /// Returns total DoT damage dealt.
    /// </summary>
    public int TickDoTEffects()
    {
        int dotDamage = statusEffects.TickDoTEffects();
        if (dotDamage > 0)
        {
            TakeDamage(dotDamage);
        }
        return dotDamage;
    }
    
    // Legacy method - kept for compatibility
    public int TickStatusEffects()
    {
        return TickDoTEffects();
    }
    
    public System.Collections.Generic.List<StatusEffect> GetStatusEffects() => statusEffects.GetAllEffects();
    
    // Tick all DoTs instantly without consuming duration (for reaction effects)
    public int TickDoTEffectsInstant()
    {
        int dotDamage = statusEffects.TickDoTEffectsInstant();
        if (dotDamage > 0)
        {
            TakeDamage(dotDamage);
        }
        return dotDamage;
    }
    
    // Apply temporary resistance modifier to all elements
    public void ApplyTempResistAll(int deltaPct, int duration)
    {
        statusEffects.AddTempResist("All", deltaPct, duration);
    }
    
    // Apply temporary resistance modifier to a specific element
    public void ApplyTempResist(Element element, int deltaPct, int duration)
    {
        statusEffects.AddTempResist(element.ToString(), deltaPct, duration);
    }
    
    // Get total temp resist for an element (includes "All" effects)
    public int GetTempResist(Element element)
    {
        return statusEffects.GetTempResist(element.ToString());
    }
    
    // Tick temp resist durations at end of turn
    public void TickTempResists()
    {
        statusEffects.TickTempResists();
    }
    
    // ========== ELEMENTAL MARK SYSTEM ==========
    
    /// <summary>
    /// Add elemental marks to this enemy. Returns true if a reaction was triggered.
    /// </summary>
    public bool AddMarks(Element element, int count)
    {
        if (element == Element.None || count <= 0) return false;
        
        if (!elementalMarks.ContainsKey(element))
        {
            elementalMarks[element] = 0;
        }
        elementalMarks[element] += count;
        
        GameLog.Combat(GameLog.Join(
            "MarkApplied",
            GameLog.KV("target", Name),
            GameLog.KV("element", element),
            GameLog.KV("count", count),
            GameLog.KV("total", elementalMarks[element])
        ), GameLogVerbosity.Verbose);
        
        return CheckReactionTrigger();
    }
    
    /// <summary>
    /// Check if marks meet reaction trigger conditions.
    /// Returns true if 6 of same element OR 3+3 of two different elements.
    /// </summary>
    public bool CheckReactionTrigger()
    {
        // Check for 6 of same element
        foreach (var kvp in elementalMarks)
        {
            if (kvp.Value >= MARKS_FOR_SINGLE_REACTION)
            {
                return true;
            }
        }
        
        // Check for 3+3 of two different elements
        var elementsWithThreeOrMore = elementalMarks.Where(kvp => kvp.Value >= MARKS_FOR_DUAL_REACTION).ToList();
        if (elementsWithThreeOrMore.Count >= 2)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get reaction info if triggered. Returns null if no reaction.
    /// </summary>
    public ReactionTriggerInfo GetReactionTriggerInfo()
    {
        // Check for 6 of same element (single element reaction)
        foreach (var kvp in elementalMarks)
        {
            if (kvp.Value >= MARKS_FOR_SINGLE_REACTION)
            {
                return new ReactionTriggerInfo
                {
                    IsSingleElement = true,
                    PrimaryElement = kvp.Key,
                    SecondaryElement = Element.None,
                    MarksConsumed = MARKS_FOR_SINGLE_REACTION
                };
            }
        }
        
        // Check for 3+3 of two different elements (dual element reaction)
        var elementsWithThreeOrMore = elementalMarks
            .Where(kvp => kvp.Value >= MARKS_FOR_DUAL_REACTION)
            .OrderByDescending(kvp => kvp.Value)
            .ToList();
            
        if (elementsWithThreeOrMore.Count >= 2)
        {
            return new ReactionTriggerInfo
            {
                IsSingleElement = false,
                PrimaryElement = elementsWithThreeOrMore[0].Key,
                SecondaryElement = elementsWithThreeOrMore[1].Key,
                MarksConsumed = MARKS_FOR_DUAL_REACTION * 2
            };
        }
        
        return null;
    }
    
    /// <summary>
    /// Consume marks after reaction triggers.
    /// </summary>
    public void ConsumeMarksForReaction(ReactionTriggerInfo info)
    {
        if (info == null) return;
        
        if (info.IsSingleElement)
        {
            // Consume 6 marks of the single element
            if (elementalMarks.ContainsKey(info.PrimaryElement))
            {
                elementalMarks[info.PrimaryElement] -= MARKS_FOR_SINGLE_REACTION;
                if (elementalMarks[info.PrimaryElement] <= 0)
                {
                    elementalMarks.Remove(info.PrimaryElement);
                }
            }
        }
        else
        {
            // Consume 3 marks from each element
            if (elementalMarks.ContainsKey(info.PrimaryElement))
            {
                elementalMarks[info.PrimaryElement] -= MARKS_FOR_DUAL_REACTION;
                if (elementalMarks[info.PrimaryElement] <= 0)
                {
                    elementalMarks.Remove(info.PrimaryElement);
                }
            }
            if (elementalMarks.ContainsKey(info.SecondaryElement))
            {
                elementalMarks[info.SecondaryElement] -= MARKS_FOR_DUAL_REACTION;
                if (elementalMarks[info.SecondaryElement] <= 0)
                {
                    elementalMarks.Remove(info.SecondaryElement);
                }
            }
        }
        
        GameLog.Combat(GameLog.Join(
            "MarksConsumed",
            GameLog.KV("target", Name),
            GameLog.KV("type", info.IsSingleElement ? "single" : "dual"),
            GameLog.KV("primary", info.PrimaryElement),
            GameLog.KV("secondary", info.SecondaryElement)
        ), GameLogVerbosity.Verbose);
    }
    
    /// <summary>
    /// Get current marks for display.
    /// </summary>
    public Dictionary<Element, int> GetMarks() => new Dictionary<Element, int>(elementalMarks);
    
    /// <summary>
    /// Get total mark count across all elements.
    /// </summary>
    public int GetTotalMarkCount() => elementalMarks.Values.Sum();
    
    /// <summary>
    /// Clear all marks (e.g., on combat end).
    /// </summary>
    public void ClearAllMarks()
    {
        elementalMarks.Clear();
    }
}

/// <summary>
/// Info about a triggered reaction.
/// </summary>
public class ReactionTriggerInfo
{
    public bool IsSingleElement;
    public Element PrimaryElement;
    public Element SecondaryElement;
    public int MarksConsumed;
    
    public string GetReactionId()
    {
        if (IsSingleElement)
        {
            // Single element reactions (6 of same)
            return $"{PrimaryElement}_{PrimaryElement}";
        }
        else
        {
            // Dual element reactions (3+3)
            // Sort alphabetically to ensure consistent reaction IDs
            var elements = new[] { PrimaryElement.ToString(), SecondaryElement.ToString() };
            System.Array.Sort(elements);
            return $"{elements[0]}_{elements[1]}";
        }
    }
}
