using UnityEngine;
using Core;

namespace Interaction
{
    public class InteractionTrigger : MonoBehaviour
    {
        [SerializeField] private GameObject choiceMenu;
        [SerializeField] private GameObject restChoiceMenu;
        
        [Header("Combat Settings")]
        [SerializeField] private System.Collections.Generic.List<Combat.EnemyData> possibleEnemies;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                ShowChoiceMenu();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                HideChoiceMenu();
                HideRestChoiceMenu();
            }
        }

        private void ShowChoiceMenu()
        {
            if (choiceMenu != null && GameManager.Instance.CurrentState == GameState.FreeRoam)
            {
                choiceMenu.SetActive(true);
                GameManager.Instance.SetState(GameState.Menu);
                Debug.Log("Choice menu opened!");
            }
        }

        private void HideChoiceMenu()
        {
            if (choiceMenu != null)
            {
                choiceMenu.SetActive(false);
                GameManager.Instance.SetState(GameState.FreeRoam);
            }
        }

        private void ShowRestChoiceMenu()
        {
            if (restChoiceMenu != null)
            {
                choiceMenu.SetActive(false);
                restChoiceMenu.SetActive(true);
            }
        }

        private void HideRestChoiceMenu()
        {
            if (restChoiceMenu != null)
            {
                restChoiceMenu.SetActive(false);
            }
        }

        public void OnFightChoice()
        {
            Debug.Log("Fight chosen!");
            HideChoiceMenu();
            
            if (possibleEnemies != null && possibleEnemies.Count > 0)
            {
                // Pick a random enemy from the list
                int randomIndex = Random.Range(0, possibleEnemies.Count);
                Combat.EnemyData selectedEnemy = possibleEnemies[randomIndex];
                
                Debug.Log($"Encounter started! Fighting: {selectedEnemy.enemyName}");
                GameManager.Instance.StartBattle(selectedEnemy);
            }
            else
            {
                Debug.LogError("No enemies assigned to this InteractionTrigger!");
            }
        }

        public void OnShopChoice()
        {
            Debug.Log("Shop chosen! (Not implemented yet)");
            HideChoiceMenu();
            // TODO: Open shop UI
        }

        public void OnRestChoice()
        {
            Debug.Log("Rest chosen! Choose your upgrade:");
            ShowRestChoiceMenu();
        }

        public void OnHealChoice()
        {
            var playerFighter = FindFirstObjectByType<Combat.PlayerFighter>();
            if (playerFighter != null)
            {
                // Heal +30 HP (can't exceed max)
                playerFighter.Stats.currentHealth += 30;
                if (playerFighter.Stats.currentHealth > playerFighter.Stats.maxHealth)
                {
                    playerFighter.Stats.currentHealth = playerFighter.Stats.maxHealth;
                }
                playerFighter.UpdateHealthDisplay();
                Debug.Log($"Player healed +30 HP! Now at {playerFighter.Stats.currentHealth}/{playerFighter.Stats.maxHealth}");
            }
            HideRestChoiceMenu();
            HideChoiceMenu();
        }

        public void OnBoostAttackChoice()
        {
            var playerFighter = FindFirstObjectByType<Combat.PlayerFighter>();
            if (playerFighter != null)
            {
                // Increase attack damage by 10
                playerFighter.Stats.attackDamage += 10;
                Debug.Log($"Player attack increased! Now at {playerFighter.Stats.attackDamage} damage");
            }
            HideRestChoiceMenu();
            HideChoiceMenu();
        }

        public void OnBackFromRest()
        {
            HideRestChoiceMenu();
            ShowChoiceMenu();
        }
    }
}
