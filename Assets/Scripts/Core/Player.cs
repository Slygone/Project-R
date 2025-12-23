using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private int health;
    private int maxHealth;
    private int baseDamage;
    private int gold;
    private int energy;
    private int maxEnergy;
    private int critChance;
    private float critDamage;
    private int baseResistance;
    private int bonusResistance;
    private int tempCritChanceBonus = 0;
    private int tempCritDamageBonus = 0;
    
    // Wound/Threshold System (Phase 2)
    private const int THRESHOLD_SIZE = 50;
    private int lowestThresholdLevel = -1; // -1 means not initialized yet
    public int WoundCount => lowestThresholdLevel >= 0 ? (maxHealth / THRESHOLD_SIZE) - lowestThresholdLevel : 0;
    public int ThresholdSize => THRESHOLD_SIZE;
    private List<RelicData> relics = new List<RelicData>();
    private List<PotionData> potionInventory = new List<PotionData>();
    private const int MAX_POTIONS = 4;
    private ElementalDamage elementalDamage = new ElementalDamage();
    private Element affinity = Element.None;
    private CharacterData selectedCharacter = null;
    
    private ElementalOrbSystem orbSystem = new ElementalOrbSystem();
    private bool hasElementPair = false;
    
    // Skill cooldown tracking (index 0 = Skill1, 1 = Skill2, 2 = Skill3)
    private int[] skillCooldowns = new int[3];
    private int combatTurnCount = 0;
    
    // Status effects (Shield, Block)
    private StatusEffectManager statusEffects = new StatusEffectManager();
    
    // DirtyStab consecutive use tracking
    private int dirtyStabConsecutiveUses = 0;

    void Awake()
    {
        // Ensure DataCache is loaded
        if (!DataCache.IsLoaded)
        {
            DataCache.LoadAll();
        }
        
        var stats = DataCache.PlayerStats;
        if (stats == null)
        {
            GameLog.Error(
                GameLogCategory.Data,
                "[Player]",
                GameLog.Join(
                    "InitFail",
                    GameLog.KV("reason", "PlayerStatsNull")
                )
            );
            return;
        }
        
        maxHealth = stats.MaxHealth;
        health = maxHealth;
        baseDamage = stats.Damage;
        gold = stats.Gold;
        maxEnergy = stats.MaxEnergy;
        energy = maxEnergy;
        critChance = stats.CritChance;
        critDamage = stats.CritDamage;
        baseResistance = stats.BaseResistance;
        bonusResistance = stats.BonusResistance;

        GameLog.System(GameLog.Join(
            "PlayerInit",
            GameLog.KV("hp", $"{health}/{maxHealth}"),
            GameLog.KV("gold", gold),
            GameLog.KV("energy", $"{energy}/{maxEnergy}"),
            GameLog.KV("crit", $"{critChance}%x{critDamage}"),
            GameLog.KV("resist", $"{baseResistance}+{bonusResistance}")
        ), GameLogVerbosity.Verbose);
    }

    // Damage variance range (applied each attack)
    private const float VARIANCE_MIN = 0.90f;
    private const float VARIANCE_MAX = 1.10f;
    
    public int GetHealth() => health;
    public int GetCurrentHealth() => health;
    public int GetMaxHealth() => maxHealth;
    public int GetCharacterDamage() => selectedCharacter != null ? selectedCharacter.Damage : 0;
    public int GetDamageMin() => Mathf.RoundToInt(GetCharacterDamage() * VARIANCE_MIN);
    public int GetDamageMax() => Mathf.RoundToInt(GetCharacterDamage() * VARIANCE_MAX);
    
    // Apply variance roll to any damage value (0.90-1.10)
    public int ApplyVariance(float damage)
    {
        float variance = Random.Range(VARIANCE_MIN, VARIANCE_MAX);
        return Mathf.RoundToInt(damage * variance);
    }
    
    public int GetTotalDamage()
    {
        // If player has an orb pair selected, apply dual elemental bonuses
        if (hasElementPair && orbSystem != null)
        {
            int a = elementalDamage.Get(orbSystem.OrbAElement);
            int b = elementalDamage.Get(orbSystem.OrbBElement);
            return GetCharacterDamage() + a + b;
        }
        // Legacy single-affinity bonus
        return GetCharacterDamage() + GetAffinityBonus();
    }
    public int GetAffinityBonus() => affinity != Element.None ? elementalDamage.Get(affinity) : 0;
    
    public (int damage, bool isCrit) CalculateDamageWithCrit(int baseDamage)
    {
        int totalCritChance = GetCritChance();
        bool isCrit = Random.Range(0, 100) < totalCritChance;
        
        if (isCrit)
        {
            float critMultiplier = GetCritDamage();
            int critDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
            return (critDamage, true);
        }
        
        return (baseDamage, false);
    }
    public int GetElementalBonus(Element element) => elementalDamage.Get(element);
    public ElementalDamage GetElementalDamage() => elementalDamage;
    public Element GetAffinity() => affinity;
    public bool HasAffinity() => affinity != Element.None;
    public CharacterData GetCharacter() => selectedCharacter;
    public bool HasCharacter() => selectedCharacter != null;
    public int GetGold() => gold;
    public int GetEnergy() => energy;
    public int GetMaxEnergy() => maxEnergy;
    public int GetCritChance() => critChance + tempCritChanceBonus;
    public float GetCritDamage() => critDamage + (tempCritDamageBonus / 100f);
    public int GetBaseResistance() => baseResistance;
    public int GetBonusResistance() => bonusResistance;

    public int CalculateResistance(Element attackerAffinity)
    {
        int totalResistance = baseResistance;
        
        if (hasElementPair && orbSystem != null)
        {
            if (attackerAffinity == orbSystem.OrbAElement || attackerAffinity == orbSystem.OrbBElement)
            {
                totalResistance += bonusResistance;
            }
        }
        else if (affinity != Element.None && attackerAffinity == affinity)
        {
            totalResistance += bonusResistance;
        }
        
        return totalResistance;
    }

    public int ApplyResistance(int damage, Element attackerAffinity)
    {
        int resistance = CalculateResistance(attackerAffinity);
        float multiplier = 1f - (resistance / 100f);
        if (multiplier < 0f) multiplier = 0f;
        return Mathf.RoundToInt(damage * multiplier);
    }

    // Stores info about last damage taken for floating text
    public struct DamageInfo
    {
        public int originalDamage;
        public int blockedDamage;
        public int shieldAbsorbed;
        public bool shieldBroken;
        public int finalDamage;
    }
    
    private DamageInfo lastDamageInfo;
    public DamageInfo GetLastDamageInfo() => lastDamageInfo;
    
    public void TakeDamage(int amount)
    {
        int originalAmount = amount;
        lastDamageInfo = new DamageInfo { originalDamage = originalAmount };
        
        // Apply block reduction first
        int afterBlock = statusEffects.ApplyBlock(amount);
        lastDamageInfo.blockedDamage = amount - afterBlock;
        amount = afterBlock;
        
        // Then absorb with shield
        int shieldBefore = GetShield();
        amount = statusEffects.DamageShield(amount);
        int shieldAfter = GetShield();
        lastDamageInfo.shieldAbsorbed = shieldBefore - shieldAfter;
        lastDamageInfo.shieldBroken = shieldBefore > 0 && shieldAfter == 0;
        
        if (amount <= 0)
        {
            lastDamageInfo.finalDamage = 0;
            GameLog.Combat(GameLog.Join(
                "DamageApply",
                GameLog.KV("target", "Player"),
                GameLog.KV("amount", 0),
                GameLog.KV("incoming", originalAmount),
                GameLog.KV("blocked", lastDamageInfo.blockedDamage),
                GameLog.KV("shieldAbsorb", lastDamageInfo.shieldAbsorbed),
                GameLog.KV("shieldBroken", lastDamageInfo.shieldBroken)
            ), GameLogVerbosity.Verbose);
            return;
        }
        
        lastDamageInfo.finalDamage = amount;
        health -= amount;
        if (health < 0) health = 0;
        
        // Track lowest threshold level reached (Phase 2)
        int currentThresholdLevel = health / THRESHOLD_SIZE;
        
        // Initialize on first damage or update if we dropped lower
        if (lowestThresholdLevel < 0 || currentThresholdLevel < lowestThresholdLevel)
        {
            int oldLowest = lowestThresholdLevel;
            lowestThresholdLevel = currentThresholdLevel;
            if (oldLowest >= 0)
            {
                GameLog.Status(GameLog.Join(
                    "Threshold",
                    GameLog.KV("who", "Player"),
                    GameLog.KV("level", currentThresholdLevel),
                    GameLog.KV("maxRecoverable", GetMaxRecoverableHP()),
                    GameLog.KV("wounds", WoundCount)
                ), GameLogVerbosity.Verbose);
            }
        }

        GameLog.Combat(GameLog.Join(
            "DamageApply",
            GameLog.KV("target", "Player"),
            GameLog.KV("amount", amount),
            GameLog.KV("incoming", originalAmount),
            GameLog.KV("blocked", lastDamageInfo.blockedDamage),
            GameLog.KV("shieldAbsorb", lastDamageInfo.shieldAbsorbed),
            GameLog.KV("shieldBroken", lastDamageInfo.shieldBroken),
            GameLog.KV("hpAfter", $"{health}/{maxHealth}")
        ), GameLogVerbosity.Verbose);
    }

    public void Heal(int amount)
    {
        // Calculate max recoverable HP based on lowest threshold reached (Phase 2)
        int maxRecoverable = GetMaxRecoverableHP();
        
        health += amount;
        
        // Clamp to max recoverable (can't heal past the threshold you dropped below)
        if (health > maxRecoverable)
        {
            health = maxRecoverable;
        }
        if (health > maxHealth) health = maxHealth;

        GameLog.Combat(GameLog.Join(
            "Heal",
            GameLog.KV("who", "Player"),
            GameLog.KV("amount", amount),
            GameLog.KV("hpAfter", $"{health}/{maxHealth}"),
            GameLog.KV("maxRecoverable", maxRecoverable),
            GameLog.KV("wounds", WoundCount)
        ), GameLogVerbosity.Verbose);
    }
    
    // Get max HP player can recover to (limited by lowest threshold reached)
    public int GetMaxRecoverableHP()
    {
        if (lowestThresholdLevel < 0)
        {
            // Not initialized yet, return max health
            return maxHealth;
        }
        // Can heal up to the boundary of the next threshold above the lowest reached
        int maxRecoverable = (lowestThresholdLevel + 1) * THRESHOLD_SIZE;
        return Mathf.Min(maxRecoverable, maxHealth);
    }
    
    // Clear all wounds (used by Rest node healing)
    public void ClearWounds()
    {
        lowestThresholdLevel = -1;
        GameLog.Status(GameLog.Join(
            "WoundsClear",
            GameLog.KV("who", "Player"),
            GameLog.KV("maxRecoverable", GetMaxRecoverableHP())
        ), GameLogVerbosity.Verbose);
    }
    
    // Heal and clear wounds (full rest)
    public void FullRest(int healPercent)
    {
        int healAmount = Mathf.RoundToInt(maxHealth * (healPercent / 100f));
        health += healAmount;
        if (health > maxHealth) health = maxHealth;
        lowestThresholdLevel = -1;
        GameLog.System(GameLog.Join(
            "FullRest",
            GameLog.KV("heal", healAmount),
            GameLog.KV("hpAfter", $"{health}/{maxHealth}"),
            GameLog.KV("maxRecoverable", GetMaxRecoverableHP())
        ), GameLogVerbosity.Verbose);
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        health += amount;
        GameLog.System(GameLog.Join(
            "StatGain",
            GameLog.KV("stat", "MaxHealth"),
            GameLog.KV("delta", amount),
            GameLog.KV("hp", $"{health}/{maxHealth}")
        ), GameLogVerbosity.Verbose);
    }

    public void IncreaseCharacterDamage(int amount)
    {
        if (selectedCharacter != null)
        {
            selectedCharacter.Damage += amount;
            GameLog.System(GameLog.Join(
                "StatGain",
                GameLog.KV("stat", "CharacterDamage"),
                GameLog.KV("delta", amount),
                GameLog.KV("now", selectedCharacter.Damage)
            ), GameLogVerbosity.Verbose);
        }
        else
        {
            GameLog.Warn(
                GameLogCategory.System,
                "[Player]",
                GameLog.Join(
                    "StatGainFail",
                    GameLog.KV("reason", "NoCharacter"),
                    GameLog.KV("stat", "CharacterDamage")
                )
            );
        }
    }
    
    // Phase 6: XP-based stat upgrades
    public void AddCritChance(int amount)
    {
        critChance += amount;
        GameLog.System(GameLog.Join(
            "StatGain",
            GameLog.KV("stat", "CritChance"),
            GameLog.KV("delta", amount),
            GameLog.KV("now", critChance)
        ), GameLogVerbosity.Verbose);
    }
    
    public void AddCritDamage(float amount)
    {
        critDamage += amount;
        GameLog.System(GameLog.Join(
            "StatGain",
            GameLog.KV("stat", "CritDamage"),
            GameLog.KV("delta", amount),
            GameLog.KV("now", critDamage.ToString("F2"))
        ), GameLogVerbosity.Verbose);
    }
    
    public void IncreaseDamageRange(int amount)
    {
        if (selectedCharacter != null)
        {
            selectedCharacter.Damage += amount;
            GameLog.System(GameLog.Join(
                "StatGain",
                GameLog.KV("stat", "DamageRange"),
                GameLog.KV("delta", amount),
                GameLog.KV("now", selectedCharacter.Damage)
            ), GameLogVerbosity.Verbose);
        }
        else
        {
            GameLog.Warn(
                GameLogCategory.System,
                "[Player]",
                GameLog.Join(
                    "StatGainFail",
                    GameLog.KV("reason", "NoCharacter"),
                    GameLog.KV("stat", "DamageRange")
                )
            );
        }
    }
    
    public void ClearDebuffs()
    {
        statusEffects.ClearDebuffs();
        dirtyStabConsecutiveUses = 0;
        GameLog.Status(GameLog.Join(
            "ClearDebuffs",
            GameLog.KV("who", "Player")
        ), GameLogVerbosity.Verbose);
    }

    public void AddBaseDamage(int amount)
    {
        baseDamage += amount;
        GameLog.System(GameLog.Join(
            "StatGain",
            GameLog.KV("stat", "BaseDamage"),
            GameLog.KV("delta", amount),
            GameLog.KV("now", baseDamage)
        ), GameLogVerbosity.Verbose);
    }

    public void AddElementalDamage(Element element, int amount)
    {
        elementalDamage.Add(element, amount);
        GameLog.System(GameLog.Join(
            "StatGain",
            GameLog.KV("stat", $"ElementalDamage_{element}"),
            GameLog.KV("delta", amount),
            GameLog.KV("now", elementalDamage.Get(element))
        ), GameLogVerbosity.Verbose);
    }

    public void SetAffinity(Element element)
    {
        affinity = element;
        GameLog.System(GameLog.Join(
            "AffinitySet",
            GameLog.KV("element", element)
        ), GameLogVerbosity.Verbose);
    }
    
    public void SetElementPair(ElementPair pair)
    {
        orbSystem.SetElementPair(pair.OrbA, pair.OrbB);
        affinity = pair.OrbA;
        hasElementPair = true;
        GameLog.System(GameLog.Join(
            "ElementPairSet",
            GameLog.KV("pair", pair.DisplayName),
            GameLog.KV("a", pair.OrbA),
            GameLog.KV("b", pair.OrbB)
        ), GameLogVerbosity.Verbose);
    }
    
    public ElementalOrbSystem GetOrbSystem() => orbSystem;
    public bool HasElementPair() => hasElementPair;
    public Element GetOrbAElement() => orbSystem.OrbAElement;
    public Element GetOrbBElement() => orbSystem.OrbBElement;
    
    public void InfuseOrb(bool useOrbA)
    {
        orbSystem.InfuseOrb(useOrbA);
        affinity = orbSystem.GetInfusedElement(useOrbA);
    }
    
    public Element GetInfusedElement(bool useOrbA)
    {
        return orbSystem.GetInfusedElement(useOrbA);
    }
    
    public bool HasReactionReady() => orbSystem.HasReactionReady();
    
    public float TriggerReactionAndGetMultiplier()
    {
        if (!orbSystem.HasReactionReady()) return 1f;
        
        float multiplier = orbSystem.GetReactionDamageMultiplier();
        orbSystem.TriggerReaction();
        return multiplier;
    }

    public void SelectCharacter(CharacterData character)
    {
        selectedCharacter = character;
        // Set max energy from character data and start at 0
        maxEnergy = character.MaxEnergy;
        energy = 0;
        
        // Sync crit stats from character CSV data
        critChance = Mathf.RoundToInt(character.CritChance);
        critDamage = character.CritDamage;
        
        // Sync resistances from character CSV data
        baseResistance = character.BaseResistance;
        bonusResistance = character.BonusResistance;
        // Reset cooldowns
        skillCooldowns[0] = 0;
        skillCooldowns[1] = 0;
        skillCooldowns[2] = 0;
        GameLog.System(GameLog.Join(
            "CharacterSelect",
            GameLog.KV("name", character.DisplayName),
            GameLog.KV("damage", character.Damage),
            GameLog.KV("maxEnergy", maxEnergy)
        ), GameLogVerbosity.Verbose);
    }
    
    // ========== SKILL COOLDOWN & ENERGY SYSTEM ==========
    
    public int GetSkillCooldown(int skillIndex) => skillIndex >= 0 && skillIndex < 3 ? skillCooldowns[skillIndex] : 0;
    
    public void SetSkillCooldown(int skillIndex, int cooldown)
    {
        if (skillIndex >= 0 && skillIndex < 3)
        {
            skillCooldowns[skillIndex] = cooldown;
            GameLog.System(GameLog.Join(
                "CooldownSet",
                GameLog.KV("skill", skillIndex + 1),
                GameLog.KV("cd", cooldown)
            ), GameLogVerbosity.Verbose);
        }
    }
    
    public bool IsSkillOnCooldown(int skillIndex) => GetSkillCooldown(skillIndex) > 0;
    
    public bool CanUseSkill3()
    {
        if (selectedCharacter == null) return false;
        return energy >= selectedCharacter.Skill3EnergyCost && !IsSkillOnCooldown(2);
    }
    
    public int GetSkill3EnergyCost() => selectedCharacter != null ? selectedCharacter.Skill3EnergyCost : 0;
    
    public void UseSkillAndApplyEffects(int skillIndex)
    {
        if (selectedCharacter == null) return;
        
        // Apply cooldown based on skill
        switch (skillIndex)
        {
            case 0: // Skill 1
                skillCooldowns[0] = selectedCharacter.Skill1Cooldown;
                GainEnergy(selectedCharacter.Skill1EnergyGain);
                GameLog.Combat(GameLog.Join(
                    "SkillUse",
                    GameLog.KV("who", "Player"),
                    GameLog.KV("skill", 1),
                    GameLog.KV("cd", skillCooldowns[0]),
                    GameLog.KV("energyGain", selectedCharacter.Skill1EnergyGain),
                    GameLog.KV("energy", $"{energy}/{maxEnergy}")
                ), GameLogVerbosity.Verbose);
                break;
            case 1: // Skill 2
                skillCooldowns[1] = selectedCharacter.Skill2Cooldown;
                GainEnergy(selectedCharacter.Skill2EnergyGain);
                GameLog.Combat(GameLog.Join(
                    "SkillUse",
                    GameLog.KV("who", "Player"),
                    GameLog.KV("skill", 2),
                    GameLog.KV("cd", skillCooldowns[1]),
                    GameLog.KV("energyGain", selectedCharacter.Skill2EnergyGain),
                    GameLog.KV("energy", $"{energy}/{maxEnergy}")
                ), GameLogVerbosity.Verbose);
                break;
            case 2: // Skill 3
                skillCooldowns[2] = selectedCharacter.Skill3Cooldown;
                SpendEnergy(selectedCharacter.Skill3EnergyCost);
                GameLog.Combat(GameLog.Join(
                    "SkillUse",
                    GameLog.KV("who", "Player"),
                    GameLog.KV("skill", 3),
                    GameLog.KV("cd", skillCooldowns[2]),
                    GameLog.KV("energySpend", selectedCharacter.Skill3EnergyCost),
                    GameLog.KV("energy", $"{energy}/{maxEnergy}")
                ), GameLogVerbosity.Verbose);
                break;
        }
    }
    
    public void GainEnergy(int amount)
    {
        energy += amount;
        if (energy > maxEnergy) energy = maxEnergy;
        GameLog.System(GameLog.Join(
            "EnergyGain",
            GameLog.KV("delta", amount),
            GameLog.KV("energy", $"{energy}/{maxEnergy}")
        ), GameLogVerbosity.Verbose);
    }
    
    // Called at start of player's turn to tick down cooldowns
    public void TickCooldowns()
    {
        for (int i = 0; i < 3; i++)
        {
            if (skillCooldowns[i] > 0)
            {
                skillCooldowns[i]--;
            }
        }
        combatTurnCount++;
        GameLog.System(GameLog.Join(
            "CooldownTick",
            GameLog.KV("turn", combatTurnCount),
            GameLog.KV("s1", skillCooldowns[0]),
            GameLog.KV("s2", skillCooldowns[1]),
            GameLog.KV("s3", skillCooldowns[2])
        ), GameLogVerbosity.Verbose);
    }
    
    // Reset combat-specific state at combat start (cooldowns PERSIST between combats)
    public void ResetCombatState()
    {
        // NOTE: Cooldowns are NOT reset - they persist between combats
        combatTurnCount = 0;
        dirtyStabConsecutiveUses = 0;
        statusEffects.ClearCombatEffects(); // Keep shield, clear block
        GameLog.System(GameLog.Join(
            "CombatStateReset",
            GameLog.KV("energy", $"{energy}/{maxEnergy}"),
            GameLog.KV("shield", GetShield()),
            GameLog.KV("s1", skillCooldowns[0]),
            GameLog.KV("s2", skillCooldowns[1]),
            GameLog.KV("s3", skillCooldowns[2])
        ), GameLogVerbosity.Verbose);
    }
    
    public int GetCombatTurnCount() => combatTurnCount;
    
    // ========== STATUS EFFECTS (SHIELD, BLOCK) ==========
    
    public int GetShield() => statusEffects.GetShieldValue();
    public int GetMaxShield() => Mathf.RoundToInt(maxHealth * 0.4f); // 40% of max HP cap
    
    public void AddShield(int amount)
    {
        int maxShield = GetMaxShield();
        statusEffects.AddShield(amount, maxShield);
        GameLog.Combat(GameLog.Join(
            "ShieldGain",
            GameLog.KV("who", "Player"),
            GameLog.KV("amount", amount),
            GameLog.KV("shield", $"{GetShield()}/{maxShield}")
        ), GameLogVerbosity.Verbose);
    }
    
    public void ApplyBlock(float percentReduction)
    {
        statusEffects.AddEffect(new StatusEffect(StatusEffectType.Block, 1, percentReduction, "Riposte"));
        GameLog.Status(GameLog.Join(
            "Apply",
            GameLog.KV("target", "Player"),
            GameLog.KV("type", "Block"),
            GameLog.KV("value", percentReduction),
            GameLog.KV("dur", 1),
            GameLog.KV("source", "Riposte")
        ), GameLogVerbosity.Verbose);
    }
    
    public bool HasBlock() => statusEffects.HasEffect(StatusEffectType.Block);
    
    // Clear shield on world transition
    public void ClearShieldForWorldTransition()
    {
        statusEffects.ClearAllIncludingShield();
        GameLog.System(GameLog.Join(
            "ShieldClear",
            GameLog.KV("reason", "WorldTransition")
        ), GameLogVerbosity.Verbose);
    }
    
    // Apply temporary resistance modifier to all elements
    public void ApplyTempResistAll(int deltaPct, int duration)
    {
        statusEffects.AddTempResist("All", deltaPct, duration);
        GameLog.Status(GameLog.Join(
            "Apply",
            GameLog.KV("target", "Player"),
            GameLog.KV("type", "ResistAllDelta"),
            GameLog.KV("value", deltaPct),
            GameLog.KV("dur", duration)
        ), GameLogVerbosity.Verbose);
    }
    
    // Apply temporary resistance modifier to a specific element
    public void ApplyTempResist(Element element, int deltaPct, int duration)
    {
        statusEffects.AddTempResist(element.ToString(), deltaPct, duration);
        GameLog.Status(GameLog.Join(
            "Apply",
            GameLog.KV("target", "Player"),
            GameLog.KV("type", $"ResistDelta_{element}"),
            GameLog.KV("value", deltaPct),
            GameLog.KV("dur", duration)
        ), GameLogVerbosity.Verbose);
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
    
    // ========== DIRTYSTAB TRACKING ==========
    // Can be used 3 turns in a row. Each consecutive use grants +20% damage (max 2 stacks).
    // After 3rd consecutive use, goes on 4-turn cooldown and stacks reset.
    
    public int GetDirtyStabStacks() => dirtyStabConsecutiveUses;
    
    public void IncrementDirtyStabUse()
    {
        dirtyStabConsecutiveUses++;
        GameLog.System(GameLog.Join(
            "DirtyStab",
            GameLog.KV("event", "Use"),
            GameLog.KV("stacks", dirtyStabConsecutiveUses)
        ), GameLogVerbosity.Verbose);
        
        // After 3rd use, apply 4-turn cooldown and reset stacks
        if (dirtyStabConsecutiveUses >= 3)
        {
            skillCooldowns[0] = 4; // Force 4-turn cooldown on Skill1 (DirtyStab)
            dirtyStabConsecutiveUses = 0;
            GameLog.System(GameLog.Join(
                "DirtyStab",
                GameLog.KV("event", "CooldownTrigger"),
                GameLog.KV("cd", 4),
                GameLog.KV("stacks", 0)
            ), GameLogVerbosity.Verbose);
        }
    }
    
    public void ResetDirtyStabStacks()
    {
        if (dirtyStabConsecutiveUses > 0)
        {
            GameLog.System(GameLog.Join(
                "DirtyStab",
                GameLog.KV("event", "Reset"),
                GameLog.KV("was", dirtyStabConsecutiveUses)
            ), GameLogVerbosity.Verbose);
            dirtyStabConsecutiveUses = 0;
        }
    }
    
    public bool IsDirtyStabOnExtendedCooldown() => skillCooldowns[0] == 4 && dirtyStabConsecutiveUses == 0;

    public void AddGold(int amount)
    {
        gold += amount;
        GameLog.System(GameLog.Join(
            "Gold",
            GameLog.KV("delta", amount),
            GameLog.KV("total", gold)
        ), GameLogVerbosity.Verbose);
    }

    public void SpendEnergy(int amount)
    {
        energy -= amount;
        if (energy < 0) energy = 0;
    }

    public void RestoreEnergy(int amount)
    {
        energy += amount;
        if (energy > maxEnergy) energy = maxEnergy;
    }

    public void AddRelic(RelicData relic)
    {
        relics.Add(relic);
        ApplyRelicBonus(relic);
        GameLog.System(GameLog.Join(
            "RelicGain",
            GameLog.KV("relic", relic != null ? relic.DisplayName : "null")
        ), GameLogVerbosity.Verbose);
    }

    private void ApplyRelicBonus(RelicData relic)
    {
        var stat = relic.StatAffected.ToLower().Trim();
        
        if (stat == "all elements")
        {
            elementalDamage.Add(Element.Fire, relic.Amount);
            elementalDamage.Add(Element.Ice, relic.Amount);
            elementalDamage.Add(Element.Water, relic.Amount);
            elementalDamage.Add(Element.Wind, relic.Amount);
            elementalDamage.Add(Element.Rock, relic.Amount);
            GameLog.System(GameLog.Join(
                "RelicApply",
                GameLog.KV("type", "AllElements"),
                GameLog.KV("amount", relic.Amount)
            ), GameLogVerbosity.Verbose);
            return;
        }
        
        Element element = ElementalDamage.ParseElement(stat);
        if (element != Element.None)
        {
            elementalDamage.Add(element, relic.Amount);
            if (element == affinity)
            {
                GameLog.System(GameLog.Join(
                    "RelicApply",
                    GameLog.KV("type", "Element"),
                    GameLog.KV("element", element),
                    GameLog.KV("amount", relic.Amount),
                    GameLog.KV("matchesAffinity", true)
                ), GameLogVerbosity.Verbose);
            }
            else
            {
                GameLog.System(GameLog.Join(
                    "RelicApply",
                    GameLog.KV("type", "Element"),
                    GameLog.KV("element", element),
                    GameLog.KV("amount", relic.Amount),
                    GameLog.KV("matchesAffinity", false),
                    GameLog.KV("affinity", affinity)
                ), GameLogVerbosity.Verbose);
            }
            return;
        }

        switch (stat)
        {
            case "health":
            case "maxhealth":
                maxHealth += relic.Amount;
                health += relic.Amount;
                GameLog.System(GameLog.Join(
                    "RelicApply",
                    GameLog.KV("stat", "MaxHealth"),
                    GameLog.KV("delta", relic.Amount),
                    GameLog.KV("hp", $"{health}/{maxHealth}")
                ), GameLogVerbosity.Verbose);
                break;
            case "crit rate":
            case "critrate":
            case "critchance":
                critChance += relic.Amount;
                GameLog.System(GameLog.Join(
                    "RelicApply",
                    GameLog.KV("stat", "CritChance"),
                    GameLog.KV("delta", relic.Amount),
                    GameLog.KV("now", critChance)
                ), GameLogVerbosity.Verbose);
                break;
            case "crit damage":
            case "critdamage":
                critDamage += relic.Amount / 100f;
                GameLog.System(GameLog.Join(
                    "RelicApply",
                    GameLog.KV("stat", "CritDamage"),
                    GameLog.KV("deltaPct", relic.Amount),
                    GameLog.KV("now", critDamage.ToString("F2"))
                ), GameLogVerbosity.Verbose);
                break;
            case "energy":
            case "maxenergy":
                maxEnergy += relic.Amount;
                energy += relic.Amount;
                GameLog.System(GameLog.Join(
                    "RelicApply",
                    GameLog.KV("stat", "MaxEnergy"),
                    GameLog.KV("delta", relic.Amount),
                    GameLog.KV("energy", $"{energy}/{maxEnergy}")
                ), GameLogVerbosity.Verbose);
                break;
        }
    }

    public List<RelicData> GetRelics() => relics;

    public bool IsAlive() => health > 0;

    public bool CanAddPotion() => potionInventory.Count < MAX_POTIONS;
    public int GetPotionCount() => potionInventory.Count;
    public int GetMaxPotions() => MAX_POTIONS;
    public List<PotionData> GetPotions() => potionInventory;

    public void AddPotionToInventory(PotionData potion)
    {
        if (potionInventory.Count >= MAX_POTIONS)
        {
            GameLog.Warn(
                GameLogCategory.System,
                "[Player]",
                GameLog.Join(
                    "PotionAddFail",
                    GameLog.KV("reason", "InventoryFull"),
                    GameLog.KV("potion", potion != null ? potion.DisplayName : "null"),
                    GameLog.KV("count", potionInventory.Count),
                    GameLog.KV("max", MAX_POTIONS)
                )
            );
            return;
        }
        potionInventory.Add(potion);
        GameLog.System(GameLog.Join(
            "PotionAdd",
            GameLog.KV("potion", potion != null ? potion.DisplayName : "null"),
            GameLog.KV("count", potionInventory.Count),
            GameLog.KV("max", MAX_POTIONS)
        ), GameLogVerbosity.Verbose);
    }

    public bool UsePotion(int index, CombatEnemy target = null, Element elementOverride = Element.None)
    {
        if (index < 0 || index >= potionInventory.Count)
        {
            GameLog.Warn(
                GameLogCategory.System,
                "[Player]",
                GameLog.Join(
                    "PotionUseFail",
                    GameLog.KV("reason", "InvalidIndex"),
                    GameLog.KV("index", index),
                    GameLog.KV("count", potionInventory.Count)
                )
            );
            return false;
        }

        var potion = potionInventory[index];
        potionInventory.RemoveAt(index);

        var stat = potion.StatAffected.ToLower().Trim();
        switch (stat)
        {
            case "health":
                Heal(potion.Amount);
                GameLog.Combat(GameLog.Join(
                    "PotionUse",
                    GameLog.KV("potion", potion.DisplayName),
                    GameLog.KV("type", "Heal"),
                    GameLog.KV("amount", potion.Amount)
                ), GameLogVerbosity.Normal);
                break;
            case "elemental afinity direct damage":
                if (target != null && target.IsAlive())
                {
                    Element element = elementOverride != Element.None ? elementOverride : GetRandomElement();
                    int damage = target.ApplyResistance(potion.Amount, element);
                    target.TakeDamage(damage);
                    GameLog.Combat(GameLog.Join(
                        "PotionUse",
                        GameLog.KV("potion", potion.DisplayName),
                        GameLog.KV("type", "Damage"),
                        GameLog.KV("element", element),
                        GameLog.KV("target", target.Name),
                        GameLog.KV("amount", damage)
                    ), GameLogVerbosity.Normal);
                }
                else
                {
                    GameLog.Warn(
                        GameLogCategory.Combat,
                        "[Combat]",
                        GameLog.Join(
                            "PotionUseFail",
                            GameLog.KV("potion", potion.DisplayName),
                            GameLog.KV("reason", "NoValidTarget")
                        )
                    );
                    potionInventory.Insert(index, potion);
                    return false;
                }
                break;
            case "crit rate":
                tempCritChanceBonus += potion.Amount;
                GameLog.Status(GameLog.Join(
                    "PotionUse",
                    GameLog.KV("potion", potion.DisplayName),
                    GameLog.KV("type", "CritChance"),
                    GameLog.KV("delta", potion.Amount),
                    GameLog.KV("now", GetCritChance())
                ), GameLogVerbosity.Normal);
                break;
            case "crit damage":
                tempCritDamageBonus += potion.Amount;
                GameLog.Status(GameLog.Join(
                    "PotionUse",
                    GameLog.KV("potion", potion.DisplayName),
                    GameLog.KV("type", "CritDamage"),
                    GameLog.KV("deltaPct", potion.Amount),
                    GameLog.KV("now", GetCritDamage().ToString("F2"))
                ), GameLogVerbosity.Normal);
                break;
            default:
                GameLog.Warn(
                    GameLogCategory.System,
                    "[Player]",
                    GameLog.Join(
                        "PotionUseFail",
                        GameLog.KV("reason", "UnknownStat"),
                        GameLog.KV("stat", potion.StatAffected)
                    )
                );
                return false;
        }

        return true;
    }

    public bool IsElementalPotion(int index)
    {
        if (index < 0 || index >= potionInventory.Count) return false;
        var stat = potionInventory[index].StatAffected.ToLower().Trim();
        return stat.Contains("elemental");
    }

    public PotionData GetPotion(int index)
    {
        if (index < 0 || index >= potionInventory.Count) return null;
        return potionInventory[index];
    }

    private Element GetRandomElement()
    {
        var elements = new Element[] { Element.Fire, Element.Ice, Element.Water, Element.Wind, Element.Rock };
        return elements[Random.Range(0, elements.Length)];
    }

    public void ResetWorldBonuses()
    {
        tempCritChanceBonus = 0;
        tempCritDamageBonus = 0;
        GameLog.System(GameLog.Join(
            "WorldBonusesReset"
        ), GameLogVerbosity.Verbose);
    }
    
    public void ResetForNewWorld()
    {
        // Reset temporary potion bonuses
        tempCritChanceBonus = 0;
        tempCritDamageBonus = 0;
        
        // Clear potion inventory
        potionInventory.Clear();
        
        // Restore health to max (keep max health from relics/rests)
        health = maxHealth;
        
        // Restore energy to max
        energy = maxEnergy;

        GameLog.System(GameLog.Join(
            "WorldReset",
            GameLog.KV("hp", $"{health}/{maxHealth}"),
            GameLog.KV("potions", 0),
            GameLog.KV("tempBonuses", 0)
        ), GameLogVerbosity.Verbose);
        GameLog.System(GameLog.Join(
            "WorldResetKeep",
            GameLog.KV("relics", relics.Count),
            GameLog.KV("charDamage", GetCharacterDamage())
        ), GameLogVerbosity.Verbose);
    }
}
