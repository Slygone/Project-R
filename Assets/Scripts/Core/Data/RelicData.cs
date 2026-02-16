using System.Collections.Generic;

/// <summary>
/// Data-driven relic definition loaded from relics.json.
/// Trigger determines when effects are applied:
///   "onAcquire"    - immediately when relic is gained (stat bonuses)
///   "combatStart"  - at the start of each combat encounter
///   "onSkillUse"   - every N skill uses (triggerInterval)
///   "onTurnEnd"    - at the end of each player turn
///   "onTurnStart"  - at the start of each player turn (AP mods, periodic effects)
///   "onCombatEnd"  - at the end of combat (self-damage, etc.)
///   "onDeath"      - when player would die (revival)
///   "permanent"    - permanent mode change (applied once, cannot be removed)
///   "passive"      - always active, checked by relevant systems
/// </summary>
public class RelicData
{
    public string Id;
    public string DisplayName;
    public int RelicID;
    public string Rarity;          // "Common", "Legendary", "Cursed"
    public string Description;
    public string Trigger;         // when effects activate (see summary above)
    public int TriggerInterval;    // for periodic triggers (e.g., every N skills/turns)
    public int Price;              // gold cost in shop (0 = not purchasable)
    public List<EffectEntry> Effects;
}
