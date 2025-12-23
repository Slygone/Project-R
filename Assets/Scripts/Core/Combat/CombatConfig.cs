public static class CombatConfig
{
    // Damage variance applied per hit to base damage before other multipliers
    public const float VARIANCE_MIN = 0.90f;
    public const float VARIANCE_MAX = 1.10f;

    // Helper to get variance percent string, e.g., "±10%"
    public static string GetVariancePercentLabel()
    {
        float pct = (VARIANCE_MAX - 1f) * 100f; // assumes symmetric min/max around 1.0
        return $"±{UnityEngine.Mathf.RoundToInt(pct)}%";
    }
}
