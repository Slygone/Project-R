using UnityEngine;
using System.Collections;
using Core;

namespace Combat
{
    public enum BattleState
    {
        Start,
        PlayerTurn,
        EnemyTurn,
        Won,
        Lost
    }

    public class BattleSystem : MonoBehaviour
    {
        public BattleState State { get; private set; }

        [Header("Fighters")]
        [SerializeField] private PlayerFighter player;

        [Header("Spawning")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform enemySpawnPoint;


        private EnemyFighter currentEnemy;

        public void StartBattle(EnemyData enemyData)
        {
            State = BattleState.Start;
            StartCoroutine(SetupBattle(enemyData));
        }

        private IEnumerator SetupBattle(EnemyData enemyData)
        {
            Debug.Log("Setting up battle...");
            
            if (enemyPrefab != null && enemySpawnPoint != null)
            {
                GameObject go = Instantiate(enemyPrefab, enemySpawnPoint.position, Quaternion.identity);
                currentEnemy = go.GetComponent<EnemyFighter>();

                currentEnemy.InitializeEnemy(enemyData);
                currentEnemy.Show();
            }
            else
            {
                Debug.LogError("Enemy Prefab or Spawn Point is missing");
                yield break;
            }

            player.UpdateHealthDisplay();
            
            Debug.Log($"Spawning {currentEnemy.Stats.name} with {currentEnemy.Stats.maxHealth} HP");
            Debug.Log($"{player.Stats.name}: {player.Stats.currentHealth}/{player.Stats.maxHealth} HP");
            yield return new WaitForSeconds(1f);

            State = BattleState.PlayerTurn;
            PlayerTurn();
        }

        private void PlayerTurn()
        {
            Debug.Log("Player's Turn! Choose an action.");
        }

        public void OnAttackButton()
        {
            if (State != BattleState.PlayerTurn) return;

            StartCoroutine(PlayerAttack());
        }

        private IEnumerator PlayerAttack()
        {
            Debug.Log($"Player attacks for {player.Stats.attackDamage} damage!");
            currentEnemy.Stats.TakeDamage(player.Stats.attackDamage);
            currentEnemy.UpdateHealthDisplay();
            Debug.Log($"Enemy HP: {currentEnemy.Stats.currentHealth}/{currentEnemy.Stats.maxHealth}");
            
            yield return new WaitForSeconds(1f);

            // Check if enemy is defeated
            if (!currentEnemy.Stats.IsAlive())
            {
                Debug.Log("Enemy defeated! You won!");
                State = BattleState.Won;
                yield return new WaitForSeconds(1f);
                
                // Destroy the spawned enemy
                Destroy(currentEnemy.gameObject);
                
                Core.GameManager.Instance.EndBattle();
                yield break;
            }

            // Enemy turn
            State = BattleState.EnemyTurn;
            StartCoroutine(EnemyTurn());
        }

        private IEnumerator EnemyTurn()
        {
            Debug.Log($"Enemy attacks for {currentEnemy.Stats.attackDamage} damage!");
            player.Stats.TakeDamage(currentEnemy.Stats.attackDamage);
            player.UpdateHealthDisplay();
            Debug.Log($"Player HP: {player.Stats.currentHealth}/{player.Stats.maxHealth}");
            
            yield return new WaitForSeconds(1f);

            // Check if player is defeated
            if (!player.Stats.IsAlive())
            {
                State = BattleState.Lost;
                Debug.Log("You were defeated! Game Over!");   
                
                Core.GameManager.Instance.EndBattle();
                yield break;
            }

            // Back to player turn
            State = BattleState.PlayerTurn;
            PlayerTurn();
        }
    }
}
