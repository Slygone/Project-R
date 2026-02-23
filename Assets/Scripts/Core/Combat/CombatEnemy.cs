using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Tracks what the player did on their last turn, used by reactive bosses (MirrorBoss).
/// </summary>
public enum PlayerLastAction
{
    Attack,
    Shield,
    Reaction
}

public class CombatEnemy
{
    public string Name;
    public int Health;
    public int MaxHealth;
    public int Damage; // Base damage value (can be modified by reborn bonus)
    public int BaseDamage; // Original base damage (before reborn)
    public int EnemyID;
    public int PhysicalResist;
    public int ElementalResist;
    public Element DamageElement; // What type of damage this enemy deals (None = physical)
    public int DamageMin; // For tooltip display
    public int DamageMax; // For tooltip display
    public bool IsBoss;
    public bool IsElite;
    
    // Rewards from JSON
    public int RewardXP;
    public int RewardGoldMin;
    public int RewardGoldMax;
    public int SigilChance;
    public int RelicChance;
    public int RegularCoreChance;
    public int AscendedCoreChance;
    
    // Skill system
    public string Skill1Id;
    public string Skill2Id;
    public int Skill2Cooldown;      // Max cooldown from data
    public int Skill2CurrentCD;     // Current cooldown remaining (0 = ready)
    public string Skill3Id;
    public int Skill3Cooldown;
    public int Skill3CurrentCD;
    public string Skill4Id;
    
    // Attack pattern (1-indexed skill numbers, repeating cycle)
    public int[] AttackPattern;
    private int patternIndex;
    private int turnCount;
    
    // Enemy shield (absorbed before health damage)
    public int Shield;
    
    // Granite Bastion stacks (StoneColossus: each stack adds 10% to Crush)
    public int GraniteStacks;
    
    // Frost Shield state
    public bool FrostShieldActive;
    public int FrostShieldResistBonus;      // resistance % bonus while active
    public int FrostShieldBreakThreshold;   // damage needed to break (% of max health)
    public float FrostShieldBreakDamageMult; // damage multiplier on break
    public int FrostShieldDamageTaken;      // damage accumulated this turn
    
    // Retaliation state
    public bool RetaliationActive;
    public float RetaliationDamageMult;     // damage multiplier for counter-attacks
    
    // Mark block state (Null Sigil)
    public int MarkBlockTurns;              // turns remaining where marks are blocked
    public float MarkBlockDamageMult;       // damage per mark blocked
    public int MarksBlocked;                // count of marks blocked this combat
    
    // HoT (heal over time) - from Monsoon
    public int HoTAmount;                   // heal per turn (flat or % based on magnitude)
    public bool HoTActive;
    
    // Damage buff - from Battle Shout / status_empowered
    public float DamageBuffPercent;
    public int DamageBuffTurns;
    
    // Boss: Split mechanic (SlimeBoss)
    public float SplitThreshold;
    public string[] SplitInto;
    public int SplitHealthPercent;
    public bool HasSplit;
    public bool PendingSplit; // threshold reached, split deferred until End Turn
    
    // Boss: Reactive pattern (MirrorBoss)
    public bool ReactivePattern;
    public int ReactiveOnAttack;
    public int ReactiveOnShield;
    public int ReactiveOnReaction;
    
    // Boss: Reborn mechanic (FallenChampion)
    public int RebornHealthPercent;
    public float RebornDamageBonus;
    public int[] RebornPattern;
    public bool HasReborn;
    public bool IsReborn;
    
    // Spawn control
    public bool SpawnOnly;
    
    // Reference to the source EnemyData for spawning sub-enemies
    public EnemyData SourceData;
    
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
    
    // ========== REACTION DEBUFF TRACKING ==========
    // Freeze (separate from Stun per GDD)
    private int freezeTurns;
    public bool IsFrozen => freezeTurns > 0;
    
    // Weak (enemy deals less damage)
    private float weakPercent;   // e.g., 50 = 50% less damage
    private int weakTurns;
    public bool IsWeakened => weakTurns > 0;
    public float GetWeakMultiplier() => weakTurns > 0 ? 1f - (weakPercent / 100f) : 1f;
    
    // Shatter (accumulates damage, pops when threshold reached)
    private bool hasShatter;
    private int shatterThreshold;
    private int shatterPopDamage;
    private int shatterTurns;
    private int shatterAccumulated;
    private bool shatterRefreshable;
    
    // Named DoTs (Ignite, Magma Scorch, etc.)
    private Dictionary<string, NamedDoTState> namedDoTs = new Dictionary<string, NamedDoTState>();
    
    public class NamedDoTState
    {
        public string Name;
        public int DamagePerTurn;
        public int TurnsRemaining;
        public int MaxStacks;
        public int CurrentStacks;
        public bool Refreshable;
    }
    
    // Generic reaction debuffs (HealOnHit, Electrocute, Mudslide)
    private Dictionary<string, ReactionDebuffState> reactionDebuffs = new Dictionary<string, ReactionDebuffState>();
    
