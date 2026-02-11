using System.Collections.Generic;

/// <summary>
/// Display data for a reaction status chip shown on enemy/player UI.
/// Separate from gameplay buff/debuff tracking — this is purely for UI display.
/// </summary>
[System.Serializable]
public class ReactionChipInfo
{
    public string ChipName;      // Display name: "Tidal Surge", "Frozen", "Glacial Focus"
    public string Tooltip;       // Mouseover text: "Player heals 1% max HP when this enemy attacks (2 turns)"
    public int TurnsRemaining;   // Ticks down each turn
    public bool IsPlayerBuff;    // true = show on player UI, false = show on enemy UI
    
    public ReactionChipInfo(string name, string tooltip, int turns, bool isPlayerBuff = false)
    {
        ChipName = name;
        Tooltip = tooltip;
        TurnsRemaining = turns;
        IsPlayerBuff = isPlayerBuff;
    }
}
