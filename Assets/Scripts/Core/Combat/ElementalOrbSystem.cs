using UnityEngine;

public class ElementalOrbSystem
{
    private Element orbAElement = Element.None;
    private Element orbBElement = Element.None;
    private Element orbAMark = Element.None;
    private Element orbBMark = Element.None;
    private bool orbAActive = false;
    private bool orbBActive = false;
    private Element detonatorElement = Element.None;
    private Element firstElement = Element.None;  // The element applied BEFORE the detonator
    private int infusionOrder = 0;
    
    public Element OrbAElement => orbAElement;
    public Element OrbBElement => orbBElement;
    public Element OrbAMark => orbAMark;
    public Element OrbBMark => orbBMark;
    public bool IsOrbAActive => orbAActive;
    public bool IsOrbBActive => orbBActive;
    public Element DetonatorElement => detonatorElement;
    
    public void SetElementPair(Element a, Element b)
    {
        orbAElement = a;
        orbBElement = b;
        ClearMarks();
        GameLog.Reaction(GameLog.Join(
            "ElementPairSet",
            GameLog.KV("orbA", a),
            GameLog.KV("orbB", b)
        ), GameLogVerbosity.Normal);
    }
    
    public void InfuseOrb(bool useOrbA)
    {
        infusionOrder++;
        Element infusedElement;
        Element previousMark = Element.None;
        
        // Track what was already applied before this infusion
        if (orbAActive) previousMark = orbAMark;
        if (orbBActive && previousMark == Element.None) previousMark = orbBMark;
        
        if (useOrbA)
        {
            orbAMark = orbAElement;
            orbAActive = true;
            infusedElement = orbAElement;
            ApplyElementBuff(orbAElement);
            GameLog.Reaction(GameLog.Join(
                "Infuse",
                GameLog.KV("orb", "A"),
                GameLog.KV("element", orbAElement),
                GameLog.KV("order", infusionOrder)
            ), GameLogVerbosity.Verbose);
        }
        else
        {
            orbBMark = orbBElement;
            orbBActive = true;
            infusedElement = orbBElement;
            ApplyElementBuff(orbBElement);
            GameLog.Reaction(GameLog.Join(
                "Infuse",
                GameLog.KV("orb", "B"),
                GameLog.KV("element", orbBElement),
                GameLog.KV("order", infusionOrder)
            ), GameLogVerbosity.Verbose);
        }
        
        // Update first/detonator tracking
        if (previousMark != Element.None && previousMark != infusedElement)
        {
            // There was already a mark - that's the first element, this is the detonator
            firstElement = previousMark;
            detonatorElement = infusedElement;
        }
        else
        {
            // This is the first mark
            firstElement = infusedElement;
            detonatorElement = Element.None;
        }
        
        GameLog.Reaction(GameLog.Join(
            "Marks",
            GameLog.KV("first", firstElement),
            GameLog.KV("detonator", detonatorElement)
        ), GameLogVerbosity.Verbose);
    }
    
    public Element GetInfusedElement(bool useOrbA)
    {
        return useOrbA ? orbAElement : orbBElement;
    }
    
    public bool HasReactionReady()
    {
        return orbAActive && orbBActive && orbAMark != Element.None && orbBMark != Element.None && orbAMark != orbBMark;
    }
    
    public (Element, Element) GetReactionPair()
    {
        return (orbAMark, orbBMark);
    }
    
    public void TriggerReaction()
    {
        if (!HasReactionReady())
        {
            GameLog.Reaction(GameLog.Join(
                "Trigger",
                GameLog.KV("ready", false)
            ), GameLogVerbosity.Verbose);
            return;
        }
        
        string reactionId = GetReactionId();
        string reactionName = GetReactionName();
        GameLog.Reaction(GameLog.Join(
            "Trigger",
            GameLog.KV("ready", true),
            GameLog.KV("reactionId", reactionId ?? "null"),
            GameLog.KV("reaction", reactionName)
        ), GameLogVerbosity.Normal);
        ClearMarks();
    }
    
    public void ClearMarks()
    {
        orbAMark = Element.None;
        orbBMark = Element.None;
        orbAActive = false;
        orbBActive = false;
        detonatorElement = Element.None;
        firstElement = Element.None;
        infusionOrder = 0;
        GameLog.Reaction(GameLog.Join(
            "MarksClear"
        ), GameLogVerbosity.Verbose);
    }
    
    // Get the first element (the one applied before the detonator)
    public Element GetFirstElement()
    {
        return firstElement;
    }
    
    // Build the directional ReactionId (FirstElement_DetonatorElement)
    public string GetReactionId()
    {
        if (!HasReactionReady()) return null;
        return DataCache.BuildReactionId(firstElement, detonatorElement);
    }
    
    private void ApplyElementBuff(Element element)
    {
        switch (element)
        {
            case Element.Fire:
                GameLog.Reaction(GameLog.Join("Buff", GameLog.KV("element", "Fire")), GameLogVerbosity.Verbose);
                break;
            case Element.Ice:
                GameLog.Reaction(GameLog.Join("Buff", GameLog.KV("element", "Ice")), GameLogVerbosity.Verbose);
                break;
            case Element.Water:
                GameLog.Reaction(GameLog.Join("Buff", GameLog.KV("element", "Water")), GameLogVerbosity.Verbose);
                break;
            case Element.Wind:
                GameLog.Reaction(GameLog.Join("Buff", GameLog.KV("element", "Wind")), GameLogVerbosity.Verbose);
                break;
            case Element.Rock:
                GameLog.Reaction(GameLog.Join("Buff", GameLog.KV("element", "Rock")), GameLogVerbosity.Verbose);
                break;
            default:
                GameLog.Reaction(GameLog.Join("Buff", GameLog.KV("element", "None")), GameLogVerbosity.Verbose);
                break;
        }
    }
    
    // Legacy method - now uses data-driven lookup
    public float GetReactionDamageMultiplier()
    {
        if (!HasReactionReady()) return 1f;
        
        string reactionId = GetReactionId();
        if (string.IsNullOrEmpty(reactionId)) return 1f;
        
        var reactionDef = DataCache.GetReactionDef(reactionId);
        return reactionDef.DamageMultiplier;
    }
    
    // Get reaction name from data
    public string GetReactionName()
    {
        if (!HasReactionReady()) return "None";
        
        string reactionId = GetReactionId();
        if (string.IsNullOrEmpty(reactionId)) return "Unknown";
        
        var reactionDef = DataCache.GetReactionDef(reactionId);
        return reactionDef.Name;
    }
}
