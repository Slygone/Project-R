/// <summary>
/// Data structure for world encounter composition loaded from CSV.
/// Controls how many nodes and enemies spawn per world.
/// </summary>
public class WorldEncounterData
{
    public int World;
    public int NodeCount;
    public int CombatNodeCount;
    public int RestNodeCount;
    public int ShopNodeCount;
    public int EliteNodeCount;
    public int RegularEnemy;
    public int EliteEnemy;
    public int BossEnemy;
}
