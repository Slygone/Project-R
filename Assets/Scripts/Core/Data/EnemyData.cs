public class EnemyData
{
    public string Type;
    public string Element;
    public int ElementID;
    public int Health;
    public int Damage;
    public int BaseResistance;
    public int BonusResistance;
    
    public bool IsElite => Type == "Elite Enemy";
    public bool IsBoss => Type == "Boss Enemy";
    public bool IsRegular => Type == "Enemy";
}
