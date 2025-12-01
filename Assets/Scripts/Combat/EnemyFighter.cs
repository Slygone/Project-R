using UnityEngine;

namespace Combat
{
    public class EnemyFighter : Fighter
    {
        public void InitializeEnemy(EnemyData enemyData)
        {
            if(enemyData == null)
            {
                Debug.LogError("EnemyData is null");
                return;
            }    

            stats = new BattleStats
            {
                name = enemyData.enemyName,
                maxHealth = enemyData.maxHealth,
                currentHealth = enemyData.maxHealth,
                attackDamage = enemyData.attackDamage
            };

            if(enemyData.enemySprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = enemyData.enemySprite;
            }

            UpdateHealthDisplay();
        }        
    }
}
