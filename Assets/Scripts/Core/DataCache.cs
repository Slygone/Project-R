using System.Collections.Generic;
using UnityEngine;

public static class DataCache
{
    public static List<EnemyData> Enemies { get; private set; }
    public static List<EnemyData> RegularEnemies { get; private set; }
    public static List<EnemyData> EliteEnemies { get; private set; }
    public static List<EnemyData> BossEnemies { get; private set; }
    public static List<RestData> RestOptions { get; private set; }
    public static List<CharacterData> Characters { get; private set; }
    public static List<PotionData> Potions { get; private set; }
    public static List<RelicData> Relics { get; private set; }
    public static PlayerData PlayerStats { get; private set; }
    public static Dictionary<string, float> QTEMultipliers { get; private set; }
    public static List<ReactionData> Reactions { get; private set; }

    public static bool IsLoaded { get; private set; }

    public static void LoadAll()
    {
        Enemies = LoadEnemies();
        RestOptions = LoadRestOptions();
        Characters = LoadCharacters();
        Potions = LoadPotions();
        Relics = LoadRelics();
        PlayerStats = LoadPlayerStats();
        QTEMultipliers = LoadQTEMultipliers();
        Reactions = LoadReactions();

        IsLoaded = true;
        Debug.Log($"[DataCache] Loaded: {Enemies.Count} enemies, {RestOptions.Count} rest options, {Characters.Count} characters, {Potions.Count} potions, {Relics.Count} relics, {QTEMultipliers.Count} QTE results, {Reactions.Count} reactions, player stats");
    }

    // Parse multiplier from formula like "Health *1.7" or "Damage * 2"
    private static float ParseMultiplier(string formula)
    {
        if (string.IsNullOrEmpty(formula)) return 1f;
        
        // Find the * character and extract the number after it
        int starIndex = formula.IndexOf('*');
        if (starIndex >= 0 && starIndex < formula.Length - 1)
        {
            string numPart = formula.Substring(starIndex + 1).Trim();
            if (float.TryParse(numPart, System.Globalization.NumberStyles.Float, 
                System.Globalization.CultureInfo.InvariantCulture, out float result))
            {
                return result;
            }
        }
        
        // Try parsing as plain number
        if (float.TryParse(formula.Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float plain))
        {
            return plain;
        }
        
        return 1f;
    }
    
