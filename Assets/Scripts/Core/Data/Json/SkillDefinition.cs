using System;
using System.Collections.Generic;

[Serializable]
public class SkillDefinition
{
    public string id;
    public string displayName;
    public string description;
    public int cooldownTurns;
    public int apCost = 2; // Action Points cost per use
    public string element = "none"; // Enchanted element (none, fire, water, lightning, ice, earth)
    public int markChance = 100; // Chance to apply mark (0-100)
    public int markCount = 1; // Number of marks to apply
    public List<SkillExecution> executions;
}

[Serializable]
public class SkillExecution
{
    public TargetData target;
    public string effectId;
    public ExecutionParams @params;
    public string when;
}

[Serializable]
public class TargetData
{
    public string selector;
}

[Serializable]
public class ExecutionParams
{
    public ScalingData scaling;
    public int amount;
    public string statusId;
    public int durationTurns;
    public float magnitude;
    public int percentOfDamage;
    public int capPercentMaxHP;
    public int critChanceBonus;
    public int critDamageBonus;
    public int hitCount;
    public float bonusDamageToNextTargetMultiplier;
    public int? cooldownOverride;
    public int energyRefundPercent;
    public float stackBonusPerUse;
    public int maxStacks;
    public int forceCooldownAfterMaxChain;
    public int maxChainUses;
}

[Serializable]
public class SkillsFile
{
    public List<SkillDefinition> skills;
}
