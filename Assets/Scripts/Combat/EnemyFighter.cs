using UnityEngine;

namespace Combat
{
    public class EnemyFighter : Fighter
    {
        protected override void Awake()
        {
            base.Awake();
            
            // Initialize default enemy stats if not set
            if (stats == null)
            {
                stats = new BattleStats
                {
                    name = "Enemy",
                    maxHealth = 50,
                    currentHealth = 50,
                    attackDamage = 5
                };
            }
        }
    }
}
