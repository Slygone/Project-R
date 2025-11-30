using UnityEngine;

namespace Core
{
    public enum GameState
    {
        FreeRoam,
        Battle,
        Dialog,
        Menu
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState CurrentState { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        [SerializeField] private Combat.BattleSystem battleSystem;

        private void Start()
        {
            SetState(GameState.FreeRoam);
        }

        public void SetState(GameState newState)
        {
            CurrentState = newState;
            Debug.Log($"Game State Changed to: {newState}");
            
            switch (newState)
            {
                case GameState.FreeRoam:
                    if (battleSystem != null) battleSystem.gameObject.SetActive(false);
                    break;
                case GameState.Battle:
                    if (battleSystem != null)
                    {
                        battleSystem.gameObject.SetActive(true);
                        battleSystem.StartBattle();
                    }
                    break;
            }
        }

        public void StartBattle()
        {
            Debug.Log($"StartBattle called! BattleSystem is {(battleSystem == null ? "NULL" : "set")}");
            SetState(GameState.Battle);
        }

        public void EndBattle()
        {
            SetState(GameState.FreeRoam);
        }
    }
}
