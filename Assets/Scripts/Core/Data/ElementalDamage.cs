using System;
using System.Collections.Generic;

[Serializable]
public class ElementalDamage
{
    private Dictionary<Element, int> damages = new Dictionary<Element, int>();

    public ElementalDamage()
    {
        foreach (Element element in Enum.GetValues(typeof(Element)))
        {
            if (element != Element.None)
            {
                damages[element] = 0;
            }
        }
    }

    public int Get(Element element)
    {
        if (element == Element.None) return 0;
        return damages.ContainsKey(element) ? damages[element] : 0;
    }

    public void Set(Element element, int value)
    {
        if (element == Element.None) return;
        damages[element] = value;
    }

    public void Add(Element element, int value)
    {
        if (element == Element.None) return;
        if (!damages.ContainsKey(element))
            damages[element] = 0;
        damages[element] += value;
    }

    public int GetTotal()
    {
        int total = 0;
        foreach (var kvp in damages)
        {
            total += kvp.Value;
        }
        return total;
    }

    public Dictionary<Element, int> GetAll()
    {
        return new Dictionary<Element, int>(damages);
    }

    public static Element ParseElement(string elementName)
    {
        if (string.IsNullOrEmpty(elementName)) return Element.None;

        switch (elementName.ToLower().Trim())
        {
            case "fire": return Element.Fire;
            case "ice": return Element.Ice;
            case "water": return Element.Water;
            case "wind": return Element.Wind;
            case "rock": return Element.Rock;
            default: return Element.None;
        }
    }
}
