using System.Collections.Generic;
using UnityEngine;

public static class DataCache
{
    public static List<EnemyData> Enemies { get; private set; }
    public static List<EnemyData> RegularEnemies { get; private set; }
    public static List<EnemyData> EliteEnemies { get; private set; }
    public static List<EnemyData> BossEnemies { get; private set; }
    public static List<RestData> RestOptions { get; private set; }
    public static List<WeaponData> Weapons { get; private set; }
    public static List<PotionData> Potions { get; private set; }
    public static List<RelicData> Relics { get; private set; }
    public static PlayerData PlayerStats { get; private set; }

    public static bool IsLoaded { get; private set; }

    public static void LoadAll()
    {
        Enemies = LoadEnemies();
        RestOptions = LoadRestOptions();
        Weapons = LoadWeapons();
        Potions = LoadPotions();
        Relics = LoadRelics();
        PlayerStats = LoadPlayerStats();

        IsLoaded = true;
        Debug.Log($"[DataCache] Loaded: {Enemies.Count} enemies, {RestOptions.Count} rest options, {Weapons.Count} weapons, {Potions.Count} potions, {Relics.Count} relics, player stats");
    }

    private static List<EnemyData> LoadEnemies()
    {
        var csv = Resources.Load<TextAsset>("Data/enemy");
        var rows = CSVParser.Parse(csv.text);
        var list = new List<EnemyData>();

        RegularEnemies = new List<EnemyData>();
        EliteEnemies = new List<EnemyData>();
        BossEnemies = new List<EnemyData>();

        foreach (var row in rows)
        {
            var enemy = new EnemyData
            {
                Type = CSVParser.ParseString(row, "Type"),
                Element = CSVParser.ParseString(row, "Element"),
                ElementID = CSVParser.ParseInt(row, "ElementID"),
                Health = CSVParser.ParseInt(row, "Health"),
                Damage = CSVParser.ParseInt(row, "Damage")
            };
            
            list.Add(enemy);
            
            if (enemy.IsRegular) RegularEnemies.Add(enemy);
            else if (enemy.IsElite) EliteEnemies.Add(enemy);
            else if (enemy.IsBoss) BossEnemies.Add(enemy);
        }

        return list;
    }

    private static List<RestData> LoadRestOptions()
    {
        var csv = Resources.Load<TextAsset>("Data/rest");
        var rows = CSVParser.Parse(csv.text);
        var list = new List<RestData>();

        foreach (var row in rows)
        {
            list.Add(new RestData
            {
                Element = CSVParser.ParseString(row, "Element"),
                ElementID = CSVParser.ParseInt(row, "ElementID"),
                StatAffected = CSVParser.ParseString(row, "Stat Affected"),
                Amount = CSVParser.ParseInt(row, "Amount")
            });
        }

        return list;
    }

    private static List<WeaponData> LoadWeapons()
    {
        var csv = Resources.Load<TextAsset>("Data/weapon");
        var rows = CSVParser.Parse(csv.text);
        var list = new List<WeaponData>();

        foreach (var row in rows)
        {
            list.Add(new WeaponData
            {
                DisplayName = CSVParser.ParseString(row, "DisplayName"),
                ElementID = CSVParser.ParseInt(row, "ElementID"),
                WeaponType = CSVParser.ParseString(row, "Weapon Type"),
                Damage = CSVParser.ParseInt(row, "Damage"),
                Skill1 = CSVParser.ParseString(row, "Skill1"),
                Skill2 = CSVParser.ParseString(row, "Skill2"),
                Skill3 = CSVParser.ParseString(row, "Skill3")
            });
        }

        return list;
    }

    private static List<PotionData> LoadPotions()
    {
        var csv = Resources.Load<TextAsset>("Data/potion");
        var rows = CSVParser.Parse(csv.text);
        var list = new List<PotionData>();

        foreach (var row in rows)
        {
            list.Add(new PotionData
            {
                DisplayName = CSVParser.ParseString(row, "DisplayName"),
                ElementID = CSVParser.ParseInt(row, "ElementID"),
                StatAffected = CSVParser.ParseString(row, "Stat Affected"),
                Amount = CSVParser.ParseInt(row, "Amount")
            });
        }

        return list;
    }

    private static List<RelicData> LoadRelics()
    {
        var csv = Resources.Load<TextAsset>("Data/relic");
        var rows = CSVParser.Parse(csv.text);
        var list = new List<RelicData>();

        foreach (var row in rows)
        {
            list.Add(new RelicData
            {
                DisplayName = CSVParser.ParseString(row, "DisplayName"),
                ElementID = CSVParser.ParseInt(row, "ElementID"),
                StatAffected = CSVParser.ParseString(row, "Stat Affected"),
                Amount = CSVParser.ParseInt(row, "Amount")
            });
        }

        return list;
    }

    private static PlayerData LoadPlayerStats()
    {
        var csv = Resources.Load<TextAsset>("Data/player");
        if (csv == null)
        {
            throw new System.Exception("Missing player.csv. Expected a TextAsset at Resources/Data/player.csv. Run Tools/Data/Update All CSVs to generate it.");
        }

        var rows = CSVParser.Parse(csv.text);
        if (rows == null || rows.Count == 0)
        {
            throw new System.Exception("player.csv has no data rows. Ensure the sheet contains at least one non-empty row and is exported correctly.");
        }

        var row = rows[0];

        return new PlayerData
        {
            MaxHealth = CSVParser.ParseInt(row, "MaxHealth"),
            Damage = CSVParser.ParseInt(row, "Damage"),
            Gold = CSVParser.ParseInt(row, "Gold"),
            MaxEnergy = CSVParser.ParseInt(row, "MaxEnergy"),
            CritChance = CSVParser.ParseInt(row, "CritChance"),
            CritDamage = CSVParser.ParseFloat(row, "CritDamage")
        };
    }
}
