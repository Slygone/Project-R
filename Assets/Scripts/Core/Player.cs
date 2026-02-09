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
    
    // Action Points System
    private int currentAP;
    private int maxAP = 10;
    
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
    
    
    // Skill cooldown tracking (index 0-3 = Skill1-4, index 4 = Skill5/Ultimate)
    private int[] skillCooldowns = new int[5];
    private int combatTurnCount = 0;
    
    // Skill element enchantments (runtime overrides from sigils)
    private Element[] skillElements = new Element[5] { Element.None, Element.None, Element.None, Element.None, Element.None };
    
    // Status effects (Shield, Block)
    private StatusEffectManager statusEffects = new StatusEffectManager();
    
    // Per-skill chain use tracking (for skills with chainSettings)
    private int[] chainConsecutiveUses = new int[5];

    void Awake()
    {
        // Ensure DataCache is loaded
        if (!DataCache.IsLoaded)
        {
            DataCache.LoadAll();
        }
        
        // Initialize with minimal defaults - real stats come from character selection
        maxHealth = 100;
        health = maxHealth;
        baseDamage = 10;
        gold = 0;
        maxEnergy = 100;
        energy = 0;
        critChance = 5;
        critDamage = 1.5f;
        baseResistance = 10;
        bonusResistance = 0;

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
        
        if (affinity != Element.None && attackerAffinity == affinity)
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
        for (int i = 0; i < 5; i++) chainConsecutiveUses[i] = 0;
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

    public void AddElementalDamageBonus(string elementName, int amount)
    {
        if (elementName == "All")
        {
            foreach (Element el in System.Enum.GetValues(typeof(Element)))
            {
                if (el != Element.None) elementalDamage.Add(el, amount);
            }
        }
        else if (System.Enum.TryParse<Element>(elementName, true, out var parsed))
        {
            elementalDamage.Add(parsed, amount);
        }
    }

    public void AddTempCritChance(int amount)
    {
        tempCritChanceBonus += amount;
    }

    public void AddTempCritDamage(int amount)
    {
        tempCritDamageBonus += amount;
    }

    public void AddPermanentCritChance(int amount)
    {
        critChance += amount;
    }

    public void AddPermanentCritDamage(int amount)
    {
        critDamage += amount / 100f;
    }

    public void AddMaxHealth(int amount)
    {
        maxHealth += amount;
        health += amount;
    }

    public void ApplyDamageBuff(float magnitude, int duration)
    {
        // TODO: Extend StatusEffectType to support DamageUp when needed
        GameLog.Status(GameLog.Join("ApplyDamageBuff",
            GameLog.KV("magnitude", magnitude),
            GameLog.KV("duration", duration)
        ), GameLogVerbosity.Verbose);
    }

    public void ApplyEvasion(float magnitude, int duration)
    {
        // TODO: Extend StatusEffectType to support Evasion when needed
        GameLog.Status(GameLog.Join("ApplyEvasion",
            GameLog.KV("magnitude", magnitude),
            GameLog.KV("duration", duration)
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
    
    public void SelectCharacter(CharacterData character)
    {
        selectedCharacter = character;
        // Set max energy from character data and start at 0
        maxEnergy = character.MaxEnergy;
        energy = 0;
        
        // Sync crit stats from character data
        critChance = Mathf.RoundToInt(character.CritChance);
        critDamage = character.CritDamage;
        
        // Sync resistances from character data
        baseResistance = character.BaseResistance;
        bonusResistance = character.BonusResistance;
        // Reset cooldowns for all 5 skills
        for (int i = 0; i < 5; i++) skillCooldowns[i] = 0;
        GameLog.System(GameLog.Join(
            "CharacterSelect",
            GameLog.KV("name", character.DisplayName),
            GameLog.KV("damage", character.Damage),
            GameLog.KV("maxEnergy", maxEnergy)
        ), GameLogVerbosity.Verbose);
    }
    
    // ========== SKILL COOLDOWN & ENERGY SYSTEM ==========
    
    public int GetSkillCooldown(int skillIndex) => skillIndex >= 0 && skillIndex < 5 ? skillCooldowns[skillIndex] : 0;
    
    public void SetSkillCooldown(int skillIndex, int cooldown)
    {
        if (skillIndex >= 0 && skillIndex < 5)
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
    
    // ========== SKILL ELEMENT ENCHANTMENT SYSTEM ==========
    
    public Element GetSkillElement(int skillNumber)
    {
        int index = skillNumber - 1;
        if (index >= 0 && index < 5)
        {
            // Return runtime enchantment if set, otherwise return base element from character data
            if (skillElements[index] != Element.None)
            {
                return skillElements[index];
            }
            
            // Fallback to character data element
            if (selectedCharacter != null)
            {
                string elemStr = skillNumber switch
                {
                    1 => selectedCharacter.Skill1Element,
                    2 => selectedCharacter.Skill2Element,
                    3 => selectedCharacter.Skill3Element,
                    4 => selectedCharacter.Skill4Element,
                    5 => selectedCharacter.Skill5Element,
                    _ => "none"
                };
                if (!string.IsNullOrEmpty(elemStr) && elemStr.ToLower() != "none")
                {
                    if (System.Enum.TryParse<Element>(elemStr, true, out Element result))
                    {
                        return result;
                    }
                }
            }
        }
        return Element.None;
    }
    
    public void EnchantSkill(int skillNumber, Element element)
    {
        int index = skillNumber - 1;
        if (index >= 0 && index < 5)
        {
            skillElements[index] = element;
            GameLog.System(GameLog.Join(
                "SkillEnchant",
                GameLog.KV("skill", skillNumber),
                GameLog.KV("element", element)
            ));
        }
    }
    
    public void ClearSkillEnchantments()
    {
        for (int i = 0; i < 5; i++)
        {
            skillElements[i] = Element.None;
        }
    }
    
    public bool CanUseUltimate()
    {
        if (selectedCharacter == null) return false;
        return energy >= selectedCharacter.Skill5EnergyCost && !IsSkillOnCooldown(4);
    }
    
    public int GetUltimateEnergyCost() => selectedCharacter != null ? selectedCharacter.Skill5EnergyCost : 0;
    
    public void UseSkillAndApplyEffects(int skillIndex)
    {
        if (selectedCharacter == null) return;
        if (skillIndex < 0 || skillIndex >= 5) return;
        
        // Look up SkillDefinition directly from JSON for cooldown and energy
        var charDef = GameDataLoader.GetCharacterByNumericId(selectedCharacter.CharacterID);
        SkillDefinition skillDef = null;
        if (charDef != null && charDef.skillIds != null && skillIndex < charDef.skillIds.Count)
        {
            skillDef = GameDataLoader.GetSkill(charDef.skillIds[skillIndex]);
        }
        
        // Set cooldown directly from JSON skill definition
        int cooldown = skillDef != null ? skillDef.cooldown : 0;
        skillCooldowns[skillIndex] = cooldown;
        
        
        
        // Apply energy: skills 1-4 gain energy, skill 5 costs energy
        int energyGain = skillDef != null ? SkillEffectEngine.GetEnergyGain(skillDef.effects) : 0;
        int energyCost = skillDef != null ? SkillEffectEngine.GetEnergyCost(skillDef.effects) : 0;
        
        if (energyCost > 0)
        {
            SpendEnergy(energyCost);
        }
        else if (energyGain > 0)
        {
            GainEnergy(energyGain);
        }
        
        GameLog.Combat(GameLog.Join(
            "SkillUse",
            GameLog.KV("who", "Player"),
            GameLog.KV("skill", skillIndex + 1),
            GameLog.KV("cd", cooldown),
            GameLog.KV("energyGain", energyGain),
            GameLog.KV("energyCost", energyCost),
            GameLog.KV("energy", $"{energy}/{maxEnergy}")
        ), GameLogVerbosity.Verbose);
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
        for (int i = 0; i < 5; i++)
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
            GameLog.KV("s3", skillCooldowns[2]),
            GameLog.KV("s4", skillCooldowns[3]),
            GameLog.KV("s5", skillCooldowns[4])
        ), GameLogVerbosity.Verbose);
    }
    
    // Reset combat-specific state at combat start (cooldowns PERSIST between combats)
    public void ResetCombatState()
    {
        // NOTE: Cooldowns are NOT reset - they persist between combats
        combatTurnCount = 0;
        for (int i = 0; i < 5; i++) chainConsecutiveUses[i] = 0;
        currentAP = maxAP; // Start combat with full AP
        statusEffects.ClearCombatEffects(); // Keep shield, clear block
        GameLog.System(GameLog.Join(
            "CombatStateReset",
            GameLog.KV("energy", $"{energy}/{maxEnergy}"),
            GameLog.KV("shield", GetShield()),
            GameLog.KV("s1", skillCooldowns[0]),
            GameLog.KV("s2", skillCooldowns[1]),
            GameLog.KV("s3", skillCooldowns[2]),
            GameLog.KV("s4", skillCooldowns[3]),
            GameLog.KV("s5", skillCooldowns[4])
        ), GameLogVerbosity.Verbose);
    }
    
    public int GetCombatTurnCount() => combatTurnCount;
    
    // ========== ACTION POINTS SYSTEM ==========
    
    public int GetCurrentAP() => currentAP;
    public int GetMaxAP() => maxAP;
    
    public bool HasEnoughAP(int cost) => currentAP >= cost;
    
    public void SpendAP(int cost)
    {
        currentAP -= cost;
        if (currentAP < 0) currentAP = 0;
        GameLog.Combat(GameLog.Join(
            "APSpend",
            GameLog.KV("cost", cost),
            GameLog.KV("ap", $"{currentAP}/{maxAP}")
        ), GameLogVerbosity.Verbose);
    }
    
    public void RefreshAP()
    {
        currentAP = maxAP;
        GameLog.Combat(GameLog.Join(
            "APRefresh",
            GameLog.KV("ap", $"{currentAP}/{maxAP}")
        ), GameLogVerbosity.Verbose);
    }
    
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
    
    // ========== CHAIN SKILL TRACKING ==========
    // Generic per-skill chain tracking driven by ChainSettings from JSON.
    // e.g. DirtyStab: maxChainUses=3, stackBonusPerUse=0.20, maxStacks=2, forceCooldownAfterMaxChain=4
    
    public int GetChainStacks(int skillIndex) => skillIndex >= 0 && skillIndex < 5 ? chainConsecutiveUses[skillIndex] : 0;
    
    public void IncrementChainUse(int skillIndex, ChainSettings settings)
    {
        if (skillIndex < 0 || skillIndex >= 5 || settings == null) return;
        
        chainConsecutiveUses[skillIndex]++;
        GameLog.System(GameLog.Join(
            "ChainUse",
            GameLog.KV("skill", skillIndex + 1),
            GameLog.KV("stacks", chainConsecutiveUses[skillIndex])
        ), GameLogVerbosity.Verbose);
        
        // After max chain uses, apply forced cooldown from JSON and reset stacks
        if (chainConsecutiveUses[skillIndex] >= settings.maxChainUses)
        {
            skillCooldowns[skillIndex] = settings.forceCooldownAfterMaxChain;
            chainConsecutiveUses[skillIndex] = 0;
            GameLog.System(GameLog.Join(
                "ChainCooldown",
                GameLog.KV("skill", skillIndex + 1),
                GameLog.KV("cd", settings.forceCooldownAfterMaxChain),
                GameLog.KV("stacks", 0)
            ), GameLogVerbosity.Verbose);
        }
    }
    
    public void ResetChainStacks(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= 5) return;
        if (chainConsecutiveUses[skillIndex] > 0)
        {
            GameLog.System(GameLog.Join(
                "ChainReset",
                GameLog.KV("skill", skillIndex + 1),
                GameLog.KV("was", chainConsecutiveUses[skillIndex])
            ), GameLogVerbosity.Verbose);
            chainConsecutiveUses[skillIndex] = 0;
        }
    }

    public void ResetAllChainStacksExcept(int exceptSkillIndex)
    {
        for (int i = 0; i < 5; i++)
        {
            if (i != exceptSkillIndex)
                ResetChainStacks(i);
        }
    }

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
        if (relic.Effects == null || relic.Effects.Count == 0) return;
        
        var result = SkillEffectEngine.ExecuteRest(relic.Effects, this);
        
        GameLog.System(GameLog.Join(
            "RelicApply",
            GameLog.KV("relic", relic.DisplayName),
            GameLog.KV("effectCount", relic.Effects.Count)
        ), GameLogVerbosity.Verbose);
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
        
        // Check if potion needs a target but none available
        if (SkillEffectEngine.NeedsEnemyTarget(potion.Effects) && (target == null || !target.IsAlive()))
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
            return false;
        }
        
        potionInventory.RemoveAt(index);
        
        var result = SkillEffectEngine.ExecutePotion(potion.Effects, this, target, elementOverride);
        
        GameLog.Combat(GameLog.Join(
            "PotionUse",
            GameLog.KV("potion", potion.DisplayName),
            GameLog.KV("heal", result.HealAmount),
            GameLog.KV("damage", result.TotalDamageDealt),
            GameLog.KV("shield", result.ShieldGained)
        ), GameLogVerbosity.Normal);

        return true;
    }

    public bool IsElementalPotion(int index)
    {
        if (index < 0 || index >= potionInventory.Count) return false;
        return SkillEffectEngine.NeedsEnemyTarget(potionInventory[index].Effects);
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
    
    /// <summary>
    /// Full reset for starting a new run. Clears all run-specific state.
    /// Only meta-progression (talents, elemental ascensions) persists - those are managed externally.
    /// </summary>
    public void ResetForNewRun()
    {
        // Reset to minimal defaults - SelectCharacter will set real stats from character JSON
        maxHealth = 100;
        health = maxHealth;
        baseDamage = 10;
        gold = 0;
        maxEnergy = 100;
        energy = 0;
        critChance = 5;
        critDamage = 1.5f;
        baseResistance = 10;
        bonusResistance = 0;
        
        // Clear all run-specific collections
        relics.Clear();
        potionInventory.Clear();
        
        // Reset temporary bonuses
        tempCritChanceBonus = 0;
        tempCritDamageBonus = 0;
        
        // Reset skill enchantments (sigils don't carry over)
        for (int i = 0; i < 5; i++)
        {
            skillElements[i] = Element.None;
            skillCooldowns[i] = 0;
        }
        
        // Clear status effects (shield, block, etc.)
        statusEffects.ClearAll();
        
        // Reset combat tracking
        combatTurnCount = 0;
        for (int i = 0; i < 5; i++) chainConsecutiveUses[i] = 0;
        currentAP = maxAP;
        
        // Reset wound/threshold tracking
        lowestThresholdLevel = -1;
        
        // Clear affinity (will be set when character is selected)
        affinity = Element.None;
        selectedCharacter = null;
        
        // Reset elemental damage bonuses
        elementalDamage = new ElementalDamage();
        
        GameLog.System(GameLog.Join(
            "NewRunReset",
            GameLog.KV("hp", $"{health}/{maxHealth}"),
            GameLog.KV("gold", gold),
            GameLog.KV("relics", 0),
            GameLog.KV("potions", 0)
        ));
    }
}
