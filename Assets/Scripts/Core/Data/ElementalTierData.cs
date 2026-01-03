using System;

/// <summary>
/// Data model for elemental tier CSV rows.
/// Schema: Element, Level, XPRequiredToReachLevel, Bonus
/// </summary>
[Serializable]
public class ElementalTierData
{
    public string Element;
    public int Level;
    public int XPRequiredToReachLevel;
    public string Bonus;
}
