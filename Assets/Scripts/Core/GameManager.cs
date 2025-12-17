using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Referencer refs;
    private int completedNodes = 0;
    private const int TOTAL_NODES = 10;

    void Start()
    {
        refs = FindFirstObjectByType<Referencer>();
        UpdateNodeCounter();
    }

    public void OnNodeCompleted()
    {
        completedNodes++;
        UpdateNodeCounter();

        if (completedNodes >= TOTAL_NODES)
        {
            ShowVictory();
        }
    }

    private void UpdateNodeCounter()
    {
        if (refs != null && refs.nodeCounterText != null)
        {
            refs.nodeCounterText.text = $"Nodes Completed: {completedNodes}/{TOTAL_NODES}";
        }
    }

    private void ShowVictory()
    {
        if (refs != null && refs.victoryPanel != null)
        {
            refs.victoryPanel.SetActive(true);
        }

        if (refs != null && refs.player != null)
        {
            refs.player.SetCanMove(false);
        }
    }
}
