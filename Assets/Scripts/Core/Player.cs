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
    private List<RelicData> relics = new List<RelicData>();
    private List<PotionData> potionInventory = new List<PotionData>();
    private const int MAX_POTIONS = 4;
    private ElementalDamage elementalDamage = new ElementalDamage();
    private Element affinity = Element.None;
    private CharacterData selectedCharacter = null;
    
    private ElementalOrbSystem orbSystem = new ElementalOrbSystem();
    private bool hasElementPair = false;

    void Awake()
    {
        var stats = DataCache.PlayerStats;
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

    public int GetHealth() => health;
    public int GetMaxHealth() => maxHealth;
    public int GetCharacterDamage() => selectedCharacter != null ? selectedCharacter.Damage : 0;
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

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health < 0) health = 0;
        Debug.Log($"[Player] Took {amount} damage. Health: {health}/{maxHealth}");
    }

    public void Heal(int amount)
    {
        health += amount;
        if (health > maxHealth) health = maxHealth;
        Debug.Log($"[Player] Healed {amount}. Health: {health}/{maxHealth}");
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
        Debug.Log($"[Player] Selected {character.DisplayName} (+{character.Damage} damage)");
    }

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
}
