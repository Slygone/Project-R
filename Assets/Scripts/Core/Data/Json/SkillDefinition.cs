using System;
using System.Collections.Generic;

[Serializable]
public class SkillDefinition
{
    public string id;
    public string name;
    public string description;
    public int apCost = 2;
    public int cooldown;
    public string element = "none";
    public int markChance = 100;
    public int markCount = 1;
    public List<EffectEntry> effects;
    public ChainSettings chainSettings;

    // Convenience accessors used by GameDataLoader.ToCharacterData
    public string displayName => name;
    public int cooldownTurns => cooldown;
}

[Serializable]
public class SkillsFile
{
    public List<SkillDefinition> skills;
}
