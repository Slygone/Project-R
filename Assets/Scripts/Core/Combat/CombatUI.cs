using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CombatUI : MonoBehaviour
{
    private GameObject combatPanel;
    private GameObject lootPanel;
    private GameObject playerHealthBar;
    private TextMeshProUGUI playerHealthText;
    private Image playerHealthFill;
    private Button skill1Button;
    private Button skill2Button;
    private Button skill3Button;
    private Button skill4Button;
    private Button skill5Button;
    private TextMeshProUGUI skill1Text;
    private TextMeshProUGUI skill2Text;
    private TextMeshProUGUI skill3Text;
    private TextMeshProUGUI skill4Text;
    private TextMeshProUGUI skill5Text;
    private TooltipTrigger skill1Tooltip;
    private TooltipTrigger skill2Tooltip;
    private TooltipTrigger skill3Tooltip;
    private TooltipTrigger skill4Tooltip;
    private TooltipTrigger skill5Tooltip;
    private Transform enemyContainer;
    private Dictionary<CombatEnemy, EnemyUISlot> enemySlots = new Dictionary<CombatEnemy, EnemyUISlot>();
    private CombatManager combatManager;
    
    private TextMeshProUGUI lootGoldText;
    private TextMeshProUGUI lootXPText;
    private TextMeshProUGUI lootTitleText;
    private Button collectLootButton;
    private int pendingGold;
    private int pendingXP;
    private Player pendingPlayer;
    private NodeBase pendingNode;
    private TextMeshProUGUI combatTitleText;
    
    private bool isTargeting = false;
    private int selectedSkillNumber = 0;
    private int selectedTargetIndex = 0;
    private bool isAoESkill = false;
    private List<CombatEnemy> currentEnemies = new List<CombatEnemy>();
    private Button backButton;
    private GameObject actionButtonContainer;
    
    private GameObject potionContainer;
    private List<Button> potionButtons = new List<Button>();
    private List<TextMeshProUGUI> potionTexts = new List<TextMeshProUGUI>();
    private int selectedPotionIndex = -1;
    private Element selectedPotionElement = Element.None;
    private bool isPotionTargeting = false;
    
    private GameObject potionTooltipPanel;
    private TextMeshProUGUI potionTooltipText;
    
    // New UI components for Slay the Spire style layout
    private TextMeshProUGUI playerNameText;
    private TextMeshProUGUI playerGoldText;
    private TextMeshProUGUI floorText;
    private TextMeshProUGUI floorSubtitleText;
    private TextMeshProUGUI energyText;
    private GameObject energyCircle;
    private Button endTurnButton;
    private GameObject topBar;
    private GameObject bottomBar;
    private GameObject relicsPanel;
    private GameObject relicsContainer;
    private Button relicScrollLeftButton;
    private Button relicScrollRightButton;
    private List<GameObject> relicIcons = new List<GameObject>();
    private int relicScrollIndex = 0;
    private const int MAX_VISIBLE_RELICS = 5;
    private Player currentPlayer;

    void Awake()
    {
        combatManager = FindFirstObjectByType<CombatManager>();
        SetupUI();
    }

    void Update()
    {
        if (!isTargeting) return;
        
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            ExitTargetingMode();
            return;
        }
        
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            CycleTarget(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            CycleTarget(1);
        }
        
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ConfirmTarget();
        }
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            GameLog.Error(
                GameLogCategory.System,
                "[CombatUI]",
                GameLog.Join(
                    "SetupFail",
                    GameLog.KV("reason", "CanvasNotFound")
                )
            );
            return;
        }

        combatPanel = CreateCombatPanel(canvas.transform);
        combatPanel.SetActive(false);
        
        lootPanel = CreateLootPanel(canvas.transform);
        lootPanel.SetActive(false);
    }

    private GameObject CreateCombatPanel(Transform parent)
    {
        var panel = new GameObject("CombatPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Fully opaque dark background to hide free-roam elements during combat
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 1f);

        // Create UI sections
        CreateTopBar(panel.transform);
        CreatePotionContainer(panel.transform);
        CreateRelicsPanel(panel.transform);
        CreateEnemyContainer(panel.transform);
        CreateBottomActionBar(panel.transform);
        CreatePlayerHealthBar(panel.transform);

        return panel;
    }
    
    private void CreateTopBar(Transform parent)
    {
        // Top bar container
        topBar = new GameObject("TopBar");
        topBar.transform.SetParent(parent, false);
        var topBarRect = topBar.AddComponent<RectTransform>();
        topBarRect.anchorMin = new Vector2(0, 0.92f);
        topBarRect.anchorMax = new Vector2(1, 1);
        topBarRect.offsetMin = new Vector2(15, 0);
        topBarRect.offsetMax = new Vector2(-15, -8);

        // Left section: Player info
        var leftSection = new GameObject("LeftSection");
        leftSection.transform.SetParent(topBar.transform, false);
        var leftRect = leftSection.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.35f, 1);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;

        var leftLayout = leftSection.AddComponent<HorizontalLayoutGroup>();
        leftLayout.spacing = 20;
        leftLayout.childAlignment = TextAnchor.MiddleLeft;
        leftLayout.childControlWidth = false;
        leftLayout.childControlHeight = true;
        leftLayout.childForceExpandWidth = false;
        leftLayout.padding = new RectOffset(10, 0, 0, 0);

        // Player name
        var nameObj = new GameObject("PlayerName");
        nameObj.transform.SetParent(leftSection.transform, false);
        playerNameText = nameObj.AddComponent<TextMeshProUGUI>();
        playerNameText.text = "THE WARRIOR";
        playerNameText.fontSize = 22;
        playerNameText.fontStyle = FontStyles.Bold;
        playerNameText.color = new Color(0.9f, 0.85f, 0.7f);
        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredWidth = 140;

        // HP display (heart icon + HP)
        var hpObj = new GameObject("HPDisplay");
        hpObj.transform.SetParent(leftSection.transform, false);
        var hpText = hpObj.AddComponent<TextMeshProUGUI>();
        hpText.text = "<color=#cc3333>♥</color> 72/80";
        hpText.fontSize = 18;
        hpText.color = Color.white;
        playerHealthText = hpText;
        var hpLayout = hpObj.AddComponent<LayoutElement>();
        hpLayout.preferredWidth = 90;

        // Gold display
        var goldObj = new GameObject("GoldDisplay");
        goldObj.transform.SetParent(leftSection.transform, false);
        playerGoldText = goldObj.AddComponent<TextMeshProUGUI>();
        playerGoldText.text = "<color=#ffcc00>G</color> 100";
        playerGoldText.fontSize = 18;
        playerGoldText.color = Color.white;
        var goldLayout = goldObj.AddComponent<LayoutElement>();
        goldLayout.preferredWidth = 80;


        // Center section: Floor indicator
        var centerSection = new GameObject("CenterSection");
        centerSection.transform.SetParent(topBar.transform, false);
        var centerRect = centerSection.AddComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.35f, 0);
        centerRect.anchorMax = new Vector2(0.65f, 1);
        centerRect.offsetMin = Vector2.zero;
        centerRect.offsetMax = Vector2.zero;

        var centerLayout = centerSection.AddComponent<VerticalLayoutGroup>();
        centerLayout.childAlignment = TextAnchor.MiddleCenter;
        centerLayout.childControlWidth = true;
        centerLayout.childControlHeight = true;
        centerLayout.spacing = -2;

        var floorObj = new GameObject("FloorText");
        floorObj.transform.SetParent(centerSection.transform, false);
        floorText = floorObj.AddComponent<TextMeshProUGUI>();
        floorText.text = "FLOOR 1";
        floorText.fontSize = 24;
        floorText.fontStyle = FontStyles.Bold;
        floorText.alignment = TextAlignmentOptions.Center;
        floorText.color = Color.white;

        var subtitleObj = new GameObject("FloorSubtitle");
        subtitleObj.transform.SetParent(centerSection.transform, false);
        floorSubtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        floorSubtitleText.text = "";
        floorSubtitleText.fontSize = 12;
        floorSubtitleText.alignment = TextAlignmentOptions.Center;
        floorSubtitleText.color = new Color(0.7f, 0.7f, 0.7f);
        
        // Keep combatTitleText reference for special titles (Boss Fight, etc.)
        combatTitleText = floorText;

    }
    
    private void CreateRelicsPanel(Transform parent)
    {
        // Main relics panel - positioned in top-right corner below top bar
        relicsPanel = new GameObject("RelicsPanel");
        relicsPanel.transform.SetParent(parent, false);
        var panelRect = relicsPanel.AddComponent<RectTransform>();
        // Position: top-right area, with padding from edges
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        // Total width: leftArrow(24) + relics(5*40 + 4*4 spacing) + rightArrow(24) + padding = 280
        // Height: 50 for relics + padding
        panelRect.sizeDelta = new Vector2(340, 60);
        panelRect.anchoredPosition = new Vector2(-15, -45); // 15px from right, 45px from top
        
        // Visible background with border effect
        var panelBg = relicsPanel.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
        
        // Add outline for border effect
        var outline = relicsPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.4f, 0.5f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);
        
        // Left scroll button - fixed position on left side
        var leftBtnObj = new GameObject("RelicScrollLeft");
        leftBtnObj.transform.SetParent(relicsPanel.transform, false);
        var leftBtnRect = leftBtnObj.AddComponent<RectTransform>();
        leftBtnRect.anchorMin = new Vector2(0, 0);
        leftBtnRect.anchorMax = new Vector2(0, 1);
        leftBtnRect.pivot = new Vector2(0, 0.5f);
        leftBtnRect.sizeDelta = new Vector2(24, 0);
        leftBtnRect.anchoredPosition = new Vector2(4, 0);
        var leftBtnImage = leftBtnObj.AddComponent<Image>();
        leftBtnImage.color = new Color(0.25f, 0.25f, 0.3f, 1f);
        relicScrollLeftButton = leftBtnObj.AddComponent<Button>();
        relicScrollLeftButton.targetGraphic = leftBtnImage;
        relicScrollLeftButton.onClick.AddListener(OnRelicScrollLeft);
        var leftText = new GameObject("Text");
        leftText.transform.SetParent(leftBtnObj.transform, false);
        var leftTextRect = leftText.AddComponent<RectTransform>();
        leftTextRect.anchorMin = Vector2.zero;
        leftTextRect.anchorMax = Vector2.one;
        leftTextRect.offsetMin = Vector2.zero;
        leftTextRect.offsetMax = Vector2.zero;
        var leftTmp = leftText.AddComponent<TextMeshProUGUI>();
        leftTmp.text = "<";
        leftTmp.fontSize = 20;
        leftTmp.fontStyle = FontStyles.Bold;
        leftTmp.alignment = TextAlignmentOptions.Center;
        leftTmp.color = Color.white;
        leftBtnObj.SetActive(false);
        
        // Relic icons container - centered between arrows
        relicsContainer = new GameObject("RelicsContainer");
        relicsContainer.transform.SetParent(relicsPanel.transform, false);
        var containerRect = relicsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.offsetMin = new Vector2(32, 5); // Left padding for arrow
        containerRect.offsetMax = new Vector2(-32, -5); // Right padding for arrow
        
        // Inner container background (slightly lighter to show relic area)
        var containerBg = relicsContainer.AddComponent<Image>();
        containerBg.color = new Color(0.08f, 0.08f, 0.12f, 0.6f);
        
        var containerLayout = relicsContainer.AddComponent<HorizontalLayoutGroup>();
        containerLayout.spacing = 4;
        containerLayout.childAlignment = TextAnchor.MiddleCenter;
        containerLayout.childControlWidth = true;
        containerLayout.childControlHeight = true;
        containerLayout.childForceExpandWidth = false;
        containerLayout.childForceExpandHeight = false;
        containerLayout.padding = new RectOffset(4, 4, 2, 2);
        
        // Right scroll button - fixed position on right side
        var rightBtnObj = new GameObject("RelicScrollRight");
        rightBtnObj.transform.SetParent(relicsPanel.transform, false);
        var rightBtnRect = rightBtnObj.AddComponent<RectTransform>();
        rightBtnRect.anchorMin = new Vector2(1, 0);
        rightBtnRect.anchorMax = new Vector2(1, 1);
        rightBtnRect.pivot = new Vector2(1, 0.5f);
        rightBtnRect.sizeDelta = new Vector2(24, 0);
        rightBtnRect.anchoredPosition = new Vector2(-4, 0);
        var rightBtnImage = rightBtnObj.AddComponent<Image>();
        rightBtnImage.color = new Color(0.25f, 0.25f, 0.3f, 1f);
        relicScrollRightButton = rightBtnObj.AddComponent<Button>();
        relicScrollRightButton.targetGraphic = rightBtnImage;
        relicScrollRightButton.onClick.AddListener(OnRelicScrollRight);
        var rightText = new GameObject("Text");
        rightText.transform.SetParent(rightBtnObj.transform, false);
        var rightTextRect = rightText.AddComponent<RectTransform>();
        rightTextRect.anchorMin = Vector2.zero;
        rightTextRect.anchorMax = Vector2.one;
        rightTextRect.offsetMin = Vector2.zero;
        rightTextRect.offsetMax = Vector2.zero;
        var rightTmp = rightText.AddComponent<TextMeshProUGUI>();
        rightTmp.text = ">";
        rightTmp.fontSize = 20;
        rightTmp.fontStyle = FontStyles.Bold;
        rightTmp.alignment = TextAlignmentOptions.Center;
        rightTmp.color = Color.white;
        rightBtnObj.SetActive(false);
    }

    private void CreatePlayerHealthBar(Transform parent)
    {
        // HP is now displayed in the top bar, but we still need a reference for floating text
        var container = new GameObject("PlayerHealthRef");
        container.transform.SetParent(parent, false);

        var rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.02f, 0.92f);
        rect.anchorMax = new Vector2(0.15f, 0.98f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Hidden fill for compatibility
        playerHealthFill = container.AddComponent<Image>();
        playerHealthFill.color = Color.clear;

        playerHealthBar = container;
    }

    private void CreateEnemyContainer(Transform parent)
    {
        var container = new GameObject("EnemyContainer");
        container.transform.SetParent(parent, false);

        var rect = container.AddComponent<RectTransform>();
        // Positioned in center area, leaving room for top bar and bottom action bar
        rect.anchorMin = new Vector2(0.1f, 0.25f);
        rect.anchorMax = new Vector2(0.9f, 0.88f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        enemyContainer = container.transform;
    }

    private void CreateBottomActionBar(Transform parent)
    {
        // Bottom bar container with rounded dark background
        bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(parent, false);
        var bottomRect = bottomBar.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0.05f, 0.02f);
        bottomRect.anchorMax = new Vector2(0.95f, 0.20f);
        bottomRect.offsetMin = Vector2.zero;
        bottomRect.offsetMax = Vector2.zero;

        var bottomBg = bottomBar.AddComponent<Image>();
        bottomBg.color = new Color(0.12f, 0.10f, 0.08f, 0.95f);

        // Energy Circle (left side)
        CreateEnergyCircle(bottomBar.transform);

        // Skill Cards Container (center)
        var skillsContainer = new GameObject("SkillsContainer");
        skillsContainer.transform.SetParent(bottomBar.transform, false);
        actionButtonContainer = skillsContainer;
        var skillsRect = skillsContainer.AddComponent<RectTransform>();
        skillsRect.anchorMin = new Vector2(0.12f, 0.1f);
        skillsRect.anchorMax = new Vector2(0.78f, 0.9f);
        skillsRect.offsetMin = Vector2.zero;
        skillsRect.offsetMax = Vector2.zero;

        // Dark rounded background for skill cards
        var skillsBg = skillsContainer.AddComponent<Image>();
        skillsBg.color = new Color(0.08f, 0.07f, 0.06f, 0.9f);

        var skillsLayout = skillsContainer.AddComponent<HorizontalLayoutGroup>();
        skillsLayout.spacing = 8;
        skillsLayout.padding = new RectOffset(10, 10, 8, 8);
        skillsLayout.childAlignment = TextAnchor.MiddleCenter;
        skillsLayout.childControlWidth = true;
        skillsLayout.childControlHeight = true;
        skillsLayout.childForceExpandWidth = true;
        skillsLayout.childForceExpandHeight = true;

        // Create skill cards with hotkey indicators
        skill1Button = CreateSkillCard(skillsContainer.transform, "Skill1", "1", new Color(0.6f, 0.2f, 0.2f, 1f), OnSkill1Clicked, out skill1Text, out skill1Tooltip);
        skill2Button = CreateSkillCard(skillsContainer.transform, "Skill2", "2", new Color(0.2f, 0.4f, 0.7f, 1f), OnSkill2Clicked, out skill2Text, out skill2Tooltip);
        skill3Button = CreateSkillCard(skillsContainer.transform, "Skill3", "3", new Color(0.5f, 0.4f, 0.3f, 1f), OnSkill3Clicked, out skill3Text, out skill3Tooltip);
        skill4Button = CreateSkillCard(skillsContainer.transform, "Skill4", "Q", new Color(0.3f, 0.3f, 0.4f, 1f), OnSkill4Clicked, out skill4Text, out skill4Tooltip);
        skill5Button = CreateSkillCard(skillsContainer.transform, "Skill5", "R", new Color(0.7f, 0.5f, 0.2f, 1f), OnSkill5Clicked, out skill5Text, out skill5Tooltip);

        // Back button (hidden by default, shown during targeting)
        backButton = CreateSkillCard(skillsContainer.transform, "Back", "ESC", new Color(0.4f, 0.4f, 0.4f, 1f), OnBackClicked, out _, out _);
        backButton.gameObject.SetActive(false);

        // End Turn Button (right side)
        CreateEndTurnButton(bottomBar.transform);
    }

    private void CreateEnergyCircle(Transform parent)
    {
        energyCircle = new GameObject("EnergyCircle");
        energyCircle.transform.SetParent(parent, false);
        var circleRect = energyCircle.AddComponent<RectTransform>();
        circleRect.anchorMin = new Vector2(0.01f, 0.15f);
        circleRect.anchorMax = new Vector2(0.11f, 0.85f);
        circleRect.offsetMin = Vector2.zero;
        circleRect.offsetMax = Vector2.zero;

        // Background circle
        var circleBg = energyCircle.AddComponent<Image>();
        circleBg.color = new Color(0.15f, 0.25f, 0.4f, 1f);

        // Energy number
        var energyNumObj = new GameObject("EnergyNumber");
        energyNumObj.transform.SetParent(energyCircle.transform, false);
        var energyNumRect = energyNumObj.AddComponent<RectTransform>();
        energyNumRect.anchorMin = new Vector2(0, 0.3f);
        energyNumRect.anchorMax = new Vector2(1, 0.9f);
        energyNumRect.offsetMin = Vector2.zero;
        energyNumRect.offsetMax = Vector2.zero;

        energyText = energyNumObj.AddComponent<TextMeshProUGUI>();
        energyText.text = "0";
        energyText.fontSize = 36;
        energyText.fontStyle = FontStyles.Bold;
        energyText.alignment = TextAlignmentOptions.Center;
        energyText.color = new Color(0.4f, 0.7f, 1f);

        // "ENERGY" label
        var labelObj = new GameObject("EnergyLabel");
        labelObj.transform.SetParent(energyCircle.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 0.3f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "ENERGY";
        labelText.fontSize = 10;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.6f, 0.8f, 1f);
    }

    private void CreateEndTurnButton(Transform parent)
    {
        var endTurnObj = new GameObject("EndTurnButton");
        endTurnObj.transform.SetParent(parent, false);
        var btnRect = endTurnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.80f, 0.1f);
        btnRect.anchorMax = new Vector2(0.98f, 0.9f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnBg = endTurnObj.AddComponent<Image>();
        btnBg.color = new Color(0.85f, 0.55f, 0.15f, 1f);

        endTurnButton = endTurnObj.AddComponent<Button>();
        endTurnButton.targetGraphic = btnBg;
        endTurnButton.onClick.AddListener(OnEndTurnClicked);

        // "END" text
        var endTextObj = new GameObject("EndText");
        endTextObj.transform.SetParent(endTurnObj.transform, false);
        var endTextRect = endTextObj.AddComponent<RectTransform>();
        endTextRect.anchorMin = new Vector2(0, 0.55f);
        endTextRect.anchorMax = new Vector2(1, 0.9f);
        endTextRect.offsetMin = Vector2.zero;
        endTextRect.offsetMax = Vector2.zero;

        var endText = endTextObj.AddComponent<TextMeshProUGUI>();
        endText.text = "END";
        endText.fontSize = 14;
        endText.alignment = TextAlignmentOptions.Center;
        endText.color = Color.white;

        // "TURN" text
        var turnTextObj = new GameObject("TurnText");
        turnTextObj.transform.SetParent(endTurnObj.transform, false);
        var turnTextRect = turnTextObj.AddComponent<RectTransform>();
        turnTextRect.anchorMin = new Vector2(0, 0.15f);
        turnTextRect.anchorMax = new Vector2(1, 0.55f);
        turnTextRect.offsetMin = Vector2.zero;
        turnTextRect.offsetMax = Vector2.zero;

        var turnText = turnTextObj.AddComponent<TextMeshProUGUI>();
        turnText.text = "TURN";
        turnText.fontSize = 20;
        turnText.fontStyle = FontStyles.Bold;
        turnText.alignment = TextAlignmentOptions.Center;
        turnText.color = Color.white;
    }

    private Button CreateSkillCard(Transform parent, string name, string hotkey, Color bgColor, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI textComponent, out TooltipTrigger tooltip)
    {
        var cardObj = new GameObject(name);
        cardObj.transform.SetParent(parent, false);

        var cardBg = cardObj.AddComponent<Image>();
        cardBg.color = bgColor;

        var btn = cardObj.AddComponent<Button>();
        btn.targetGraphic = cardBg;
        btn.onClick.AddListener(onClick);

        tooltip = cardObj.AddComponent<TooltipTrigger>();

        // Hotkey indicator (top-left corner)
        var hotkeyObj = new GameObject("Hotkey");
        hotkeyObj.transform.SetParent(cardObj.transform, false);
        var hotkeyRect = hotkeyObj.AddComponent<RectTransform>();
        hotkeyRect.anchorMin = new Vector2(0, 0.75f);
        hotkeyRect.anchorMax = new Vector2(0.35f, 1);
        hotkeyRect.offsetMin = Vector2.zero;
        hotkeyRect.offsetMax = Vector2.zero;

        var hotkeyBg = hotkeyObj.AddComponent<Image>();
        hotkeyBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        var hotkeyTextObj = new GameObject("HotkeyText");
        hotkeyTextObj.transform.SetParent(hotkeyObj.transform, false);
        var hotkeyTextRect = hotkeyTextObj.AddComponent<RectTransform>();
        hotkeyTextRect.anchorMin = Vector2.zero;
        hotkeyTextRect.anchorMax = Vector2.one;
        hotkeyTextRect.offsetMin = Vector2.zero;
        hotkeyTextRect.offsetMax = Vector2.zero;

        var hotkeyText = hotkeyTextObj.AddComponent<TextMeshProUGUI>();
        hotkeyText.text = hotkey;
        hotkeyText.fontSize = 12;
        hotkeyText.alignment = TextAlignmentOptions.Center;
        hotkeyText.color = Color.white;

        // Skill name text (center)
        var nameObj = new GameObject("Text");
        nameObj.transform.SetParent(cardObj.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.25f);
        nameRect.anchorMax = new Vector2(1, 0.75f);
        nameRect.offsetMin = new Vector2(2, 0);
        nameRect.offsetMax = new Vector2(-2, 0);

        textComponent = nameObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = name;
        textComponent.fontSize = 11;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        textComponent.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        textComponent.overflowMode = TextOverflowModes.Ellipsis;

        // Energy cost (bottom)
        var costObj = new GameObject("Cost");
        costObj.transform.SetParent(cardObj.transform, false);
        var costRect = costObj.AddComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0, 0);
        costRect.anchorMax = new Vector2(1, 0.25f);
        costRect.offsetMin = Vector2.zero;
        costRect.offsetMax = Vector2.zero;

        var costText = costObj.AddComponent<TextMeshProUGUI>();
        costText.text = "1 EN";
        costText.fontSize = 10;
        costText.alignment = TextAlignmentOptions.Center;
        costText.color = new Color(0.7f, 0.9f, 1f);

        return btn;
    }

    private void OnEndTurnClicked()
    {
        // End turn is currently automatic after each action
        // This button can be used to skip remaining actions if multi-action turns are implemented
        if (combatManager != null)
        {
            combatManager.EndPlayerTurn();
        }
    }

    private void CreateSkillButtons(Transform parent)
    {
        // Legacy method - now handled by CreateBottomActionBar
        // Kept for compatibility but does nothing
    }

    private void CreatePotionContainer(Transform parent)
    {
        potionContainer = new GameObject("PotionContainer");
        potionContainer.transform.SetParent(parent, false);
        var containerRect = potionContainer.AddComponent<RectTransform>();
        // Place below the top bar stats, left side - avoid clipping with floor text
        containerRect.anchorMin = new Vector2(0.01f, 0.88f);
        containerRect.anchorMax = new Vector2(0.12f, 0.93f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        // Shift the container up by 15 pixels
        containerRect.anchoredPosition = new Vector2(0f, 15f);

        var layout = potionContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.padding = new RectOffset(2, 2, 1, 1);

        // Subtle dark background to visually frame the potion slots (low alpha so circles don't look squared)
        var bg = potionContainer.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.3f);
        
        CreatePotionTooltip(potionContainer.transform);
    }

    private void CreatePotionTooltip(Transform parent)
    {
        potionTooltipPanel = new GameObject("PotionTooltip");
        potionTooltipPanel.transform.SetParent(parent, false);
        var rect = potionTooltipPanel.AddComponent<RectTransform>();
        // Position below the potion container (relative to its parent)
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(220, 60);
        rect.anchoredPosition = new Vector2(0, -65);

        var bg = potionTooltipPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        var textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(potionTooltipPanel.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8, 4);
        textRect.offsetMax = new Vector2(-8, -4);

        potionTooltipText = textObj.AddComponent<TextMeshProUGUI>();
        potionTooltipText.fontSize = 12;
        potionTooltipText.color = Color.white;
        potionTooltipText.alignment = TextAlignmentOptions.Left;

        potionTooltipPanel.SetActive(false);
    }

    private void RefreshPotionButtons(Player player)
    {
        if (potionTooltipPanel != null)
        {
            potionTooltipPanel.SetActive(false);
        }

        if (potionContainer != null)
        {
            for (int i = potionContainer.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(potionContainer.transform.GetChild(i).gameObject);
            }
        }

        potionButtons.Clear();
        potionTexts.Clear();

        var potions = player.GetPotions();
        for (int i = 0; i < player.GetMaxPotions(); i++)
        {
            CreatePotionButton(i, i < potions.Count ? potions[i] : null, player);
        }
    }

    private void CreatePotionButton(int index, PotionData potion, Player player)
    {
        var btnObj = new GameObject($"Potion_{index}");
        btnObj.transform.SetParent(potionContainer.transform, false);

        // Make it circular with compact fixed size
        var layoutElem = btnObj.AddComponent<LayoutElement>();
        layoutElem.preferredWidth = 55;
        layoutElem.preferredHeight = 55;
        layoutElem.minWidth = 55;
        layoutElem.minHeight = 55;

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.sprite = CreateCircleSprite();
        btnImage.type = Image.Type.Simple;
        btnImage.preserveAspect = true;
        
        if (potion != null)
        {
            btnImage.color = GetPotionColor(potion);
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            potionButtons.Add(btn);

            int capturedIndex = index;
            btn.onClick.AddListener(() => OnPotionClicked(capturedIndex, player));
            
            var eventTrigger = btnObj.AddComponent<EventTrigger>();
            var pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            PotionData capturedPotion = potion;
            pointerEnter.callback.AddListener((data) => ShowPotionTooltip(capturedPotion));
            eventTrigger.triggers.Add(pointerEnter);
            
            var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            pointerExit.callback.AddListener((data) => HidePotionTooltip());
            eventTrigger.triggers.Add(pointerExit);
        }
        else
        {
            // Empty slot - dark circle
            btnImage.color = new Color(0.25f, 0.25f, 0.3f, 0.6f);
            potionButtons.Add(null);
            
            // Add hover for empty slots too
            var eventTrigger = btnObj.AddComponent<EventTrigger>();
            var pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            int slotNum = index + 1;
            pointerEnter.callback.AddListener((data) => ShowEmptyPotionTooltip(slotNum));
            eventTrigger.triggers.Add(pointerEnter);
            
            var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            pointerExit.callback.AddListener((data) => HidePotionTooltip());
            eventTrigger.triggers.Add(pointerExit);
        }

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObj.AddComponent<TextMeshProUGUI>();
        if (potion != null)
        {
            string label = GetPotionLabel(potion);
            text.text = label;
            text.color = Color.white;
        }
        else
        {
            text.text = $"{index + 1}";
            text.color = new Color(0.5f, 0.5f, 0.5f);
        }
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 10;
        potionTexts.Add(text);
    }
    
    private void ShowEmptyPotionTooltip(int slotNum)
    {
        if (potionTooltipPanel != null && potionTooltipText != null)
        {
            potionTooltipText.text = $"<b>Empty Potion Slot {slotNum}</b>\nVisit a shop to purchase potions.";
            potionTooltipPanel.SetActive(true);
        }
    }
    
    private void RefreshRelicsDisplay(Player player)
    {
        if (relicsContainer == null) return;
        
        // Clear existing relic icons from the container
        for (int i = relicsContainer.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(relicsContainer.transform.GetChild(i).gameObject);
        }
        relicIcons.Clear();
        
        // Get player's relics
        var relics = player.GetRelics();
        
        // Clamp scroll index
        int maxScrollIndex = Mathf.Max(0, relics.Count - MAX_VISIBLE_RELICS);
        relicScrollIndex = Mathf.Clamp(relicScrollIndex, 0, maxScrollIndex);
        
        // Create visible relic icons (up to MAX_VISIBLE_RELICS)
        int startIndex = relicScrollIndex;
        int endIndex = Mathf.Min(startIndex + MAX_VISIBLE_RELICS, relics.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            CreateRelicIcon(relics[i]);
        }
        
        // Update scroll button visibility
        UpdateRelicScrollButtons(relics.Count);
    }
    
    private void UpdateRelicScrollButtons(int totalRelics)
    {
        if (relicScrollLeftButton != null)
        {
            relicScrollLeftButton.gameObject.SetActive(relicScrollIndex > 0);
        }
        if (relicScrollRightButton != null)
        {
            relicScrollRightButton.gameObject.SetActive(relicScrollIndex + MAX_VISIBLE_RELICS < totalRelics);
        }
    }
    
    private void OnRelicScrollLeft()
    {
        if (relicScrollIndex > 0)
        {
            relicScrollIndex--;
            if (currentPlayer != null)
            {
                RefreshRelicsDisplay(currentPlayer);
            }
        }
    }
    
    private void OnRelicScrollRight()
    {
        if (currentPlayer != null)
        {
            var relics = currentPlayer.GetRelics();
            if (relicScrollIndex + MAX_VISIBLE_RELICS < relics.Count)
            {
                relicScrollIndex++;
                RefreshRelicsDisplay(currentPlayer);
            }
        }
    }
    
    private void CreateRelicIcon(RelicData relic)
    {
        var iconObj = new GameObject($"Relic_{relic.Id}");
        iconObj.transform.SetParent(relicsContainer.transform, false);
        relicIcons.Add(iconObj);
        
        // Fixed size circular icon - sized to fit within container (46px to fit 50px inner height)
        var layoutElem = iconObj.AddComponent<LayoutElement>();
        layoutElem.preferredWidth = 46;
        layoutElem.preferredHeight = 46;
        layoutElem.minWidth = 46;
        layoutElem.minHeight = 46;
        
        var iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = CreateCircleSprite();
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true;
        // Use rarity color as placeholder
        iconImage.color = GetRelicRarityColor(relic.Rarity);
        
        // Add tooltip
        var tooltip = iconObj.AddComponent<TooltipTrigger>();
        tooltip.SetTooltip($"<b>{relic.DisplayName}</b>\n<i>{relic.Rarity}</i>\n{relic.Description}");
    }
    
    private Color GetRelicRarityColor(string rarity)
    {
        switch (rarity?.ToLower())
        {
            case "common": return new Color(0.5f, 0.5f, 0.5f);
            case "uncommon": return new Color(0.3f, 0.6f, 0.3f);
            case "rare": return new Color(0.3f, 0.4f, 0.7f);
            case "epic": return new Color(0.6f, 0.3f, 0.6f);
            case "legendary": return new Color(0.8f, 0.6f, 0.2f);
            default: return new Color(0.4f, 0.4f, 0.4f);
        }
    }

    private string GetPotionLabel(PotionData potion)
    {
        var stat = potion.StatAffected.ToLower().Trim();
        if (stat.Contains("health"))
            return $"HP\n+{potion.Amount}";
        if (stat.Contains("elemental"))
            return $"DMG\n{potion.Amount}";
        if (stat.Contains("crit rate"))
            return $"CR\n+{potion.Amount}%";
        if (stat.Contains("crit damage"))
            return $"CD\n+{potion.Amount}%";
        return potion.DisplayName;
    }

    private Color GetPotionColor(PotionData potion)
    {
        var stat = potion.StatAffected.ToLower().Trim();
        if (stat.Contains("health"))
            return new Color(0.4f, 0.2f, 0.2f);
        if (stat.Contains("elemental"))
            return new Color(0.3f, 0.25f, 0.4f);
        if (stat.Contains("crit rate"))
            return new Color(0.4f, 0.35f, 0.2f);
        if (stat.Contains("crit damage"))
            return new Color(0.35f, 0.2f, 0.35f);
        return new Color(0.25f, 0.3f, 0.25f);
    }
    
    private static Sprite cachedCircleSprite;
    private Sprite CreateCircleSprite()
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;
        
        int size = 55;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size / 2f;
        float centerX = size / 2f;
        float centerY = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (dist <= radius - 1)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else if (dist <= radius)
                {
                    // Anti-aliased edge
                    float alpha = radius - dist;
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        cachedCircleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        return cachedCircleSprite;
    }

    private void ShowPotionTooltip(PotionData potion)
    {
        if (potionTooltipPanel == null) return;
        
        var stat = potion.StatAffected.ToLower().Trim();
        string desc = "";
        
        if (stat.Contains("health"))
            desc = $"<color=#ff5555>{potion.DisplayName}</color>\nRestores <color=#55ff55>{potion.Amount}</color> HP";
        else if (stat.Contains("elemental"))
            desc = $"<color=#aa77ff>{potion.DisplayName}</color>\nDeals <color=#ffaa55>{potion.Amount}</color> elemental damage (random element)\n<color=#888888>Requires target selection</color>";
        else if (stat.Contains("crit rate"))
            desc = $"<color=#ffdd55>{potion.DisplayName}</color>\nIncreases crit chance by <color=#55ff55>+{potion.Amount}%</color>\n<color=#888888>Lasts until world ends</color>";
        else if (stat.Contains("crit damage"))
            desc = $"<color=#ff55ff>{potion.DisplayName}</color>\nIncreases crit damage by <color=#55ff55>+{potion.Amount}%</color>\n<color=#888888>Lasts until world ends</color>";
        else
            desc = potion.DisplayName;
        
        potionTooltipText.text = desc;
        potionTooltipPanel.SetActive(true);
    }

    private void HidePotionTooltip()
    {
        if (potionTooltipPanel != null)
            potionTooltipPanel.SetActive(false);
    }

    private void OnPotionClicked(int index, Player player)
    {
        if (isTargeting) return;
        
        var potion = player.GetPotion(index);
        if (potion == null) return;

        var stat = potion.StatAffected.ToLower().Trim();
        
        if (stat.Contains("elemental"))
        {
            selectedPotionIndex = index;
            selectedPotionElement = RollPotionElement();
            isPotionTargeting = true;
            EnterPotionTargetingMode(potion);
        }
        else
        {
            if (player.UsePotion(index, null))
            {
                RefreshPotionButtons(player);
                
                var refs = FindFirstObjectByType<Referencer>();
                if (refs != null && refs.playerStatsUI != null)
                {
                    refs.playerStatsUI.UpdateStats();
                }
                UpdatePlayerHealth(player);
            }
        }
    }

    private Element RollPotionElement()
    {
        var elements = new Element[] { Element.Fire, Element.Ice, Element.Water, Element.Wind, Element.Rock };
        return elements[Random.Range(0, elements.Length)];
    }

    private void EnterPotionTargetingMode(PotionData potion)
    {
        isTargeting = true;
        isAoESkill = false;
        
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0)
        {
            ExitPotionTargetingMode();
            return;
        }
        
        selectedTargetIndex = 0;
        
        skill1Button.gameObject.SetActive(false);
        skill2Button.gameObject.SetActive(false);
        skill3Button.gameObject.SetActive(false);
        skill4Button.gameObject.SetActive(false);
        skill5Button.gameObject.SetActive(false);
        backButton.gameObject.SetActive(true);
        
        var backText = backButton.GetComponentInChildren<TextMeshProUGUI>();
        if (backText != null)
        {
            backText.text = $"BACK\n({selectedPotionElement})";
        }
        
        foreach (var btn in potionButtons)
        {
            if (btn != null) btn.gameObject.SetActive(false);
        }
        
        UpdateTargetArrows();
    }

    private void ExitPotionTargetingMode()
    {
        isPotionTargeting = false;
        selectedPotionIndex = -1;
        selectedPotionElement = Element.None;
        
        var backText = backButton.GetComponentInChildren<TextMeshProUGUI>();
        if (backText != null)
        {
            backText.text = "BACK";
        }
        
        ExitTargetingMode();
        
        foreach (var btn in potionButtons)
        {
            if (btn != null) btn.gameObject.SetActive(true);
        }
    }

    private void ConfirmPotionTarget(Player player)
    {
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0 || selectedTargetIndex >= aliveEnemies.Count) return;
        
        var target = aliveEnemies[selectedTargetIndex];
        
        if (player.UsePotion(selectedPotionIndex, target, selectedPotionElement))
        {
            UpdateEnemyHealth(target);
            RefreshPotionButtons(player);
        }
        
        ExitPotionTargetingMode();
    }

    private Button CreateActionButton(Transform parent, string name, string label, Color color, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI textComponent, out TooltipTrigger tooltip)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = color;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(onClick);

        tooltip = btnObj.AddComponent<TooltipTrigger>();

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = label;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontSize = 16;
        textComponent.color = Color.white;

        return btn;
    }

    private void OnSkill1Clicked()
    {
        bool isAoE = IsSkillAoE(1);
        EnterTargetingMode(1, isAoE);
    }

    private void OnSkill2Clicked()
    {
        bool isAoE = IsSkillAoE(2);
        EnterTargetingMode(2, isAoE);
    }

    private void OnSkill3Clicked()
    {
        bool isAoE = IsSkillAoE(3);
        EnterTargetingMode(3, isAoE);
    }

    private void OnSkill4Clicked()
    {
        bool isAoE = IsSkillAoE(4);
        EnterTargetingMode(4, isAoE);
    }

    private void OnSkill5Clicked()
    {
        bool isAoE = IsSkillAoE(5);
        EnterTargetingMode(5, isAoE);
    }

    private void OnBackClicked()
    {
        if (isPotionTargeting)
        {
            ExitPotionTargetingMode();
        }
        else
        {
            ExitTargetingMode();
        }
    }

    private bool IsSkillAoE(int skillNumber)
    {
        if (combatManager == null) return false;
        var enemies = combatManager.GetEnemies();
        if (enemies == null || enemies.Count <= 1) return false;
        
        var refs = FindFirstObjectByType<Referencer>();
        if (refs == null || refs.player == null) return false;
        
        var character = refs.player.GetCharacter();
        if (character == null) return false;
        
        string skillName = skillNumber switch
        {
            1 => character.Skill1,
            2 => character.Skill2,
            3 => character.Skill3,
            4 => character.Skill4,
            5 => character.Skill5,
            _ => ""
        };
        
        // Check if skill targets AllEnemies by looking up the skill definition
        string skillId = $"skill_{skillName.ToLower()}";
        var skillDef = GameDataLoader.GetSkill(skillId);
        if (skillDef?.executions != null)
        {
            foreach (var exec in skillDef.executions)
            {
                if (exec.target?.selector == "AllEnemies" && exec.effectId == "eff_deal_damage")
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void EnterTargetingMode(int skillNumber, bool isAoE)
    {
        isTargeting = true;
        selectedSkillNumber = skillNumber;
        isAoESkill = isAoE;
        
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0)
        {
            isTargeting = false;
            return;
        }
        
        selectedTargetIndex = 0;
        
        skill1Button.gameObject.SetActive(false);
        skill2Button.gameObject.SetActive(false);
        skill3Button.gameObject.SetActive(false);
        skill4Button.gameObject.SetActive(false);
        skill5Button.gameObject.SetActive(false);
        backButton.gameObject.SetActive(true);
        
        foreach (var btn in potionButtons)
        {
            if (btn != null) btn.gameObject.SetActive(false);
        }
        
        UpdateTargetArrows();
    }

    private void ExitTargetingMode()
    {
        isTargeting = false;
        selectedSkillNumber = 0;
        isAoESkill = false;
        
        skill1Button.gameObject.SetActive(true);
        skill2Button.gameObject.SetActive(true);
        skill3Button.gameObject.SetActive(true);
        skill4Button.gameObject.SetActive(true);
        skill5Button.gameObject.SetActive(true);
        backButton.gameObject.SetActive(false);
        
        foreach (var btn in potionButtons)
        {
            if (btn != null) btn.gameObject.SetActive(true);
        }
        
        HideAllArrows();
    }

    private void CycleTarget(int direction)
    {
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0) return;
        
        selectedTargetIndex += direction;
        
        if (selectedTargetIndex < 0)
            selectedTargetIndex = aliveEnemies.Count - 1;
        else if (selectedTargetIndex >= aliveEnemies.Count)
            selectedTargetIndex = 0;
        
        UpdateTargetArrows();
    }

    private void ConfirmTarget()
    {
        if (isPotionTargeting)
        {
            var refs = FindFirstObjectByType<Referencer>();
            if (refs != null && refs.player != null)
            {
                ConfirmPotionTarget(refs.player);
            }
            return;
        }
        
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0 || selectedTargetIndex >= aliveEnemies.Count) return;
        
        var target = aliveEnemies[selectedTargetIndex];
        
        if (combatManager == null)
        {
            combatManager = FindFirstObjectByType<CombatManager>();
        }
        
        if (combatManager != null)
        {
            if (selectedSkillNumber == 0)
            {
                combatManager.OnPlayerAttackTarget(target);
            }
            else
            {
                combatManager.OnPlayerSkillTarget(selectedSkillNumber, target);
            }
        }
        
        ExitTargetingMode();
    }

    private List<CombatEnemy> GetAliveEnemies()
    {
        var alive = new List<CombatEnemy>();
        foreach (var enemy in currentEnemies)
        {
            if (enemy.IsAlive()) alive.Add(enemy);
        }
        return alive;
    }

    private void UpdateTargetArrows()
    {
        var aliveEnemies = GetAliveEnemies();
        
        foreach (var kvp in enemySlots)
        {
            var enemy = kvp.Key;
            var slot = kvp.Value;
            
            if (slot.Arrow == null) continue;
            
            if (!enemy.IsAlive())
            {
                slot.Arrow.SetActive(false);
                continue;
            }
            
            int aliveIndex = aliveEnemies.IndexOf(enemy);
            
            if (isAoESkill)
            {
                slot.Arrow.SetActive(true);
                var arrowImage = slot.Arrow.GetComponent<Image>();
                if (arrowImage != null)
                {
                    if (aliveIndex == selectedTargetIndex)
                    {
                        arrowImage.color = new Color(1f, 0.8f, 0.2f);
                        slot.Arrow.transform.localScale = Vector3.one;
                    }
                    else
                    {
                        arrowImage.color = new Color(1f, 0.5f, 0.2f, 0.6f);
                        slot.Arrow.transform.localScale = Vector3.one * 0.7f;
                    }
                }
            }
            else
            {
                bool isSelected = aliveIndex == selectedTargetIndex;
                slot.Arrow.SetActive(isSelected);
                if (isSelected)
                {
                    var arrowImage = slot.Arrow.GetComponent<Image>();
                    if (arrowImage != null)
                    {
                        arrowImage.color = new Color(1f, 0.8f, 0.2f);
                    }
                    slot.Arrow.transform.localScale = Vector3.one;
                }
            }
        }
    }

    private void HideAllArrows()
    {
        foreach (var slot in enemySlots.Values)
        {
            if (slot.Arrow != null)
            {
                slot.Arrow.SetActive(false);
            }
        }
    }

    private void OnEnemyClicked(CombatEnemy enemy)
    {
        if (!isTargeting) return;
        if (!enemy.IsAlive()) return;
        
        var aliveEnemies = GetAliveEnemies();
        int index = aliveEnemies.IndexOf(enemy);
        if (index >= 0)
        {
            selectedTargetIndex = index;
            ConfirmTarget();
        }
    }

    private void OnEnemyHover(CombatEnemy enemy)
    {
        if (!isTargeting) return;
        if (!enemy.IsAlive()) return;
        
        var aliveEnemies = GetAliveEnemies();
        int index = aliveEnemies.IndexOf(enemy);
        if (index >= 0)
        {
            selectedTargetIndex = index;
            UpdateTargetArrows();
        }
    }

    public void ShowCombat(List<CombatEnemy> enemies, Player player, string title = null)
    {
        if (combatPanel == null)
        {
            GameLog.Error(
                GameLogCategory.System,
                "[CombatUI]",
                GameLog.Join(
                    "ShowCombatFail",
                    GameLog.KV("reason", "CombatPanelNull")
                )
            );
            SetupUI();
        }

        currentEnemies = new List<CombatEnemy>(enemies);
        currentPlayer = player;
        isTargeting = false;
        
        ClearEnemySlots();

        foreach (var enemy in enemies)
        {
            CreateEnemySlot(enemy);
        }

        // Update top bar - Player name, HP, Gold
        var character = player.GetCharacter();
        if (playerNameText != null && character != null)
        {
            playerNameText.text = character.DisplayName.ToUpper();
        }
        
        // Update floor display
        if (floorText != null)
        {
            if (title != null && title.Contains("BOSS"))
            {
                floorText.text = title;
                floorText.color = new Color(1f, 0.5f, 0.2f);
                if (floorSubtitleText != null)
                {
                    floorSubtitleText.text = "";
                }
            }
            else
            {
                // Calculate floor number based on world and nodes completed
                var gameManager = FindFirstObjectByType<GameManager>();
                int world = GameManager.CurrentWorld;
                int nodesPerWorld = gameManager != null ? gameManager.GetTotalNodesForCurrentWorld() : 5;
                int floorNumber = (world - 1) * nodesPerWorld + 1;
                floorText.text = $"FLOOR {floorNumber}";
                floorText.color = Color.white;
                
                // Show subtitle for boss approaching
                if (floorSubtitleText != null)
                {
                    floorSubtitleText.text = title ?? "";
                }
            }
        }

        UpdateSkillButtons(player);
        UpdatePlayerHealth(player);
        UpdateEnergyDisplay(player);
        RefreshPotionButtons(player);
        RefreshRelicsDisplay(player);
        combatPanel.SetActive(true);
        GameLog.System(GameLog.Join(
            "CombatUIShow",
            GameLog.KV("enemies", enemies.Count),
            GameLog.KV("title", title ?? "COMBAT")
        ), GameLogVerbosity.Verbose);
        SetPlayerTurn(true);
        isPotionTargeting = false;
        selectedPotionIndex = -1;
        selectedPotionElement = Element.None;
        
        if (backButton != null) backButton.gameObject.SetActive(false);
    }

    public void UpdateSkillButtons(Player player)
    {
        var character = player.GetCharacter();
        if (character != null)
        {
            int currentEnergy = player.GetEnergy();
            
            // Skill 1 - show cooldown if on cooldown, and DirtyStab stacks if applicable
            int cd1 = player.GetSkillCooldown(0);
            string skill1Name = character.Skill1;
            string dirtyStabIndicator = "";
            
            // Show DirtyStab stack indicator for Rogue
            if (skill1Name.ToLower() == "dirtystab")
            {
                int stacks = player.GetDirtyStabStacks();
                if (stacks > 0)
                {
                    dirtyStabIndicator = $" <color=#ffaa00>[+{stacks * 20}%]</color>";
                }
            }
            
            if (cd1 > 0)
            {
                if (skill1Text != null) skill1Text.text = $"{skill1Name}{dirtyStabIndicator}\n<size=12><color=#888888>CD: {cd1}</color></size>";
                if (skill1Button != null) skill1Button.interactable = false;
            }
            else
            {
                if (skill1Text != null) skill1Text.text = $"{skill1Name}{dirtyStabIndicator}";
                if (skill1Button != null) skill1Button.interactable = true;
            }
            
            // Skill 2 - show cooldown if on cooldown
            int cd2 = player.GetSkillCooldown(1);
            if (cd2 > 0)
            {
                if (skill2Text != null) skill2Text.text = $"{character.Skill2}\n<size=12><color=#888888>CD: {cd2}</color></size>";
                if (skill2Button != null) skill2Button.interactable = false;
            }
            else
            {
                if (skill2Text != null) skill2Text.text = character.Skill2;
                if (skill2Button != null) skill2Button.interactable = true;
            }
            
            // Skill 3 - show cooldown if on cooldown
            int cd3 = player.GetSkillCooldown(2);
            if (cd3 > 0)
            {
                if (skill3Text != null) skill3Text.text = $"{character.Skill3}\n<size=12><color=#888888>CD: {cd3}</color></size>";
                if (skill3Button != null) skill3Button.interactable = false;
            }
            else
            {
                if (skill3Text != null) skill3Text.text = character.Skill3;
                if (skill3Button != null) skill3Button.interactable = true;
            }
            
            // Skill 4 - show cooldown if on cooldown
            int cd4 = player.GetSkillCooldown(3);
            if (cd4 > 0)
            {
                if (skill4Text != null) skill4Text.text = $"{character.Skill4}\n<size=12><color=#888888>CD: {cd4}</color></size>";
                if (skill4Button != null) skill4Button.interactable = false;
            }
            else
            {
                if (skill4Text != null) skill4Text.text = character.Skill4;
                if (skill4Button != null) skill4Button.interactable = true;
            }
            
            // Skill 5 (Ultimate) - show energy and cooldown, disable if not enough energy or on cooldown
            int cd5 = player.GetSkillCooldown(4);
            int energyCost = character.Skill5EnergyCost;
            bool canUseUltimate = player.CanUseUltimate();
            
            string skill5Status = "";
            if (cd5 > 0)
            {
                skill5Status = $"\n<size=12><color=#888888>CD: {cd5}</color></size>";
            }
            else
            {
                skill5Status = $"\n<size=12><color={(canUseUltimate ? "#44ff44" : "#ff4444")}>{currentEnergy}/{energyCost}</color></size>";
            }
            
            if (skill5Text != null) skill5Text.text = $"{character.Skill5}{skill5Status}";
            if (skill5Button != null) skill5Button.interactable = canUseUltimate;
            
            // Update tooltips
            if (skill1Tooltip != null) skill1Tooltip.SetTooltip($"<b>{character.Skill1}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill1)}\nEnergy Gain: +{character.Skill1EnergyGain}\nCooldown: {character.Skill1Cooldown} turn(s)");
            if (skill2Tooltip != null) skill2Tooltip.SetTooltip($"<b>{character.Skill2}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill2)}\nEnergy Gain: +{character.Skill2EnergyGain}\nCooldown: {character.Skill2Cooldown} turn(s)");
            if (skill3Tooltip != null) skill3Tooltip.SetTooltip($"<b>{character.Skill3}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill3)}\nEnergy Gain: +{character.Skill3EnergyGain}\nCooldown: {character.Skill3Cooldown} turn(s)");
            if (skill4Tooltip != null) skill4Tooltip.SetTooltip($"<b>{character.Skill4}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill4)}\nEnergy Gain: +{character.Skill4EnergyGain}\nCooldown: {character.Skill4Cooldown} turn(s)");
            if (skill5Tooltip != null) skill5Tooltip.SetTooltip($"<b>{character.Skill5}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill5)}\nEnergy Cost: {character.Skill5EnergyCost}\nCooldown: {character.Skill5Cooldown} turn(s)");
        }
        else
        {
            if (skill1Text != null) skill1Text.text = "---";
            if (skill2Text != null) skill2Text.text = "---";
            if (skill3Text != null) skill3Text.text = "---";
            if (skill4Text != null) skill4Text.text = "---";
            if (skill5Text != null) skill5Text.text = "---";
            
            if (skill1Button != null) skill1Button.interactable = false;
            if (skill2Button != null) skill2Button.interactable = false;
            if (skill3Button != null) skill3Button.interactable = false;
            if (skill4Button != null) skill4Button.interactable = false;
            if (skill5Button != null) skill5Button.interactable = false;
            
            if (skill1Tooltip != null) skill1Tooltip.SetTooltip("");
            if (skill2Tooltip != null) skill2Tooltip.SetTooltip("");
            if (skill3Tooltip != null) skill3Tooltip.SetTooltip("");
            if (skill4Tooltip != null) skill4Tooltip.SetTooltip("");
            if (skill5Tooltip != null) skill5Tooltip.SetTooltip("");
        }
    }
    
    public void UpdatePlayerEnergy(Player player)
    {
        // Update both the energy orb display and skill buttons
        UpdateEnergyDisplay(player);
        UpdateSkillButtons(player);
    }

    public void HideCombat()
    {
        var tooltipUI = FindFirstObjectByType<TooltipUI>();
        if (tooltipUI != null)
        {
            tooltipUI.Hide();
        }
        
        combatPanel.SetActive(false);
        ClearEnemySlots();
    }

    private void ClearEnemySlots()
    {
        foreach (var slot in enemySlots.Values)
        {
            if (slot.Root != null)
                Destroy(slot.Root);
        }
        enemySlots.Clear();
    }

    private void CreateEnemySlot(CombatEnemy enemy)
    {
        var slot = new GameObject($"Enemy_{enemy.Name}");
        slot.transform.SetParent(enemyContainer, false);

        var rect = slot.AddComponent<RectTransform>();

        var bg = slot.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.1f, 0.1f, 1f);
        
        var clickBtn = slot.AddComponent<Button>();
        clickBtn.targetGraphic = bg;
        CombatEnemy capturedEnemy = enemy;
        clickBtn.onClick.AddListener(() => OnEnemyClicked(capturedEnemy));
        
        var eventTrigger = slot.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => OnEnemyHover(capturedEnemy));
        eventTrigger.triggers.Add(pointerEnter);

        var layout = slot.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(slot.transform, false);
        var arrowRect = arrowObj.AddComponent<RectTransform>();
        var arrowLayout = arrowObj.AddComponent<LayoutElement>();
        arrowLayout.preferredHeight = 30;
        arrowLayout.preferredWidth = 40;
        var arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
        arrowText.text = "▼";
        arrowText.alignment = TextAlignmentOptions.Center;
        arrowText.fontSize = 28;
        arrowText.color = new Color(1f, 0.8f, 0.2f);
        arrowObj.SetActive(false);

        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(slot.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = enemy.Name;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 20;
        nameText.color = Color.white;
        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 30;

        var elementObj = new GameObject("Element");
        elementObj.transform.SetParent(slot.transform, false);
        var elementText = elementObj.AddComponent<TextMeshProUGUI>();
        if (enemy.IsBoss)
        {
            elementText.text = "BOSS";
            elementText.color = new Color(1f, 0.3f, 0.3f);
        }
        else
        {
            elementText.text = enemy.Affinity.ToString();
            elementText.color = GetElementColor(enemy.Affinity);
        }
        elementText.alignment = TextAlignmentOptions.Center;
        elementText.fontSize = 14;
        var elementLayout = elementObj.AddComponent<LayoutElement>();
        elementLayout.preferredHeight = 20;

        var healthBarBg = new GameObject("HealthBarBg");
        healthBarBg.transform.SetParent(slot.transform, false);
        var healthBgRect = healthBarBg.AddComponent<RectTransform>();
        var healthBgImage = healthBarBg.AddComponent<Image>();
        healthBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var healthBgLayout = healthBarBg.AddComponent<LayoutElement>();
        healthBgLayout.preferredHeight = 20;

        var healthFill = new GameObject("Fill");
        healthFill.transform.SetParent(healthBarBg.transform, false);
        var fillRect = healthFill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        var fillImage = healthFill.AddComponent<Image>();
        fillImage.color = Color.red;

        var healthTextObj = new GameObject("HealthText");
        healthTextObj.transform.SetParent(slot.transform, false);
        var healthText = healthTextObj.AddComponent<TextMeshProUGUI>();
        healthText.text = $"{enemy.Health}/{enemy.MaxHealth}";
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.fontSize = 16;
        healthText.color = Color.white;
        var healthTextLayout = healthTextObj.AddComponent<LayoutElement>();
        healthTextLayout.preferredHeight = 25;

        var damageObj = new GameObject("Damage");
        damageObj.transform.SetParent(slot.transform, false);
        var damageText = damageObj.AddComponent<TextMeshProUGUI>();
        damageText.text = $"DMG: {enemy.Damage}";
        damageText.alignment = TextAlignmentOptions.Center;
        damageText.fontSize = 14;
        damageText.color = new Color(1f, 0.6f, 0.6f, 1f);
        var damageLayout = damageObj.AddComponent<LayoutElement>();
        damageLayout.preferredHeight = 20;

        enemySlots[enemy] = new EnemyUISlot
        {
            Root = slot,
            HealthFill = fillImage,
            HealthText = healthText,
            NameText = nameText,
            Arrow = arrowObj,
            ClickArea = clickBtn
        };
    }

    public void UpdateEnemyHealth(CombatEnemy enemy)
    {
        if (!enemySlots.ContainsKey(enemy)) return;

        var slot = enemySlots[enemy];
        float healthPercent = (float)enemy.Health / enemy.MaxHealth;
        slot.HealthFill.fillAmount = healthPercent;
        
        // Build health text with status effects
        string healthDisplay = $"{enemy.Health}/{enemy.MaxHealth}";
        
        // Show status effect indicators
        var effects = enemy.GetStatusEffects();
        foreach (var effect in effects)
        {
            if (effect.Type == StatusEffectType.DoT)
            {
                string stackText = effect.StackCount > 1 ? $"x{effect.StackCount}" : "";
                healthDisplay += $" <color=#ff6600>-{(int)effect.Value} DoT{stackText} ({effect.Duration}t)</color>";
            }
            else if (effect.Type == StatusEffectType.Stun)
            {
                healthDisplay += $" <color=#ffff00>STUNNED ({effect.Duration}t)</color>";
            }
        }
        
        slot.HealthText.text = healthDisplay;

        if (!enemy.IsAlive())
        {
            slot.Root.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            slot.NameText.color = Color.gray;
        }
    }

    public void UpdatePlayerHealth(Player player)
    {
        float healthPercent = (float)player.GetHealth() / player.GetMaxHealth();
        
        // Update hidden fill for compatibility (used by floating text positioning)
        if (playerHealthFill != null)
        {
            playerHealthFill.fillAmount = healthPercent;
        }
        
        // Display HP in top bar format with heart icon
        int shield = player.GetShield();
        string shieldText = shield > 0 ? $" <color=#44aaff>[+{shield}]</color>" : "";
        
        if (playerHealthText != null)
        {
            string hpColor = healthPercent > 0.5f ? "#55ff55" : (healthPercent > 0.25f ? "#ffff55" : "#ff5555");
            playerHealthText.text = $"<color=#cc3333>♥</color> <color={hpColor}>{player.GetHealth()}/{player.GetMaxHealth()}</color>{shieldText}";
        }
        
        // Update gold display
        if (playerGoldText != null)
        {
            playerGoldText.text = $"<color=#ffcc00>G</color> {player.GetGold()}";
        }
    }
    
    public void UpdateEnergyDisplay(Player player)
    {
        if (energyText != null)
        {
            energyText.text = player.GetEnergy().ToString();
        }
    }

    public void ShowDamageToEnemy(CombatEnemy enemy, int damage, bool isCrit = false)
    {
        GameLog.System(GameLog.Join(
            "FloatingText",
            GameLog.KV("target", enemy != null ? enemy.Name : "null"),
            GameLog.KV("type", "DamageToEnemy"),
            GameLog.KV("amount", damage),
            GameLog.KV("crit", isCrit)
        ), GameLogVerbosity.Verbose);
        
        // Show floating text
        var fctManager = FloatingTextManager.Instance;
        if (fctManager == null)
        {
            GameLog.Warn(
                GameLogCategory.System,
                "[CombatUI]",
                GameLog.Join(
                    "FloatingTextMissing",
                    GameLog.KV("reason", "InstanceNull")
                )
            );
            return;
        }
        
        Transform enemyTransform = GetEnemyTransform(enemy);
        if (enemyTransform == null)
        {
            GameLog.Warn(
                GameLogCategory.System,
                "[CombatUI]",
                GameLog.Join(
                    "FloatingTextMissing",
                    GameLog.KV("reason", "EnemyTransformNull"),
                    GameLog.KV("enemy", enemy != null ? enemy.Name : "null")
                )
            );
            return;
        }
        
        fctManager.ShowDamage(enemyTransform, damage, isCrit);
    }

    public void ShowDamageToPlayer(int damage)
    {
        GameLog.System(GameLog.Join(
            "FloatingText",
            GameLog.KV("target", "Player"),
            GameLog.KV("type", "DamageToPlayer"),
            GameLog.KV("amount", damage)
        ), GameLogVerbosity.Verbose);
        
        // Show floating text
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null && playerHealthBar != null)
        {
            fctManager.ShowDamageTaken(playerHealthBar.transform, damage);
        }
    }
    
    public void ShowHealToPlayer(int amount)
    {
        GameLog.System(GameLog.Join(
            "FloatingText",
            GameLog.KV("target", "Player"),
            GameLog.KV("type", "HealToPlayer"),
            GameLog.KV("amount", amount)
        ), GameLogVerbosity.Verbose);
        
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null && playerHealthBar != null)
        {
            fctManager.ShowHeal(playerHealthBar.transform, amount);
        }
    }
    
    public void ShowShieldToPlayer(int amount, FloatingTextType shieldType)
    {
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null && playerHealthBar != null)
        {
            fctManager.ShowShield(playerHealthBar.transform, amount, shieldType);
        }
    }
    
    public void ShowStatusToEnemy(CombatEnemy enemy, string statusName, bool gained, int stacksDelta = 0)
    {
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null)
        {
            Transform enemyTransform = GetEnemyTransform(enemy);
            if (enemyTransform != null)
            {
                fctManager.ShowStatus(enemyTransform, statusName, gained, stacksDelta);
            }
        }
    }
    
    public void ShowDoTTickToEnemy(CombatEnemy enemy, int damage, string dotName = "DoT")
    {
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null)
        {
            Transform enemyTransform = GetEnemyTransform(enemy);
            if (enemyTransform != null)
            {
                fctManager.ShowDoTTick(enemyTransform, damage, dotName);
            }
        }
    }
    
    public void ShowTurnSkippedToEnemy(CombatEnemy enemy, string reason = "Stunned!")
    {
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null)
        {
            Transform enemyTransform = GetEnemyTransform(enemy);
            if (enemyTransform != null)
            {
                fctManager.ShowTurnSkipped(enemyTransform, reason);
            }
        }
    }
    
    public void ShowReactionToEnemy(CombatEnemy enemy, string reactionName, float multiplier = 0f, int bonusDamage = 0)
    {
        var fctManager = FloatingTextManager.Instance;
        if (fctManager != null)
        {
            Transform enemyTransform = GetEnemyTransform(enemy);
            if (enemyTransform != null)
            {
                fctManager.ShowReaction(enemyTransform, reactionName, multiplier, bonusDamage);
            }
        }
    }
    
    public Transform GetEnemyTransform(CombatEnemy enemy)
    {
        if (enemySlots.ContainsKey(enemy))
        {
            return enemySlots[enemy].Root.transform;
        }
        return null;
    }
    
    public Transform GetPlayerTransform()
    {
        return playerHealthBar?.transform;
    }

    public void SetPlayerTurn(bool isPlayerTurn)
    {
        if (isPlayerTurn)
        {
            // Update skill buttons with cooldown/energy state
            var refs = FindFirstObjectByType<Referencer>();
            if (refs != null && refs.player != null)
            {
                UpdateSkillButtons(refs.player);
            }
            UpdateReactionIndicator();
        }
        else
        {
            // Disable all skill buttons during enemy turn
            if (skill1Button != null) skill1Button.interactable = false;
            if (skill2Button != null) skill2Button.interactable = false;
            if (skill3Button != null) skill3Button.interactable = false;
            if (skill4Button != null) skill4Button.interactable = false;
            if (skill5Button != null) skill5Button.interactable = false;
        }
    }
    
    public void UpdateReactionIndicator()
    {
        var refs = FindFirstObjectByType<Referencer>();
        if (refs == null || refs.player == null) return;
        
        var player = refs.player;
        bool reactionReady = player.HasReactionReady();
        
        Color normalColor = new Color(0.2f, 0.5f, 0.7f, 1f);
        Color ultimateColor = new Color(0.7f, 0.4f, 0.2f, 1f);
        Color unavailableColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        // Check skill availability (cooldown and energy)
        int cd1 = player.GetSkillCooldown(0);
        int cd2 = player.GetSkillCooldown(1);
        int cd3 = player.GetSkillCooldown(2);
        int cd4 = player.GetSkillCooldown(3);
        int cd5 = player.GetSkillCooldown(4);
        var character = player.GetCharacter();
        bool canUseSkill1 = cd1 <= 0;
        bool canUseSkill2 = cd2 <= 0;
        bool canUseSkill3 = cd3 <= 0;
        bool canUseSkill4 = cd4 <= 0;
        bool canUseUltimate = player.CanUseUltimate();
        
        if (reactionReady)
        {
            var orbSystem = player.GetOrbSystem();
            Color colorA = GetElementColor(orbSystem.OrbAMark);
            Color colorB = GetElementColor(orbSystem.OrbBMark);
            
            // Only apply reaction colors to skills that are actually usable
            if (canUseSkill1)
                SetButtonDiagonalSplit(skill1Button, colorA, colorB);
            else
                ClearButtonDiagonalSplit(skill1Button, unavailableColor);
                
            if (canUseSkill2)
                SetButtonDiagonalSplit(skill2Button, colorA, colorB);
            else
                ClearButtonDiagonalSplit(skill2Button, unavailableColor);
                
            if (canUseSkill3)
                SetButtonDiagonalSplit(skill3Button, colorA, colorB);
            else
                ClearButtonDiagonalSplit(skill3Button, unavailableColor);
                
            if (canUseSkill4)
                SetButtonDiagonalSplit(skill4Button, colorA, colorB);
            else
                ClearButtonDiagonalSplit(skill4Button, unavailableColor);
                
            if (canUseUltimate)
                SetButtonDiagonalSplit(skill5Button, colorA, colorB);
            else
                ClearButtonDiagonalSplit(skill5Button, unavailableColor);
        }
        else
        {
            ClearButtonDiagonalSplit(skill1Button, canUseSkill1 ? normalColor : unavailableColor);
            ClearButtonDiagonalSplit(skill2Button, canUseSkill2 ? normalColor : unavailableColor);
            ClearButtonDiagonalSplit(skill3Button, canUseSkill3 ? normalColor : unavailableColor);
            ClearButtonDiagonalSplit(skill4Button, canUseSkill4 ? normalColor : unavailableColor);
            ClearButtonDiagonalSplit(skill5Button, canUseUltimate ? ultimateColor : unavailableColor);
        }
    }
    
    private void SetButtonDiagonalSplit(Button button, Color topColor, Color bottomColor)
    {
        if (button == null) return;
        
        var buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = Color.clear;
        }
        
        var diagonalOverlay = button.transform.Find("DiagonalOverlay");
        RawImage rawImage;
        
        if (diagonalOverlay == null)
        {
            var obj = new GameObject("DiagonalOverlay");
            obj.transform.SetParent(button.transform, false);
            obj.transform.SetAsFirstSibling();
            
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            rawImage = obj.AddComponent<RawImage>();
            rawImage.raycastTarget = false;
            diagonalOverlay = obj.transform;
        }
        else
        {
            rawImage = diagonalOverlay.GetComponent<RawImage>();
            diagonalOverlay.gameObject.SetActive(true);
        }
        
        rawImage.texture = CreateDiagonalTexture(topColor, bottomColor);
        
        var outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2, 2);
        outline.enabled = true;
    }
    
    private Texture2D CreateDiagonalTexture(Color topColor, Color bottomColor)
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x + y < size)
                {
                    tex.SetPixel(x, y, topColor);
                }
                else
                {
                    tex.SetPixel(x, y, bottomColor);
                }
            }
        }
        
        tex.Apply();
        return tex;
    }
    
    private void ClearButtonDiagonalSplit(Button button, Color normalColor)
    {
        if (button == null) return;
        
        var buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
        
        var diagonalOverlay = button.transform.Find("DiagonalOverlay");
        if (diagonalOverlay != null)
        {
            diagonalOverlay.gameObject.SetActive(false);
        }
        
        var outline = button.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }
    }

    private Color GetElementColor(Element element)
    {
        return element switch
        {
            Element.Fire => new Color(1f, 0.4f, 0.2f),
            Element.Ice => new Color(0.4f, 0.8f, 1f),
            Element.Water => new Color(0.2f, 0.5f, 1f),
            Element.Wind => new Color(0.6f, 1f, 0.6f),
            Element.Rock => new Color(0.7f, 0.5f, 0.3f),
            _ => Color.white
        };
    }

    private class EnemyUISlot
    {
        public GameObject Root;
        public Image HealthFill;
        public TextMeshProUGUI HealthText;
        public TextMeshProUGUI NameText;
        public GameObject Arrow;
        public Button ClickArea;
    }

    private GameObject CreateLootPanel(Transform parent)
    {
        var panel = new GameObject("LootPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.3f, 0.3f);
        rect.anchorMax = new Vector2(0.7f, 0.7f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.75f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        lootTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        lootTitleText.text = "VICTORY!";
        lootTitleText.alignment = TextAlignmentOptions.Center;
        lootTitleText.fontSize = 32;
        lootTitleText.color = new Color(1f, 0.85f, 0.2f);

        var goldObj = new GameObject("GoldText");
        goldObj.transform.SetParent(panel.transform, false);
        var goldRect = goldObj.AddComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0.1f, 0.5f);
        goldRect.anchorMax = new Vector2(0.9f, 0.65f);
        goldRect.offsetMin = Vector2.zero;
        goldRect.offsetMax = Vector2.zero;
        lootGoldText = goldObj.AddComponent<TextMeshProUGUI>();
        lootGoldText.alignment = TextAlignmentOptions.Center;
        lootGoldText.fontSize = 24;
        lootGoldText.color = new Color(1f, 0.85f, 0.2f);

        var xpObj = new GameObject("XPText");
        xpObj.transform.SetParent(panel.transform, false);
        var xpRect = xpObj.AddComponent<RectTransform>();
        xpRect.anchorMin = new Vector2(0.1f, 0.3f);
        xpRect.anchorMax = new Vector2(0.9f, 0.5f);
        xpRect.offsetMin = Vector2.zero;
        xpRect.offsetMax = Vector2.zero;
        lootXPText = xpObj.AddComponent<TextMeshProUGUI>();
        lootXPText.alignment = TextAlignmentOptions.Center;
        lootXPText.fontSize = 24;
        lootXPText.color = new Color(0.4f, 0.9f, 1f);

        var btnObj = new GameObject("CollectButton");
        btnObj.transform.SetParent(panel.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.25f, 0.08f);
        btnRect.anchorMax = new Vector2(0.75f, 0.22f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.6f, 0.2f, 1f);
        collectLootButton = btnObj.AddComponent<Button>();
        collectLootButton.targetGraphic = btnImage;
        collectLootButton.onClick.AddListener(OnCollectLootClicked);

        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(btnObj.transform, false);
        var btnTextRect = btnTextObj.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.offsetMin = Vector2.zero;
        btnTextRect.offsetMax = Vector2.zero;
        var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "COLLECT";
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 22;
        btnText.color = Color.white;

        return panel;
    }

    public void ShowLootPanel(int gold, int xp, Player player, NodeBase node, string title = null)
    {
        pendingGold = gold;
        pendingXP = xp;
        pendingPlayer = player;
        pendingNode = node;

        if (lootTitleText != null)
        {
            lootTitleText.text = title ?? "VICTORY!";
        }
        lootGoldText.text = $"Gold: +{gold}";
        lootXPText.text = $"XP: +{xp}";

        combatPanel.SetActive(false);
        lootPanel.SetActive(true);
    }

    private void OnCollectLootClicked()
    {
        if (pendingPlayer != null)
        {
            pendingPlayer.AddGold(pendingGold);
            // XP is already added in CombatManager.EndCombat (Phase 1)
        }

        lootPanel.SetActive(false);

        if (combatManager == null)
        {
            combatManager = FindFirstObjectByType<CombatManager>();
        }

        if (combatManager != null)
        {
            combatManager.OnLootCollected();
        }
    }
}
