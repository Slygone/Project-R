using System;
using System.Collections.Generic;

[Serializable]
public class EffectDefinition
{
    public string id;
    public string type;
    public string description;
    public EffectDefaults defaults;
}

[Serializable]
public class EffectDefaults
{
    public ScalingData scaling;
    public string resource;
    public int amount;
    public string statusId;
    public int durationTurns;
    public float magnitude;
    public int percentOfDamage;
    public int capPercentMaxHP;
    public int critChanceBonus;
    public int critDamageBonus;
    public int hitCount;
    public float bonusDamageToNextTarget;
    public int? cooldownOverride;
    public int energyRefundPercent;
}

[Serializable]
public class ScalingData
{
    public string source;
    public float multiplier = 1.0f;
}

[Serializable]
public class StatusDefinitionLegacy
{
    public string id;
    public string displayName;
    public string type;
    public string description;
    public StatusDefaults defaults;
}

[Serializable]
public class StatusDefaults
{
    public int durationTurns;
    public float magnitude;
    public int maxStacks;
    public int damagePerTurn;
}

[Serializable]
public class EffectsFile
{
    public List<EffectDefinition> effects;
    public List<StatusDefinitionLegacy> statuses;
}
