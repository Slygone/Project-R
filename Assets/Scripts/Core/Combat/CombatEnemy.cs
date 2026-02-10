using System.Collections.Generic;
using System.Linq;

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
    
    // Damage buff - from Battle Shout / status_enemy_damage_up
    public float DamageBuffPercent;
    public int DamageBuffTurns;
    
    // Boss: Split mechanic (SlimeBoss)
    public float SplitThreshold;
    public string[] SplitInto;
    public int SplitHealthPercent;
    public bool HasSplit;
    
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
        BaseResistance = data.GetBaseResistance(world);
        BonusResistance = data.BonusResistance;
        IsBoss = data.IsBoss;
        IsElite = data.IsElite;
        SpawnOnly = data.SpawnOnly;
        
        // Load rewards from JSON
        RewardXP = data.RewardXP;
        RewardGoldMin = data.RewardGoldMin;
        RewardGoldMax = data.RewardGoldMax;
        SigilChance = data.SigilChance;
        RelicChance = data.RelicChance;
        
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
        
        ReactivePattern = data.ReactivePattern;
        ReactiveOnAttack = data.ReactiveOnAttack;
        ReactiveOnShield = data.ReactiveOnShield;
        ReactiveOnReaction = data.ReactiveOnReaction;
        
        RebornHealthPercent = data.RebornHealthPercent;
        RebornDamageBonus = data.RebornDamageBonus;
        RebornPattern = data.RebornPattern;
        HasReborn = false;
        IsReborn = false;
        
        if (data.IsBoss)
        {
            Affinity = Element.None;
        }
        else
        {
            Affinity = GetRandomElement();
        }
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
        return skillId ?? Skill1Id;
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
    /// Check if SlimeBoss should split (health <= threshold).
    /// </summary>
    public bool ShouldSplit()
    {
        if (HasSplit) return false;
        if (SplitThreshold <= 0f) return false;
        if (SplitInto == null || SplitInto.Length == 0) return false;
        return (float)Health / MaxHealth <= SplitThreshold;
    }
    
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

    private Element GetRandomElement()
    {
        var elements = new Element[] { Element.Fire, Element.Ice, Element.Water, Element.Wind, Element.Rock, Element.Lightning };
        return elements[UnityEngine.Random.Range(0, elements.Length)];
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

    public int CalculateResistance(Element attackerAffinity)
    {
        int totalResistance = BaseResistance;
        
        if (Affinity != Element.None && attackerAffinity == Affinity)
        {
            totalResistance += BonusResistance;
        }
        
        // Frost Shield resistance bonus
        if (FrostShieldActive && FrostShieldResistBonus > 0)
        {
            totalResistance += FrostShieldResistBonus;
        }
        
        // Temp resist from status effects
        totalResistance += GetTempResist(attackerAffinity);
        
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
        // TODO: Extend StatusEffectType to support Weak when needed
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
