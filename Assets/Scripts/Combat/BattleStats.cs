using UnityEngine;

namespace Combat
{
    [System.Serializable]
    public class BattleStats
    {
        public string name;
        public int maxHealth;
        public int currentHealth;
        public int attackDamage;

        public void ResetHealth()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(int damage)
        {
            currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;
        }

        public bool IsAlive()
        {
            return currentHealth > 0;
        }
    }
}