    public class ReactionDebuffState
    {
        public string Type;
        public float Value;
        public int TurnsRemaining;
        public bool Refreshable;
        public int CurrentStacks;
        public int MaxStacks;
        public float[] StackValues;
        public int DamageRange;
    }

    public CombatEnemy(EnemyData data) : this(data, 1) { }
    
    public CombatEnemy(EnemyData data, int world)
    {
        SourceData = data;
        Name = data.DisplayName;
        MaxHealth = data.GetHealth(world);
        Health = MaxHealth;
        Damage = data.GetDamage(world);
        BaseDamage = Damage;
        EnemyID = data.EnemyID;
        PhysicalResist = data.GetPhysicalResist(world);
        ElementalResist = data.GetElementalResist(world);
        DamageElement = ParseElement(data.DamageElement);
        IsBoss = data.IsBoss;
        IsElite = data.IsElite;
        SpawnOnly = data.SpawnOnly;
        
        // Load rewards from JSON
        RewardXP = data.RewardXP;
        RewardGoldMin = data.RewardGoldMin;
        RewardGoldMax = data.RewardGoldMax;
        SigilChance = data.SigilChance;
        RelicChance = data.RelicChance;
        RegularCoreChance = data.RegularCoreChance;
        AscendedCoreChance = data.AscendedCoreChance;
        
        // Load skill system
        Skill1Id = data.Skill1Id;
        Skill2Id = data.Skill2Id;
        Skill2Cooldown = data.Skill2Cooldown;
        Skill2CurrentCD = 0;
        Skill3Id = data.Skill3Id;
        Skill3Cooldown = data.Skill3Cooldown;
        Skill3CurrentCD = 0;
        Skill4Id = data.Skill4Id;
        
        // Attack pattern
        AttackPattern = data.AttackPattern;
        patternIndex = 0;
        turnCount = 0;
        
        // Boss mechanics
        SplitThreshold = data.SplitThreshold;
        SplitInto = data.SplitInto;
        SplitHealthPercent = data.SplitHealthPercent;
        HasSplit = false;
        PendingSplit = false;
        
        ReactivePattern = data.ReactivePattern;
        ReactiveOnAttack = data.ReactiveOnAttack;
        ReactiveOnShield = data.ReactiveOnShield;
        ReactiveOnReaction = data.ReactiveOnReaction;
        
        RebornHealthPercent = data.RebornHealthPercent;
        RebornDamageBonus = data.RebornDamageBonus;
        RebornPattern = data.RebornPattern;
        HasReborn = false;
        IsReborn = false;
        
        // DamageMin/DamageMax for tooltip display
        DamageMin = data.DamageMin;
        DamageMax = data.DamageMax;
    }
    
    // ========== SKILL SELECTION ==========
    
    /// <summary>
    /// Get the skill ID for the current turn based on attack pattern and cooldowns.
    /// For regular/elite: first turn uses skill1, then skill2 when off cooldown, else skill1.
    /// For bosses: follows attackPattern cycle. If pattern skill is on cooldown, uses skill1.
    /// </summary>
    public string GetNextSkillId()
    {
        turnCount++;
        
        // Bosses and enemies with explicit attack patterns
        if (AttackPattern != null && AttackPattern.Length > 0)
        {
            int skillNumber = AttackPattern[patternIndex % AttackPattern.Length];
            string skillId = GetSkillIdByNumber(skillNumber);
            
            // Check if this skill is on cooldown; if so, fall back to skill1
            if (IsSkillOnCooldown(skillNumber))
            {
                return Skill1Id;
            }
            
            return skillId ?? Skill1Id;
        }
        
        // Regular/Elite: first turn is always basic (skill1)
        if (turnCount == 1)
        {
            return Skill1Id;
        }
        
        // After first turn: use special (skill2) when off cooldown
        if (!string.IsNullOrEmpty(Skill2Id) && Skill2CurrentCD <= 0)
        {
            return Skill2Id;
        }
        
        // Fallback to basic
        return Skill1Id;
    }
    
    /// <summary>
    /// Advance the attack pattern index after using a skill.
    /// Also applies cooldown for the used skill.
    /// </summary>
    public void OnSkillUsed(string skillId)
    {
        // Advance pattern
        if (AttackPattern != null && AttackPattern.Length > 0)
        {
            patternIndex = (patternIndex + 1) % AttackPattern.Length;
        }
        
        // Apply cooldown for the used skill
        if (skillId == Skill2Id && Skill2Cooldown > 0)
        {
            Skill2CurrentCD = Skill2Cooldown;
        }
        else if (skillId == Skill3Id && Skill3Cooldown > 0)
        {
            Skill3CurrentCD = Skill3Cooldown;
        }
    }
    
    /// <summary>
    /// Tick down all skill cooldowns. Call at the start of each enemy turn.
    /// </summary>
    public void TickSkillCooldowns()
    {
        if (Skill2CurrentCD > 0) Skill2CurrentCD--;
        if (Skill3CurrentCD > 0) Skill3CurrentCD--;
    }
    
