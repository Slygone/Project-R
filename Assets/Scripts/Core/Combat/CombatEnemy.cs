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
    
    // Damage variance range (applied each attack)
    private const float VARIANCE_MIN = 0.90f;
    private const float VARIANCE_MAX = 1.10f;
    
    // Status effects (DoT, Stun)
    private StatusEffectManager statusEffects = new StatusEffectManager();

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
        UnityEngine.Debug.Log($"[CombatEnemy] {Name} is stunned for {duration} turn(s)!");
    }
    
    public void ApplyDoT(int damagePerTurn, int duration, string source)
    {
        statusEffects.AddEffect(new StatusEffect(StatusEffectType.DoT, duration, damagePerTurn, source));
        UnityEngine.Debug.Log($"[CombatEnemy] {Name} has DoT applied: {damagePerTurn} damage/turn for {duration} turns from {source}");
    }
    
    /// <summary>
    /// Check if enemy is stunned and consume the stun turn.
    /// Call at START of enemy turn BEFORE any actions.
    /// Returns true if stunned (should skip turn), false if not stunned.
    /// </summary>
    public bool CheckAndConsumeStun()
    {
        bool wasStunned = statusEffects.CheckAndConsumeStun();
        if (wasStunned)
        {
            UnityEngine.Debug.Log($"[CombatEnemy] {Name} is STUNNED and skips their turn!");
        }
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
            UnityEngine.Debug.Log($"[CombatEnemy] {Name} took {dotDamage} DoT damage. Health: {Health}/{MaxHealth}");
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
            UnityEngine.Debug.Log($"[CombatEnemy] {Name} took {dotDamage} instant DoT damage. Health: {Health}/{MaxHealth}");
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
}
