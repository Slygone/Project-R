public class ElementPair
{
    public string DisplayName;
    public Element OrbA;
    public Element OrbB;
    
    public ElementPair(string name, Element a, Element b)
    {
        DisplayName = name;
        OrbA = a;
        OrbB = b;
    }
    
    public static ElementPair[] GetPredefinedPairs()
    {
        return new ElementPair[]
        {
            new ElementPair("Fire & Ice", Element.Fire, Element.Ice),
            new ElementPair("Rock & Water", Element.Rock, Element.Water),
            new ElementPair("Wind & Ice", Element.Wind, Element.Ice),
            new ElementPair("Fire & Wind", Element.Fire, Element.Wind),
            new ElementPair("Water & Wind", Element.Water, Element.Wind),
            new ElementPair("Rock & Fire", Element.Rock, Element.Fire),
            new ElementPair("Fire & Water", Element.Fire, Element.Water),
            new ElementPair("Ice & Water", Element.Ice, Element.Water),
            new ElementPair("Rock & Ice", Element.Rock, Element.Ice),
            new ElementPair("Rock & Wind", Element.Rock, Element.Wind)
        };
    }
}
