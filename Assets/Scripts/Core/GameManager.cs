using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Referencer refs;
    private int completedNodes = 0;
    private const int TOTAL_NODES = 10;
    private bool affinityChosen = false;
    private bool characterChosen = false;

    void Start()
    {
        refs = FindFirstObjectByType<Referencer>();
        UpdateNodeCounter();
        
        StartCharacterSelection();
    }

    private void StartAffinitySelection()
    {
        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }

        if (refs != null && refs.affinitySelectionUI != null)
        {
            refs.affinitySelectionUI.Show(OnAffinityChosen);
        }
        else
        {
            Debug.LogError("[GameManager] AffinitySelectionUI not found");
            affinityChosen = true;
            if (refs != null && refs.playerController != null)
            {
                refs.playerController.SetCanMove(true);
            }
        }
    }

    private void StartCharacterSelection()
    {
        if (refs != null && refs.characterSelectionUI != null)
        {
            refs.characterSelectionUI.Show(OnCharacterChosen);
        }
        else
        {
            Debug.LogError("[GameManager] CharacterSelectionUI not found");
            characterChosen = true;
            StartAffinitySelection();
        }
    }

    private void OnCharacterChosen(CharacterData character)
    {
        characterChosen = true;

        if (refs != null && refs.player != null)
        {
            refs.player.SelectCharacter(character);
        }

        Debug.Log($"[GameManager] Character chosen: {character.DisplayName}");
        
        StartAffinitySelection();
    }

    private void OnAffinityChosen(Element element)
    {
        affinityChosen = true;

        if (refs != null && refs.player != null)
        {
            refs.player.SetAffinity(element);
        }

        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(true);
        }

        if (refs != null && refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
        {
            refs.playerStatsUI.UpdateStats();
        }

        Debug.Log($"[GameManager] Run started with {refs.player.GetCharacter().DisplayName} and {element} affinity");
    }

    public bool HasAffinityBeenChosen() => affinityChosen;
    public bool HasCharacterBeenChosen() => characterChosen;

    public void OnNodeCompleted()
    {
        completedNodes++;
        UpdateNodeCounter();

        if (completedNodes >= TOTAL_NODES)
        {
            StartBossFight();
        }
    }

    private void UpdateNodeCounter()
    {
        if (refs != null && refs.nodeCounterText != null)
        {
            refs.nodeCounterText.text = $"Nodes Completed: {completedNodes}/{TOTAL_NODES}";
        }
    }

    private void StartBossFight()
    {
        Debug.Log("[GameManager] All nodes completed! Starting BOSS FIGHT!");
        
        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }

        if (refs != null && refs.combatManager != null && refs.player != null)
        {
            refs.combatManager.StartBossCombat(refs.player, OnBossFightComplete);
        }
        else
        {
            Debug.LogError("[GameManager] CombatManager or Player not found for boss fight");
            ShowVictory();
        }
    }

    private void OnBossFightComplete(bool victory)
    {
        if (victory)
        {
            Debug.Log("[GameManager] BOSS DEFEATED! Player wins the level!");
            ShowVictory();
        }
        else
        {
            Debug.Log("[GameManager] Player defeated by boss. Game Over!");
            ShowDefeat();
        }
    }

    private void ShowVictory()
    {
        if (refs != null && refs.victoryPanel != null)
        {
            refs.victoryPanel.SetActive(true);
        }

        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }
    }

    private void ShowDefeat()
    {
        Debug.Log("[GameManager] Showing defeat screen");
        
        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }
        
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var defeatPanel = new GameObject("DefeatPanel");
            defeatPanel.transform.SetParent(canvas.transform, false);
            
            var rect = defeatPanel.AddComponent<UnityEngine.RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var bg = defeatPanel.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.1f, 0f, 0f, 0.95f);
            
            var textObj = new GameObject("DefeatText");
            textObj.transform.SetParent(defeatPanel.transform, false);
            var textRect = textObj.AddComponent<UnityEngine.RectTransform>();
            textRect.anchorMin = new Vector2(0.2f, 0.4f);
            textRect.anchorMax = new Vector2(0.8f, 0.6f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = "DEFEATED\nThe boss was too powerful...";
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.fontSize = 48;
            text.color = Color.red;
        }
    }
}