    /// <summary>
    /// Get skill ID by 1-indexed skill number.
    /// </summary>
    public string GetSkillIdByNumber(int skillNumber)
    {
        return skillNumber switch
        {
            1 => Skill1Id,
            2 => Skill2Id,
            3 => Skill3Id,
            4 => Skill4Id,
            _ => Skill1Id
        };
    }
    
    /// <summary>
    /// Check if a skill by number is on cooldown.
    /// </summary>
    public bool IsSkillOnCooldown(int skillNumber)
    {
        return skillNumber switch
        {
            2 => Skill2CurrentCD > 0,
            3 => Skill3CurrentCD > 0,
            _ => false // skill1 and skill4 have no cooldown
        };
    }
    
    /// <summary>
    /// Preview the next skill without advancing state. Used by intent UI.
    /// Simulates one cooldown tick (which happens before skill selection).
    /// Returns null for reactive bosses (skill depends on player action).
    /// </summary>
    public string PeekNextSkillId()
    {
        if (ReactivePattern) return null;
        
        int nextTurn = turnCount + 1;
        
        // Bosses and enemies with explicit attack patterns
        if (AttackPattern != null && AttackPattern.Length > 0)
        {
            int skillNumber = AttackPattern[patternIndex % AttackPattern.Length];
            string skillId = GetSkillIdByNumber(skillNumber);
            
            // Simulate cooldown tick: skill available if CD <= 1 (will be 0 after tick)
            if (IsSkillOnCooldownAfterTick(skillNumber))
            {
                return Skill1Id;
            }
            
            return skillId ?? Skill1Id;
        }
        
        // Regular/Elite: first turn is always basic (skill1)
        if (nextTurn == 1)
        {
            return Skill1Id;
        }
        
        // After first turn: use special (skill2) when off cooldown after tick
        if (!string.IsNullOrEmpty(Skill2Id) && Skill2CurrentCD <= 1)
        {
            return Skill2Id;
        }
        
        return Skill1Id;
    }
    
    /// <summary>
    /// Check if a skill by number would still be on cooldown after one tick.
    /// </summary>
    private bool IsSkillOnCooldownAfterTick(int skillNumber)
    {
        return skillNumber switch
        {
            2 => Skill2CurrentCD > 1,
            3 => Skill3CurrentCD > 1,
            _ => false
        };
    }
    
    /// <summary>
    /// For reactive bosses (MirrorBoss): select skill based on player's last action.
    /// </summary>
    public string GetReactiveSkillId(PlayerLastAction lastAction)
    {
        if (!ReactivePattern) return GetNextSkillId();
        
        turnCount++;
        int skillNumber = lastAction switch
        {
            PlayerLastAction.Attack => ReactiveOnAttack,
            PlayerLastAction.Shield => ReactiveOnShield,
            PlayerLastAction.Reaction => ReactiveOnReaction,
            _ => ReactiveOnAttack
        };
        
        string skillId = GetSkillIdByNumber(skillNumber);
        string resolvedId = skillId ?? Skill1Id;
        GameLog.Combat(GameLog.Join(
            "ReactiveSkill",
            GameLog.KV("enemy", Name),
            GameLog.KV("playerAction", lastAction.ToString()),
            GameLog.KV("skillNum", skillNumber),
            GameLog.KV("skillId", resolvedId ?? "null"),
            GameLog.KV("fallback", skillId == null)
        ));
        return resolvedId;
    }
    
    // ========== ENEMY SHIELD ==========
    
    public void AddShield(int amount)
    {
        Shield += amount;
    }
    
    /// <summary>
    /// Apply damage to this enemy, absorbing with shield first.
    /// Returns actual health damage dealt (after shield absorption).
    /// </summary>
    public int TakeDamageWithShield(int amount)
    {
        int shieldAbsorbed = 0;
        if (Shield > 0)
        {
            shieldAbsorbed = UnityEngine.Mathf.Min(Shield, amount);
            Shield -= shieldAbsorbed;
            amount -= shieldAbsorbed;
        }
        
        // Track damage for Frost Shield break mechanic
        if (FrostShieldActive)
        {
            FrostShieldDamageTaken += shieldAbsorbed + amount;
        }
        
        if (amount > 0)
        {
            Health -= amount;
            if (Health < 0) Health = 0;
        }
        
        return amount; // health damage after shield
    }
    
    // ========== BOSS MECHANICS ==========
    
    /// <summary>
    /// Check if SlimeBoss has crossed the split threshold and mark as pending.
    /// The actual split is deferred until the player presses End Turn.
    /// </summary>
    public void CheckSplitThreshold()
    {
        if (HasSplit || PendingSplit) return;
        if (SplitThreshold <= 0f) return;
        if (SplitInto == null || SplitInto.Length == 0) return;
        if ((float)Health / MaxHealth <= SplitThreshold)
        {
            PendingSplit = true;
        }
    }
    
