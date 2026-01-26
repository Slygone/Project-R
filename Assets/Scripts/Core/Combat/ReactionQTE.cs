using System;
using UnityEngine;

public enum QTEResult
{
    Bad,
    Good,
    Perfect
}

public static class ReactionQTE
{
    public static float GetQteMultiplier(QTEResult result)
    {
        return DataCache.GetQTEOffensiveMultiplier(result);
    }
    
    public static int GetQteDefensiveShieldPercent(QTEResult result)
    {
        return DataCache.GetQTEDefensiveShield(result);
    }

    // Get reaction effect info by ReactionId
    public static (string effectId, int effectValue) GetReactionEffect(string reactionId)
    {
        var reactionDef = DataCache.GetReactionDef(reactionId);
        if (reactionDef == null) return ("eff_reaction_damage", 50);
        return (reactionDef.EffectId, reactionDef.EffectValue);
    }

    public static string GetReactionName(string reactionId)
    {
        var reactionDef = DataCache.GetReactionDef(reactionId);
        return reactionDef.Name;
    }
    
    // Build ReactionId from first element and detonator
    public static string BuildReactionId(Element firstElement, Element detonatorElement)
    {
        return DataCache.BuildReactionId(firstElement, detonatorElement);
    }

    public static int ComputeFinalDamage(int baseDamage, int elementalBonus, float reactionMultiplier, float critMultiplier, float qteMultiplier)
    {
        float damageAfterReaction = (baseDamage + elementalBonus) * reactionMultiplier;
        float damageAfterCrit = damageAfterReaction * critMultiplier;
        float finalDamage = damageAfterCrit * qteMultiplier;
        return Mathf.RoundToInt(finalDamage);
    }
}
