using UnityEngine;

/// <summary>
/// Shared utility for consistent element colors across all UI systems.
/// </summary>
public static class ElementColors
{
    public static Color Get(Element element)
    {
        return element switch
        {
            Element.Fire => new Color(1f, 0.4f, 0.2f),
            Element.Ice => new Color(0.4f, 0.8f, 1f),
            Element.Water => new Color(0.2f, 0.5f, 1f),
            Element.Wind => new Color(0.6f, 1f, 0.6f),
            Element.Rock => new Color(0.7f, 0.5f, 0.3f),
            Element.Lightning => new Color(0.9f, 0.8f, 0.2f),
            _ => Color.white
        };
    }

    public static Color Get(string element)
    {
        if (string.IsNullOrEmpty(element)) return Color.white;
        
        return element.ToLower() switch
        {
            "fire" => new Color(1f, 0.4f, 0.2f),
            "ice" => new Color(0.4f, 0.8f, 1f),
            "water" => new Color(0.2f, 0.5f, 1f),
            "wind" => new Color(0.6f, 1f, 0.6f),
            "rock" => new Color(0.7f, 0.5f, 0.3f),
            "lightning" => new Color(0.9f, 0.8f, 0.2f),
            _ => Color.white
        };
    }

    public static string GetHex(Element element)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(Get(element));
    }

    public static string GetHtmlTag(Element element)
    {
        return $"<color={GetHex(element)}>";
    }
}
