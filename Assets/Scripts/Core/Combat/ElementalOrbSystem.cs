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
        Debug.Log($"[ElementalOrbSystem] Element pair set: Orb A = {a}, Orb B = {b}");
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
            Debug.Log($"[ElementalOrbSystem] Orb A infused with {orbAElement} - mark applied (order: {infusionOrder})");
        }
        else
        {
            orbBMark = orbBElement;
            orbBActive = true;
            infusedElement = orbBElement;
            ApplyElementBuff(orbBElement);
            Debug.Log($"[ElementalOrbSystem] Orb B infused with {orbBElement} - mark applied (order: {infusionOrder})");
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
        
        Debug.Log($"[ElementalOrbSystem] First={firstElement}, Detonator={detonatorElement}");
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
            Debug.Log("[ElementalOrbSystem] No reaction ready to trigger");
            return;
        }
        
        string reactionId = GetReactionId();
        string reactionName = GetReactionName();
        Debug.Log($"[ElementalOrbSystem] REACTION TRIGGERED: {reactionId} ({reactionName})");
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
        Debug.Log("[ElementalOrbSystem] Marks cleared");
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
                Debug.Log("[ElementalOrbSystem] BUFF: Fire - Attack power increased (TBD)");
                break;
            case Element.Ice:
                Debug.Log("[ElementalOrbSystem] BUFF: Ice - Crit chance increased (TBD)");
                break;
            case Element.Water:
                Debug.Log("[ElementalOrbSystem] BUFF: Water - Damage reduction applied (TBD)");
                break;
            case Element.Wind:
                Debug.Log("[ElementalOrbSystem] BUFF: Wind - Speed/evasion increased (TBD)");
                break;
            case Element.Rock:
                Debug.Log("[ElementalOrbSystem] BUFF: Rock - Shield granted (TBD)");
                break;
            default:
                Debug.Log("[ElementalOrbSystem] BUFF: None");
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
