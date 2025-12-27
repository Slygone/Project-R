/// <summary>
/// Data structure for world encounter composition loaded from CSV.
/// Controls how many nodes and enemies spawn per world.
/// </summary>
public class WorldEncounterData
{
    public int World;
    public int NodeCount;
    public int RegularEnemyMin;
    public int RegularEnemyMax;
    public int EliteEnemyMin;
    public int EliteEnemyMax;
    public int BossEnemyMin;
    public int BossEnemyMax;
    
    public int GetRegularEnemyCount() => UnityEngine.Random.Range(RegularEnemyMin, RegularEnemyMax + 1);
    public int GetEliteEnemyCount() => UnityEngine.Random.Range(EliteEnemyMin, EliteEnemyMax + 1);
    public int GetBossEnemyCount() => UnityEngine.Random.Range(BossEnemyMin, BossEnemyMax + 1);
}
