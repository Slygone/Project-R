using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Referencer refs;
    private int completedNodes = 0;
    // Pair selection reroll: 1 use per run
    private static bool pairRerollUsed = false;
    private const int WORLD1_NODES = 20;
    private const int WORLD2_NODES = 25;
    private bool affinityChosen = false;
    private bool elementPairChosen = false;
    private bool characterChosen = false;
    
    // Track which character is used for the current run (for ascension award)
    private CharacterData currentRunCharacter = null;
    
    private static int currentWorld = 1;
    public static int CurrentWorld => currentWorld;
    
    // XP System (Phase 1 prep)
    private static int runXP = 0;
    public static int RunXP => runXP;
    
    // Last QTE result for debug overlay
    private static string lastQTEResult = "None";
    public static string LastQTEResult => lastQTEResult;
    
    public static void AddRunXP(int amount)
    {
        runXP += amount;
        Debug.Log($"[GameManager] Gained {amount} XP. Total: {runXP}");
    }

    // Reroll API for AffinitySelectionUI
    public static bool IsPairRerollAvailable()
    {
        return !pairRerollUsed;
    }

    public static void UsePairReroll()
    {
        pairRerollUsed = true;
        Debug.Log("[GameManager] Pair reroll used for this run");
    }
    
    public static void SetLastQTEResult(string result)
    {
        lastQTEResult = result;
    }
    
    public static void ResetRunXP()
    {
        runXP = 0;
    }

    void Start()
    {
        refs = FindFirstObjectByType<Referencer>();
        currentWorld = 1;
        completedNodes = 0;
        runXP = 0;
        lastQTEResult = "None";
        pairRerollUsed = false;
        UpdateNodeCounter();
        
        // Start with Main Menu instead of jumping to character selection
        ShowMainMenu();
    }
    
    private void ShowMainMenu()
    {
        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }
        
        if (refs != null && refs.mainMenuUI != null)
        {
            refs.mainMenuUI.Show(OnNewRunClicked);
        }
        else
        {
            Debug.LogWarning("[GameManager] MainMenuUI not found, starting character selection directly");
            StartNewRunCharacterSelect();
        }
    }
    
    private void OnNewRunClicked()
    {
        StartNewRunCharacterSelect();
    }
    
    private void StartNewRunCharacterSelect()
    {
        if (refs != null && refs.newRunCharacterSelectUI != null)
        {
            refs.newRunCharacterSelectUI.Show(OnCharacterCardClicked, ShowMainMenu);
        }
        else
        {
            Debug.LogWarning("[GameManager] NewRunCharacterSelectUI not found, using legacy selection");
            StartCharacterSelection();
        }
    }
    
    private void OnCharacterCardClicked(CharacterData character)
    {
        // Show the loadout screen for this character
        if (refs != null && refs.characterLoadoutUI != null)
        {
            refs.characterLoadoutUI.Show(character, () => OnStartRunFromLoadout(character), StartNewRunCharacterSelect);
        }
        else
        {
            Debug.LogWarning("[GameManager] CharacterLoadoutUI not found, proceeding directly");
            OnStartRunFromLoadout(character);
        }
    }
    
    private void OnStartRunFromLoadout(CharacterData character)
    {
        // Store character for ascension award at end of run
        currentRunCharacter = character;
        characterChosen = true;
        
        // Apply character to player
        if (refs != null && refs.player != null)
        {
            refs.player.SelectCharacter(character);
        }
        
        Debug.Log($"[GameManager] Starting run with {character.DisplayName}");
        
        // Now show orb pair selection
        StartAffinitySelection();
    }
    
    public int GetTotalNodesForCurrentWorld() => currentWorld == 1 ? WORLD1_NODES : WORLD2_NODES;

    private void StartAffinitySelection()
    {
        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }

        if (refs != null && refs.affinitySelectionUI != null)
        {
            refs.affinitySelectionUI.ShowPairSelection(OnElementPairChosen);
        }
        else
        {
            Debug.LogError("[GameManager] AffinitySelectionUI not found");
            elementPairChosen = true;
            if (refs != null && refs.playerController != null)
            {
                refs.playerController.SetCanMove(true);
            }
        }
    }
    
    private void OnElementPairChosen(ElementPair pair)
    {
        elementPairChosen = true;
        affinityChosen = true;

        if (refs != null && refs.player != null)
        {
            refs.player.SetElementPair(pair);
        }

        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(true);
        }

        if (refs != null && refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
        {
            refs.playerStatsUI.UpdateStats();
        }

        Debug.Log($"[GameManager] Run started with {refs.player.GetCharacter().DisplayName} and {pair.DisplayName} orb pair");
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
    public bool HasElementPairBeenChosen() => elementPairChosen;
    public bool HasCharacterBeenChosen() => characterChosen;

    public void OnNodeCompleted()
    {
        completedNodes++;
        UpdateNodeCounter();

        int totalNodes = GetTotalNodesForCurrentWorld();
        if (completedNodes >= totalNodes)
        {
            StartBossFight();
        }
    }

    private void UpdateNodeCounter()
    {
        int totalNodes = GetTotalNodesForCurrentWorld();
        string worldText = currentWorld == 1 ? "World 1" : "World 2";
        if (refs != null && refs.nodeCounterText != null)
        {
            refs.nodeCounterText.text = $"{worldText}: {completedNodes}/{totalNodes}";
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
            if (currentWorld == 1)
            {
                Debug.Log("[GameManager] World 1 BOSS DEFEATED! Transitioning to World 2...");
                TransitionToWorld2();
            }
            else
            {
                Debug.Log("[GameManager] World 2 BOSS DEFEATED! RUN COMPLETE!");
                ShowRunComplete();
            }
        }
        else
        {
            Debug.Log("[GameManager] Player defeated by boss. Game Over!");
            ShowDefeat();
        }
    }
    
    private void TransitionToWorld2()
    {
        currentWorld = 2;
        completedNodes = 0;
        
        // Reset potion bonuses but keep relic/rest upgrades
        if (refs != null && refs.player != null)
        {
            refs.player.ResetForNewWorld();
        }
        
        // Clear existing nodes
        ClearAllNodes();
        
        // Spawn new nodes for World 2
        var nodeSpawner = FindFirstObjectByType<NodeSpawner>();
        if (nodeSpawner != null)
        {
            nodeSpawner.SpawnNodesForWorld(2);
        }
        
        // Reset player position
        if (refs != null && refs.playerController != null)
        {
            refs.playerController.transform.position = Vector3.zero;
            refs.playerController.SetCanMove(true);
        }
        
        UpdateNodeCounter();
        Debug.Log("[GameManager] World 2 started!");
    }
    
    private void ClearAllNodes()
    {
        var nodes = FindObjectsByType<NodeBase>(FindObjectsSortMode.None);
        foreach (var node in nodes)
        {
            Destroy(node.gameObject);
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
    
    private void ShowRunComplete()
    {
        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }
        
        // Award ascension level to the character used in this run
        string ascensionMessage = "";
        if (currentRunCharacter != null)
        {
            MetaProgressionManager.Instance.AwardAscension(
                currentRunCharacter.CharacterID,
                currentRunCharacter.DisplayName
            );
            var progress = MetaProgressionManager.Instance.GetProgress(currentRunCharacter.CharacterID);
            ascensionMessage = $"\n\n{currentRunCharacter.DisplayName} reached Ascension {progress.AscensionLevel}!";
        }
        
        var canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            var victoryPanel = new GameObject("RunCompletePanel");
            victoryPanel.transform.SetParent(canvas.transform, false);
            
            var rect = victoryPanel.AddComponent<UnityEngine.RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            var bg = victoryPanel.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0f, 0.1f, 0f, 0.95f);
            
            var textObj = new GameObject("VictoryText");
            textObj.transform.SetParent(victoryPanel.transform, false);
            var textRect = textObj.AddComponent<UnityEngine.RectTransform>();
            textRect.anchorMin = new Vector2(0.2f, 0.45f);
            textRect.anchorMax = new Vector2(0.8f, 0.75f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = $"CONGRATULATIONS!\nYou beat the run!{ascensionMessage}";
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.fontSize = 42;
            text.color = new Color(1f, 0.84f, 0f);
            
            // Restart button
            var btnObj = new GameObject("RestartButton");
            btnObj.transform.SetParent(victoryPanel.transform, false);
            var btnRect = btnObj.AddComponent<UnityEngine.RectTransform>();
            btnRect.anchorMin = new Vector2(0.35f, 0.2f);
            btnRect.anchorMax = new Vector2(0.65f, 0.3f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            
            var btnImage = btnObj.AddComponent<UnityEngine.UI.Image>();
            btnImage.color = new Color(0.2f, 0.5f, 0.2f);
            
            var btn = btnObj.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = btnImage;
            btn.onClick.AddListener(RestartGame);
            
            var btnTextObj = new GameObject("ButtonText");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            var btnTextRect = btnTextObj.AddComponent<UnityEngine.RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            var btnText = btnTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            btnText.text = "RESTART";
            btnText.alignment = TMPro.TextAlignmentOptions.Center;
            btnText.fontSize = 28;
            btnText.color = Color.white;
        }
    }
    
    private void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
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
