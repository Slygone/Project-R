using UnityEngine;
using UnityEngine.UI;

namespace Combat
{
    public class BattleUI : MonoBehaviour
    {
        [SerializeField] private BattleSystem battleSystem;
        [SerializeField] private Button attackButton;
        [SerializeField] private GameObject buttonPanel;

        private void Start()
        {
            if (attackButton != null)
            {
                attackButton.onClick.AddListener(OnAttackClicked);
            }
        }

        private void Update()
        {
            // Show buttons only during player turn
            if (buttonPanel != null)
            {
                buttonPanel.SetActive(battleSystem != null && 
                                     battleSystem.State == BattleState.PlayerTurn);
            }
        }

        private void OnAttackClicked()
        {
            Debug.Log("Attack button clicked!");
            if (battleSystem != null)
            {
                battleSystem.OnAttackButton();
            }
        }
    }
}
