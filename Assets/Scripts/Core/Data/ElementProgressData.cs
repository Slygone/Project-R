using System;

/// <summary>
/// Per-element meta progression save data.
/// Tracks ascension level for a single element.
/// Level-ups are paid with Essence Cores (no XP).
/// </summary>
[Serializable]
public class ElementProgressData
{
    public string ElementName;
    public int AscensionLevel = 1;
    
    public ElementProgressData() { }
    
    public ElementProgressData(string elementName)
    {
        ElementName = elementName;
        AscensionLevel = 1;
    }
}
