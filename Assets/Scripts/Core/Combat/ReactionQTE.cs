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

    // New directional lookup by ReactionId
    public static float GetReactionMultiplier(string reactionId)
    {
        var reactionDef = DataCache.GetReactionDef(reactionId);
        return reactionDef.DamageMultiplier;
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
