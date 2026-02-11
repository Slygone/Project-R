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
    public static Dictionary<string, float> QTEOffensiveMultipliers { get; private set; }
    public static Dictionary<string, int> QTEDefensiveShield { get; private set; }
    public static Dictionary<string, ReactionData> Reactions { get; private set; }
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
        LoadQTEEffects();
        Reactions = LoadReactions();
        ElementalTiers = LoadElementalTiers();
        WorldEncounters = LoadWorldEncounters();

        IsLoaded = true;
        Debug.Log($"[DataCache] Loaded: {Enemies.Count} enemies, {RestOptions.Count} rest options, {Characters.Count} characters, {Potions.Count} potions, {Relics.Count} relics, {QTEOffensiveMultipliers.Count} QTE offensive, {QTEDefensiveShield.Count} QTE defensive, {Reactions.Count} reactions, {ElementalTiers.Count} element tier groups, {WorldEncounters.Count} world encounters");
    }

    /// <summary>
    /// Look up an EnemyData by display name (used for split/spawn mechanics).
    /// </summary>
    public static EnemyData GetEnemyByName(string displayName)
    {
        if (Enemies == null || string.IsNullOrEmpty(displayName)) return null;
        foreach (var e in Enemies)
        {
            if (e.DisplayName == displayName) return e;
        }
        return null;
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
                
                // Skills
                Skill1Id = e.skill1,
                Skill2Id = e.skill2,
                Skill2Cooldown = e.skill2Cooldown,
                Skill3Id = e.skill3,
                Skill3Cooldown = e.skill3Cooldown,
                Skill4Id = e.skill4,
                
                // Attack pattern & spawn control
                AttackPattern = e.attackPattern,
                SpawnOnly = e.spawnOnly,
                
                // Boss: Split mechanic
                SplitThreshold = e.splitThreshold,
                SplitInto = e.splitInto,
                SplitHealthPercent = e.splitHealthPercent,
                
                // Boss: Reactive pattern
                ReactivePattern = e.reactivePattern,
                ReactiveOnAttack = e.reactiveOnAttack,
                ReactiveOnShield = e.reactiveOnShield,
                ReactiveOnReaction = e.reactiveOnReaction,
                
                // Boss: Reborn mechanic
                RebornHealthPercent = e.rebornHealthPercent,
                RebornDamageBonus = e.rebornDamageBonus,
                RebornPattern = e.rebornPattern,
                
                // Boss: Spawn requirement
                SpawnRequirement = e.spawnRequirement,
                SpawnRequirementCount = e.spawnRequirementCount,
                
                // Rewards
                RewardXP = e.rewards != null ? e.rewards.xp : 0,
                RewardGoldMin = e.rewards != null ? e.rewards.goldMin : 0,
                RewardGoldMax = e.rewards != null ? e.rewards.goldMax : 0,
                SigilChance = e.rewards != null ? e.rewards.sigilChance : 0,
                RelicChance = e.rewards != null ? e.rewards.relicChance : 0,
                
                // World scaling
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
            
            // SpawnOnly enemies (MadSlime, SadSlime) are not added to normal spawn pools
            if (enemy.SpawnOnly)
            {
                // Still in the master list but not in any spawn category
            }
            else if (enemy.IsRegular) RegularEnemies.Add(enemy);
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
        public int skill2Cooldown;
        public string skill3;
        public int skill3Cooldown;
        public string skill4;
        public int[] attackPattern;
        public bool spawnOnly;
        public int baseDamageMin;
        public int baseDamageMax;
        public float[] damageModifiers;
        public int baseResistance;
        public int[] resistanceModifiers;
        public int bonusResistance;
        public EnemyRewardsJson rewards;
        
        // Boss: Split mechanic (SlimeBoss)
        public float splitThreshold;
        public string[] splitInto;
        public int splitHealthPercent;
        
        // Boss: Reactive pattern (MirrorBoss)
        public bool reactivePattern;
        public int reactiveOnAttack;
        public int reactiveOnShield;
        public int reactiveOnReaction;
        
        // Boss: Reborn mechanic (FallenChampion)
        public int rebornHealthPercent;
        public float rebornDamageBonus;
        public int[] rebornPattern;
        
        // Boss: Spawn requirement (FallenChampion)
        public string spawnRequirement;
        public int spawnRequirementCount;
    }
    
    [System.Serializable]
    private class EnemyRewardsJson
    {
        public int xp;
        public int goldMin;
        public int goldMax;
        public int sigilChance;
        public int relicChance;
    }

    private static List<RestData> LoadRestOptions()
    {
        var json = Resources.Load<TextAsset>("Data/rest");
        if (json == null)
        {
            throw new System.Exception("Missing rest.json. Expected at Resources/Data/rest.json");
        }
        
        var wrapper = JsonUtility.FromJson<RestListWrapper>(json.text);
        if (wrapper == null || wrapper.restOptions == null)
        {
            throw new System.Exception("Failed to parse rest.json or restOptions is null");
        }
        
        var list = new List<RestData>();
        foreach (var r in wrapper.restOptions)
        {
            list.Add(new RestData
            {
                DisplayName = r.displayName,
                RestID = r.restId,
                Description = r.description ?? "",
                Effects = r.effects ?? new List<EffectEntry>()
            });
        }

        return list;
    }
    
    [System.Serializable]
    private class RestListWrapper
    {
        public RestJsonEntry[] restOptions;
    }
    
    [System.Serializable]
    private class RestJsonEntry
    {
        public string displayName;
        public int restId;
        public string description;
        public List<EffectEntry> effects;
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
            throw new System.Exception("Missing potions.json. Expected at Resources/Data/potions.json");
        }
        
        var wrapper = JsonUtility.FromJson<PotionListWrapper>(json.text);
        if (wrapper == null || wrapper.potions == null)
        {
            throw new System.Exception("Failed to parse potions.json or potions is null");
        }
        
        var list = new List<PotionData>();
        
        foreach (var p in wrapper.potions)
        {
            list.Add(new PotionData
            {
                Id = p.id,
                DisplayName = p.displayName,
                PotionID = p.potionID,
                Rarity = p.rarity,
                Description = p.description,
                Effects = p.effects ?? new List<EffectEntry>()
            });
        }
        
        return list;
    }

    private static List<RelicData> LoadRelics()
    {
        var json = Resources.Load<TextAsset>("Data/relics");
        if (json == null)
        {
            throw new System.Exception("Missing relics.json. Expected at Resources/Data/relics.json");
        }
        
        var wrapper = JsonUtility.FromJson<RelicListWrapper>(json.text);
        if (wrapper == null || wrapper.relics == null)
        {
            throw new System.Exception("Failed to parse relics.json or relics is null");
        }
        
        var list = new List<RelicData>();
        
        foreach (var r in wrapper.relics)
        {
            list.Add(new RelicData
            {
                Id = r.id,
                DisplayName = r.displayName,
                RelicID = r.relicID,
                Rarity = r.rarity,
                Description = r.description,
                Effects = r.effects ?? new List<EffectEntry>()
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
        public string rarity;
        public string description;
        public List<EffectEntry> effects;
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
        public string rarity;
        public string description;
        public List<EffectEntry> effects;
    }

    private static void LoadQTEEffects()
    {
        QTEOffensiveMultipliers = new Dictionary<string, float>();
        QTEDefensiveShield = new Dictionary<string, int>();
        
        // Load Offensive QTE from separate file
        var offensiveJson = Resources.Load<TextAsset>("Data/qteOffensive");
        if (offensiveJson == null)
        {
            throw new System.Exception("Missing qteOffensive.json. Expected at Resources/Data/qteOffensive.json");
        }
        
        var offensiveWrapper = JsonUtility.FromJson<QTEWrapper>(offensiveJson.text);
        if (offensiveWrapper == null || offensiveWrapper.qteResults == null || offensiveWrapper.qteResults.Length == 0)
        {
            throw new System.Exception("qteOffensive.json is empty or malformed.");
        }
        
        foreach (var qte in offensiveWrapper.qteResults)
        {
            // Extract multiplier from effects array
            float mult = 1f;
            if (qte.effects != null)
            {
                foreach (var eff in qte.effects)
                {
                    if (eff.effectId == "eff_damage_multiplier" && eff.multiplier != 0f)
                    {
                        mult = eff.multiplier;
                    }
                }
            }
            QTEOffensiveMultipliers[qte.result] = mult;
        }
        
        // Load Defensive QTE from separate file
        var defensiveJson = Resources.Load<TextAsset>("Data/qteDefensive");
        if (defensiveJson == null)
        {
            throw new System.Exception("Missing qteDefensive.json. Expected at Resources/Data/qteDefensive.json");
        }
        
        var defensiveWrapper = JsonUtility.FromJson<QTEWrapper>(defensiveJson.text);
        if (defensiveWrapper == null || defensiveWrapper.qteResults == null || defensiveWrapper.qteResults.Length == 0)
        {
            throw new System.Exception("qteDefensive.json is empty or malformed.");
        }
        
        foreach (var qte in defensiveWrapper.qteResults)
        {
            // Extract shieldPercent from effects array
            int shield = 0;
            if (qte.effects != null)
            {
                foreach (var eff in qte.effects)
                {
                    if (eff.effectId == "eff_shield_gain" && eff.percentOfMaxHealth > 0)
                    {
                        shield = eff.percentOfMaxHealth;
                    }
                }
            }
            QTEDefensiveShield[qte.result] = shield;
        }
    }
    
    [System.Serializable]
    private class QTEWrapper
    {
        public QTEEntry[] qteResults;
    }
    
    [System.Serializable]
    private class QTEEntry
    {
        public string result;
        public string description;
        public List<EffectEntry> effects;
    }

    private static Dictionary<string, ReactionData> LoadReactions()
    {
        var json = Resources.Load<TextAsset>("Data/elementalReactions");
        var dict = new Dictionary<string, ReactionData>();
        
        if (json == null)
        {
            throw new System.Exception("Missing elementalReactions.json. Expected at Resources/Data/elementalReactions.json");
        }

        var wrapper = JsonUtility.FromJson<ReactionListWrapper>(json.text);
        if (wrapper == null || wrapper.reactions == null)
        {
            throw new System.Exception("Failed to parse elementalReactions.json or reactions is null");
        }
        
        foreach (var r in wrapper.reactions)
        {
            if (string.IsNullOrEmpty(r.id)) continue;
            
            var data = new ReactionData
            {
                ReactionId = r.id,
                Name = r.name,
                Type = r.type,
                CanCrit = r.canCrit
            };
            
            if (r.effects != null)
            {
                data.Effects.AddRange(r.effects);
            }
            
            dict[r.id] = data;
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
        public string type;
        public bool canCrit;
        public List<ReactionEffectEntry> effects;
    }

    public static float GetQTEOffensiveMultiplier(QTEResult result)
    {
        string key = result.ToString();
        if (QTEOffensiveMultipliers != null && QTEOffensiveMultipliers.TryGetValue(key, out float mult))
        {
            return mult;
        }
        throw new System.Exception($"QTE offensive multiplier not found for result: {key}");
    }
    
    public static int GetQTEDefensiveShield(QTEResult result)
    {
        string key = result.ToString();
        if (QTEDefensiveShield != null && QTEDefensiveShield.TryGetValue(key, out int shieldPercent))
        {
            return shieldPercent;
        }
        throw new System.Exception($"QTE defensive shield not found for result: {key}");
    }

    /// <summary>
    /// Get reaction definition by directional ReactionId (e.g., "Ice_Fire" for Ice then Fire).
    /// Returns null if not found.
    /// </summary>
    public static ReactionData GetReactionDef(string reactionId)
    {
        if (string.IsNullOrEmpty(reactionId)) return null;
        
        if (Reactions != null && Reactions.TryGetValue(reactionId, out var reaction))
        {
            return reaction;
        }
        
        return null;
    }
    
    /// <summary>
    /// Get reaction effects by ReactionId (returns empty list if none).
    /// Effects are now stored directly on ReactionData.
    /// </summary>
    public static List<ReactionEffectEntry> GetReactionEffects(string reactionId)
    {
        if (string.IsNullOrEmpty(reactionId)) return new List<ReactionEffectEntry>();
        
        var def = GetReactionDef(reactionId);
        if (def != null && def.Effects != null)
        {
            return def.Effects;
        }
        
        return new List<ReactionEffectEntry>();
    }
    
    // Build ReactionId from two elements (normalized alphabetically so Fire_Ice = Ice_Fire)
    public static string BuildReactionId(Element elementA, Element elementB)
    {
        if (elementA == Element.None || elementB == Element.None)
            return null;
        
        // Same element reaction (e.g., Fire_Fire)
        if (elementA == elementB)
            return $"{elementA}_{elementB}";
        
        // Normalize order alphabetically so Fire_Ice = Ice_Fire
        string a = elementA.ToString();
        string b = elementB.ToString();
        return string.Compare(a, b, System.StringComparison.Ordinal) < 0 
            ? $"{a}_{b}" 
            : $"{b}_{a}";
    }
    
    private static Dictionary<string, List<ElementalTierData>> LoadElementalTiers()
    {
        var result = new Dictionary<string, List<ElementalTierData>>();
        
        var json = Resources.Load<TextAsset>("Data/elementalTier");
        if (json == null)
        {
            throw new System.Exception("Missing elementalTier.json. Expected at Resources/Data/elementalTier.json");
        }
        
        var wrapper = JsonUtility.FromJson<ElementalTierWrapper>(json.text);
        if (wrapper == null || wrapper.elementalTiers == null)
        {
            throw new System.Exception("Failed to parse elementalTier.json or elementalTiers is null");
        }
        
        foreach (var t in wrapper.elementalTiers)
        {
            if (string.IsNullOrEmpty(t.element)) continue;
            
            var tierData = new ElementalTierData
            {
                Element = t.element,
                Level = t.level,
                XPRequiredToReachLevel = t.xpRequired,
                Bonus = t.bonus
            };
            
            if (!result.ContainsKey(t.element))
            {
                result[t.element] = new List<ElementalTierData>();
            }
            result[t.element].Add(tierData);
        }
        
        foreach (var kvp in result)
        {
            kvp.Value.Sort((a, b) => a.Level.CompareTo(b.Level));
        }
        
        return result;
    }
    
    [System.Serializable]
    private class ElementalTierWrapper
    {
        public ElementalTierJsonEntry[] elementalTiers;
    }
    
    [System.Serializable]
    private class ElementalTierJsonEntry
    {
        public string element;
        public int level;
        public int xpRequired;
        public string bonus;
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
    /// Get the max level for an element from JSON data.
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
    /// Get list of all elements from JSON data.
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
        
        var json = Resources.Load<TextAsset>("Data/worldEncounter");
        if (json == null)
        {
            throw new System.Exception("Missing worldEncounter.json. Expected at Resources/Data/worldEncounter.json");
        }
        
        var wrapper = JsonUtility.FromJson<WorldEncounterWrapper>(json.text);
        if (wrapper == null || wrapper.worldEncounters == null)
        {
            throw new System.Exception("Failed to parse worldEncounter.json or worldEncounters is null");
        }
        
        foreach (var w in wrapper.worldEncounters)
        {
            if (w.world <= 0) continue;
            
            dict[w.world] = new WorldEncounterData
            {
                World = w.world,
                NodeCount = w.nodeCount,
                CombatNodeCount = w.combatNodeCount,
                RestNodeCount = w.restNodeCount,
                ShopNodeCount = w.shopNodeCount,
                EliteNodeCount = w.eliteNodeCount,
                RegularEnemy = w.regularEnemy,
                EliteEnemy = w.eliteEnemy,
                BossEnemy = w.bossEnemy
            };
        }
        
        return dict;
    }
    
    [System.Serializable]
    private class WorldEncounterWrapper
    {
        public WorldEncounterJsonEntry[] worldEncounters;
    }
    
    [System.Serializable]
    private class WorldEncounterJsonEntry
    {
        public int world;
        public int nodeCount;
        public int combatNodeCount;
        public int restNodeCount;
        public int shopNodeCount;
        public int eliteNodeCount;
        public int regularEnemy;
        public int eliteEnemy;
        public int bossEnemy;
    }
    
    /// <summary>
    /// Get world encounter data for a specific world.
    /// </summary>
    public static WorldEncounterData GetWorldEncounter(int world)
    {
        if (WorldEncounters == null)
        {
            throw new System.Exception("WorldEncounters not loaded. Call DataCache.LoadAll() first.");
        }
        
        if (!WorldEncounters.ContainsKey(world))
        {
            throw new System.Exception($"World encounter data not found for world {world}. Check worldEncounter.json.");
        }
        
        return WorldEncounters[world];
    }
}
