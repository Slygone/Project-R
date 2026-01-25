using System.Collections.Generic;
using UnityEngine;

public static class GameDataLoader
{
    private static CharactersFile _charactersFile;
    private static SkillsFile _skillsFile;
    private static EffectsFile _effectsFile;
    
    private static Dictionary<string, CharacterDefinition> _characterById;
    private static Dictionary<string, SkillDefinition> _skillById;
    private static Dictionary<string, EffectDefinition> _effectById;
    private static Dictionary<string, StatusDefinition> _statusById;
    
    public static bool IsLoaded { get; private set; }
    
    public static void LoadAll()
    {
        LoadCharacters();
        LoadSkills();
        LoadEffects();
        IsLoaded = true;
        
        Debug.Log($"[GameDataLoader] Loaded: {_characterById.Count} characters, {_skillById.Count} skills, {_effectById.Count} effects, {_statusById.Count} statuses");
    }
    
    private static void LoadCharacters()
    {
        var json = Resources.Load<TextAsset>("Data/characters");
        if (json == null)
        {
            throw new System.Exception("[GameDataLoader] characters.json not found at Resources/Data/characters.json");
        }
        
        _charactersFile = JsonUtility.FromJson<CharactersFile>(json.text);
        _characterById = new Dictionary<string, CharacterDefinition>();
        
        foreach (var character in _charactersFile.characters)
        {
            _characterById[character.id] = character;
        }
    }
    
    private static void LoadSkills()
    {
        var json = Resources.Load<TextAsset>("Data/skills");
        if (json == null)
        {
            throw new System.Exception("[GameDataLoader] skills.json not found at Resources/Data/skills.json");
        }
        
        _skillsFile = JsonUtility.FromJson<SkillsFile>(json.text);
        _skillById = new Dictionary<string, SkillDefinition>();
        
        foreach (var skill in _skillsFile.skills)
        {
            _skillById[skill.id] = skill;
        }
    }
    
    private static void LoadEffects()
    {
        var json = Resources.Load<TextAsset>("Data/effects");
        if (json == null)
        {
            throw new System.Exception("[GameDataLoader] effects.json not found at Resources/Data/effects.json");
        }
        
        _effectsFile = JsonUtility.FromJson<EffectsFile>(json.text);
        _effectById = new Dictionary<string, EffectDefinition>();
        _statusById = new Dictionary<string, StatusDefinition>();
        
        foreach (var effect in _effectsFile.effects)
        {
            _effectById[effect.id] = effect;
        }
        
        foreach (var status in _effectsFile.statuses)
        {
            _statusById[status.id] = status;
        }
    }
    
    public static List<CharacterDefinition> GetAllCharacters()
    {
        return _charactersFile?.characters ?? new List<CharacterDefinition>();
    }
    
    public static CharacterDefinition GetCharacter(string id)
    {
        if (_characterById != null && _characterById.TryGetValue(id, out var character))
        {
            return character;
        }
        return null;
    }
    
    public static CharacterDefinition GetCharacterByNumericId(int numericId)
    {
        if (_charactersFile == null) return null;
        
        foreach (var character in _charactersFile.characters)
        {
            if (character.characterId == numericId)
                return character;
        }
        return null;
    }
    
    public static List<SkillDefinition> GetAllSkills()
    {
        return _skillsFile?.skills ?? new List<SkillDefinition>();
    }
    
    public static SkillDefinition GetSkill(string id)
    {
        if (_skillById != null && _skillById.TryGetValue(id, out var skill))
        {
            return skill;
        }
        return null;
    }
    
    public static List<EffectDefinition> GetAllEffects()
    {
        return _effectsFile?.effects ?? new List<EffectDefinition>();
    }
    
    public static EffectDefinition GetEffect(string id)
    {
        if (_effectById != null && _effectById.TryGetValue(id, out var effect))
        {
            return effect;
        }
        return null;
    }
    
    public static List<StatusDefinition> GetAllStatuses()
    {
        return _effectsFile?.statuses ?? new List<StatusDefinition>();
    }
    
    public static StatusDefinition GetStatus(string id)
    {
        if (_statusById != null && _statusById.TryGetValue(id, out var status))
        {
            return status;
        }
        return null;
    }
    
