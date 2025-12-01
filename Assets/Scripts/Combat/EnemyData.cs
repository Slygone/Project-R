using UnityEngine;

namespace Combat
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Combat/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        public string enemyName;
        public int maxHealth;
        public int attackDamage;
        public Sprite enemySprite;
    }
}
