using UnityEngine;

/// <summary>
/// Enum for different floating text event types.
/// Used to determine styling and behavior.
/// </summary>
public enum FloatingTextType
{
    // Damage
    DamageDealt,
    DamageTaken,
    CriticalHit,
    DoTTick,
    
    // Healing & Shields
    Heal,
    ShieldGain,
    ShieldAbsorb,
    ShieldBroken,
    
    // Status Effects
    StatusGain,
    StatusLost,
    StatusStack,
    Immune,
    Resisted,
    
    // Elemental
    OrbActivated,
    ReactionTriggered,
    ReactionDamage,
    
    // Turn Feedback
    TurnSkipped,
    EnergyChange,
    
    // Generic
    Generic
}

/// <summary>
/// Configuration for a specific floating text type.
/// </summary>
[System.Serializable]
public class FloatingTextStyle
{
    public FloatingTextType type;
    public Color color = Color.white;
    public Color outlineColor = Color.black;
    public float fontSize = 24f;
    public float duration = 1.2f;
    public float floatSpeed = 50f;       // Pixels per second upward
    public float floatDirection = 1f;    // 1 = up, -1 = down (for damage taken below character)
    public float fadeStartTime = 0.6f;   // When to start fading (0-1 normalized)
    public bool shake = false;
    public float shakeIntensity = 3f;
    public bool scalePunch = false;      // Pop-in effect
    public float scaleMultiplier = 1.2f; // Initial scale for punch
    public string prefix = "";           // e.g., "+" for heals
    public string suffix = "";           // e.g., " Shield"
    public bool enabled = true;          // Toggle for experimentation
}

/// <summary>
/// ScriptableObject configuration for all floating text styles.
/// Create via Assets > Create > Combat > Floating Text Config
/// </summary>
[CreateAssetMenu(fileName = "FloatingTextConfig", menuName = "Combat/Floating Text Config")]
public class FloatingTextConfig : ScriptableObject
{
    [Header("Global Settings")]
    public float globalDelay = 0.15f;           // Delay between queued texts
    public float verticalOffset = 1.5f;         // Base offset above target
    public float stackOffset = 0.3f;            // Vertical offset per stacked text
    public int maxConcurrentTexts = 8;          // Max texts on screen at once
    public float combatPauseAfterPlayerAction = 0.5f;
    public float combatPauseAfterEnemyAction = 0.4f;
    
    [Header("Text Styles")]
    public FloatingTextStyle[] styles = new FloatingTextStyle[]
    {
        // Damage
        new FloatingTextStyle { type = FloatingTextType.DamageDealt, color = new Color(1f, 0.3f, 0.3f), fontSize = 28f, duration = 1.2f },
        new FloatingTextStyle { type = FloatingTextType.DamageTaken, color = new Color(1f, 0.2f, 0.2f), fontSize = 26f, duration = 1.0f, floatDirection = -1f },
        new FloatingTextStyle { type = FloatingTextType.CriticalHit, color = new Color(1f, 0.8f, 0f), fontSize = 36f, duration = 1.4f, shake = true, scalePunch = true, prefix = "CRIT! " },
        new FloatingTextStyle { type = FloatingTextType.DoTTick, color = new Color(1f, 0.5f, 0f), fontSize = 22f, duration = 1.0f, prefix = "" },
        
        // Healing & Shields
        new FloatingTextStyle { type = FloatingTextType.Heal, color = new Color(0.3f, 1f, 0.3f), fontSize = 26f, duration = 1.2f, prefix = "+" },
        new FloatingTextStyle { type = FloatingTextType.ShieldGain, color = new Color(0.3f, 0.7f, 1f), fontSize = 24f, duration = 1.2f, prefix = "+", suffix = " Shield" },
        new FloatingTextStyle { type = FloatingTextType.ShieldAbsorb, color = new Color(0.5f, 0.8f, 1f), fontSize = 22f, duration = 1.0f, prefix = "-", suffix = " Shield" },
        new FloatingTextStyle { type = FloatingTextType.ShieldBroken, color = new Color(0.6f, 0.6f, 1f), fontSize = 24f, duration = 1.4f, shake = true },
        
        // Status Effects
        new FloatingTextStyle { type = FloatingTextType.StatusGain, color = new Color(1f, 0.6f, 0f), fontSize = 22f, duration = 1.2f },
        new FloatingTextStyle { type = FloatingTextType.StatusLost, color = new Color(0.7f, 0.7f, 0.7f), fontSize = 20f, duration = 1.0f },
        new FloatingTextStyle { type = FloatingTextType.StatusStack, color = new Color(1f, 0.7f, 0.3f), fontSize = 20f, duration = 1.0f },
        new FloatingTextStyle { type = FloatingTextType.Immune, color = new Color(0.8f, 0.8f, 0.8f), fontSize = 22f, duration = 1.0f },
        new FloatingTextStyle { type = FloatingTextType.Resisted, color = new Color(0.6f, 0.6f, 0.6f), fontSize = 20f, duration = 1.0f },
        
        // Elemental
        new FloatingTextStyle { type = FloatingTextType.OrbActivated, color = new Color(1f, 0.9f, 0.5f), fontSize = 24f, duration = 1.4f, scalePunch = true },
        new FloatingTextStyle { type = FloatingTextType.ReactionTriggered, color = new Color(1f, 0.5f, 1f), fontSize = 28f, duration = 1.5f, scalePunch = true, shake = true },
        new FloatingTextStyle { type = FloatingTextType.ReactionDamage, color = new Color(1f, 0.6f, 0.8f), fontSize = 24f, duration = 1.2f },
        
        // Turn Feedback
        new FloatingTextStyle { type = FloatingTextType.TurnSkipped, color = new Color(1f, 1f, 0.3f), fontSize = 22f, duration = 1.4f },
        new FloatingTextStyle { type = FloatingTextType.EnergyChange, color = new Color(0.5f, 0.8f, 1f), fontSize = 18f, duration = 0.8f, enabled = false },
        
        // Generic
        new FloatingTextStyle { type = FloatingTextType.Generic, color = Color.white, fontSize = 22f, duration = 1.0f }
    };
    
    /// <summary>
    /// Get style for a specific text type.
    /// </summary>
    public FloatingTextStyle GetStyle(FloatingTextType type)
    {
        foreach (var style in styles)
        {
            if (style.type == type)
                return style;
        }
        // Fallback to generic
        return styles[styles.Length - 1];
    }
    
    /// <summary>
    /// Check if a text type is enabled.
    /// </summary>
    public bool IsEnabled(FloatingTextType type)
    {
        var style = GetStyle(type);
        return style != null && style.enabled;
    }
}
