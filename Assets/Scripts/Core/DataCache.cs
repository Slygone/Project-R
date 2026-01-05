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
    public static Dictionary<string, List<ElementalTierData>> ElementalTiers { get; private set; }
    public static Dictionary<int, WorldEncounterData> WorldEncounters { get; private set; }

    public static bool IsLoaded { get; private set; }

    public static void LoadAll()
    {
        GameDataLoader.LoadAll();
        
        Enemies = LoadEnemies();
        RestOptions = LoadRestOptions();
        Characters = LoadCharactersFromJson();
        Potions = LoadPotions();
        Relics = LoadRelics();
        PlayerStats = LoadPlayerStats();
        QTEMultipliers = LoadQTEMultipliers();
        Reactions = LoadReactions();
        ReactionEffects = LoadReactionEffects();
        ElementalTiers = LoadElementalTiers();
        WorldEncounters = LoadWorldEncounters();

        IsLoaded = true;
        Debug.Log($"[DataCache] Loaded: {Enemies.Count} enemies, {RestOptions.Count} rest options, {Characters.Count} characters, {Potions.Count} potions, {Relics.Count} relics, {QTEMultipliers.Count} QTE results, {Reactions.Count} reactions, {ReactionEffects.Count} reaction effect groups, {ElementalTiers.Count} element tier groups, {WorldEncounters.Count} world encounters, player stats");
    }

    private static List<EnemyData> LoadEnemies()
    {
        var json = Resources.Load<TextAsset>("Data/enemies");
        var list = new List<EnemyData>();

        RegularEnemies = new List<EnemyData>();
        EliteEnemies = new List<EnemyData>();
        BossEnemies = new List<EnemyData>();

        if (json == null)
        {
            Debug.LogWarning("[DataCache] enemies.json not found");
            return list;
        }

        var wrapper = JsonUtility.FromJson<EnemyListWrapper>(json.text);
        if (wrapper == null || wrapper.enemies == null)
        {
            Debug.LogWarning("[DataCache] Failed to parse enemies.json");
            return list;
        }

        foreach (var e in wrapper.enemies)
        {
            // Use average of damage range as base damage
            int baseDamage = (e.baseDamageMin + e.baseDamageMax) / 2;
            
            var enemy = new EnemyData
            {
                Type = e.type,
                DisplayName = e.displayName,
                EnemyID = e.enemyId,
                Health = e.baseHealth,
                Damage = baseDamage,
                BaseResistance = e.baseResistance,
                BonusResistance = e.bonusResistance,
                World2HealthMultiplier = e.healthModifiers.Length > 1 ? e.healthModifiers[1] : 1f,
                World2DamageMultiplier = e.damageModifiers.Length > 1 ? e.damageModifiers[1] : 1f,
                World2BaseResistanceAddend = e.resistanceModifiers.Length > 1 ? e.resistanceModifiers[1] : 0,
                World3HealthMultiplier = e.healthModifiers.Length > 2 ? e.healthModifiers[2] : 1f,
                World3DamageMultiplier = e.damageModifiers.Length > 2 ? e.damageModifiers[2] : 1f,
                World3BaseResistanceAddend = e.resistanceModifiers.Length > 2 ? e.resistanceModifiers[2] : 0,
                World4HealthMultiplier = e.healthModifiers.Length > 3 ? e.healthModifiers[3] : 1f,
                World4DamageMultiplier = e.damageModifiers.Length > 3 ? e.damageModifiers[3] : 1f,
                World4BaseResistanceAddend = e.resistanceModifiers.Length > 3 ? e.resistanceModifiers[3] : 0,
                World5HealthMultiplier = e.healthModifiers.Length > 4 ? e.healthModifiers[4] : 1f,
                World5DamageMultiplier = e.damageModifiers.Length > 4 ? e.damageModifiers[4] : 1f,
                World5BaseResistanceAddend = e.resistanceModifiers.Length > 4 ? e.resistanceModifiers[4] : 0
            };
            
            list.Add(enemy);
            
            if (enemy.IsRegular) RegularEnemies.Add(enemy);
            else if (enemy.IsElite) EliteEnemies.Add(enemy);
            else if (enemy.IsBoss) BossEnemies.Add(enemy);
        }

        return list;
    }
    
    [System.Serializable]
    private class EnemyListWrapper
    {
        public List<EnemyJsonEntry> enemies;
    }
    
    [System.Serializable]
    private class EnemyJsonEntry
    {
        public string type;
        public string displayName;
        public int enemyId;
        public int baseHealth;
        public float[] healthModifiers;
        public string skill1;
        public string skill2;
        public int baseDamageMin;
        public int baseDamageMax;
        public float[] damageModifiers;
        public int baseResistance;
        public int[] resistanceModifiers;
        public int bonusResistance;
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

    private static List<CharacterData> LoadCharactersFromJson()
    {
        var definitions = GameDataLoader.GetAllCharacters();
        var list = new List<CharacterData>();
        
        foreach (var def in definitions)
        {
            list.Add(GameDataLoader.ToCharacterData(def));
        }
        
        return list;
    }

    private static List<PotionData> LoadPotions()
    {
        var json = Resources.Load<TextAsset>("Data/potions");
        if (json == null)
        {
            Debug.LogError("[DataCache] potions.json not found");
            return new List<PotionData>();
        }
        
        var wrapper = JsonUtility.FromJson<PotionListWrapper>(json.text);
        var list = new List<PotionData>();
        
        foreach (var p in wrapper.potions)
        {
            list.Add(new PotionData
            {
                Id = p.id,
                DisplayName = p.displayName,
                PotionID = p.potionID,
                StatAffected = p.statAffected,
                Amount = p.amount,
                Rarity = p.rarity,
                Description = p.description
            });
        }
        
        return list;
    }

    private static List<RelicData> LoadRelics()
    {
        var json = Resources.Load<TextAsset>("Data/relics");
        if (json == null)
        {
            Debug.LogError("[DataCache] relics.json not found");
            return new List<RelicData>();
        }
        
        var wrapper = JsonUtility.FromJson<RelicListWrapper>(json.text);
        var list = new List<RelicData>();
        
        foreach (var r in wrapper.relics)
        {
            list.Add(new RelicData
            {
                Id = r.id,
                DisplayName = r.displayName,
                RelicID = r.relicID,
                StatAffected = r.statAffected,
                Amount = r.amount,
                Rarity = r.rarity,
                Description = r.description
            });
        }
        
        return list;
    }
    
    // JSON wrapper classes for potions and relics
    [System.Serializable]
    private class PotionListWrapper
    {
        public PotionJsonData[] potions;
    }
    
    [System.Serializable]
    private class PotionJsonData
    {
        public string id;
        public string displayName;
        public int potionID;
        public string statAffected;
        public int amount;
        public string rarity;
        public string description;
    }
    
    [System.Serializable]
    private class RelicListWrapper
    {
        public RelicJsonData[] relics;
    }
    
    [System.Serializable]
    private class RelicJsonData
    {
        public string id;
        public string displayName;
        public int relicID;
        public string statAffected;
        public int amount;
        public string rarity;
        public string description;
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
        var json = Resources.Load<TextAsset>("Data/elementalReactions");
        var dict = new Dictionary<string, ReactionData>();
        
        if (json == null)
        {
            Debug.LogWarning("[DataCache] elementalReactions.json not found");
            return dict;
        }

        var wrapper = JsonUtility.FromJson<ReactionListWrapper>(json.text);
        if (wrapper == null || wrapper.reactions == null)
        {
            Debug.LogWarning("[DataCache] Failed to parse elementalReactions.json");
            return dict;
        }
        
        foreach (var r in wrapper.reactions)
        {
            if (string.IsNullOrEmpty(r.id)) continue;
            
            dict[r.id] = new ReactionData
            {
                ReactionId = r.id,
                Name = r.name,
                DamageMultiplier = r.damageMultiplier
            };
        }

        return dict;
    }
    
    [System.Serializable]
    private class ReactionListWrapper
    {
        public List<ReactionJsonEntry> reactions;
    }
    
    [System.Serializable]
    private class ReactionJsonEntry
    {
        public string id;
        public string name;
        public float damageMultiplier;
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
    
    // Load elemental tier data (XP thresholds and bonuses per element per level)
    private static Dictionary<string, List<ElementalTierData>> LoadElementalTiers()
    {
        var result = new Dictionary<string, List<ElementalTierData>>();
        
        var asset = Resources.Load<TextAsset>("Data/elementalTier");
        if (asset == null)
        {
            Debug.LogWarning("[DataCache] elementalTier.csv not found");
            return result;
        }
        
        var rows = CSVParser.Parse(asset.text);
        foreach (var row in rows)
        {
            string element = CSVParser.ParseString(row, "Element");
            if (string.IsNullOrEmpty(element)) continue;
            
            var tierData = new ElementalTierData
            {
                Element = element,
                Level = CSVParser.ParseInt(row, "Level"),
                XPRequiredToReachLevel = CSVParser.ParseInt(row, "XPRequiredToReachLevel"),
                Bonus = CSVParser.ParseString(row, "Bonus")
            };
            
            if (!result.ContainsKey(element))
            {
                result[element] = new List<ElementalTierData>();
            }
            result[element].Add(tierData);
        }
        
        // Sort each element's tiers by level
        foreach (var kvp in result)
        {
            kvp.Value.Sort((a, b) => a.Level.CompareTo(b.Level));
        }
        
        return result;
    }
    
    /// <summary>
    /// Get the XP required to reach a specific level for an element.
    /// </summary>
    public static int GetElementXPThreshold(string element, int level)
    {
        if (ElementalTiers == null || !ElementalTiers.ContainsKey(element))
            return int.MaxValue;
        
        var tiers = ElementalTiers[element];
        foreach (var tier in tiers)
        {
            if (tier.Level == level)
                return tier.XPRequiredToReachLevel;
        }
        return int.MaxValue;
    }
    
    /// <summary>
    /// Get the bonus text for a specific element level.
    /// </summary>
    public static string GetElementBonus(string element, int level)
    {
        if (ElementalTiers == null || !ElementalTiers.ContainsKey(element))
            return "";
        
        var tiers = ElementalTiers[element];
        foreach (var tier in tiers)
        {
            if (tier.Level == level)
                return tier.Bonus;
        }
        return "";
    }
    
    /// <summary>
    /// Get the max level for an element from CSV data.
    /// </summary>
    public static int GetElementMaxLevel(string element)
    {
        if (ElementalTiers == null || !ElementalTiers.ContainsKey(element))
            return 1;
        
        var tiers = ElementalTiers[element];
        if (tiers.Count == 0) return 1;
        return tiers[tiers.Count - 1].Level;
    }
    
    /// <summary>
    /// Get list of all elements from CSV data.
    /// </summary>
    public static List<string> GetAllElements()
    {
        if (ElementalTiers == null)
            return new List<string>();
        return new List<string>(ElementalTiers.Keys);
    }
    
    private static Dictionary<int, WorldEncounterData> LoadWorldEncounters()
    {
        var dict = new Dictionary<int, WorldEncounterData>();
        
        var csv = Resources.Load<TextAsset>("Data/worldEncounter");
        if (csv == null)
        {
            Debug.LogWarning("[DataCache] worldEncounter.csv not found, using defaults");
            // Provide defaults for 5 worlds
            for (int w = 1; w <= 5; w++)
            {
                dict[w] = new WorldEncounterData
                {
                    World = w,
                    NodeCount = 10 + (w - 1) * 2,
                    CombatNodeCount = 20,
                    RestNodeCount = 5,
                    ShopNodeCount = 3,
                    EliteNodeCount = 10,
                    RegularEnemy = w,
                    EliteEnemy = w,
                    BossEnemy = w
                };
            }
            return dict;
        }
        
        var rows = CSVParser.Parse(csv.text);
        foreach (var row in rows)
        {
            int world = CSVParser.ParseInt(row, "World");
            if (world <= 0) continue;
            
            dict[world] = new WorldEncounterData
            {
                World = world,
                NodeCount = CSVParser.ParseInt(row, "NodeCount", 10),
                CombatNodeCount = CSVParser.ParseInt(row, "CombatNodeCount", 20),
                RestNodeCount = CSVParser.ParseInt(row, "RestNodeCount", 5),
                ShopNodeCount = CSVParser.ParseInt(row, "ShopNodeCount", 3),
                EliteNodeCount = CSVParser.ParseInt(row, "EliteNodeCount", 10),
                RegularEnemy = CSVParser.ParseInt(row, "RegularEnemy", 1),
                EliteEnemy = CSVParser.ParseInt(row, "EliteEnemy", 1),
                BossEnemy = CSVParser.ParseInt(row, "BossEnemy", 1)
            };
        }
        
        return dict;
    }
    
    /// <summary>
    /// Get world encounter data for a specific world. Returns defaults if not found.
    /// </summary>
    public static WorldEncounterData GetWorldEncounter(int world)
    {
        if (WorldEncounters != null && WorldEncounters.ContainsKey(world))
            return WorldEncounters[world];
        
        // Return defaults
        return new WorldEncounterData
        {
            World = world,
            NodeCount = 10 + (world - 1) * 2,
            CombatNodeCount = 20,
            RestNodeCount = 5,
            ShopNodeCount = 3,
            EliteNodeCount = 10,
            RegularEnemy = world,
            EliteEnemy = world,
            BossEnemy = world
        };
    }
}
