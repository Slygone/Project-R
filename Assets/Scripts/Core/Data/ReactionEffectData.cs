public class ReactionEffectData
{
    public string ReactionId { get; set; }
    public int Order { get; set; }
    public string EffectType { get; set; }
    public string Target { get; set; }
    public string Value { get; set; }  // Raw string - parse as needed (can be numeric or keyword like "Burn")
    public int DurationTurns { get; set; }
    public int ChancePct { get; set; }
    public string Notes { get; set; }
    
    // Helper to parse Value as float when needed
    public float GetValueAsFloat(float defaultValue = 0f)
    {
        if (string.IsNullOrEmpty(Value)) return defaultValue;
        if (float.TryParse(Value, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out float result))
        {
            return result;
        }
        return defaultValue;
    }
    
    // Helper to parse Value as int when needed
    public int GetValueAsInt(int defaultValue = 0)
    {
        if (string.IsNullOrEmpty(Value)) return defaultValue;
        if (int.TryParse(Value, out int result))
        {
            return result;
        }
        return defaultValue;
    }
}
