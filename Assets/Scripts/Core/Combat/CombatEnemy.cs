public class CombatEnemy
{
    public string Name;
    public int Health;
    public int MaxHealth;
    public int Damage;
    public int ElementID;
    public int BaseResistance;
    public int BonusResistance;
    public Element Affinity;
    public bool IsBoss;

    public CombatEnemy(EnemyData data)
    {
        Name = data.Element;
        MaxHealth = data.Health;
        Health = MaxHealth;
        Damage = data.Damage;
        ElementID = data.ElementID;
        BaseResistance = data.BaseResistance;
        BonusResistance = data.BonusResistance;
        IsBoss = data.IsBoss;
        
        if (data.IsBoss)
        {
            Affinity = Element.None;
        }
        else
        {
            Affinity = GetRandomElement();
        }
    }

    private Element GetRandomElement()
    {
        var elements = new Element[] { Element.Fire, Element.Ice, Element.Water, Element.Wind, Element.Rock };
        return elements[UnityEngine.Random.Range(0, elements.Length)];
    }

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health < 0) Health = 0;
    }

    public int CalculateResistance(Element attackerAffinity)
    {
        int totalResistance = BaseResistance;
        
        if (Affinity != Element.None && attackerAffinity == Affinity)
        {
            totalResistance += BonusResistance;
        }
        
        return totalResistance;
    }

    public int ApplyResistance(int damage, Element attackerAffinity)
    {
        int resistance = CalculateResistance(attackerAffinity);
        float multiplier = 1f - (resistance / 100f);
        if (multiplier < 0f) multiplier = 0f;
        return UnityEngine.Mathf.RoundToInt(damage * multiplier);
    }

    public bool IsAlive() => Health > 0;
}