    // Parse addend from formula like "Base Resistance + 10"
    private static int ParseAddend(string formula)
    {
        if (string.IsNullOrEmpty(formula)) return 0;
        
        // Find the + character and extract the number after it
        int plusIndex = formula.IndexOf('+');
        if (plusIndex >= 0 && plusIndex < formula.Length - 1)
        {
            string numPart = formula.Substring(plusIndex + 1).Trim();
            if (int.TryParse(numPart, out int result))
            {
                return result;
            }
        }
        
        // Try parsing as plain number
        if (int.TryParse(formula.Trim(), out int plain))
        {
            return plain;
        }
        
        return 0;
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
                DisplayName = CSVParser.ParseString(row, "DisplayName"),
                EnemyID = CSVParser.ParseInt(row, "EnemyID"),
                Health = CSVParser.ParseInt(row, "Health"),
                Damage = CSVParser.ParseInt(row, "Damage"),
                BaseResistance = CSVParser.ParseInt(row, "BaseRessistance"),
                BonusResistance = CSVParser.ParseInt(row, "BonusRessistance"),
                World2HealthMultiplier = ParseMultiplier(CSVParser.ParseString(row, "World2HealthModifer", "1")),
                World2DamageMultiplier = ParseMultiplier(CSVParser.ParseString(row, "World2DamageModifer", "1")),
                World2BaseResistanceAddend = ParseAddend(CSVParser.ParseString(row, "World2BaseResistanceModifier", "0"))
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
                DisplayName = CSVParser.ParseString(row, "DisplayName"),
                RestID = CSVParser.ParseInt(row, "RestID"),
                StatAffected = CSVParser.ParseString(row, "Stat Affected"),
                Amount = CSVParser.ParseInt(row, "Amount")
            });
        }

        return list;
    }

    private static List<CharacterData> LoadCharacters()
    {
        var csv = Resources.Load<TextAsset>("Data/character");
        var rows = CSVParser.Parse(csv.text);
        var list = new List<CharacterData>();

        foreach (var row in rows)
        {
            list.Add(new CharacterData
            {
                DisplayName = CSVParser.ParseString(row, "DisplayName"),
                CharacterID = CSVParser.ParseInt(row, "CharacterID"),
                MaxHealth = CSVParser.ParseInt(row, "MaxHealth"),
                Damage = CSVParser.ParseInt(row, "Damage"),
                Gold = CSVParser.ParseInt(row, "Gold"),
                MaxEnergy = CSVParser.ParseInt(row, "MaxEnergy"),
                CritChance = CSVParser.ParseFloat(row, "CritChance"),
                CritDamage = CSVParser.ParseFloat(row, "CritDamage"),
                BaseResistance = CSVParser.ParseInt(row, "BaseRessistance"),
                BonusResistance = CSVParser.ParseInt(row, "BonusRessistance"),
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
                PotionID = CSVParser.ParseInt(row, "PotionID"),
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
                RelicID = CSVParser.ParseInt(row, "RelicID"),
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
            CritDamage = CSVParser.ParseFloat(row, "CritDamage"),
            BaseResistance = CSVParser.ParseInt(row, "BaseRessistance"),
            BonusResistance = CSVParser.ParseInt(row, "BonusRessistance")
        };
    }

    private static Dictionary<string, float> LoadQTEMultipliers()
    {
        var csv = Resources.Load<TextAsset>("Data/qte");
        var dict = new Dictionary<string, float>();
        
        if (csv == null)
        {
            Debug.LogWarning("[DataCache] qte.csv not found, using defaults");
            dict["Bad"] = 0.9f;
            dict["Good"] = 1.0f;
            dict["Perfect"] = 1.1f;
            return dict;
        }

        var rows = CSVParser.Parse(csv.text);
        foreach (var row in rows)
        {
            string result = CSVParser.ParseString(row, "Result");
            float multiplier = CSVParser.ParseFloat(row, "Multiplier");
            dict[result] = multiplier;
        }

        return dict;
    }

    private static List<ReactionData> LoadReactions()
    {
        var csv = Resources.Load<TextAsset>("Data/reaction");
        var list = new List<ReactionData>();
        
        if (csv == null)
        {
            Debug.LogWarning("[DataCache] reaction.csv not found");
            return list;
        }

        var rows = CSVParser.Parse(csv.text);
        foreach (var row in rows)
        {
            list.Add(new ReactionData
            {
                ElementA = CSVParser.ParseString(row, "ElementA"),
                ElementB = CSVParser.ParseString(row, "ElementB"),
                ReactionName = CSVParser.ParseString(row, "ReactionName"),
                Multiplier = CSVParser.ParseFloat(row, "Multiplier")
            });
        }

        return list;
    }

    public static float GetQTEMultiplier(QTEResult result)
    {
        string key = result.ToString();
        if (QTEMultipliers != null && QTEMultipliers.TryGetValue(key, out float mult))
        {
            return mult;
        }
        return 1.0f;
    }

    public static (string name, float multiplier) GetReaction(Element a, Element b)
    {
        if (a == b) return ("None", 1.0f);
        
        string aStr = a.ToString();
        string bStr = b.ToString();
        
        if (Reactions != null)
        {
            foreach (var r in Reactions)
            {
                if ((r.ElementA == aStr && r.ElementB == bStr) ||
                    (r.ElementA == bStr && r.ElementB == aStr))
                {
                    return (r.ReactionName, r.Multiplier);
                }
            }
        }
        
        return ("Reaction", 1.25f);
    }
}
