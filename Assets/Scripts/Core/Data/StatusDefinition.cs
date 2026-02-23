using System;

/// <summary>
/// Data-driven status definition loaded from effects.json.
/// Defines display info (name, category, description) for a status effect.
/// Gameplay logic remains in Player.cs / CombatEnemy.cs — this is purely metadata.
/// </summary>
[Serializable]
public class StatusDefinition
{
    public string Id;           // e.g. "status_weaken"
    public string Name;         // e.g. "Weakened"
    public string Type;         // e.g. "weaken"
    public string Category;     // "buff" or "debuff"
    public string Description;  // Tooltip template with {magnitude}, {duration}, {stacks} placeholders

    public bool IsBuff => Category == "buff";
    public bool IsDebuff => Category == "debuff";

    /// <summary>
    /// Resolve description placeholders with runtime values.
    /// </summary>
    public string GetDescription(float magnitude = 0f, int duration = 0, int stacks = 0)
    {
        string desc = Description ?? "";
        desc = desc.Replace("{magnitude}", magnitude.ToString("F0"));
        desc = desc.Replace("{duration}", duration.ToString());
        desc = desc.Replace("{stacks}", stacks.ToString());
        return desc;
    }
}
