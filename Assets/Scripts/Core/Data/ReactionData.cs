using System.Collections.Generic;

public class ReactionData
{
    public string ReactionId;
    public string Name;
    public string Type;     // "mono" or "dual"
    public bool CanCrit;
    public List<ReactionEffectEntry> Effects = new List<ReactionEffectEntry>();
}
