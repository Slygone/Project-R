using System;

/// <summary>
/// Data model for elemental tier data.
/// Schema: element, level, xpRequired, bonus
/// </summary>
[Serializable]
public class ElementalTierData
{
    public string Element;
    public int Level;
    public int XPRequiredToReachLevel;
    public string Bonus;
}
