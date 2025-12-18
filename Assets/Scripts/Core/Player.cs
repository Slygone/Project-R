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
    private List<RelicData> relics = new List<RelicData>();
    private ElementalDamage elementalDamage = new ElementalDamage();
    private Element affinity = Element.None;
    private WeaponData equippedWeapon = null;

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

        Debug.Log($"[Player] Initialized - HP: {health}/{maxHealth}, Base Damage: {baseDamage}, Gold: {gold}, Energy: {energy}/{maxEnergy}, Crit: {critChance}% x{critDamage}");
    }

    public int GetHealth() => health;
    public int GetMaxHealth() => maxHealth;
    public int GetBaseDamage() => baseDamage;
    public int GetWeaponDamage() => equippedWeapon != null ? equippedWeapon.Damage : 0;
    public int GetTotalDamage() => baseDamage + GetWeaponDamage() + GetAffinityBonus();
    public int GetAffinityBonus() => affinity != Element.None ? elementalDamage.Get(affinity) : 0;
    public int GetElementalBonus(Element element) => elementalDamage.Get(element);
    public ElementalDamage GetElementalDamage() => elementalDamage;
    public Element GetAffinity() => affinity;
    public bool HasAffinity() => affinity != Element.None;
    public WeaponData GetWeapon() => equippedWeapon;
    public bool HasWeapon() => equippedWeapon != null;
    public int GetGold() => gold;
    public int GetEnergy() => energy;
    public int GetMaxEnergy() => maxEnergy;
    public int GetCritChance() => critChance;
    public float GetCritDamage() => critDamage;

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

    public void EquipWeapon(WeaponData weapon)
    {
        equippedWeapon = weapon;
        Debug.Log($"[Player] Equipped {weapon.DisplayName} (+{weapon.Damage} damage)");
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
                break;
            case "damage":
            case "basedamage":
                baseDamage += relic.Amount;
                break;
            case "critchance":
                critChance += relic.Amount;
                break;
            case "energy":
            case "maxenergy":
                maxEnergy += relic.Amount;
                energy += relic.Amount;
                break;
        }
    }

    public List<RelicData> GetRelics() => relics;

    public bool IsAlive() => health > 0;
}