    /// <summary>
    /// Returns true if the split threshold was reached and is awaiting End Turn.
    /// </summary>
    public bool IsPendingSplit => PendingSplit && !HasSplit;
    
    /// <summary>
    /// Mark this enemy as having split.
    /// </summary>
    public void MarkAsSplit()
    {
        HasSplit = true;
    }
    
    /// <summary>
    /// Check if FallenChampion should activate Reborn (on death, once per combat).
    /// </summary>
    public bool ShouldReborn()
    {
        if (HasReborn) return false;
        if (RebornHealthPercent <= 0) return false;
        return Health <= 0;
    }
    
    /// <summary>
    /// Activate Reborn: restore health, boost damage, switch attack pattern.
    /// </summary>
    public void ActivateReborn()
    {
        HasReborn = true;
        IsReborn = true;
        
        // Restore health
        Health = UnityEngine.Mathf.RoundToInt(MaxHealth * (RebornHealthPercent / 100f));
        
        // Boost damage
        Damage = UnityEngine.Mathf.RoundToInt(BaseDamage * (1f + RebornDamageBonus));
        
        // Switch to reborn attack pattern
        if (RebornPattern != null && RebornPattern.Length > 0)
        {
            AttackPattern = RebornPattern;
            patternIndex = 0;
        }
        
        // Remove Long Combo cooldown (skill3)
        Skill3CurrentCD = 0;
        
        GameLog.Combat(GameLog.Join(
            "Reborn",
            GameLog.KV("enemy", Name),
            GameLog.KV("health", Health),
            GameLog.KV("damage", Damage),
            GameLog.KV("pattern", RebornPattern != null ? string.Join(",", RebornPattern) : "none")
        ));
    }
    
    /// <summary>
    /// Check if Frost Shield should break (damage threshold reached).
    /// Returns break damage to deal to player, or 0 if no break.
    /// </summary>
    public int CheckFrostShieldBreak()
    {
        if (!FrostShieldActive) return 0;
        
        int breakPoint = UnityEngine.Mathf.RoundToInt(MaxHealth * (FrostShieldBreakThreshold / 100f));
        if (FrostShieldDamageTaken >= breakPoint)
        {
            FrostShieldActive = false;
            FrostShieldDamageTaken = 0;
            int breakDamage = UnityEngine.Mathf.RoundToInt(Damage * FrostShieldBreakDamageMult);
            return breakDamage;
        }
        return 0;
    }
    
    /// <summary>
    /// Activate Frost Shield with parameters from skill effect data.
    /// </summary>
    public void ActivateFrostShield(int resistBonus, int breakThreshold, float breakDamageMult)
    {
        FrostShieldActive = true;
        FrostShieldResistBonus = resistBonus;
        FrostShieldBreakThreshold = breakThreshold;
        FrostShieldBreakDamageMult = breakDamageMult;
        FrostShieldDamageTaken = 0;
    }
    
    /// <summary>
    /// Tick Frost Shield expiry (call at end of enemy turn).
    /// </summary>
    public void TickFrostShield()
    {
        if (FrostShieldActive)
        {
            FrostShieldActive = false;
            FrostShieldDamageTaken = 0;
        }
    }
    
    /// <summary>
    /// Tick Retaliation expiry at end of enemy turn.
    /// </summary>
    public void TickRetaliation()
    {
        RetaliationActive = false;
        RetaliationDamageMult = 0f;
    }
    
    /// <summary>
    /// Tick Mark Block turns. Call at start of enemy turn.
    /// </summary>
    public void TickMarkBlock()
    {
        if (MarkBlockTurns > 0) MarkBlockTurns--;
    }
    
    /// <summary>
    /// Tick HoT (heal over time). Call during enemy turn. Returns amount healed.
    /// </summary>
    public int TickHoT()
    {
        if (!HoTActive || HoTAmount <= 0) return 0;
        int healAmount = UnityEngine.Mathf.RoundToInt(MaxHealth * (HoTAmount / 100f));
        Health = UnityEngine.Mathf.Min(Health + healAmount, MaxHealth);
        return healAmount;
    }
    
    /// <summary>
    /// Tick damage buff duration. Call at start of enemy turn.
    /// </summary>
    public void TickDamageBuff()
    {
        if (DamageBuffTurns > 0)
        {
            DamageBuffTurns--;
            if (DamageBuffTurns <= 0)
            {
                DamageBuffPercent = 0f;
            }
        }
    }
    
    /// <summary>
    /// Get the effective damage multiplier including buffs.
    /// </summary>
    public float GetDamageMultiplier()
    {
        float mult = 1f;
        if (DamageBuffPercent > 0f)
        {
            mult += DamageBuffPercent / 100f;
        }
        return mult;
    }
    
