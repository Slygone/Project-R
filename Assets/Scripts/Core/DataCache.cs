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
    public static Dictionary<string, ReactionData> Reactions { get; private set; }
    public static Dictionary<string, List<ReactionEffectData>> ReactionEffects { get; private set; }

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
        ReactionEffects = LoadReactionEffects();

        IsLoaded = true;
        Debug.Log($"[DataCache] Loaded: {Enemies.Count} enemies, {RestOptions.Count} rest options, {Characters.Count} characters, {Potions.Count} potions, {Relics.Count} relics, {QTEMultipliers.Count} QTE results, {Reactions.Count} reactions, {ReactionEffects.Count} reaction effect groups, player stats");
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
    
    // Parse damage range like "14-16" into (min, max) tuple
    private static (int min, int max) ParseDamageRange(string rangeStr)
    {
        if (string.IsNullOrEmpty(rangeStr)) return (0, 0);
        
        // Try to split by dash
        var parts = rangeStr.Split('-');
        if (parts.Length == 2)
        {
            if (int.TryParse(parts[0].Trim(), out int min) && 
                int.TryParse(parts[1].Trim(), out int max))
            {
                return (min, max);
            }
        }
        
        // Fallback: try parsing as single number
        if (int.TryParse(rangeStr.Trim(), out int single))
        {
            return (single, single);
        }
        
        return (0, 0);
    }
    
    // Parse cooldown from strings like "1 Turn", "2 Turns", "4 Turns" -> returns integer
    private static int ParseCooldown(string cooldownStr)
    {
        if (string.IsNullOrEmpty(cooldownStr)) return 0;
        
        // Extract the first number from the string
        string numPart = "";
        foreach (char c in cooldownStr)
        {
            if (char.IsDigit(c))
                numPart += c;
            else if (numPart.Length > 0)
                break; // Stop after we've collected digits
        }
        
        if (int.TryParse(numPart, out int result))
            return result;
        
        return 0;
    }
    
    // Parse damage percent from strings like "125% of base damage", "150%" -> returns float (e.g., 125)
    private static float ParseDamagePercent(string damageStr)
    {
        if (string.IsNullOrEmpty(damageStr)) return 100f;
        
        // Extract the first number from the string
        string numPart = "";
        foreach (char c in damageStr)
        {
            if (char.IsDigit(c) || c == '.')
                numPart += c;
            else if (numPart.Length > 0)
                break; // Stop after we've collected digits
        }
        
        if (float.TryParse(numPart, out float result))
            return result;
        
        return 100f; // Default to 100% if parsing fails
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
            // Parse damage range from CSV (e.g., "9-11") and use average as base damage
            var damageRange = ParseDamageRange(CSVParser.ParseString(row, "DamageRange"));
            int baseDamage = (damageRange.min + damageRange.max) / 2;
            
            var enemy = new EnemyData
            {
                Type = CSVParser.ParseString(row, "Type"),
                DisplayName = CSVParser.ParseString(row, "DisplayName"),
                EnemyID = CSVParser.ParseInt(row, "EnemyID"),
                Health = CSVParser.ParseInt(row, "Health"),
                Damage = baseDamage, // Base damage - variance applied at attack time
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
            // Parse damage range from CSV (e.g., "14-16") and use average as base damage
            string damageRangeLabel = CSVParser.ParseString(row, "DamageRange");
            var damageRange = ParseDamageRange(damageRangeLabel);
            int baseDamage = (damageRange.min + damageRange.max) / 2;
            
            list.Add(new CharacterData
            {
                DisplayName = CSVParser.ParseString(row, "DisplayName"),
                CharacterID = CSVParser.ParseInt(row, "CharacterID"),
                MaxHealth = CSVParser.ParseInt(row, "MaxHealth"),
                Damage = baseDamage, // Base damage - variance applied at attack time
                DamageRangeLabel = damageRangeLabel,
                Gold = CSVParser.ParseInt(row, "Gold"),
                MaxEnergy = CSVParser.ParseInt(row, "MaxEnergy"),
                CritChance = CSVParser.ParseFloat(row, "CritChance"),
                CritDamage = CSVParser.ParseFloat(row, "CritDamage"),
                BaseResistance = CSVParser.ParseInt(row, "BaseRessistance"),
                BonusResistance = CSVParser.ParseInt(row, "BonusRessistance"),
                // Skill 1 (supports new header Skill1_Name)
                Skill1 = CSVParser.ParseString(row, "Skill1_Name", CSVParser.ParseString(row, "Skill1", "")),
                Skill1DamagePercent = ParseDamagePercent(CSVParser.ParseString(row, "Skill1_Damage")),
                Skill1Effect = CSVParser.ParseString(row, "Skill1_Effect_1", "null"),
                Skill1Cooldown = ParseCooldown(CSVParser.ParseString(row, "Skill1_Cooldown")),
                Skill1EnergyGain = CSVParser.ParseInt(row, "Skill1_EnergyGain"),
                // Skill 2 (supports new header Skill2_Name)
                Skill2 = CSVParser.ParseString(row, "Skill2_Name", CSVParser.ParseString(row, "Skill2", "")),
                Skill2DamagePercent = ParseDamagePercent(CSVParser.ParseString(row, "Skill2_Damage")),
                Skill2Effect = CSVParser.ParseString(row, "Skill2_Effect_1", "null"),
                Skill2Cooldown = ParseCooldown(CSVParser.ParseString(row, "Skill2_Cooldown")),
                Skill2EnergyGain = CSVParser.ParseInt(row, "Skill2_EnergyGain"),
                // Skill 3 (supports new header Skill3_Name)
                Skill3 = CSVParser.ParseString(row, "Skill3_Name", CSVParser.ParseString(row, "Skill3", "")),
                Skill3DamagePercent = ParseDamagePercent(CSVParser.ParseString(row, "Skill3_Damage")),
                Skill3Effect = CSVParser.ParseString(row, "Skill3_Effect_1", "null"),
                Skill3Cooldown = ParseCooldown(CSVParser.ParseString(row, "Skill3_Cooldown")),
                Skill3EnergyCost = CSVParser.ParseInt(row, "Skill3_EnergyCost")
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

    private static Dictionary<string, ReactionData> LoadReactions()
    {
        var csv = Resources.Load<TextAsset>("Data/elementalReactions");
        var dict = new Dictionary<string, ReactionData>();
        
        if (csv == null)
        {
            Debug.LogWarning("[DataCache] elementalReactions.csv not found");
            return dict;
        }

        var rows = CSVParser.Parse(csv.text);
        foreach (var row in rows)
        {
            string reactionId = CSVParser.ParseString(row, "ReactionId", "");
            if (string.IsNullOrEmpty(reactionId)) continue;
            
            dict[reactionId] = new ReactionData
            {
                ReactionId = reactionId,
                Name = CSVParser.ParseString(row, "Name", "Unknown"),
                DamageMultiplier = CSVParser.ParseFloat(row, "DamageMultiplier", 1f)
            };
        }

        return dict;
    }
    
    private static Dictionary<string, List<ReactionEffectData>> LoadReactionEffects()
    {
        var csv = Resources.Load<TextAsset>("Data/elementalReactionEffects");
        var dict = new Dictionary<string, List<ReactionEffectData>>();
        
        if (csv == null)
        {
            Debug.LogWarning("[DataCache] elementalReactionEffects.csv not found");
            return dict;
        }

        var rows = CSVParser.Parse(csv.text);
        foreach (var row in rows)
        {
            string reactionId = CSVParser.ParseString(row, "ReactionId", "");
            if (string.IsNullOrEmpty(reactionId)) continue;
            
            var effect = new ReactionEffectData
            {
                ReactionId = reactionId,
                Order = CSVParser.ParseInt(row, "Order", 0),
                EffectType = CSVParser.ParseString(row, "EffectType", ""),
                Target = CSVParser.ParseString(row, "Target", "Enemy"),
                Value = CSVParser.ParseString(row, "Value", ""),
                DurationTurns = CSVParser.ParseInt(row, "DurationTurns", 0),
                ChancePct = CSVParser.ParseInt(row, "ChancePct", 100),
                Notes = CSVParser.ParseString(row, "Notes", "")
            };
            
            if (!dict.ContainsKey(reactionId))
            {
                dict[reactionId] = new List<ReactionEffectData>();
            }
            dict[reactionId].Add(effect);
        }
        
        // Sort each list by Order
        foreach (var kvp in dict)
        {
            kvp.Value.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        return dict;
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

    // Get reaction definition by directional ReactionId (e.g., "Ice_Fire" for Ice then Fire)
    public static ReactionData GetReactionDef(string reactionId)
    {
        if (string.IsNullOrEmpty(reactionId)) return GetDefaultReactionDef();
        
        if (Reactions != null && Reactions.TryGetValue(reactionId, out var reaction))
        {
            return reaction;
        }
        
        return GetDefaultReactionDef();
    }
    
    // Get reaction effects by ReactionId (returns empty list if none)
    public static List<ReactionEffectData> GetReactionEffects(string reactionId)
    {
        if (string.IsNullOrEmpty(reactionId)) return new List<ReactionEffectData>();
        
        if (ReactionEffects != null && ReactionEffects.TryGetValue(reactionId, out var effects))
        {
            return effects;
        }
        
        return new List<ReactionEffectData>();
    }
    
    private static ReactionData GetDefaultReactionDef()
    {
        return new ReactionData
        {
            ReactionId = "Unknown",
            Name = "Unknown",
            DamageMultiplier = 1.0f
        };
    }
    
    // Build ReactionId from two elements (FirstElement_DetonatorElement)
    public static string BuildReactionId(Element firstElement, Element detonatorElement)
    {
        if (firstElement == Element.None || detonatorElement == Element.None)
            return null;
        if (firstElement == detonatorElement)
            return null;
        return $"{firstElement}_{detonatorElement}";
    }
}
