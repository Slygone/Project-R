/// <summary>
/// Runtime snapshot of an active status effect on a combat entity.
/// Used by StatusDisplayUI to render buff/debuff chips dynamically.
/// Callers populate Category and Description from DataCache so the UI is fully data-driven.
/// </summary>
public struct ActiveStatusInfo
{
    public string StatusId;         // References StatusDefinition.Id (e.g. "status_weaken")
    public string DisplayName;     // Resolved display name (e.g. "Weakened", "Deadeye Ready")
    public int Duration;           // Turns remaining (0 = permanent/passive)
    public float Magnitude;        // Effect value for tooltip (%, damage, etc.)
    public int Stacks;             // Current stack count (0 if not stackable)
    public string Source;          // What applied this (relic name, reaction name, enemy skill)
    public string Category;        // "buff" or "debuff" — resolved by caller from DataCache
    public string Description;     // Pre-resolved tooltip text — resolved by caller from DataCache

    public ActiveStatusInfo(string statusId, int duration, float magnitude = 0f, int stacks = 0,
        string displayName = null, string source = null, string category = "debuff", string description = null)
    {
        StatusId = statusId;
        Duration = duration;
        Magnitude = magnitude;
        Stacks = stacks;
        DisplayName = displayName;
        Source = source;
        Category = category;
        Description = description;
    }

    public bool IsBuff => Category == "buff";
    public bool IsDebuff => Category != "buff";
}