    // Roll damage with variance (0.90-1.10) applied each attack
    public int RollDamageWithVariance()
    {
        float variance = UnityEngine.Random.Range(VARIANCE_MIN, VARIANCE_MAX);
        return UnityEngine.Mathf.RoundToInt(Damage * GetDamageMultiplier() * variance);
    }

    private static Element ParseElement(string elementStr)
    {
        if (string.IsNullOrEmpty(elementStr) || elementStr == "none") return Element.None;
        switch (elementStr.ToLower())
        {
            case "fire": return Element.Fire;
            case "ice": return Element.Ice;
            case "water": return Element.Water;
            case "wind": return Element.Wind;
            case "rock": return Element.Rock;
            case "lightning": return Element.Lightning;
            default: return Element.None;
        }
    }

    public void TakeDamage(int amount)
    {
        // Route through shield system if enemy has shield
        if (Shield > 0)
        {
            TakeDamageWithShield(amount);
            return;
        }
        
        // Track damage for Frost Shield break mechanic
        if (FrostShieldActive)
        {
            FrostShieldDamageTaken += amount;
        }
        
        Health -= amount;
        if (Health < 0) Health = 0;
    }

    /// <summary>
    /// Calculate total resistance against an attack. If attackElement is None, uses PhysicalResist.
    /// Otherwise uses ElementalResist. Frost Shield bonus applies to elemental only.
    /// </summary>
    public int CalculateResistance(Element attackElement)
    {
        bool isElemental = attackElement != Element.None;
        int totalResistance = isElemental ? ElementalResist : PhysicalResist;
        
        // Frost Shield resistance bonus (elemental only)
        if (FrostShieldActive && FrostShieldResistBonus > 0 && isElemental)
        {
            totalResistance += FrostShieldResistBonus;
        }
        
        // Temp resist from status effects (general buff)
        totalResistance += GetTempResist(attackElement);
        
        return totalResistance;
    }

