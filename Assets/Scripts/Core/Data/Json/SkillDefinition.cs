using System;
using System.Collections.Generic;

[Serializable]
public class SkillDefinition
{
    public string id;
    public string displayName;
    public string description;
    public int cooldownTurns;
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
