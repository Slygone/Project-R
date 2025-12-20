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
        return DataCache.GetQTEMultiplier(result);
    }

    public static float GetReactionMultiplier(Element a, Element b)
    {
        var (_, multiplier) = DataCache.GetReaction(a, b);
        return multiplier;
    }

    public static string GetReactionName(Element a, Element b)
    {
        var (name, _) = DataCache.GetReaction(a, b);
        return name;
    }

    public static int ComputeFinalDamage(int baseDamage, int elementalBonus, float reactionMultiplier, float critMultiplier, float qteMultiplier)
    {
        float damageAfterReaction = (baseDamage + elementalBonus) * reactionMultiplier;
        float damageAfterCrit = damageAfterReaction * critMultiplier;
        float finalDamage = damageAfterCrit * qteMultiplier;
        return Mathf.RoundToInt(finalDamage);
    }
}
