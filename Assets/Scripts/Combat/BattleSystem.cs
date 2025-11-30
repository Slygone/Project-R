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
        [SerializeField] private EnemyFighter enemy;

        public void StartBattle()
        {
            State = BattleState.Start;
            StartCoroutine(SetupBattle());
        }

        private IEnumerator SetupBattle()
        {
            Debug.Log("Setting up battle...");
            
            if (player == null || enemy == null)
            {
                Debug.LogError("Player or Enemy fighter not assigned!");
                yield break;
            }

            // Reset health for new battle
            // Player health carries over - only update display
            player.UpdateHealthDisplay();
            // Enemy health resets each battle
            enemy.ResetForBattle();

            // Show enemy sprite and health
            enemy.Show();
            
            Debug.Log($"{player.Stats.name}: {player.Stats.currentHealth}/{player.Stats.maxHealth} HP");
            Debug.Log($"{enemy.Stats.name}: {enemy.Stats.currentHealth}/{enemy.Stats.maxHealth} HP");
            
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
            enemy.Stats.TakeDamage(player.Stats.attackDamage);
            enemy.UpdateHealthDisplay();
            Debug.Log($"Enemy HP: {enemy.Stats.currentHealth}/{enemy.Stats.maxHealth}");
            
            yield return new WaitForSeconds(1f);

            // Check if enemy is defeated
            if (!enemy.Stats.IsAlive())
            {
                Debug.Log("Enemy defeated! You won!");
                State = BattleState.Won;
                yield return new WaitForSeconds(1f);
                
                // Hide only enemy
                enemy.Hide();
                
                Core.GameManager.Instance.EndBattle();
                yield break;
            }

            // Enemy turn
            State = BattleState.EnemyTurn;
            StartCoroutine(EnemyTurn());
        }

        private IEnumerator EnemyTurn()
        {
            Debug.Log($"Enemy attacks for {enemy.Stats.attackDamage} damage!");
            player.Stats.TakeDamage(enemy.Stats.attackDamage);
            player.UpdateHealthDisplay();
            Debug.Log($"Player HP: {player.Stats.currentHealth}/{player.Stats.maxHealth}");
            
            yield return new WaitForSeconds(1f);

            // Check if player is defeated
            if (!player.Stats.IsAlive())
            {
                Debug.Log("You were defeated! Game Over!");
                State = BattleState.Lost;
                yield return new WaitForSeconds(1f);
                
                // Hide only enemy
                enemy.Hide();
                
                Core.GameManager.Instance.EndBattle();
                yield break;
            }

            // Back to player turn
            State = BattleState.PlayerTurn;
            PlayerTurn();
        }
    }
}