    public int ApplyResistance(int damage, Element attackElement)
    {
        int resistance = CalculateResistance(attackElement);
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
    
    public void ApplySlow(int duration)
    {
        // TODO: Extend StatusEffectType to support Slow when needed
        GameLog.Status(GameLog.Join(
            "Apply",
            GameLog.KV("target", Name),
            GameLog.KV("type", "Slow"),
            GameLog.KV("dur", duration)
        ), GameLogVerbosity.Minimal);
    }
    
    public void ApplyWeak(int duration, float magnitude)
    {
        weakPercent = magnitude;
        weakTurns = duration;
        GameLog.Status(GameLog.Join(
            "Apply",
            GameLog.KV("target", Name),
            GameLog.KV("type", "Weak"),
            GameLog.KV("magnitude", magnitude),
            GameLog.KV("dur", duration)
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
    /// If mark block is active (Null Sigil), marks are blocked and counted for damage.
    /// </summary>
    public bool AddMarks(Element element, int count)
    {
        if (element == Element.None || count <= 0) return false;
        
        // Null Sigil: block marks and track them for damage
        if (MarkBlockTurns > 0)
        {
            MarksBlocked += count;
            GameLog.Combat(GameLog.Join(
                "MarkBlocked",
                GameLog.KV("target", Name),
                GameLog.KV("element", element),
                GameLog.KV("count", count),
                GameLog.KV("totalBlocked", MarksBlocked)
            ));
            return false;
        }
        
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
    /// monoThreshold: marks needed for single-element reaction (default 6).
    /// dualPerElement: marks needed per element for dual reaction (default 3).
    /// dualExtraTotal: extra marks needed total across both elements (default 0).
    /// </summary>
    public bool CheckReactionTrigger(int monoThreshold = 6, int dualPerElement = 3, int dualExtraTotal = 0)
    {
        // Check for mono reaction (e.g. 6 or 7 of same element)
        foreach (var kvp in elementalMarks)
        {
            if (kvp.Value >= monoThreshold)
            {
                return true;
            }
        }
        
        // Check for dual reaction (e.g. 3+3 or 2+2, with optional +1 extra total)
        var qualifying = elementalMarks
            .Where(kvp => kvp.Value >= dualPerElement)
            .OrderByDescending(kvp => kvp.Value)
            .ToList();
        if (qualifying.Count >= 2)
        {
            int totalAvailable = qualifying[0].Value + qualifying[1].Value;
            int totalNeeded = dualPerElement * 2 + dualExtraTotal;
            if (totalAvailable >= totalNeeded)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get reaction info if triggered. Returns null if no reaction.
    /// monoThreshold: marks needed for single-element reaction (default 6).
    /// dualPerElement: marks needed per element for dual reaction (default 3).
    /// dualExtraTotal: extra marks needed total across both elements (default 0).
    /// </summary>
    public ReactionTriggerInfo GetReactionTriggerInfo(int monoThreshold = 6, int dualPerElement = 3, int dualExtraTotal = 0)
    {
        // Check for mono reaction
        foreach (var kvp in elementalMarks)
        {
            if (kvp.Value >= monoThreshold)
            {
                return new ReactionTriggerInfo
                {
                    IsSingleElement = true,
                    PrimaryElement = kvp.Key,
                    SecondaryElement = Element.None,
                    MarksConsumed = monoThreshold
                };
            }
        }
        
        // Check for dual reaction
        var qualifying = elementalMarks
            .Where(kvp => kvp.Value >= dualPerElement)
            .OrderByDescending(kvp => kvp.Value)
            .ToList();
            
        if (qualifying.Count >= 2)
        {
            int totalAvailable = qualifying[0].Value + qualifying[1].Value;
            int totalNeeded = dualPerElement * 2 + dualExtraTotal;
            if (totalAvailable >= totalNeeded)
            {
                // Primary consumes dualPerElement, secondary consumes dualPerElement + dualExtraTotal
                return new ReactionTriggerInfo
                {
                    IsSingleElement = false,
                    PrimaryElement = qualifying[0].Key,
                    SecondaryElement = qualifying[1].Key,
                    MarksConsumed = totalNeeded
                };
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Consume marks after reaction triggers.
    /// dualPerElement: base marks consumed per element for dual (default 3).
    /// dualExtraTotal: extra marks consumed from secondary element (default 0).
    /// </summary>
    public void ConsumeMarksForReaction(ReactionTriggerInfo info, int dualPerElement = 3, int dualExtraTotal = 0)
    {
        if (info == null) return;
        
        if (info.IsSingleElement)
        {
            // Consume monoThreshold marks (stored in MarksConsumed)
            if (elementalMarks.ContainsKey(info.PrimaryElement))
            {
                elementalMarks[info.PrimaryElement] -= info.MarksConsumed;
                if (elementalMarks[info.PrimaryElement] <= 0)
                {
                    elementalMarks.Remove(info.PrimaryElement);
                }
            }
        }
        else
        {
            // Consume dualPerElement from primary, dualPerElement + dualExtraTotal from secondary
            if (elementalMarks.ContainsKey(info.PrimaryElement))
            {
                elementalMarks[info.PrimaryElement] -= dualPerElement;
                if (elementalMarks[info.PrimaryElement] <= 0)
                {
                    elementalMarks.Remove(info.PrimaryElement);
                }
            }
            if (elementalMarks.ContainsKey(info.SecondaryElement))
            {
                elementalMarks[info.SecondaryElement] -= (dualPerElement + dualExtraTotal);
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
    
    // ========== REACTION DEBUFF METHODS ==========
    
    /// <summary>
    /// Check if enemy has a named DoT with the given name.
    /// </summary>
    public bool HasDoT(string dotName)
    {
        return namedDoTs.ContainsKey(dotName) && namedDoTs[dotName].TurnsRemaining > 0;
    }
    
    /// <summary>
    /// Apply a named DoT (e.g., Ignite, Magma Scorch). Supports stacking and refresh.
    /// </summary>
    public void ApplyNamedDoT(string name, int damagePerTurn, int duration, int maxStacks, bool refreshable)
    {
        if (namedDoTs.TryGetValue(name, out var existing))
        {
            if (existing.CurrentStacks < maxStacks)
            {
                existing.CurrentStacks++;
                existing.DamagePerTurn = damagePerTurn; // Update to latest damage value
            }
            if (refreshable)
            {
                existing.TurnsRemaining = duration;
            }
        }
        else
        {
            namedDoTs[name] = new NamedDoTState
            {
                Name = name,
                DamagePerTurn = damagePerTurn,
                TurnsRemaining = duration,
                MaxStacks = maxStacks,
                CurrentStacks = 1,
                Refreshable = refreshable
            };
        }
    }
    
    /// <summary>
    /// Tick all named DoTs, dealing damage. Called at start of enemy turn.
    /// Returns total DoT damage dealt.
    /// </summary>
    public int TickNamedDoTs()
    {
        int totalDamage = 0;
        var toRemove = new List<string>();
        
        foreach (var kvp in namedDoTs)
        {
            var dot = kvp.Value;
            if (dot.TurnsRemaining <= 0) { toRemove.Add(kvp.Key); continue; }
            
            int dmg = dot.DamagePerTurn * dot.CurrentStacks;
            if (dmg > 0)
            {
                TakeDamage(dmg);
                totalDamage += dmg;
                GameLog.Status(GameLog.Join("DoTTick", GameLog.KV("target", Name), GameLog.KV("dot", dot.Name), GameLog.KV("dmg", dmg), GameLog.KV("stacks", dot.CurrentStacks)));
            }
            
            dot.TurnsRemaining--;
            if (dot.TurnsRemaining <= 0) toRemove.Add(kvp.Key);
        }
        
        foreach (var key in toRemove) namedDoTs.Remove(key);
        return totalDamage;
    }
    
    /// <summary>
    /// Apply Freeze debuff (separate from Stun per GDD). Enemy skips turn.
    /// </summary>
    public void ApplyFreeze(int duration)
    {
        freezeTurns = Mathf.Max(freezeTurns, duration);
        GameLog.Status(GameLog.Join("Apply", GameLog.KV("target", Name), GameLog.KV("type", "Freeze"), GameLog.KV("dur", duration)), GameLogVerbosity.Minimal);
    }
    
    /// <summary>
    /// Check if frozen and consume a freeze turn. Call at start of enemy turn.
    /// Returns true if frozen (should skip turn).
    /// </summary>
    public bool CheckAndConsumeFreeze()
    {
        if (freezeTurns > 0)
        {
            freezeTurns--;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Apply Shatter debuff. Accumulates damage taken; pops when threshold reached.
    /// Re-applying does NOT refresh if refreshable is false.
    /// </summary>
    public void ApplyShatter(int threshold, int popDamage, int duration, bool refreshable)
    {
        if (hasShatter && !refreshable) return; // Already has Shatter, don't refresh
        
        hasShatter = true;
        shatterThreshold = threshold;
        shatterPopDamage = popDamage;
        shatterTurns = duration;
        shatterAccumulated = 0;
        shatterRefreshable = refreshable;
        GameLog.Status(GameLog.Join("Apply", GameLog.KV("target", Name), GameLog.KV("type", "Shatter"), GameLog.KV("threshold", threshold), GameLog.KV("dur", duration)), GameLogVerbosity.Minimal);
    }
    
    /// <summary>
    /// Called when this enemy takes damage while Shatter is active.
    /// Returns pop damage if threshold reached, 0 otherwise.
    /// </summary>
    public int AccumulateShatterDamage(int damageDealt)
    {
        if (!hasShatter) return 0;
        
        shatterAccumulated += damageDealt;
        if (shatterAccumulated >= shatterThreshold)
        {
            // Pop!
            hasShatter = false;
            int pop = shatterPopDamage;
            GameLog.Status(GameLog.Join("ShatterPop", GameLog.KV("target", Name), GameLog.KV("accumulated", shatterAccumulated), GameLog.KV("threshold", shatterThreshold), GameLog.KV("popDmg", pop)));
            TakeDamage(pop);
            return pop;
        }
        return 0;
    }
    
    public bool HasShatter => hasShatter;
    
    /// <summary>
    /// Apply a generic reaction debuff (HealOnHit, etc.).
    /// </summary>
    public void ApplyReactionDebuff(string type, float value, int duration, bool refreshable)
    {
        if (reactionDebuffs.TryGetValue(type, out var existing))
        {
            if (refreshable) existing.TurnsRemaining = duration;
            existing.Value = value;
        }
        else
        {
            reactionDebuffs[type] = new ReactionDebuffState
            {
                Type = type,
                Value = value,
                TurnsRemaining = duration,
                Refreshable = refreshable
            };
        }
        GameLog.Status(GameLog.Join("Apply", GameLog.KV("target", Name), GameLog.KV("type", type), GameLog.KV("value", value), GameLog.KV("dur", duration)), GameLogVerbosity.Minimal);
    }
    
    /// <summary>
    /// Apply a stackable reaction debuff (Electrocute, Mudslide).
    /// </summary>
    public void ApplyReactionDebuff(string type, float value, int duration, bool refreshable, int maxStacks, float[] stackValues, int damageRange)
    {
        if (reactionDebuffs.TryGetValue(type, out var existing))
        {
            if (existing.CurrentStacks < existing.MaxStacks)
                existing.CurrentStacks++;
            if (refreshable) existing.TurnsRemaining = duration;
            existing.DamageRange = damageRange; // Update to latest damage range
        }
        else
        {
            reactionDebuffs[type] = new ReactionDebuffState
            {
                Type = type,
                Value = value,
                TurnsRemaining = duration,
                Refreshable = refreshable,
                CurrentStacks = 1,
                MaxStacks = maxStacks,
                StackValues = stackValues,
                DamageRange = damageRange
            };
        }
        GameLog.Status(GameLog.Join("Apply", GameLog.KV("target", Name), GameLog.KV("type", type), GameLog.KV("stacks", reactionDebuffs[type].CurrentStacks), GameLog.KV("dur", duration)), GameLogVerbosity.Minimal);
    }
    
    /// <summary>
    /// Get a reaction debuff state by type. Returns null if not present.
    /// </summary>
    public ReactionDebuffState GetReactionDebuff(string type)
    {
        return reactionDebuffs.TryGetValue(type, out var state) && state.TurnsRemaining > 0 ? state : null;
    }
    
    /// <summary>
    /// Tick all reaction debuffs at end of turn. Also ticks Weak, Freeze, Shatter.
    /// </summary>
    public void TickReactionDebuffs()
    {
        // Tick Weak
        if (weakTurns > 0)
        {
            weakTurns--;
            if (weakTurns <= 0) weakPercent = 0;
        }
        
        // Tick Shatter
        if (hasShatter)
        {
            shatterTurns--;
            if (shatterTurns <= 0)
            {
                hasShatter = false;
                shatterAccumulated = 0;
            }
        }
        
        // Tick generic reaction debuffs
        var toRemove = new List<string>();
        foreach (var kvp in reactionDebuffs)
        {
            kvp.Value.TurnsRemaining--;
            if (kvp.Value.TurnsRemaining <= 0) toRemove.Add(kvp.Key);
        }
        foreach (var key in toRemove) reactionDebuffs.Remove(key);
    }
    
    /// <summary>
    /// Clear all reaction debuffs (on combat end).
    /// </summary>
    public void ClearReactionDebuffs()
    {
        freezeTurns = 0;
        weakPercent = 0;
        weakTurns = 0;
        hasShatter = false;
        shatterAccumulated = 0;
        namedDoTs.Clear();
        reactionDebuffs.Clear();
    }
    
    // ========== UNIFIED STATUS DISPLAY ==========

    /// <summary>
    /// Returns all active statuses (debuffs + buffs) for StatusDisplayUI.
    /// Category and Description are resolved here from DataCache so the UI is fully data-driven.
    /// </summary>
    public System.Collections.Generic.List<ActiveStatusInfo> GetActiveStatuses()
    {
        var list = new System.Collections.Generic.List<ActiveStatusInfo>();

        // Debuffs
        if (IsFrozen)
            list.Add(MakeStatus("status_frozen", freezeTurns));
        if (IsWeakened)
            list.Add(MakeStatus("status_weaken", weakTurns, weakPercent));
        if (IsStunned)
            list.Add(MakeStatus("status_stun", statusEffects.GetEffect(StatusEffectType.Stun)?.Duration ?? 1));

        // DoTs (from StatusEffectManager) — use source as display name for uniqueness
        foreach (var eff in statusEffects.GetAllEffects())
        {
            if (eff.Type == StatusEffectType.DoT)
            {
                string dotName = !string.IsNullOrEmpty(eff.Source) ? eff.Source : "Burning";
                list.Add(MakeStatus("status_dot", eff.Duration, eff.Value, eff.StackCount, dotName));
            }
        }

        // Named DoTs (Ignite, Magma Scorch, etc.)
        foreach (var kvp in namedDoTs)
        {
            if (kvp.Value.TurnsRemaining > 0)
                list.Add(MakeStatus("status_dot", kvp.Value.TurnsRemaining, kvp.Value.DamagePerTurn, kvp.Value.CurrentStacks, kvp.Value.Name));
        }

        // Reaction debuffs (Shatter, HealOnHit, Electrocute, Mudslide)
        foreach (var kvp in reactionDebuffs)
        {
            if (kvp.Value.TurnsRemaining > 0)
            {
                string statusId = kvp.Value.Type switch
                {
                    "Shatter" => "status_shatter",
                    "HealOnHit" => "status_heal_on_hit",
                    "Electrocute" => "status_electrocute",
                    "Mudslide" => "status_mudslide",
                    _ => "status_weaken"
                };
                list.Add(MakeStatus(statusId, kvp.Value.TurnsRemaining, kvp.Value.Value, kvp.Value.CurrentStacks));
            }
        }

        // Resist down (from temp resists — negative values)
        int tempResist = statusEffects.GetTempResist("All");
        if (tempResist < 0)
            list.Add(MakeStatus("status_resist_down", 0, -tempResist));

        // Buffs
        if (FrostShieldActive)
            list.Add(MakeStatus("status_frost_shield", 1, FrostShieldResistBonus));
        if (RetaliationActive)
            list.Add(MakeStatus("status_retaliation", 1, RetaliationDamageMult * 100f));
        if (MarkBlockTurns > 0)
            list.Add(MakeStatus("status_mark_block", MarkBlockTurns));
        if (HoTActive && HoTAmount > 0)
            list.Add(MakeStatus("status_regenerating", 0, HoTAmount));
        if (DamageBuffTurns > 0 && DamageBuffPercent > 0f)
            list.Add(MakeStatus("status_empowered", DamageBuffTurns, DamageBuffPercent));
        if (GraniteStacks > 0)
            list.Add(MakeStatus("status_granite_bastion", 0, GraniteStacks));
        if (Shield > 0)
            list.Add(MakeStatus("status_riposte_stance", 0, Shield));

        return list;
    }

    /// <summary>
    /// Helper: creates an ActiveStatusInfo with Category and Description resolved from DataCache.
    /// </summary>
    private ActiveStatusInfo MakeStatus(string statusId, int duration, float magnitude = 0f, int stacks = 0, string displayNameOverride = null)
    {
        var def = DataCache.GetStatusDef(statusId);
        string displayName = displayNameOverride ?? (def != null ? def.Name : statusId);
        string category = def != null ? def.Category : "debuff";
        string description = def != null ? def.GetDescription(magnitude, duration, stacks) : displayName;
        return new ActiveStatusInfo(statusId, duration, magnitude, stacks, displayName, null, category, description);
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
