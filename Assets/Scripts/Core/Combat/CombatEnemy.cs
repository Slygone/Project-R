public class CombatEnemy
{
    public string Name;
    public int Health;
    public int MaxHealth;
    public int Damage;
    public int ElementID;

    public CombatEnemy(EnemyData data)
    {
        Name = data.Element;
        MaxHealth = data.Health;
        Health = MaxHealth;
        Damage = data.Damage;
        ElementID = data.ElementID;
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health < 0) Health = 0;
    }

    public bool IsAlive() => Health > 0;
}
