using UnityEngine;
using UnityEngine.UI;

namespace Combat
{
    public class Fighter : MonoBehaviour
    {
        [SerializeField] protected BattleStats stats;
        [SerializeField] protected Text healthText;
        protected SpriteRenderer spriteRenderer;

        public BattleStats Stats => stats;

        protected virtual void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (stats != null)
            {
                stats.ResetHealth();
            }

            // Hide by default
            Hide();
        }

        public void ResetForBattle()
        {
            stats.ResetHealth();
            UpdateHealthDisplay();
        }

        public void UpdateHealthDisplay()
        {
            if (healthText != null)
            {
                healthText.text = $"{stats.currentHealth}/{stats.maxHealth}";
            }
        }

        public void Show()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
            if (healthText != null)
            {
                healthText.gameObject.SetActive(true);
            }
            gameObject.SetActive(true);
            UpdateHealthDisplay();
        }

        public void ShowHealthOnly()
        {
            if (healthText != null)
            {
                healthText.gameObject.SetActive(true);
            }
            UpdateHealthDisplay();
        }

        public void Hide()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
            if (healthText != null)
            {
                healthText.gameObject.SetActive(false);
            }
        }
    }
}
