using UnityEngine;

namespace Combat
{
    public class PlayerFighter : Fighter
    {
        protected override void Awake()
        {
            base.Awake();
            
            // Initialize default player stats if not set
            if (stats == null)
            {
                stats = new BattleStats
                {
                    name = "Player",
                    maxHealth = 100,
                    currentHealth = 100,
                    attackDamage = 10
                };
            }

            // Player health should always be visible
            if (healthText != null)
            {
                healthText.gameObject.SetActive(true);
                UpdateHealthDisplay();
            }
        }
    }
}