    public static CharacterData ToCharacterData(CharacterDefinition def)
    {
        if (def == null) return null;
        
        int baseDamage = (def.stats.damageMin + def.stats.damageMax) / 2;
        string damageRangeLabel = $"{def.stats.damageMin}-{def.stats.damageMax}";
        
        // Load 5 skills: skills 1-4 are regular (energy gain), skill 5 is ultimate (energy cost)
        var skill1 = def.skillIds.Count > 0 ? GetSkill(def.skillIds[0]) : null;
        var skill2 = def.skillIds.Count > 1 ? GetSkill(def.skillIds[1]) : null;
        var skill3 = def.skillIds.Count > 2 ? GetSkill(def.skillIds[2]) : null;
        var skill4 = def.skillIds.Count > 3 ? GetSkill(def.skillIds[3]) : null;
        var skill5 = def.skillIds.Count > 4 ? GetSkill(def.skillIds[4]) : null;
        
        return new CharacterData
        {
            DisplayName = def.displayName,
            CharacterID = def.characterId,
            MaxHealth = def.stats.maxHealth,
            Damage = baseDamage,
            DamageRangeLabel = damageRangeLabel,
            Gold = def.gold,
            MaxEnergy = def.stats.maxEnergy,
            CritChance = def.stats.critChance,
            CritDamage = def.stats.critDamage,
            BaseResistance = def.stats.baseResistance,
            BonusResistance = def.stats.bonusResistance,
            Skill1 = skill1?.displayName ?? "",
            Skill1DamagePercent = GetSkillDamagePercent(skill1),
            Skill1Effect = GetSkillEffectDescription(skill1),
            Skill1Cooldown = skill1?.cooldownTurns ?? 0,
            Skill1EnergyGain = GetSkillEnergyGain(skill1),
            Skill1APCost = skill1?.apCost ?? 2,
            Skill1Element = skill1?.element ?? "none",
            Skill1MarkChance = skill1?.markChance ?? 100,
            Skill1MarkCount = skill1?.markCount ?? 1,
            Skill2 = skill2?.displayName ?? "",
            Skill2DamagePercent = GetSkillDamagePercent(skill2),
            Skill2Effect = GetSkillEffectDescription(skill2),
            Skill2Cooldown = skill2?.cooldownTurns ?? 0,
            Skill2EnergyGain = GetSkillEnergyGain(skill2),
            Skill2APCost = skill2?.apCost ?? 2,
            Skill2Element = skill2?.element ?? "none",
            Skill2MarkChance = skill2?.markChance ?? 100,
            Skill2MarkCount = skill2?.markCount ?? 1,
            Skill3 = skill3?.displayName ?? "",
            Skill3DamagePercent = GetSkillDamagePercent(skill3),
            Skill3Effect = GetSkillEffectDescription(skill3),
            Skill3Cooldown = skill3?.cooldownTurns ?? 0,
            Skill3EnergyGain = GetSkillEnergyGain(skill3),
            Skill3APCost = skill3?.apCost ?? 2,
            Skill3Element = skill3?.element ?? "none",
            Skill3MarkChance = skill3?.markChance ?? 100,
            Skill3MarkCount = skill3?.markCount ?? 1,
            Skill4 = skill4?.displayName ?? "",
            Skill4DamagePercent = GetSkillDamagePercent(skill4),
            Skill4Effect = GetSkillEffectDescription(skill4),
            Skill4Cooldown = skill4?.cooldownTurns ?? 0,
            Skill4EnergyGain = GetSkillEnergyGain(skill4),
            Skill4APCost = skill4?.apCost ?? 2,
            Skill4Element = skill4?.element ?? "none",
            Skill4MarkChance = skill4?.markChance ?? 100,
            Skill4MarkCount = skill4?.markCount ?? 1,
            Skill5 = skill5?.displayName ?? "",
            Skill5DamagePercent = GetSkillDamagePercent(skill5),
            Skill5Effect = GetSkillEffectDescription(skill5),
            Skill5Cooldown = skill5?.cooldownTurns ?? 0,
            Skill5EnergyCost = GetSkillEnergyCost(skill5),
            Skill5APCost = skill5?.apCost ?? 3,
            Skill5Element = skill5?.element ?? "none",
            Skill5MarkChance = skill5?.markChance ?? 100,
            Skill5MarkCount = skill5?.markCount ?? 1,
            Perk1a = def.perkIds?.tier1?.Count > 0 ? def.perkIds.tier1[0] : "",
            Perk1b = def.perkIds?.tier1?.Count > 1 ? def.perkIds.tier1[1] : "",
            Perk2a = def.perkIds?.tier2?.Count > 0 ? def.perkIds.tier2[0] : "",
            Perk2b = def.perkIds?.tier2?.Count > 1 ? def.perkIds.tier2[1] : "",
            Perk3a = def.perkIds?.tier3?.Count > 0 ? def.perkIds.tier3[0] : "",
            Perk3b = def.perkIds?.tier3?.Count > 1 ? def.perkIds.tier3[1] : "",
            Perk4a = def.perkIds?.tier4?.Count > 0 ? def.perkIds.tier4[0] : "",
            Perk4b = def.perkIds?.tier4?.Count > 1 ? def.perkIds.tier4[1] : ""
        };
    }
    
    private static float GetSkillDamagePercent(SkillDefinition skill)
    {
        if (skill?.executions == null) return 100f;
        
        foreach (var exec in skill.executions)
        {
            if (exec.effectId == "eff_deal_damage" && exec.@params?.scaling != null)
            {
                return exec.@params.scaling.multiplier * 100f;
            }
        }
        return 100f;
    }
    
    private static string GetSkillEffectDescription(SkillDefinition skill)
    {
        return skill?.description ?? "";
    }
    
    private static int GetSkillEnergyGain(SkillDefinition skill)
    {
        if (skill?.executions == null) return 0;
        
        foreach (var exec in skill.executions)
        {
            if (exec.effectId == "eff_energy_delta" && exec.@params != null && exec.@params.amount > 0)
            {
                return exec.@params.amount;
            }
        }
        return 0;
    }
    
    private static int GetSkillEnergyCost(SkillDefinition skill)
    {
        if (skill?.executions == null) return 0;
        
        foreach (var exec in skill.executions)
        {
            if (exec.effectId == "eff_energy_delta" && exec.@params != null && exec.@params.amount < 0)
            {
                return -exec.@params.amount;
            }
        }
        return 0;
    }
}
