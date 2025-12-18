using UnityEngine;
using TMPro;
using UnityEngine.UI;

[DefaultExecutionOrder(-10000)]
public class Referencer : MonoBehaviour
{
    public GameManager gm;
    public PlayerController playerController;
    public Player player;
    public UIManager ui;
    public CombatManager combatManager;
    public CombatUI combatUI;
    public PlayerStatsUI playerStatsUI;
    public AffinitySelectionUI affinitySelectionUI;
    public WeaponSelectionUI weaponSelectionUI;
    public TooltipUI tooltipUI;
    
    public GameObject nodePopupPanel;
    public GameObject victoryPanel;
    public TextMeshProUGUI nodeCounterText;
    public Button continueButton;
    public TextMeshProUGUI popupTitleText;

    void Awake()
    {
        gm = FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            var gmObj = new GameObject("GameManager");
            gm = gmObj.AddComponent<GameManager>();
        }


        playerController = FindFirstObjectByType<PlayerController>();
        player = FindFirstObjectByType<Player>();
        ui = FindFirstObjectByType<UIManager>();
        
        combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager == null)
        {
            var cmObj = new GameObject("CombatManager");
            combatManager = cmObj.AddComponent<CombatManager>();
        }
        
        combatUI = FindFirstObjectByType<CombatUI>();
        if (combatUI == null)
        {
            var cuiObj = new GameObject("CombatUI");
            combatUI = cuiObj.AddComponent<CombatUI>();
        }
        
        playerStatsUI = FindFirstObjectByType<PlayerStatsUI>();
        if (playerStatsUI == null)
        {
            var psuiObj = new GameObject("PlayerStatsUI");
            playerStatsUI = psuiObj.AddComponent<PlayerStatsUI>();
        }
        
        affinitySelectionUI = FindFirstObjectByType<AffinitySelectionUI>();
        if (affinitySelectionUI == null)
        {
            var asuiObj = new GameObject("AffinitySelectionUI");
            affinitySelectionUI = asuiObj.AddComponent<AffinitySelectionUI>();
        }
        
        weaponSelectionUI = FindFirstObjectByType<WeaponSelectionUI>();
        if (weaponSelectionUI == null)
        {
            var wsuiObj = new GameObject("WeaponSelectionUI");
            weaponSelectionUI = wsuiObj.AddComponent<WeaponSelectionUI>();
        }
        
        tooltipUI = FindFirstObjectByType<TooltipUI>();
        if (tooltipUI == null)
        {
            var ttObj = new GameObject("TooltipUI");
            tooltipUI = ttObj.AddComponent<TooltipUI>();
        }
        
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas not found in scene");
        }
        
        var popup = GameObject.Find("Canvas/NodePopup");
        if (popup == null) Debug.LogError("NodePopup not found at Canvas/NodePopup");
        nodePopupPanel = popup;
        
        if (nodePopupPanel != null)
        {
            var btn = nodePopupPanel.transform.Find("ContinueButton");
            if (btn == null) Debug.LogError("ContinueButton not found under Canvas/NodePopup/ContinueButton");
            continueButton = btn != null ? btn.GetComponent<Button>() : null;
            
            var title = nodePopupPanel.transform.Find("Title");
            if (title == null) Debug.LogError("Title not found under Canvas/NodePopup/Title");
            popupTitleText = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
            
            nodePopupPanel.SetActive(false);
        }
        
        var victory = GameObject.Find("Canvas/VictoryPanel");
        if (victory == null) Debug.LogError("VictoryPanel not found at Canvas/VictoryPanel");
        victoryPanel = victory;
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
        
        var counter = GameObject.Find("Canvas/NodeCounter");
        if (counter == null) Debug.LogError("NodeCounter not found at Canvas/NodeCounter");
        nodeCounterText = counter != null ? counter.GetComponent<TextMeshProUGUI>() : null;
    }
}
