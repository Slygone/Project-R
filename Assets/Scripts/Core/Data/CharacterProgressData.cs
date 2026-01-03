using System;

/// <summary>
/// Per-character meta-progression data for the ascension system.
/// Stored and persisted via MetaProgressionManager.
/// </summary>
[Serializable]
public class CharacterProgressData
{
    public int CharacterID;
    public int AscensionLevel = 1;  // Characters start at Ascension Level 1
    
    /// <summary>
    /// Talent choices per tier. Index = tier (0-3), Value = 0 (None), 1 (A), 2 (B)
    /// </summary>
    public int[] TalentChoices = new int[4];
    
    public CharacterProgressData() { }
    
    public CharacterProgressData(int characterId)
    {
        CharacterID = characterId;
        AscensionLevel = 1;
        TalentChoices = new int[4];
    }
    
    /// <summary>
    /// Returns the tier index (0-based) that is unlocked at the given ascension level.
    /// Level 2 => Tier 0, Level 3 => Tier 1, etc.
    /// Returns -1 if no tier is unlocked at that level.
    /// </summary>
    public static int GetUnlockedTierForLevel(int level)
    {
        return level - 2;  // Level 2 unlocks tier 0, level 3 unlocks tier 1, etc.
    }
    
    /// <summary>
    /// Check if a specific tier is unlocked based on current ascension level.
    /// </summary>
    public bool IsTierUnlocked(int tierIndex)
    {
        // Tier 0 unlocks at level 2, tier 1 at level 3, etc.
        int requiredLevel = tierIndex + 2;
        return AscensionLevel >= requiredLevel;
    }
    
    /// <summary>
    /// Check if there are any unlocked tiers without a selection (for "UP" badge).
    /// </summary>
    public bool HasUnselectedUnlockedTier(int totalTiers = 4)
    {
        for (int i = 0; i < totalTiers; i++)
        {
            if (IsTierUnlocked(i) && TalentChoices[i] == 0)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Get the talent choice for a tier: 0 = None, 1 = A, 2 = B
    /// </summary>
    public int GetTalentChoice(int tierIndex)
    {
        if (tierIndex >= 0 && tierIndex < TalentChoices.Length)
        {
            return TalentChoices[tierIndex];
        }
        return 0;
    }
    
    /// <summary>
    /// Set the talent choice for a tier: 1 = A, 2 = B
    /// </summary>
    public void SetTalentChoice(int tierIndex, int choice)
    {
        if (tierIndex >= 0 && tierIndex < TalentChoices.Length)
        {
            TalentChoices[tierIndex] = choice;
        }
    }
}
