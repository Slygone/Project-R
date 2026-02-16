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
    private int physicalResist;
    private int elementalResist;
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
    
    // Relic trigger tracking
    private int relicSkillUseCounter = 0;
    private int relicTurnCounter = 0;
    private int relicApSpentCounter = 0;
    private int bankedAP = 0;
    private bool hasAPBanking = false;
    private List<string> statusImmunities = new List<string>();
    private float energyCostMultiplier = 1f;
    private int extraMarksThisTurn = 0;
    private int apReductionThisTurn = 0;
    private int apReductionSkillsRemaining = 0;
    private bool phoenixFeatherUsed = false;
    private int crackedBatteryTurnsLeft = 0;
    private int sipheringApDelta = 0;
    private bool hasFirstMarkBonus = false;
    private bool firstMarkAppliedThisTurn = false;
    private bool hasPerfectQteAp = false;
    private bool perfectQteApUsedThisTurn = false;
    private int perfectQteApAmount = 0;
    private bool hasReactionCostReduction = false;
    private bool reactionCostReductionUsed = false;
    private bool hasReactionApRefund = false;
    private bool reactionApRefundUsedThisTurn = false;
    private int reactionApRefundAmount = 0;
    private bool hasMarkTransfer = false;
    private int markTransferCount = 0;
    private bool hasPerfectReactionSaveMark = false;
    private int perfectReactionSaveMarkCount = 0;
    private bool hasReactionDouble = false;
    private bool reactionDoubleUsed = false;
    private float reactionDoubleMultiplier = 0.5f;
    private bool hasDualReactionShield = false;
    private bool dualReactionShieldUsed = false;
    private int dualReactionShieldPercent = 0;
    private bool hasReactionWeaken = false;
    private bool reactionWeakenUsedThisTurn = false;
    private int reactionWeakenDuration = 0;
    private bool hideEnemyMarks = false;
    private int reactionExtraMarkCost = 0;
    private bool deadeyeCritReady = false;
    private HashSet<string> relicJustTriggered = new HashSet<string>();
    private int disableDefensiveQTETurns = 0;
    private bool disableReactionQTE = false;
    private bool disableSigils = false;
    private int lastCombatStartHeal = 0;
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
    
    // Enemy debuff tracking
    private float weakenPercent;    // % damage reduction (0-100)
    private int weakenTurns;        // turns remaining
    private float sunderPercent;    // % shield gain reduction (0-100)
    private int sunderTurns;        // turns remaining
    private float vulnerablePercent; // % damage taken increase (stackable)
    private int vulnerableTurns;    // turns remaining
    private int vulnerableMaxStacks; // max stackable magnitude
    private bool isStunned;         // skip next player turn
    private int stunTurns;          // turns remaining
    private int playerDoTDamage;    // DoT damage per tick
    private int playerDoTTurns;     // turns remaining
    private string playerDoTSource; // source name for logging

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
        physicalResist = 10;
        elementalResist = 10;

        GameLog.System(GameLog.Join(
            "PlayerInit",
            GameLog.KV("hp", $"{health}/{maxHealth}"),
            GameLog.KV("gold", gold),
            GameLog.KV("energy", $"{energy}/{maxEnergy}"),
            GameLog.KV("crit", $"{critChance}%x{critDamage}"),
            GameLog.KV("resist", $"phys={physicalResist}%|elem={elementalResist}%")
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
    public Element GetAffinity() => disableSigils ? Element.None : affinity;
    public bool HasAffinity() => !disableSigils && affinity != Element.None;
    public CharacterData GetCharacter() => selectedCharacter;
    public bool HasCharacter() => selectedCharacter != null;
    public int GetGold() => gold;
    public int GetEnergy() => energy;
    public int GetMaxEnergy() => maxEnergy;
    public int GetCritChance() => critChance + tempCritChanceBonus;
    public float GetCritDamage() => critDamage + (tempCritDamageBonus / 100f);
    public int GetPhysicalResist() => physicalResist;
    public int GetElementalResist() => elementalResist;

    /// <summary>
    /// Calculate resistance against an attack. If attackElement is None, uses physicalResist.
    /// Otherwise uses elementalResist.
    /// </summary>
    public int CalculateResistance(Element attackElement)
    {
        bool isElemental = attackElement != Element.None;
        return isElemental ? elementalResist : physicalResist;
    }

    public int ApplyResistance(int damage, Element attackElement)
    {
        int resistance = CalculateResistance(attackElement);
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
        physicalResist = character.PhysicalResist;
        elementalResist = character.ElementalResist;
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
        // Sigil Renounce: all skills are physical when sigils disabled
        if (disableSigils) return Element.None;
        
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
        ClearAllDebuffs(); // Clear enemy debuffs from previous combat
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
        // Apply Sunder reduction to shield gains
        float sunderMult = GetSunderMultiplier();
        if (sunderMult < 1f)
        {
            amount = Mathf.RoundToInt(amount * sunderMult);
        }
        
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
        
        string trigger = relic.Trigger ?? "onAcquire";
        
        // Only apply effects immediately for onAcquire and permanent triggers
        if (trigger == "onAcquire" || trigger == "permanent")
        {
            ApplyRelicEffectsImmediate(relic);
        }
        
        // Track passive flags
        if (relic.Effects != null)
        {
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_status_immunity" && !string.IsNullOrEmpty(eff.immuneStatus))
                {
                    if (!statusImmunities.Contains(eff.immuneStatus))
                        statusImmunities.Add(eff.immuneStatus);
                }
                else if (eff.effectId == "eff_energy_cost_multiplier")
                {
                    if (eff.multiplier > 0) energyCostMultiplier *= eff.multiplier;
                }
                else if (eff.effectId == "eff_ap_banking")
                {
                    hasAPBanking = true;
                }
                else if (eff.effectId == "eff_first_mark_bonus")
                {
                    hasFirstMarkBonus = true;
                }
                else if (eff.effectId == "eff_perfect_qte_ap")
                {
                    hasPerfectQteAp = true;
                    perfectQteApAmount = eff.amount;
                }
                else if (eff.effectId == "eff_reaction_cost_reduction")
                {
                    hasReactionCostReduction = true;
                }
                else if (eff.effectId == "eff_reaction_ap_refund")
                {
                    hasReactionApRefund = true;
                    reactionApRefundAmount = eff.amount;
                }
                else if (eff.effectId == "eff_mark_transfer")
                {
                    hasMarkTransfer = true;
                    markTransferCount = eff.value;
                }
                else if (eff.effectId == "eff_perfect_reaction_save_mark")
                {
                    hasPerfectReactionSaveMark = true;
                    perfectReactionSaveMarkCount = eff.value;
                }
                else if (eff.effectId == "eff_reaction_double")
                {
                    hasReactionDouble = true;
                    reactionDoubleMultiplier = eff.multiplier;
                }
                else if (eff.effectId == "eff_dual_reaction_shield")
                {
                    hasDualReactionShield = true;
                    dualReactionShieldPercent = eff.percentOfMaxHealth;
                }
                else if (eff.effectId == "eff_reaction_weaken")
                {
                    hasReactionWeaken = true;
                    reactionWeakenDuration = eff.duration;
                }
                else if (eff.effectId == "eff_hide_marks")
                {
                    hideEnemyMarks = true;
                }
                else if (eff.effectId == "eff_reaction_extra_mark_cost")
                {
                    reactionExtraMarkCost += eff.value;
                }
                else if (eff.effectId == "eff_disable_system")
                {
                    string dt = (eff.disableTarget ?? "").ToLower();
                    if (dt == "reactionqte") disableReactionQTE = true;
                    else if (dt == "sigils")
                    {
                        disableSigils = true;
                        ClearSkillEnchantments();
                        affinity = Element.None;
                        Debug.Log("[Player] Sigil Renounce: cleared all skill enchantments and affinity");
                    }
                }
            }
        }
        
        GameLog.System(GameLog.Join(
            "RelicGain",
            GameLog.KV("relic", relic != null ? relic.DisplayName : "null"),
            GameLog.KV("trigger", trigger)
        ), GameLogVerbosity.Verbose);
    }

    private void ApplyRelicEffectsImmediate(RelicData relic)
    {
        if (relic.Effects == null || relic.Effects.Count == 0) return;
        
        // Set of effectIds that are handled as passive flags, not immediate
        var flagEffects = new System.Collections.Generic.HashSet<string> {
            "eff_status_immunity", "eff_energy_cost_multiplier", "eff_ap_banking",
            "eff_first_mark_bonus", "eff_perfect_qte_ap", "eff_reaction_cost_reduction",
            "eff_reaction_ap_refund", "eff_mark_transfer", "eff_perfect_reaction_save_mark",
            "eff_reaction_double", "eff_dual_reaction_shield", "eff_reaction_weaken",
            "eff_hide_marks", "eff_reaction_extra_mark_cost", "eff_disable_system",
            "eff_temp_crit_bonus"
        };
        
        var standardEffects = new List<EffectEntry>();
        
        foreach (var eff in relic.Effects)
        {
            // Skip flag-tracked effects
            if (flagEffects.Contains(eff.effectId)) continue;
            
            // Handle stat bonuses that SkillEffectEngine doesn't know about
            if (eff.effectId == "eff_stat_bonus")
            {
                string stat = (eff.stat ?? "").ToLower();
                if (stat == "maxhealthpercent")
                {
                    int bonus = Mathf.RoundToInt(maxHealth * (eff.value / 100f));
                    maxHealth += bonus;
                    if (eff.value > 0) health += bonus;
                }
                else if (stat == "damagemax")
                {
                    baseDamage += eff.value;
                }
                else if (stat == "physicalresist")
                {
                    physicalResist += eff.value;
                }
                else if (stat == "elementalresist")
                {
                    elementalResist += eff.value;
                }
                else
                {
                    // critChance, critDamage, damage handled by SkillEffectEngine
                    standardEffects.Add(eff);
                }
                continue;
            }
            
            // All other effects go to SkillEffectEngine
            standardEffects.Add(eff);
        }
        
        if (standardEffects.Count > 0)
        {
            SkillEffectEngine.ExecuteRest(standardEffects, this);
        }
        
        GameLog.System(GameLog.Join(
            "RelicApply",
            GameLog.KV("relic", relic.DisplayName),
            GameLog.KV("effectCount", relic.Effects.Count)
        ), GameLogVerbosity.Verbose);
    }

    public void RemoveRelic(RelicData relic)
    {
        relics.Remove(relic);
        
        string trigger = relic.Trigger ?? "onAcquire";
        
        // Reverse immediate stat effects for onAcquire/permanent
        if (trigger == "onAcquire" || trigger == "permanent")
        {
            ReverseRelicEffectsImmediate(relic);
        }
        
        // Reverse passive flags
        if (relic.Effects != null)
        {
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_status_immunity" && !string.IsNullOrEmpty(eff.immuneStatus))
                    statusImmunities.Remove(eff.immuneStatus);
                else if (eff.effectId == "eff_energy_cost_multiplier" && eff.multiplier > 0)
                    energyCostMultiplier /= eff.multiplier;
                else if (eff.effectId == "eff_ap_banking") hasAPBanking = false;
                else if (eff.effectId == "eff_first_mark_bonus") hasFirstMarkBonus = false;
                else if (eff.effectId == "eff_perfect_qte_ap") { hasPerfectQteAp = false; perfectQteApAmount = 0; }
                else if (eff.effectId == "eff_reaction_cost_reduction") hasReactionCostReduction = false;
                else if (eff.effectId == "eff_reaction_ap_refund") { hasReactionApRefund = false; reactionApRefundAmount = 0; }
                else if (eff.effectId == "eff_mark_transfer") { hasMarkTransfer = false; markTransferCount = 0; }
                else if (eff.effectId == "eff_perfect_reaction_save_mark") { hasPerfectReactionSaveMark = false; perfectReactionSaveMarkCount = 0; }
                else if (eff.effectId == "eff_reaction_double") { hasReactionDouble = false; reactionDoubleMultiplier = 0.5f; }
                else if (eff.effectId == "eff_dual_reaction_shield") { hasDualReactionShield = false; dualReactionShieldPercent = 0; }
                else if (eff.effectId == "eff_reaction_weaken") { hasReactionWeaken = false; reactionWeakenDuration = 0; }
                else if (eff.effectId == "eff_temp_crit_bonus") deadeyeCritReady = false;
                else if (eff.effectId == "eff_hide_marks") hideEnemyMarks = false;
                else if (eff.effectId == "eff_reaction_extra_mark_cost") reactionExtraMarkCost -= eff.value;
                else if (eff.effectId == "eff_disable_system")
                {
                    string dt = (eff.disableTarget ?? "").ToLower();
                    if (dt == "reactionqte") disableReactionQTE = false;
                    else if (dt == "sigils") disableSigils = false;
                }
            }
        }
        
        GameLog.System(GameLog.Join("RelicRemove", GameLog.KV("relic", relic.DisplayName)), GameLogVerbosity.Verbose);
    }
    
    private void ReverseRelicEffectsImmediate(RelicData relic)
    {
        if (relic.Effects == null) return;
        foreach (var eff in relic.Effects)
        {
            if (eff.effectId != "eff_stat_bonus") continue;
            string stat = (eff.stat ?? "").ToLower();
            if (stat == "maxhealthpercent")
            {
                int bonus = Mathf.RoundToInt(maxHealth * (eff.value / (100f + eff.value)));
                maxHealth -= bonus;
                if (health > maxHealth) health = maxHealth;
            }
            else if (stat == "damagemax") baseDamage -= eff.value;
            else if (stat == "physicalresist") physicalResist -= eff.value;
            else if (stat == "elementalresist") elementalResist -= eff.value;
            else if (stat == "damage") baseDamage -= eff.value;
            else if (stat == "critchance") critChance -= eff.value;
            else if (stat == "critdamage") critDamage -= eff.value / 100f;
        }
    }
    
    public List<RelicData> GetRelics() => relics;
    
    // ── Relic Trigger Methods (called by CombatManager/CombatArena) ──
    
    public List<RelicData> GetRelicsByTrigger(string trigger)
    {
        var result = new List<RelicData>();
        foreach (var r in relics)
        {
            if (r.Trigger == trigger) result.Add(r);
        }
        return result;
    }
    
    public void OnCombatStart()
    {
        relicTurnCounter = 0;
        extraMarksThisTurn = 0;
        apReductionThisTurn = 0;
        apReductionSkillsRemaining = 0;
        crackedBatteryTurnsLeft = 0;
        firstMarkAppliedThisTurn = false;
        perfectQteApUsedThisTurn = false;
        reactionCostReductionUsed = false;
        reactionApRefundUsedThisTurn = false;
        reactionDoubleUsed = false;
        dualReactionShieldUsed = false;
        reactionWeakenUsedThisTurn = false;
        disableDefensiveQTETurns = 0;
        lastCombatStartHeal = 0;
        
        // Process combatStart relics
        foreach (var relic in relics)
        {
            if (relic.Trigger != "combatStart" || relic.Effects == null) continue;
            
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_ap_delta")
                {
                    // Cracked Battery: -2 AP for N turns
                    if (eff.duration > 0)
                    {
                        crackedBatteryTurnsLeft = eff.duration;
                        sipheringApDelta = eff.amount;
                    }
                    else
                    {
                        currentAP += eff.amount;
                    }
                    Debug.Log($"[Player] Relic {relic.DisplayName}: AP delta {eff.amount}");
                }
                else if (eff.effectId == "eff_post_combat_heal")
                {
                    int healAmount = Mathf.RoundToInt(maxHealth * eff.percentOfMaxHealth / 100f);
                    if (healAmount > 0)
                    {
                        Heal(healAmount);
                        lastCombatStartHeal = healAmount;
                        Debug.Log($"[Player] Relic {relic.DisplayName}: healed {healAmount} HP at combat start");
                    }
                }
                else if (eff.effectId == "eff_apply_status")
                {
                    // Status application handled by CombatManager which has access to enemies
                    // Flag it for CombatManager to pick up
                }
                else if (eff.effectId == "eff_combat_shield")
                {
                    int shieldAmount = Mathf.RoundToInt(maxHealth * eff.percentOfMaxHealth / 100f);
                    AddShield(shieldAmount);
                    Debug.Log($"[Player] Relic {relic.DisplayName}: +{shieldAmount} shield");
                }
                else if (eff.effectId == "eff_disable_system")
                {
                    string dt = (eff.disableTarget ?? "").ToLower();
                    if (dt == "defensiveqte") disableDefensiveQTETurns = eff.disableDuration;
                    // reactionQTE and sigils are permanent flags set on acquire, not per-combat
                    Debug.Log($"[Player] Relic {relic.DisplayName}: disable {eff.disableTarget} for {eff.disableDuration} turns");
                }
                else if (eff.effectId == "eff_apply_equipped_mark")
                {
                    // Handled by CombatManager which has access to enemies
                    Debug.Log($"[Player] Relic {relic.DisplayName}: apply equipped element marks");
                }
                else if (eff.effectId == "eff_hide_marks")
                {
                    Debug.Log($"[Player] Relic {relic.DisplayName}: hide enemy marks");
                }
                else if (eff.effectId == "eff_stat_bonus")
                {
                    // Resistance bonuses applied via ApplyRelicEffectsImmediate on acquire, not here
                }
            }
        }
    }
    
    public void OnRelicTurnStart()
    {
        relicTurnCounter++;
        extraMarksThisTurn = 0;
        apReductionThisTurn = 0;
        apReductionSkillsRemaining = 0;
        firstMarkAppliedThisTurn = false;
        perfectQteApUsedThisTurn = false;
        reactionApRefundUsedThisTurn = false;
        reactionWeakenUsedThisTurn = false;
        
        // Cracked Battery: AP penalty for N turns
        if (crackedBatteryTurnsLeft > 0)
        {
            currentAP += sipheringApDelta;
            if (currentAP < 0) currentAP = 0;
            crackedBatteryTurnsLeft--;
            Debug.Log($"[Player] Cracked Battery: {sipheringApDelta} AP, {crackedBatteryTurnsLeft} turns left");
        }
        
        // Process onTurnStart relics
        foreach (var relic in relics)
        {
            if (relic.Trigger != "onTurnStart") continue;
            if (relic.TriggerInterval > 0 && relicTurnCounter % relic.TriggerInterval != 0) continue;
            
            if (relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_extra_marks")
                {
                    extraMarksThisTurn += eff.extraMarks;
                    relicJustTriggered.Add(relic.Id);
                    Debug.Log($"[Player] Relic {relic.DisplayName}: +{eff.extraMarks} extra marks this turn");
                }
                else if (eff.effectId == "eff_ap_cost_reduction")
                {
                    apReductionThisTurn += eff.apReduction;
                    apReductionSkillsRemaining = eff.skillCount > 0 ? eff.skillCount : 999;
                    relicJustTriggered.Add(relic.Id);
                    Debug.Log($"[Player] Relic {relic.DisplayName}: -{eff.apReduction} AP cost for {eff.skillCount} skills");
                }
                else if (eff.effectId == "eff_ap_delta")
                {
                    // Siphoning Aura: -1 AP each turn
                    currentAP += eff.amount;
                    if (currentAP < 0) currentAP = 0;
                    Debug.Log($"[Player] Relic {relic.DisplayName}: AP delta {eff.amount}");
                }
            }
        }
        
        // AP banking: add banked AP on top of refreshed AP
        if (hasAPBanking && bankedAP > 0)
        {
            currentAP += bankedAP;
            Debug.Log($"[Player] AP Banking: added {bankedAP} banked AP, total now {currentAP}");
            bankedAP = 0;
        }
    }
    
    public void OnRelicTurnEnd()
    {
        // AP banking: save unspent AP
        if (hasAPBanking && currentAP > 0)
        {
            bankedAP = currentAP;
            Debug.Log($"[Player] AP Banking: saved {bankedAP} AP for next turn");
        }
        
        // Process onTurnEnd relics (Elemental Drip handled by CombatManager)
        foreach (var relic in relics)
        {
            if (relic.Trigger != "onTurnEnd" || relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_deal_damage" && eff.target == "Self")
                {
                    int dmg = eff.value > 0 ? eff.value : 1;
                    TakeDamage(dmg);
                    Debug.Log($"[Player] Relic {relic.DisplayName}: took {dmg} self-damage");
                }
            }
        }
    }
    
    public void OnRelicCombatEnd()
    {
        // Process onCombatEnd relics (Blood Toll self-damage)
        foreach (var relic in relics)
        {
            if (relic.Trigger != "onCombatEnd" || relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_post_combat_heal")
                {
                    int healAmount = Mathf.RoundToInt(maxHealth * eff.percentOfMaxHealth / 100f);
                    if (healAmount > 0)
                    {
                        Heal(healAmount);
                        Debug.Log($"[Player] Relic {relic.DisplayName}: healed {healAmount} HP");
                    }
                }
                else if (eff.effectId == "eff_deal_damage" && eff.target == "Self")
                {
                    int dmg = eff.value > 0 ? eff.value : 1;
                    TakeDamage(dmg);
                    Debug.Log($"[Player] Relic {relic.DisplayName}: took {dmg} self-damage");
                }
            }
        }
    }
    
    public void OnRelicSkillUsed()
    {
        relicSkillUseCounter++;
        
        // Consume AP reduction skill count
        if (apReductionSkillsRemaining > 0)
        {
            apReductionSkillsRemaining--;
            if (apReductionSkillsRemaining <= 0)
            {
                apReductionThisTurn = 0;
            }
        }
        
        foreach (var relic in relics)
        {
            if (relic.Trigger != "onSkillUse") continue;
            if (relic.TriggerInterval <= 0) continue;
            if (relicSkillUseCounter % relic.TriggerInterval != 0) continue;
            
            if (relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_temp_crit_bonus")
                {
                    deadeyeCritReady = true;
                    relicJustTriggered.Add(relic.Id);
                    Debug.Log($"[Player] Relic {relic.DisplayName}: guaranteed crit ready for next skill");
                }
            }
        }
    }
    
    public void OnRelicAPSpent(int apSpent)
    {
        relicApSpentCounter += apSpent;
        
        // Check Cooldown Lottery
        foreach (var relic in relics)
        {
            if (relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_refresh_random_skill" && eff.apSpentThreshold > 0)
                {
                    if (relicApSpentCounter >= eff.apSpentThreshold)
                    {
                        relicApSpentCounter -= eff.apSpentThreshold;
                        RefreshRandomSkillCooldown();
                        relicJustTriggered.Add(relic.Id);
                        Debug.Log($"[Player] Relic {relic.DisplayName}: triggered at {eff.apSpentThreshold} AP spent");
                    }
                }
            }
        }
    }
    
    private void RefreshRandomSkillCooldown()
    {
        // Find skills on cooldown (not ultimate = index 4)
        var onCooldown = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            if (skillCooldowns[i] > 0) onCooldown.Add(i);
        }
        
        if (onCooldown.Count > 0)
        {
            int idx = onCooldown[UnityEngine.Random.Range(0, onCooldown.Count)];
            skillCooldowns[idx] = 0;
            Debug.Log($"[Player] Cooldown Lottery: refreshed skill {idx + 1}");
        }
        // If no skill on cooldown, the next skill cast will still benefit from 0 AP cost
        // (handled by CombatManager checking a flag)
    }
    
    public bool HasPhoenixFeather()
    {
        if (phoenixFeatherUsed) return false;
        foreach (var r in relics)
        {
            if (r.Trigger == "onDeath" && !phoenixFeatherUsed) return true;
        }
        return false;
    }
    
    public bool TryPhoenixRevive()
    {
        if (phoenixFeatherUsed) return false;
        
        foreach (var relic in relics)
        {
            if (relic.Trigger != "onDeath") continue;
            
            phoenixFeatherUsed = true;
            
            if (relic.Effects != null)
            {
                foreach (var eff in relic.Effects)
                {
                    if (eff.effectId == "eff_reborn")
                    {
                        int reviveHP = Mathf.Max(1, Mathf.RoundToInt(maxHealth * (eff.healthPercent / 100f)));
                        health = reviveHP;
                    }
                    else if (eff.effectId == "eff_stat_bonus" && eff.stat == "maxHealthPercent" && eff.value < 0)
                    {
                        int reduction = Mathf.RoundToInt(maxHealth * (Mathf.Abs(eff.value) / 100f));
                        maxHealth -= reduction;
                        if (health > maxHealth) health = maxHealth;
                    }
                }
            }
            
            Debug.Log($"[Player] Phoenix Feather used! Revived at {health}/{maxHealth}");
            return true;
        }
        return false;
    }
    
    // ── Relic Query Methods ──
    public bool IsImmuneToStatus(string statusId) => statusImmunities.Contains(statusId);
    public float GetEnergyCostMultiplier() => energyCostMultiplier;
    public int GetExtraMarksThisTurn() => extraMarksThisTurn;
    public int GetAPReductionThisTurn() => apReductionThisTurn;
    public int GetAPReductionSkillsRemaining() => apReductionSkillsRemaining;
    public bool HasAPBanking() => hasAPBanking;
    public int GetLastCombatStartHeal() { int h = lastCombatStartHeal; lastCombatStartHeal = 0; return h; }
    public bool IsDefensiveQTEDisabled(int currentTurn) => disableDefensiveQTETurns > 0 && currentTurn <= disableDefensiveQTETurns;
    public bool IsReactionQTEDisabled() => disableReactionQTE;
    public bool AreSigilsDisabled() => disableSigils;
    public bool HasFirstMarkBonus() => hasFirstMarkBonus;
    public bool HasPerfectQteAp() => hasPerfectQteAp;
    public bool HasReactionCostReduction() => hasReactionCostReduction && !reactionCostReductionUsed;
    public bool HasReactionApRefund() => hasReactionApRefund && !reactionApRefundUsedThisTurn;
    public int GetReactionApRefundAmount() => reactionApRefundAmount;
    public bool HasMarkTransfer() => hasMarkTransfer;
    public int GetMarkTransferCount() => markTransferCount;
    public bool HasPerfectReactionSaveMark() => hasPerfectReactionSaveMark;
    public int GetPerfectReactionSaveMarkCount() => perfectReactionSaveMarkCount;
    public bool HasReactionDouble() => hasReactionDouble && !reactionDoubleUsed;
    public float GetReactionDoubleMultiplier() => reactionDoubleMultiplier;
    public bool HasDualReactionShield() => hasDualReactionShield && !dualReactionShieldUsed;
    public int GetDualReactionShieldPercent() => dualReactionShieldPercent;
    public bool HasReactionWeaken() => hasReactionWeaken && !reactionWeakenUsedThisTurn;
    public int GetReactionWeakenDuration() => reactionWeakenDuration;
    public bool ShouldHideEnemyMarks() => hideEnemyMarks;
    public int GetReactionExtraMarkCost() => reactionExtraMarkCost;
    
    // Called when AP reduction skill discount is consumed
    public void ConsumeAPReductionSkill()
    {
        if (apReductionSkillsRemaining > 0) apReductionSkillsRemaining--;
    }
    
    // Called when first mark bonus is used this turn
    public bool TryConsumeFirstMarkBonus()
    {
        if (!hasFirstMarkBonus || firstMarkAppliedThisTurn) return false;
        firstMarkAppliedThisTurn = true;
        return true;
    }
    
    // Called when perfect QTE AP bonus is triggered
    public bool TryConsumePerfectQteAp()
    {
        if (!hasPerfectQteAp || perfectQteApUsedThisTurn) return false;
        perfectQteApUsedThisTurn = true;
        return true;
    }
    
    // Called when reaction cost reduction is used
    public void ConsumeReactionCostReduction() { reactionCostReductionUsed = true; }
    
    // Called when reaction AP refund is used
    public void ConsumeReactionApRefund() { reactionApRefundUsedThisTurn = true; }
    
    // Called when reaction double is used
    public void ConsumeReactionDouble() { reactionDoubleUsed = true; }
    
    // Called when dual reaction shield is used
    public void ConsumeDualReactionShield() { dualReactionShieldUsed = true; }
    
    // Called when reaction weaken is triggered this turn
    public void ConsumeReactionWeaken() { reactionWeakenUsedThisTurn = true; }
    
    // Deadeye Counter: consume guaranteed crit if ready (called BEFORE OnRelicSkillUsed)
    public bool ConsumeDeadeyeCritIfReady()
    {
        if (!deadeyeCritReady) return false;
        deadeyeCritReady = false;
        Debug.Log("[Player] Deadeye Counter: guaranteed crit consumed");
        return true;
    }
    public bool IsDeadeyeCritReady() => deadeyeCritReady;
    
    // ── Relic State for UI ──
    
    public struct RelicStateInfo
    {
        public string RelicId;
        public int CurrentCount;  // progress toward next trigger
        public int MaxCount;      // threshold
        public bool IsReady;      // buff active / available
        public bool JustTriggered; // flash/shake (one frame)
    }
    
    public List<RelicStateInfo> GetRelicStates()
    {
        var states = new List<RelicStateInfo>();
        foreach (var relic in relics)
        {
            var s = new RelicStateInfo { RelicId = relic.Id };
            bool hasCounter = false;
            
            // Counter-based relics
            if (relic.Trigger == "onSkillUse" && relic.TriggerInterval > 0)
            {
                s.MaxCount = relic.TriggerInterval;
                s.CurrentCount = relicSkillUseCounter % relic.TriggerInterval;
                hasCounter = true;
                // Check ready state by effect
                if (relic.Effects != null)
                    foreach (var eff in relic.Effects)
                        if (eff.effectId == "eff_temp_crit_bonus") s.IsReady = deadeyeCritReady;
            }
            else if (relic.Trigger == "onTurnStart" && relic.TriggerInterval > 0)
            {
                s.MaxCount = relic.TriggerInterval;
                s.CurrentCount = relicTurnCounter > 0 ? relicTurnCounter % relic.TriggerInterval : 0;
                hasCounter = true;
                if (relic.Effects != null)
                    foreach (var eff in relic.Effects)
                    {
                        if (eff.effectId == "eff_extra_marks") s.IsReady = extraMarksThisTurn > 0;
                        else if (eff.effectId == "eff_ap_cost_reduction") s.IsReady = apReductionThisTurn > 0 && apReductionSkillsRemaining > 0;
                    }
            }
            else if (relic.Effects != null)
            {
                foreach (var eff in relic.Effects)
                {
                    if (eff.effectId == "eff_refresh_random_skill" && eff.apSpentThreshold > 0)
                    {
                        s.MaxCount = eff.apSpentThreshold;
                        s.CurrentCount = relicApSpentCounter;
                        hasCounter = true;
                    }
                    else if (eff.effectId == "eff_reaction_double") s.IsReady = hasReactionDouble && !reactionDoubleUsed;
                    else if (eff.effectId == "eff_dual_reaction_shield") s.IsReady = hasDualReactionShield && !dualReactionShieldUsed;
                    else if (eff.effectId == "eff_reaction_cost_reduction") s.IsReady = hasReactionCostReduction && !reactionCostReductionUsed;
                    else if (eff.effectId == "eff_reaction_ap_refund") s.IsReady = hasReactionApRefund && !reactionApRefundUsedThisTurn;
                    else if (eff.effectId == "eff_perfect_qte_ap") s.IsReady = hasPerfectQteAp && !perfectQteApUsedThisTurn;
                    else if (eff.effectId == "eff_first_mark_bonus") s.IsReady = hasFirstMarkBonus && !firstMarkAppliedThisTurn;
                }
            }
            
            s.JustTriggered = relicJustTriggered.Contains(relic.Id);
            
            if (hasCounter || s.IsReady || s.JustTriggered)
                states.Add(s);
        }
        return states;
    }
    
    public void ClearRelicJustTriggered() { relicJustTriggered.Clear(); }
    
    // Return relic buffs to show in player status bar during combat
    public List<ReactionChipInfo> GetRelicBuffChips()
    {
        var chips = new List<ReactionChipInfo>();
        foreach (var relic in relics)
        {
            if (relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_temp_crit_bonus" && deadeyeCritReady)
                    chips.Add(new ReactionChipInfo("Deadeye Ready", "Next skill is a guaranteed critical hit.", 99, true));
                else if (eff.effectId == "eff_extra_marks" && extraMarksThisTurn > 0 && relic.Trigger == "onTurnStart")
                    chips.Add(new ReactionChipInfo("Mark Echo", $"+{extraMarksThisTurn} extra mark(s) this turn.", 1, true));
                else if (eff.effectId == "eff_ap_cost_reduction" && apReductionThisTurn > 0 && apReductionSkillsRemaining > 0)
                    chips.Add(new ReactionChipInfo("Rhythm Discount", $"-{apReductionThisTurn} AP cost for {apReductionSkillsRemaining} skill(s).", 1, true));
                else if (eff.effectId == "eff_reaction_double" && hasReactionDouble && !reactionDoubleUsed)
                    chips.Add(new ReactionChipInfo("Reactor Core", "Next reaction triggers twice.", 99, true));
                else if (eff.effectId == "eff_dual_reaction_shield" && hasDualReactionShield && !dualReactionShieldUsed)
                    chips.Add(new ReactionChipInfo("Dual Specialist", $"Dual Reaction grants {dualReactionShieldPercent}% HP shield.", 99, true));
                else if (eff.effectId == "eff_reaction_cost_reduction" && hasReactionCostReduction && !reactionCostReductionUsed)
                    chips.Add(new ReactionChipInfo("Catalyst Splinter", "Next dual reaction costs less marks.", 99, true));
                else if (eff.effectId == "eff_reaction_ap_refund" && hasReactionApRefund && !reactionApRefundUsedThisTurn)
                    chips.Add(new ReactionChipInfo("Reaction Rebate", $"First reaction refunds {reactionApRefundAmount} AP.", 1, true));
                else if (eff.effectId == "eff_perfect_qte_ap" && hasPerfectQteAp && !perfectQteApUsedThisTurn)
                    chips.Add(new ReactionChipInfo("Focus Lens", $"Perfect QTE grants +{perfectQteApAmount} AP.", 1, true));
                else if (eff.effectId == "eff_first_mark_bonus" && hasFirstMarkBonus && !firstMarkAppliedThisTurn)
                    chips.Add(new ReactionChipInfo("Marking Needle", "First mark this turn applies +1 extra.", 1, true));
            }
        }
        return chips;
    }
    
    public float GetMissingHPBonusDamagePercent()
    {
        float maxBonus = 0f;
        foreach (var relic in relics)
        {
            if (relic.Trigger != "passive" || relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_missing_hp_damage" && eff.maxBonusPercent > maxBonus)
                    maxBonus = eff.maxBonusPercent;
            }
        }
        if (maxBonus <= 0) return 0f;
        float missingPercent = 1f - ((float)health / maxHealth);
        return Mathf.Min(missingPercent * 100f, maxBonus);
    }

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
        physicalResist = 10;
        elementalResist = 10;
        
        // Clear all run-specific collections
        relics.Clear();
        potionInventory.Clear();
        
        // Reset temporary bonuses
        tempCritChanceBonus = 0;
        tempCritDamageBonus = 0;
        
        // Reset relic trigger tracking
        relicSkillUseCounter = 0;
        relicTurnCounter = 0;
        relicApSpentCounter = 0;
        bankedAP = 0;
        hasAPBanking = false;
        statusImmunities.Clear();
        energyCostMultiplier = 1f;
        extraMarksThisTurn = 0;
        apReductionThisTurn = 0;
        apReductionSkillsRemaining = 0;
        phoenixFeatherUsed = false;
        crackedBatteryTurnsLeft = 0;
        sipheringApDelta = 0;
        hasFirstMarkBonus = false;
        firstMarkAppliedThisTurn = false;
        hasPerfectQteAp = false;
        perfectQteApUsedThisTurn = false;
        perfectQteApAmount = 0;
        hasReactionCostReduction = false;
        reactionCostReductionUsed = false;
        hasReactionApRefund = false;
        reactionApRefundUsedThisTurn = false;
        reactionApRefundAmount = 0;
        hasMarkTransfer = false;
        markTransferCount = 0;
        hasPerfectReactionSaveMark = false;
        perfectReactionSaveMarkCount = 0;
        hasReactionDouble = false;
        reactionDoubleUsed = false;
        reactionDoubleMultiplier = 0.5f;
        hasDualReactionShield = false;
        dualReactionShieldUsed = false;
        dualReactionShieldPercent = 0;
        hasReactionWeaken = false;
        reactionWeakenUsedThisTurn = false;
        reactionWeakenDuration = 0;
        hideEnemyMarks = false;
        reactionExtraMarkCost = 0;
        deadeyeCritReady = false;
        relicJustTriggered.Clear();
        disableDefensiveQTETurns = 0;
        disableReactionQTE = false;
        disableSigils = false;
        
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
        
        // Reset enemy debuffs
        ClearAllDebuffs();
    }
    
    // ========== ENEMY DEBUFF SYSTEM ==========
    
    /// <summary>
    /// Apply Weaken debuff: reduces player damage dealt by magnitude% for duration turns.
    /// </summary>
    public void ApplyWeaken(float magnitude, int duration)
    {
        weakenPercent = magnitude;
        weakenTurns = duration;
    }
    
    /// <summary>
    /// Apply Sunder debuff: reduces player shield gain by magnitude% for duration turns.
    /// </summary>
    public void ApplySunder(float magnitude, int duration)
    {
        sunderPercent = magnitude;
        sunderTurns = duration;
    }
    
    /// <summary>
    /// Apply Vulnerable debuff: increases damage taken by magnitude% for duration turns.
    /// Stacks additively up to maxStacks total magnitude.
    /// </summary>
    public void ApplyVulnerable(float magnitude, int duration, int maxStacks)
    {
        vulnerableMaxStacks = maxStacks;
        vulnerablePercent += magnitude;
        if (maxStacks > 0 && vulnerablePercent > maxStacks)
        {
            vulnerablePercent = maxStacks;
        }
        vulnerableTurns = Mathf.Max(vulnerableTurns, duration);
    }
    
    /// <summary>
    /// Apply Stun debuff: skips the player's next turn(s).
    /// </summary>
    public void ApplyStun(int duration)
    {
        isStunned = true;
        stunTurns = duration;
    }
    
    /// <summary>
    /// Apply DoT to the player: takes damagePerTick each turn for duration turns.
    /// </summary>
    public void ApplyDoT(int damagePerTick, int duration, string source)
    {
        playerDoTDamage = damagePerTick;
        playerDoTTurns = duration;
        playerDoTSource = source;
    }
    
    /// <summary>
    /// Remove all shield from the player (used by Syphon Magic).
    /// </summary>
    public void RemoveAllShield()
    {
        statusEffects.SetShield(0, 0);
    }
    
    /// <summary>
    /// Get the weaken damage reduction multiplier (1.0 = no reduction, 0.5 = 50% reduction).
    /// </summary>
    public float GetWeakenMultiplier()
    {
        if (weakenTurns > 0 && weakenPercent > 0f)
        {
            return 1f - (weakenPercent / 100f);
        }
        return 1f;
    }
    
    /// <summary>
    /// Get the sunder shield gain reduction multiplier (1.0 = no reduction, 0.5 = 50% reduction).
    /// </summary>
    public float GetSunderMultiplier()
    {
        if (sunderTurns > 0 && sunderPercent > 0f)
        {
            return 1f - (sunderPercent / 100f);
        }
        return 1f;
    }
    
    /// <summary>
    /// Get the vulnerable damage taken multiplier (1.0 = normal, 1.5 = +50% damage taken).
    /// </summary>
    public float GetVulnerableMultiplier()
    {
        if (vulnerableTurns > 0 && vulnerablePercent > 0f)
        {
            return 1f + (vulnerablePercent / 100f);
        }
        return 1f;
    }
    
    /// <summary>
    /// Check and consume player stun at start of player turn.
    /// Returns true if the player is stunned and turn should be skipped.
    /// </summary>
    public bool CheckAndConsumePlayerStun()
    {
        if (isStunned && stunTurns > 0)
        {
            stunTurns--;
            if (stunTurns <= 0)
            {
                isStunned = false;
            }
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Tick player DoT at start of player turn. Returns damage dealt, or 0 if no DoT active.
    /// </summary>
    public int TickPlayerDoT()
    {
        if (playerDoTTurns > 0 && playerDoTDamage > 0)
        {
            playerDoTTurns--;
            int damage = playerDoTDamage;
            health -= damage;
            if (health < 0) health = 0;
            
            GameLog.Status(GameLog.Join(
                "Tick",
                GameLog.KV("target", "Player"),
                GameLog.KV("type", "DoT"),
                GameLog.KV("damage", damage),
                GameLog.KV("turnsLeft", playerDoTTurns),
                GameLog.KV("source", playerDoTSource),
                GameLog.KV("hpAfter", health)
            ));
            
            if (playerDoTTurns <= 0)
            {
                playerDoTDamage = 0;
                playerDoTSource = null;
            }
            
            return damage;
        }
        return 0;
    }
    
    /// <summary>
    /// Tick all debuff durations. Call at start of player turn.
    /// </summary>
    public void TickDebuffs()
    {
        if (weakenTurns > 0)
        {
            weakenTurns--;
            if (weakenTurns <= 0) weakenPercent = 0f;
        }
        if (sunderTurns > 0)
        {
            sunderTurns--;
            if (sunderTurns <= 0) sunderPercent = 0f;
        }
        if (vulnerableTurns > 0)
        {
            vulnerableTurns--;
            if (vulnerableTurns <= 0) vulnerablePercent = 0f;
        }
    }
    
    /// <summary>
    /// Clear all enemy debuffs (used on new run / combat reset).
    /// </summary>
    public void ClearAllDebuffs()
    {
        weakenPercent = 0f;
        weakenTurns = 0;
        sunderPercent = 0f;
        sunderTurns = 0;
        vulnerablePercent = 0f;
        vulnerableTurns = 0;
        vulnerableMaxStacks = 0;
        isStunned = false;
        stunTurns = 0;
        playerDoTDamage = 0;
        playerDoTTurns = 0;
        playerDoTSource = null;
    }
    
    // Query debuff states for UI
    public bool IsWeakened => weakenTurns > 0 && weakenPercent > 0f;
    public bool IsSundered => sunderTurns > 0 && sunderPercent > 0f;
    public bool IsVulnerable => vulnerableTurns > 0 && vulnerablePercent > 0f;
    public bool IsPlayerStunned => isStunned && stunTurns > 0;
    public bool HasPlayerDoT => playerDoTTurns > 0 && playerDoTDamage > 0;
    
    // Debuff detail getters for UI tooltips
    public float WeakenPercent => weakenPercent;
    public int WeakenTurns => weakenTurns;
    public float SunderPercent => sunderPercent;
    public int SunderTurns => sunderTurns;
    public float VulnerablePercent => vulnerablePercent;
    public int VulnerableTurns => vulnerableTurns;
    public int StunTurns => stunTurns;
    public int PlayerDoTDamage => playerDoTDamage;
    public int PlayerDoTTurns => playerDoTTurns;
    
    // ========== REACTION BUFF SYSTEM ==========
    
    // CritDamage buff: +value% crit damage for duration turns
    private float reactionCritDamageBonus;
    private int reactionCritDamageTurns;
    
    // BonusAP buff: +value max AP and current AP for duration turns
    private int reactionBonusAP;
    private int reactionBonusAPTurns;
    
    // ReflectiveArmor buff: reflect value% damage back for duration turns
    private float reactionReflectPercent;
    private int reactionReflectTurns;
    
    // DamageReduction buff: reduce incoming damage by value% for duration turns
    private float reactionDamageReduction;
    private int reactionDamageReductionTurns;
    
    // RockDamageWhileShielded: +value% rock damage while player has shield (passive, no duration)
    private float reactionRockDmgWhileShielded;
    
    /// <summary>
    /// Apply a reaction buff by type. Called by ReactionEffectEngine.
    /// </summary>
    public void ApplyReactionBuff(string buffType, float value, int duration, bool refreshable)
    {
        switch (buffType)
        {
            case "CritDamage":
                reactionCritDamageBonus = value;
                reactionCritDamageTurns = refreshable ? Mathf.Max(reactionCritDamageTurns, duration) : duration;
                break;
                
            case "BonusAP":
                int extraAP = Mathf.RoundToInt(value);
                if (reactionBonusAPTurns <= 0)
                {
                    // First application: increase max and current AP
                    reactionBonusAP = extraAP;
                    maxAP += extraAP;
                    currentAP += extraAP;
                }
                // Refresh duration
                if (refreshable)
                    reactionBonusAPTurns = Mathf.Max(reactionBonusAPTurns, duration);
                else
                    reactionBonusAPTurns = duration;
                break;
                
            case "ReflectiveArmor":
                reactionReflectPercent = value;
                reactionReflectTurns = refreshable ? Mathf.Max(reactionReflectTurns, duration) : duration;
                break;
                
            case "DamageReduction":
                reactionDamageReduction = value;
                reactionDamageReductionTurns = refreshable ? Mathf.Max(reactionDamageReductionTurns, duration) : duration;
                break;
                
            case "RockDamageWhileShielded":
                reactionRockDmgWhileShielded = value;
                break;
        }
        
        GameLog.Status(GameLog.Join(
            "ReactionBuff",
            GameLog.KV("buff", buffType),
            GameLog.KV("value", value),
            GameLog.KV("dur", duration),
            GameLog.KV("refresh", refreshable)
        ), GameLogVerbosity.Verbose);
    }
    
    /// <summary>
    /// Tick reaction buff durations. Call at start of player turn.
    /// </summary>
    public void TickReactionBuffs()
    {
        if (reactionCritDamageTurns > 0)
        {
            reactionCritDamageTurns--;
            if (reactionCritDamageTurns <= 0) reactionCritDamageBonus = 0f;
        }
        
        if (reactionBonusAPTurns > 0)
        {
            reactionBonusAPTurns--;
            if (reactionBonusAPTurns <= 0 && reactionBonusAP > 0)
            {
                // Remove the bonus AP
                maxAP -= reactionBonusAP;
                if (currentAP > maxAP) currentAP = maxAP;
                reactionBonusAP = 0;
            }
        }
        
        if (reactionReflectTurns > 0)
        {
            reactionReflectTurns--;
            if (reactionReflectTurns <= 0) reactionReflectPercent = 0f;
        }
        
        if (reactionDamageReductionTurns > 0)
        {
            reactionDamageReductionTurns--;
            if (reactionDamageReductionTurns <= 0) reactionDamageReduction = 0f;
        }
    }
    
    /// <summary>
    /// Clear all reaction buffs (on combat end).
    /// </summary>
    public void ClearReactionBuffs()
    {
        reactionCritDamageBonus = 0f;
        reactionCritDamageTurns = 0;
        if (reactionBonusAP > 0)
        {
            maxAP -= reactionBonusAP;
            if (currentAP > maxAP) currentAP = maxAP;
        }
        reactionBonusAP = 0;
        reactionBonusAPTurns = 0;
        reactionReflectPercent = 0f;
        reactionReflectTurns = 0;
        reactionDamageReduction = 0f;
        reactionDamageReductionTurns = 0;
        reactionRockDmgWhileShielded = 0f;
        reactionChips.Clear();
    }
    
    // Query reaction buff states
    public float GetReactionCritDamageBonus() => reactionCritDamageTurns > 0 ? reactionCritDamageBonus / 100f : 0f;
    public bool HasReactionBonusAP => reactionBonusAPTurns > 0 && reactionBonusAP > 0;
    public int GetReactionBonusAPAmount() => reactionBonusAP;
    public int GetReactionBonusAPTurns() => reactionBonusAPTurns;
    public float GetReflectPercent() => reactionReflectTurns > 0 ? reactionReflectPercent / 100f : 0f;
    public bool HasReflectiveArmor => reactionReflectTurns > 0 && reactionReflectPercent > 0f;
    public float GetDamageReductionPercent() => reactionDamageReductionTurns > 0 ? reactionDamageReduction / 100f : 0f;
    public bool HasDamageReduction => reactionDamageReductionTurns > 0 && reactionDamageReduction > 0f;
    public float GetRockDamageWhileShieldedBonus() => GetShield() > 0 ? reactionRockDmgWhileShielded / 100f : 0f;
    
    // ========== REACTION CHIP DISPLAY ==========
    
    private List<ReactionChipInfo> reactionChips = new List<ReactionChipInfo>();
    
    public void AddReactionChip(string name, string tooltip, int turns)
    {
        for (int i = 0; i < reactionChips.Count; i++)
        {
            if (reactionChips[i].ChipName == name)
            {
                reactionChips[i].Tooltip = tooltip;
                reactionChips[i].TurnsRemaining = Mathf.Max(reactionChips[i].TurnsRemaining, turns);
                return;
            }
        }
        reactionChips.Add(new ReactionChipInfo(name, tooltip, turns, true));
    }
    
    public void TickReactionChips()
    {
        for (int i = reactionChips.Count - 1; i >= 0; i--)
        {
            reactionChips[i].TurnsRemaining--;
            if (reactionChips[i].TurnsRemaining <= 0)
            {
                reactionChips.RemoveAt(i);
            }
        }
    }
    
    public List<ReactionChipInfo> GetReactionChips() => reactionChips;
}
