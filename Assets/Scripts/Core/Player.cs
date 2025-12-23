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
            Debug.LogError("[Player] PlayerStats is null! DataCache may not have loaded properly.");
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

        Debug.Log($"[Player] Initialized - HP: {health}/{maxHealth}, Gold: {gold}, Energy: {energy}/{maxEnergy}, Crit: {critChance}% x{critDamage}, Resist: {baseResistance}+{bonusResistance}");
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
            Debug.Log($"[Player] Incoming {originalAmount} damage fully absorbed by block/shield!");
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
                Debug.Log($"[Player] Dropped to threshold level {currentThresholdLevel}! Max recoverable now: {GetMaxRecoverableHP()}");
            }
        }
        
        Debug.Log($"[Player] Took {amount} damage (original: {originalAmount}). Health: {health}/{maxHealth}, Shield: {GetShield()} (MaxRecoverable: {GetMaxRecoverableHP()}, Wounds: {WoundCount})");
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
        
        Debug.Log($"[Player] Healed {amount}. Health: {health}/{maxHealth} (Max recoverable: {maxRecoverable}, Wounds: {WoundCount})");
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
        lowestThresholdLevel = health / THRESHOLD_SIZE; // Reset to current threshold
        Debug.Log($"[Player] Wounds cleared! Can now heal to {GetMaxRecoverableHP()}");
    }
    
    // Heal and clear wounds (full rest)
    public void FullRest(int healPercent)
    {
        int healAmount = Mathf.RoundToInt(maxHealth * (healPercent / 100f));
        health += healAmount;
        if (health > maxHealth) health = maxHealth;
        // Clear wounds AFTER healing so we can heal to full, then reset threshold based on new health
        lowestThresholdLevel = health / THRESHOLD_SIZE;
        Debug.Log($"[Player] Full rest: healed {healAmount} HP, wounds cleared. Health: {health}/{maxHealth} (MaxRecoverable: {GetMaxRecoverableHP()})");
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        health += amount;
        Debug.Log($"[Player] Max health increased by {amount}. Now {health}/{maxHealth}");
    }

    public void IncreaseCharacterDamage(int amount)
    {
        if (selectedCharacter != null)
        {
            selectedCharacter.Damage += amount;
            Debug.Log($"[Player] Character damage increased by {amount}. Now {selectedCharacter.Damage}");
        }
        else
        {
            Debug.LogWarning("[Player] No character selected, cannot increase damage");
        }
    }
    
    // Phase 6: XP-based stat upgrades
    public void AddCritChance(int amount)
    {
        critChance += amount;
        Debug.Log($"[Player] Crit chance increased by {amount}%. Now {critChance}%");
    }
    
    public void AddCritDamage(float amount)
    {
        critDamage += amount;
        Debug.Log($"[Player] Crit damage increased by {amount}. Now {critDamage:F1}x");
    }
    
    public void IncreaseDamageRange(int amount)
    {
        if (selectedCharacter != null)
        {
            selectedCharacter.Damage += amount;
            Debug.Log($"[Player] Damage range increased by {amount}. Base damage now {selectedCharacter.Damage}");
        }
        else
        {
            Debug.LogWarning("[Player] No character selected, cannot increase damage range");
        }
    }
    
    public void ClearDebuffs()
    {
        statusEffects.ClearDebuffs();
        dirtyStabConsecutiveUses = 0;
        Debug.Log("[Player] Cleared all debuffs and counters");
    }

    public void AddBaseDamage(int amount)
    {
        baseDamage += amount;
        Debug.Log($"[Player] Base damage increased by {amount}. Total base damage: {baseDamage}");
    }

    public void AddElementalDamage(Element element, int amount)
    {
        elementalDamage.Add(element, amount);
        Debug.Log($"[Player] {element} damage increased by {amount}. Total {element} bonus: {elementalDamage.Get(element)}");
    }

    public void SetAffinity(Element element)
    {
        affinity = element;
        Debug.Log($"[Player] Affinity set to {element}");
    }
    
    public void SetElementPair(ElementPair pair)
    {
        orbSystem.SetElementPair(pair.OrbA, pair.OrbB);
        affinity = pair.OrbA;
        hasElementPair = true;
        Debug.Log($"[Player] Element pair set: {pair.DisplayName} ({pair.OrbA} + {pair.OrbB})");
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
        Debug.Log($"[Player] Selected {character.DisplayName} (+{character.Damage} damage, MaxEnergy: {maxEnergy})");
    }
    
    // ========== SKILL COOLDOWN & ENERGY SYSTEM ==========
    
    public int GetSkillCooldown(int skillIndex) => skillIndex >= 0 && skillIndex < 3 ? skillCooldowns[skillIndex] : 0;
    
    public void SetSkillCooldown(int skillIndex, int cooldown)
    {
        if (skillIndex >= 0 && skillIndex < 3)
        {
            skillCooldowns[skillIndex] = cooldown;
            Debug.Log($"[Player] Skill {skillIndex + 1} cooldown set to {cooldown} turns");
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
                Debug.Log($"[Player] Used Skill1, gained {selectedCharacter.Skill1EnergyGain} energy. Energy: {energy}/{maxEnergy}. CD: {skillCooldowns[0]} turns");
                break;
            case 1: // Skill 2
                skillCooldowns[1] = selectedCharacter.Skill2Cooldown;
                GainEnergy(selectedCharacter.Skill2EnergyGain);
                Debug.Log($"[Player] Used Skill2, gained {selectedCharacter.Skill2EnergyGain} energy. Energy: {energy}/{maxEnergy}. CD: {skillCooldowns[1]} turns");
                break;
            case 2: // Skill 3
                skillCooldowns[2] = selectedCharacter.Skill3Cooldown;
                SpendEnergy(selectedCharacter.Skill3EnergyCost);
                Debug.Log($"[Player] Used Skill3, spent {selectedCharacter.Skill3EnergyCost} energy. Energy: {energy}/{maxEnergy}. CD: {skillCooldowns[2]} turns");
                break;
        }
    }
    
    public void GainEnergy(int amount)
    {
        energy += amount;
        if (energy > maxEnergy) energy = maxEnergy;
        Debug.Log($"[Player] Gained {amount} energy. Total: {energy}/{maxEnergy}");
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
        Debug.Log($"[Player] Turn {combatTurnCount}: Cooldowns ticked. Skill1 CD: {skillCooldowns[0]}, Skill2 CD: {skillCooldowns[1]}, Skill3 CD: {skillCooldowns[2]}");
    }
    
    // Reset combat-specific state at combat start (cooldowns PERSIST between combats)
    public void ResetCombatState()
    {
        // NOTE: Cooldowns are NOT reset - they persist between combats
        energy = 0;
        combatTurnCount = 0;
        dirtyStabConsecutiveUses = 0;
        statusEffects.ClearCombatEffects(); // Keep shield, clear block
        Debug.Log($"[Player] Combat state reset. Energy: {energy}/{maxEnergy}, Shield: {GetShield()}. Cooldowns preserved: S1={skillCooldowns[0]}, S2={skillCooldowns[1]}, S3={skillCooldowns[2]}");
    }
    
    public int GetCombatTurnCount() => combatTurnCount;
    
    // ========== STATUS EFFECTS (SHIELD, BLOCK) ==========
    
    public int GetShield() => statusEffects.GetShieldValue();
    public int GetMaxShield() => Mathf.RoundToInt(maxHealth * 0.4f); // 40% of max HP cap
    
    public void AddShield(int amount)
    {
        int maxShield = GetMaxShield();
        statusEffects.AddShield(amount, maxShield);
        Debug.Log($"[Player] Gained {amount} shield. Total: {GetShield()}/{maxShield}");
    }
    
    public void ApplyBlock(float percentReduction)
    {
        statusEffects.AddEffect(new StatusEffect(StatusEffectType.Block, 1, percentReduction, "Riposte"));
        Debug.Log($"[Player] Riposte stance active: {percentReduction}% damage reduction on next hit");
    }
    
    public bool HasBlock() => statusEffects.HasEffect(StatusEffectType.Block);
    
    // Clear shield on world transition
    public void ClearShieldForWorldTransition()
    {
        statusEffects.ClearAllIncludingShield();
        Debug.Log("[Player] Shield cleared for world transition");
    }
    
    // ========== DIRTYSTAB TRACKING ==========
    // Can be used 3 turns in a row. Each consecutive use grants +20% damage (max 2 stacks).
    // After 3rd consecutive use, goes on 4-turn cooldown and stacks reset.
    
    public int GetDirtyStabStacks() => dirtyStabConsecutiveUses;
    
    public void IncrementDirtyStabUse()
    {
        dirtyStabConsecutiveUses++;
        Debug.Log($"[Player] DirtyStab consecutive use: {dirtyStabConsecutiveUses}");
        
        // After 3rd use, apply 4-turn cooldown and reset stacks
        if (dirtyStabConsecutiveUses >= 3)
        {
            skillCooldowns[0] = 4; // Force 4-turn cooldown on Skill1 (DirtyStab)
            dirtyStabConsecutiveUses = 0;
            Debug.Log("[Player] DirtyStab used 3 times! 4-turn cooldown triggered, stacks reset.");
        }
    }
    
    public void ResetDirtyStabStacks()
    {
        if (dirtyStabConsecutiveUses > 0)
        {
            Debug.Log($"[Player] DirtyStab stacks reset (was {dirtyStabConsecutiveUses})");
            dirtyStabConsecutiveUses = 0;
        }
    }
    
    public bool IsDirtyStabOnExtendedCooldown() => skillCooldowns[0] == 4 && dirtyStabConsecutiveUses == 0;

    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log($"[Player] Gold changed by {amount}. Total gold: {gold}");
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
        Debug.Log($"[Player] Acquired relic: {relic.DisplayName}");
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
            Debug.Log($"[Player] All Elements relic applied: +{relic.Amount} to each element");
            return;
        }
        
        Element element = ElementalDamage.ParseElement(stat);
        if (element != Element.None)
        {
            elementalDamage.Add(element, relic.Amount);
            if (element == affinity)
            {
                Debug.Log($"[Player] {element} relic matches affinity! +{relic.Amount} damage");
            }
            else
            {
                Debug.Log($"[Player] {element} relic stored but doesn't match {affinity} affinity");
            }
            return;
        }

        switch (stat)
        {
            case "health":
            case "maxhealth":
                maxHealth += relic.Amount;
                health += relic.Amount;
                Debug.Log($"[Player] Health relic applied: +{relic.Amount} max health");
                break;
            case "crit rate":
            case "critrate":
            case "critchance":
                critChance += relic.Amount;
                Debug.Log($"[Player] Crit Rate relic applied: +{relic.Amount}% crit chance (now {critChance}%)");
                break;
            case "crit damage":
            case "critdamage":
                critDamage += relic.Amount / 100f;
                Debug.Log($"[Player] Crit Damage relic applied: +{relic.Amount}% crit damage (now x{critDamage:F2})");
                break;
            case "energy":
            case "maxenergy":
                maxEnergy += relic.Amount;
                energy += relic.Amount;
                Debug.Log($"[Player] Energy relic applied: +{relic.Amount} max energy");
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
            Debug.Log($"[Player] Potion inventory full, cannot add {potion.DisplayName}");
            return;
        }
        potionInventory.Add(potion);
        Debug.Log($"[Player] Added {potion.DisplayName} to inventory ({potionInventory.Count}/{MAX_POTIONS})");
    }

    public bool UsePotion(int index, CombatEnemy target = null, Element elementOverride = Element.None)
    {
        if (index < 0 || index >= potionInventory.Count)
        {
            Debug.Log($"[Player] Invalid potion index: {index}");
            return false;
        }

        var potion = potionInventory[index];
        potionInventory.RemoveAt(index);

        var stat = potion.StatAffected.ToLower().Trim();
        switch (stat)
        {
            case "health":
                Heal(potion.Amount);
                Debug.Log($"[Player] Used {potion.DisplayName}: Healed {potion.Amount} HP");
                break;
            case "elemental afinity direct damage":
                if (target != null && target.IsAlive())
                {
                    Element element = elementOverride != Element.None ? elementOverride : GetRandomElement();
                    int damage = target.ApplyResistance(potion.Amount, element);
                    target.TakeDamage(damage);
                    Debug.Log($"[Player] Used {potion.DisplayName} ({element}): Dealt {damage} damage to {target.Name}");
                }
                else
                {
                    Debug.Log($"[Player] Used {potion.DisplayName}: No valid target for damage potion");
                    potionInventory.Insert(index, potion);
                    return false;
                }
                break;
            case "crit rate":
                tempCritChanceBonus += potion.Amount;
                Debug.Log($"[Player] Used {potion.DisplayName}: +{potion.Amount}% crit chance (now {GetCritChance()}%)");
                break;
            case "crit damage":
                tempCritDamageBonus += potion.Amount;
                Debug.Log($"[Player] Used {potion.DisplayName}: +{potion.Amount}% crit damage (now {GetCritDamage():F2}x)");
                break;
            default:
                Debug.LogWarning($"[Player] Unknown potion stat: {potion.StatAffected}");
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
        Debug.Log("[Player] World bonuses reset");
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
        
        Debug.Log($"[Player] Reset for new world - HP restored to {health}/{maxHealth}, potions cleared, temp bonuses reset");
        Debug.Log($"[Player] Keeping: {relics.Count} relics, {GetCharacterDamage()} character damage, elemental bonuses");
    }
}
