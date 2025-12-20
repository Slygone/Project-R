using UnityEngine;

public class ElementalOrbSystem
{
    private Element orbAElement = Element.None;
    private Element orbBElement = Element.None;
    private Element orbAMark = Element.None;
    private Element orbBMark = Element.None;
    private bool orbAActive = false;
    private bool orbBActive = false;
    
    public Element OrbAElement => orbAElement;
    public Element OrbBElement => orbBElement;
    public Element OrbAMark => orbAMark;
    public Element OrbBMark => orbBMark;
    public bool IsOrbAActive => orbAActive;
    public bool IsOrbBActive => orbBActive;
    
    public void SetElementPair(Element a, Element b)
    {
        orbAElement = a;
        orbBElement = b;
        ClearMarks();
        Debug.Log($"[ElementalOrbSystem] Element pair set: Orb A = {a}, Orb B = {b}");
    }
    
    public void InfuseOrb(bool useOrbA)
    {
        if (useOrbA)
        {
            orbAMark = orbAElement;
            orbAActive = true;
            ApplyElementBuff(orbAElement);
            Debug.Log($"[ElementalOrbSystem] Orb A infused with {orbAElement} - mark applied");
        }
        else
        {
            orbBMark = orbBElement;
            orbBActive = true;
            ApplyElementBuff(orbBElement);
            Debug.Log($"[ElementalOrbSystem] Orb B infused with {orbBElement} - mark applied");
        }
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
        
        var (elementA, elementB) = GetReactionPair();
        ApplyReactionEffect(elementA, elementB);
        ClearMarks();
    }
    
    public void ClearMarks()
    {
        orbAMark = Element.None;
        orbBMark = Element.None;
        orbAActive = false;
        orbBActive = false;
        Debug.Log("[ElementalOrbSystem] Marks cleared");
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
    
    private void ApplyReactionEffect(Element a, Element b)
    {
        string reactionName = GetReactionName(a, b);
        Debug.Log($"[ElementalOrbSystem] REACTION TRIGGERED: {a} + {b} = {reactionName}!");
        
        switch (reactionName)
        {
            case "Melt":
                Debug.Log("[ElementalOrbSystem] REACTION EFFECT: Melt - 1.5x damage multiplier (TBD)");
                break;
            case "Freeze":
                Debug.Log("[ElementalOrbSystem] REACTION EFFECT: Freeze - Enemy frozen for 1 turn (TBD)");
                break;
            case "Vaporize":
                Debug.Log("[ElementalOrbSystem] REACTION EFFECT: Vaporize - 2x damage multiplier (TBD)");
                break;
            case "Swirl":
                Debug.Log("[ElementalOrbSystem] REACTION EFFECT: Swirl - AoE damage spread (TBD)");
                break;
            case "Crystallize":
                Debug.Log("[ElementalOrbSystem] REACTION EFFECT: Crystallize - Shield generated (TBD)");
                break;
            case "Overload":
                Debug.Log("[ElementalOrbSystem] REACTION EFFECT: Overload - Explosive AoE damage (TBD)");
                break;
            case "Superconduct":
                Debug.Log("[ElementalOrbSystem] REACTION EFFECT: Superconduct - Defense shred (TBD)");
                break;
            case "Electro-Charged":
                Debug.Log("[ElementalOrbSystem] REACTION EFFECT: Electro-Charged - DoT applied (TBD)");
                break;
            default:
                Debug.Log($"[ElementalOrbSystem] REACTION EFFECT: {reactionName} - Generic bonus damage (TBD)");
                break;
        }
    }
    
    private string GetReactionName(Element a, Element b)
    {
        if ((a == Element.Fire && b == Element.Ice) || (a == Element.Ice && b == Element.Fire))
            return "Melt";
        if ((a == Element.Ice && b == Element.Water) || (a == Element.Water && b == Element.Ice))
            return "Freeze";
        if ((a == Element.Fire && b == Element.Water) || (a == Element.Water && b == Element.Fire))
            return "Vaporize";
        if ((a == Element.Wind && b != Element.Wind && b != Element.Rock) || 
            (b == Element.Wind && a != Element.Wind && a != Element.Rock))
            return "Swirl";
        if ((a == Element.Rock && b != Element.Rock) || (b == Element.Rock && a != Element.Rock))
            return "Crystallize";
        if ((a == Element.Fire && b == Element.Wind) || (a == Element.Wind && b == Element.Fire))
            return "Overload";
            
        return $"{a}+{b}";
    }
    
    public float GetReactionDamageMultiplier()
    {
        if (!HasReactionReady()) return 1f;
        
        var (a, b) = GetReactionPair();
        string reactionName = GetReactionName(a, b);
        
        return reactionName switch
        {
            "Melt" => 1.5f,
            "Vaporize" => 2.0f,
            "Freeze" => 1.0f,
            "Swirl" => 1.3f,
            "Crystallize" => 1.0f,
            "Overload" => 1.75f,
            "Superconduct" => 1.25f,
            _ => 1.2f
        };
    }
}
