using System;

/// <summary>
/// Cost data for elemental ascension level-up.
/// Loaded from progressionCosts.json → elementalAscension array.
/// </summary>
[Serializable]
public class ElementalAscensionCost
{
    public string Element;
    public int Level;
    public int RegularCost;
    public int AscendedCost;
}

/// <summary>
/// Cost data for character ascension level-up.
/// Loaded from progressionCosts.json → characterAscension array.
/// </summary>
[Serializable]
public class CharacterAscensionCost
{
    public int Level;
    public int RegularCost;
    public int AscendedCost;
}
