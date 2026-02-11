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

    public static string GetReactionName(string reactionId)
    {
        var reactionDef = DataCache.GetReactionDef(reactionId);
        return reactionDef != null ? reactionDef.Name : reactionId;
    }
    
    public static string BuildReactionId(Element firstElement, Element detonatorElement)
    {
        return DataCache.BuildReactionId(firstElement, detonatorElement);
    }
}
