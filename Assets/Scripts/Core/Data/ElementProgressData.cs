using System;

/// <summary>
/// Per-element meta progression save data.
/// Tracks ascension level and XP for a single element.
/// </summary>
[Serializable]
public class ElementProgressData
{
    public string ElementName;
    public int AscensionLevel = 1;
    public int AscensionXP = 0;
    
    public ElementProgressData() { }
    
    public ElementProgressData(string elementName)
    {
        ElementName = elementName;
        AscensionLevel = 1;
        AscensionXP = 0;
    }
}
