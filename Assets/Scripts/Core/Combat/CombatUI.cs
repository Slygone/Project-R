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
    
    // Reward system
    private GameObject rewardContainer;
    private Button goldRewardButton;
    private Button xpRewardButton;
    private GameObject sigilRewardButton;
    private Element pendingSigil = Element.None;
    private bool isEnchanting = false;
    private bool goldCollected = false;
    private TextMeshProUGUI sigilInstructionText;
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
    
    // New bottom bar components
    private GameObject healthCircle;
    private TextMeshProUGUI healthCircleText;
    private TextMeshProUGUI apText;
    private Image healthFillImage;
    private Image healthShieldImage;
    private Image energyFillImage;
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
        // Bottom bar container
        bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(parent, false);
        var bottomRect = bottomBar.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0.02f, 0.02f);
        bottomRect.anchorMax = new Vector2(0.98f, 0.22f);
        bottomRect.offsetMin = Vector2.zero;
        bottomRect.offsetMax = Vector2.zero;

        // Health Circle (LEFT side - red)
        CreateHealthCircle(bottomBar.transform);

        // Center section with AP display, End Turn button, and Skills
        CreateCenterSection(bottomBar.transform);

        // Energy Circle (RIGHT side - blue)
        CreateEnergyCircle(bottomBar.transform);
    }

    private void CreateHealthCircle(Transform parent)
    {
        healthCircle = new GameObject("HealthCircle");
        healthCircle.transform.SetParent(parent, false);
        var circleRect = healthCircle.AddComponent<RectTransform>();
        circleRect.anchorMin = new Vector2(0, 0.1f);
        circleRect.anchorMax = new Vector2(0.12f, 0.9f);
        circleRect.offsetMin = Vector2.zero;
        circleRect.offsetMax = Vector2.zero;

        // Background (dark empty state)
        var bgImage = healthCircle.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.1f, 0.1f, 1f);
        
        // Add outline for border effect
        var outline = healthCircle.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.2f, 0.2f, 1f);
        outline.effectDistance = new Vector2(2, 2);

        // Health fill (red) - fills from bottom up
        var healthFillObj = new GameObject("HealthFill");
        healthFillObj.transform.SetParent(healthCircle.transform, false);
        var healthFillRect = healthFillObj.AddComponent<RectTransform>();
        healthFillRect.anchorMin = new Vector2(0.08f, 0.08f);
        healthFillRect.anchorMax = new Vector2(0.92f, 0.92f);
        healthFillRect.offsetMin = Vector2.zero;
        healthFillRect.offsetMax = Vector2.zero;
        healthFillImage = healthFillObj.AddComponent<Image>();
        healthFillImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Vertical;
        healthFillImage.fillOrigin = 0; // Bottom
        healthFillImage.fillAmount = 1f;

        // Shield overlay (light blue, semi-transparent) - fills from bottom up on top of health
        var shieldFillObj = new GameObject("ShieldFill");
        shieldFillObj.transform.SetParent(healthCircle.transform, false);
        var shieldFillRect = shieldFillObj.AddComponent<RectTransform>();
        shieldFillRect.anchorMin = new Vector2(0.08f, 0.08f);
        shieldFillRect.anchorMax = new Vector2(0.92f, 0.92f);
        shieldFillRect.offsetMin = Vector2.zero;
        shieldFillRect.offsetMax = Vector2.zero;
        healthShieldImage = shieldFillObj.AddComponent<Image>();
        healthShieldImage.color = new Color(0.4f, 0.7f, 1f, 0.6f); // Light blue, semi-transparent
        healthShieldImage.type = Image.Type.Filled;
        healthShieldImage.fillMethod = Image.FillMethod.Vertical;
        healthShieldImage.fillOrigin = 0; // Bottom
        healthShieldImage.fillAmount = 0f; // No shield initially

        // Health number
        var healthNumObj = new GameObject("HealthNumber");
        healthNumObj.transform.SetParent(healthCircle.transform, false);
        var healthNumRect = healthNumObj.AddComponent<RectTransform>();
        healthNumRect.anchorMin = Vector2.zero;
        healthNumRect.anchorMax = Vector2.one;
        healthNumRect.offsetMin = Vector2.zero;
        healthNumRect.offsetMax = Vector2.zero;

        healthCircleText = healthNumObj.AddComponent<TextMeshProUGUI>();
        healthCircleText.text = "100";
        healthCircleText.fontSize = 24;
        healthCircleText.fontStyle = FontStyles.Bold;
        healthCircleText.alignment = TextAlignmentOptions.Center;
        healthCircleText.color = Color.white;
    }

    private void CreateCenterSection(Transform parent)
    {
        // Center container for AP, End Turn, and Skills
        var centerSection = new GameObject("CenterSection");
        centerSection.transform.SetParent(parent, false);
        var centerRect = centerSection.AddComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.14f, 0);
        centerRect.anchorMax = new Vector2(0.86f, 1);
        centerRect.offsetMin = Vector2.zero;
        centerRect.offsetMax = Vector2.zero;

        // AP Display (top) - "AP: X / Y"
        var apDisplayObj = new GameObject("APDisplay");
        apDisplayObj.transform.SetParent(centerSection.transform, false);
        var apRect = apDisplayObj.AddComponent<RectTransform>();
        apRect.anchorMin = new Vector2(0.3f, 0.78f);
        apRect.anchorMax = new Vector2(0.7f, 0.98f);
        apRect.offsetMin = Vector2.zero;
        apRect.offsetMax = Vector2.zero;

        apText = apDisplayObj.AddComponent<TextMeshProUGUI>();
        apText.text = "AP: 10 / 10";
        apText.fontSize = 24;
        apText.fontStyle = FontStyles.Bold;
        apText.alignment = TextAlignmentOptions.Center;
        apText.color = new Color(0.9f, 0.75f, 0.3f); // Golden color

        // End Turn Button (below AP display)
        var endTurnObj = new GameObject("EndTurnButton");
        endTurnObj.transform.SetParent(centerSection.transform, false);
        var btnRect = endTurnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.35f, 0.52f);
        btnRect.anchorMax = new Vector2(0.65f, 0.76f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnBg = endTurnObj.AddComponent<Image>();
        btnBg.color = new Color(0.5f, 0.15f, 0.15f, 1f); // Dark red

        endTurnButton = endTurnObj.AddComponent<Button>();
        endTurnButton.targetGraphic = btnBg;
        endTurnButton.onClick.AddListener(OnEndTurnClicked);

        // "END TURN" text
        var endTextObj = new GameObject("EndTurnText");
        endTextObj.transform.SetParent(endTurnObj.transform, false);
        var endTextRect = endTextObj.AddComponent<RectTransform>();
        endTextRect.anchorMin = Vector2.zero;
        endTextRect.anchorMax = Vector2.one;
        endTextRect.offsetMin = Vector2.zero;
        endTextRect.offsetMax = Vector2.zero;

        var endText = endTextObj.AddComponent<TextMeshProUGUI>();
        endText.text = "END TURN";
        endText.fontSize = 16;
        endText.fontStyle = FontStyles.Bold;
        endText.alignment = TextAlignmentOptions.Center;
        endText.color = Color.white;

        // Skills Container (bottom)
        var skillsContainer = new GameObject("SkillsContainer");
        skillsContainer.transform.SetParent(centerSection.transform, false);
        actionButtonContainer = skillsContainer;
        var skillsRect = skillsContainer.AddComponent<RectTransform>();
        skillsRect.anchorMin = new Vector2(0.02f, 0.02f);
        skillsRect.anchorMax = new Vector2(0.98f, 0.50f);
        skillsRect.offsetMin = Vector2.zero;
        skillsRect.offsetMax = Vector2.zero;

        // Dark background for skill cards
        var skillsBg = skillsContainer.AddComponent<Image>();
        skillsBg.color = new Color(0.08f, 0.07f, 0.06f, 0.9f);

        var skillsLayout = skillsContainer.AddComponent<HorizontalLayoutGroup>();
        skillsLayout.spacing = 6;
        skillsLayout.padding = new RectOffset(8, 8, 4, 4);
        skillsLayout.childAlignment = TextAnchor.MiddleCenter;
        skillsLayout.childControlWidth = true;
        skillsLayout.childControlHeight = true;
        skillsLayout.childForceExpandWidth = true;
        skillsLayout.childForceExpandHeight = true;

        // Create skill cards with hotkey and AP cost indicators
        skill1Button = CreateSkillCard(skillsContainer.transform, "Skill1", "1", new Color(0.25f, 0.25f, 0.3f, 1f), OnSkill1Clicked, out skill1Text, out skill1Tooltip);
        skill2Button = CreateSkillCard(skillsContainer.transform, "Skill2", "2", new Color(0.25f, 0.25f, 0.3f, 1f), OnSkill2Clicked, out skill2Text, out skill2Tooltip);
        skill3Button = CreateSkillCard(skillsContainer.transform, "Skill3", "3", new Color(0.25f, 0.25f, 0.3f, 1f), OnSkill3Clicked, out skill3Text, out skill3Tooltip);
        skill4Button = CreateSkillCard(skillsContainer.transform, "Skill4", "4", new Color(0.25f, 0.25f, 0.3f, 1f), OnSkill4Clicked, out skill4Text, out skill4Tooltip);
        skill5Button = CreateSkillCard(skillsContainer.transform, "Skill5", "5", new Color(0.25f, 0.25f, 0.3f, 1f), OnSkill5Clicked, out skill5Text, out skill5Tooltip);

        // Back button (hidden by default, shown during targeting)
        backButton = CreateSkillCard(skillsContainer.transform, "Back", "ESC", new Color(0.4f, 0.4f, 0.4f, 1f), OnBackClicked, out _, out _);
        backButton.gameObject.SetActive(false);
    }

    private void CreateEnergyCircle(Transform parent)
    {
        energyCircle = new GameObject("EnergyCircle");
        energyCircle.transform.SetParent(parent, false);
        var circleRect = energyCircle.AddComponent<RectTransform>();
        circleRect.anchorMin = new Vector2(0.88f, 0.1f);
        circleRect.anchorMax = new Vector2(1f, 0.9f);
        circleRect.offsetMin = Vector2.zero;
        circleRect.offsetMax = Vector2.zero;

        // Background (dark empty state)
        var bgImage = energyCircle.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.15f, 1f);
        
        // Add outline for border effect
        var outline = energyCircle.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.3f, 0.5f, 1f);
        outline.effectDistance = new Vector2(2, 2);

        // Energy fill (blue) - fills from bottom up
        var energyFillObj = new GameObject("EnergyFill");
        energyFillObj.transform.SetParent(energyCircle.transform, false);
        var energyFillRect = energyFillObj.AddComponent<RectTransform>();
        energyFillRect.anchorMin = new Vector2(0.08f, 0.08f);
        energyFillRect.anchorMax = new Vector2(0.92f, 0.92f);
        energyFillRect.offsetMin = Vector2.zero;
        energyFillRect.offsetMax = Vector2.zero;
        energyFillImage = energyFillObj.AddComponent<Image>();
        energyFillImage.color = new Color(0.3f, 0.5f, 0.9f, 1f);
        energyFillImage.type = Image.Type.Filled;
        energyFillImage.fillMethod = Image.FillMethod.Vertical;
        energyFillImage.fillOrigin = 0; // Bottom
        energyFillImage.fillAmount = 0f; // Start empty

        // Energy number
        var energyNumObj = new GameObject("EnergyNumber");
        energyNumObj.transform.SetParent(energyCircle.transform, false);
        var energyNumRect = energyNumObj.AddComponent<RectTransform>();
        energyNumRect.anchorMin = Vector2.zero;
        energyNumRect.anchorMax = Vector2.one;
        energyNumRect.offsetMin = Vector2.zero;
        energyNumRect.offsetMax = Vector2.zero;

        energyText = energyNumObj.AddComponent<TextMeshProUGUI>();
        energyText.text = "0";
        energyText.fontSize = 24;
        energyText.fontStyle = FontStyles.Bold;
        energyText.alignment = TextAlignmentOptions.Center;
        energyText.color = Color.white;
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
        UpdateAPDisplay(player);
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
            int currentAP = player.GetCurrentAP();
            
            // Skill 1 - show AP cost, cooldown if on cooldown, and DirtyStab stacks if applicable
            int cd1 = player.GetSkillCooldown(0);
            int ap1 = character.Skill1APCost;
            bool canUse1 = cd1 <= 0 && player.HasEnoughAP(ap1);
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
                if (skill1Text != null) skill1Text.text = $"{skill1Name}{dirtyStabIndicator}\n<size=10><color=#888888>CD: {cd1}</color></size>";
                if (skill1Button != null) skill1Button.interactable = false;
            }
            else
            {
                string apColor = player.HasEnoughAP(ap1) ? "#aaaaaa" : "#ff4444";
                if (skill1Text != null) skill1Text.text = $"<size=10><color={apColor}>{ap1} AP</color></size>\n{skill1Name}{dirtyStabIndicator}";
                if (skill1Button != null) skill1Button.interactable = canUse1;
            }
            
            // Skill 2 - show AP cost and cooldown
            int cd2 = player.GetSkillCooldown(1);
            int ap2 = character.Skill2APCost;
            bool canUse2 = cd2 <= 0 && player.HasEnoughAP(ap2);
            if (cd2 > 0)
            {
                if (skill2Text != null) skill2Text.text = $"{character.Skill2}\n<size=10><color=#888888>CD: {cd2}</color></size>";
                if (skill2Button != null) skill2Button.interactable = false;
            }
            else
            {
                string apColor = player.HasEnoughAP(ap2) ? "#aaaaaa" : "#ff4444";
                if (skill2Text != null) skill2Text.text = $"<size=10><color={apColor}>{ap2} AP</color></size>\n{character.Skill2}";
                if (skill2Button != null) skill2Button.interactable = canUse2;
            }
            
            // Skill 3 - show AP cost and cooldown
            int cd3 = player.GetSkillCooldown(2);
            int ap3 = character.Skill3APCost;
            bool canUse3 = cd3 <= 0 && player.HasEnoughAP(ap3);
            if (cd3 > 0)
            {
                if (skill3Text != null) skill3Text.text = $"{character.Skill3}\n<size=10><color=#888888>CD: {cd3}</color></size>";
                if (skill3Button != null) skill3Button.interactable = false;
            }
            else
            {
                string apColor = player.HasEnoughAP(ap3) ? "#aaaaaa" : "#ff4444";
                if (skill3Text != null) skill3Text.text = $"<size=10><color={apColor}>{ap3} AP</color></size>\n{character.Skill3}";
                if (skill3Button != null) skill3Button.interactable = canUse3;
            }
            
            // Skill 4 - show AP cost and cooldown
            int cd4 = player.GetSkillCooldown(3);
            int ap4 = character.Skill4APCost;
            bool canUse4 = cd4 <= 0 && player.HasEnoughAP(ap4);
            if (cd4 > 0)
            {
                if (skill4Text != null) skill4Text.text = $"{character.Skill4}\n<size=10><color=#888888>CD: {cd4}</color></size>";
                if (skill4Button != null) skill4Button.interactable = false;
            }
            else
            {
                string apColor = player.HasEnoughAP(ap4) ? "#aaaaaa" : "#ff4444";
                if (skill4Text != null) skill4Text.text = $"<size=10><color={apColor}>{ap4} AP</color></size>\n{character.Skill4}";
                if (skill4Button != null) skill4Button.interactable = canUse4;
            }
            
            // Skill 5 (Ultimate) - show AP cost, energy cost and cooldown
            int cd5 = player.GetSkillCooldown(4);
            int ap5 = character.Skill5APCost;
            int energyCost = character.Skill5EnergyCost;
            bool canUseUltimate = player.CanUseUltimate() && player.HasEnoughAP(ap5);
            
            string skill5Status = "";
            if (cd5 > 0)
            {
                skill5Status = $"\n<size=10><color=#888888>CD: {cd5}</color></size>";
            }
            else
            {
                string apColor = player.HasEnoughAP(ap5) ? "#aaaaaa" : "#ff4444";
                string energyColor = currentEnergy >= energyCost ? "#44ff44" : "#ff4444";
                skill5Status = $"<size=10><color={apColor}>{ap5} AP</color></size>\n{character.Skill5}\n<size=10><color={energyColor}>{currentEnergy}E</color></size>";
            }
            
            if (skill5Text != null) skill5Text.text = cd5 > 0 ? $"{character.Skill5}{skill5Status}" : skill5Status;
            if (skill5Button != null) skill5Button.interactable = canUseUltimate;
            
            // Update tooltips with AP cost
            if (skill1Tooltip != null) skill1Tooltip.SetTooltip($"<b>{character.Skill1}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill1)}\nAP Cost: {character.Skill1APCost}\nEnergy Gain: +{character.Skill1EnergyGain}\nCooldown: {character.Skill1Cooldown} turn(s)");
            if (skill2Tooltip != null) skill2Tooltip.SetTooltip($"<b>{character.Skill2}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill2)}\nAP Cost: {character.Skill2APCost}\nEnergy Gain: +{character.Skill2EnergyGain}\nCooldown: {character.Skill2Cooldown} turn(s)");
            if (skill3Tooltip != null) skill3Tooltip.SetTooltip($"<b>{character.Skill3}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill3)}\nAP Cost: {character.Skill3APCost}\nEnergy Gain: +{character.Skill3EnergyGain}\nCooldown: {character.Skill3Cooldown} turn(s)");
            if (skill4Tooltip != null) skill4Tooltip.SetTooltip($"<b>{character.Skill4}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill4)}\nAP Cost: {character.Skill4APCost}\nEnergy Gain: +{character.Skill4EnergyGain}\nCooldown: {character.Skill4Cooldown} turn(s)");
            if (skill5Tooltip != null) skill5Tooltip.SetTooltip($"<b>{character.Skill5}</b>\n{PlayerStatsUI.GetSkillDescription(character.Skill5)}\nAP Cost: {character.Skill5APCost}\nEnergy Cost: {character.Skill5EnergyCost}\nCooldown: {character.Skill5Cooldown} turn(s)");
            
            // Update skill button colors based on element enchantment
            UpdateSkillButtonElementColor(skill1Button, player.GetSkillElement(1));
            UpdateSkillButtonElementColor(skill2Button, player.GetSkillElement(2));
            UpdateSkillButtonElementColor(skill3Button, player.GetSkillElement(3));
            UpdateSkillButtonElementColor(skill4Button, player.GetSkillElement(4));
            UpdateSkillButtonElementColor(skill5Button, player.GetSkillElement(5));
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

        // Elemental marks display
        var marksObj = new GameObject("Marks");
        marksObj.transform.SetParent(slot.transform, false);
        var marksText = marksObj.AddComponent<TextMeshProUGUI>();
        marksText.text = "";
        marksText.alignment = TextAlignmentOptions.Center;
        marksText.fontSize = 12;
        marksText.color = Color.white;
        marksText.richText = true;
        var marksLayout = marksObj.AddComponent<LayoutElement>();
        marksLayout.preferredHeight = 18;

        enemySlots[enemy] = new EnemyUISlot
        {
            Root = slot,
            HealthFill = fillImage,
            HealthText = healthText,
            NameText = nameText,
            Arrow = arrowObj,
            ClickArea = clickBtn,
            MarksText = marksText
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
        
        // Update elemental marks display
        if (slot.MarksText != null)
        {
            var marks = enemy.GetMarks();
            if (marks.Count > 0)
            {
                var markStrings = new System.Collections.Generic.List<string>();
                foreach (var kvp in marks)
                {
                    string colorHex = ColorUtility.ToHtmlStringRGB(GetElementColor(kvp.Key));
                    markStrings.Add($"<color=#{colorHex}>{kvp.Key}:{kvp.Value}</color>");
                }
                slot.MarksText.text = string.Join(" ", markStrings);
            }
            else
            {
                slot.MarksText.text = "";
            }
        }

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
        int maxShield = player.GetMaxShield();
        string shieldText = shield > 0 ? $" <color=#44aaff>[+{shield}]</color>" : "";
        
        if (playerHealthText != null)
        {
            string hpColor = healthPercent > 0.5f ? "#55ff55" : (healthPercent > 0.25f ? "#ffff55" : "#ff5555");
            playerHealthText.text = $"<color=#cc3333>♥</color> <color={hpColor}>{player.GetHealth()}/{player.GetMaxHealth()}</color>{shieldText}";
        }
        
        // Update health circle fill in bottom bar
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = healthPercent;
        }
        
        // Update shield overlay - show proportional to max shield
        if (healthShieldImage != null)
        {
            if (shield > 0 && maxShield > 0)
            {
                float shieldPercent = (float)shield / maxShield;
                healthShieldImage.fillAmount = shieldPercent;
            }
            else
            {
                healthShieldImage.fillAmount = 0f;
            }
        }
        
        // Update health circle text - show health, or health+shield if shielded
        if (healthCircleText != null)
        {
            if (shield > 0)
            {
                healthCircleText.text = $"{player.GetHealth()}\n<size=14><color=#66ccff>+{shield}</color></size>";
            }
            else
            {
                healthCircleText.text = player.GetHealth().ToString();
            }
        }
        
        // Update gold display
        if (playerGoldText != null)
        {
            playerGoldText.text = $"<color=#ffcc00>G</color> {player.GetGold()}";
        }
    }
    
    public void UpdateEnergyDisplay(Player player)
    {
        int currentEnergy = player.GetEnergy();
        int maxEnergy = player.GetMaxEnergy();
        
        // Update energy fill amount
        if (energyFillImage != null && maxEnergy > 0)
        {
            float energyPercent = (float)currentEnergy / maxEnergy;
            energyFillImage.fillAmount = energyPercent;
        }
        
        // Update energy text
        if (energyText != null)
        {
            energyText.text = currentEnergy.ToString();
        }
    }
    
    public void UpdateAPDisplay(Player player)
    {
        if (apText != null)
        {
            apText.text = $"AP: {player.GetCurrentAP()} / {player.GetMaxAP()}";
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
        // New mark system: Skill colors are now based on element enchantments
        // This method now just ensures skill buttons have correct element colors
        var refs = FindFirstObjectByType<Referencer>();
        if (refs == null || refs.player == null) return;
        
        var player = refs.player;
        
        // Update skill button colors based on their element enchantments
        UpdateSkillButtonElementColor(skill1Button, player.GetSkillElement(1));
        UpdateSkillButtonElementColor(skill2Button, player.GetSkillElement(2));
        UpdateSkillButtonElementColor(skill3Button, player.GetSkillElement(3));
        UpdateSkillButtonElementColor(skill4Button, player.GetSkillElement(4));
        UpdateSkillButtonElementColor(skill5Button, player.GetSkillElement(5));
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
        public TextMeshProUGUI MarksText;
    }

    private GameObject CreateLootPanel(Transform parent)
    {
        var panel = new GameObject("LootPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.25f, 0.2f);
        rect.anchorMax = new Vector2(0.75f, 0.8f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Title: "Reward"
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.85f);
        titleRect.anchorMax = new Vector2(1, 0.98f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        lootTitleText = titleObj.AddComponent<TextMeshProUGUI>();
        lootTitleText.text = "Reward";
        lootTitleText.alignment = TextAlignmentOptions.Center;
        lootTitleText.fontSize = 36;
        lootTitleText.fontStyle = FontStyles.Bold;
        lootTitleText.color = new Color(1f, 0.85f, 0.2f);

        // Reward container for clickable items
        rewardContainer = new GameObject("RewardContainer");
        rewardContainer.transform.SetParent(panel.transform, false);
        var containerRect = rewardContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.05f, 0.25f);
        containerRect.anchorMax = new Vector2(0.95f, 0.82f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // Gold reward button (clickable)
        var goldBtnObj = new GameObject("GoldReward");
        goldBtnObj.transform.SetParent(rewardContainer.transform, false);
        var goldBtnRect = goldBtnObj.AddComponent<RectTransform>();
        goldBtnRect.anchorMin = new Vector2(0.05f, 0.7f);
        goldBtnRect.anchorMax = new Vector2(0.95f, 0.95f);
        goldBtnRect.offsetMin = Vector2.zero;
        goldBtnRect.offsetMax = Vector2.zero;
        var goldBtnImage = goldBtnObj.AddComponent<Image>();
        goldBtnImage.color = new Color(0.2f, 0.18f, 0.1f, 0.9f);
        goldRewardButton = goldBtnObj.AddComponent<Button>();
        goldRewardButton.targetGraphic = goldBtnImage;
        goldRewardButton.onClick.AddListener(OnGoldRewardClicked);

        var goldTextObj = new GameObject("Text");
        goldTextObj.transform.SetParent(goldBtnObj.transform, false);
        var goldTextRect = goldTextObj.AddComponent<RectTransform>();
        goldTextRect.anchorMin = Vector2.zero;
        goldTextRect.anchorMax = Vector2.one;
        goldTextRect.offsetMin = new Vector2(10, 0);
        goldTextRect.offsetMax = new Vector2(-10, 0);
        lootGoldText = goldTextObj.AddComponent<TextMeshProUGUI>();
        lootGoldText.alignment = TextAlignmentOptions.MidlineLeft;
        lootGoldText.fontSize = 22;
        lootGoldText.color = new Color(1f, 0.85f, 0.2f);

        // XP display (auto-collected, visual feedback)
        var xpBtnObj = new GameObject("XPReward");
        xpBtnObj.transform.SetParent(rewardContainer.transform, false);
        var xpBtnRect = xpBtnObj.AddComponent<RectTransform>();
        xpBtnRect.anchorMin = new Vector2(0.05f, 0.4f);
        xpBtnRect.anchorMax = new Vector2(0.95f, 0.65f);
        xpBtnRect.offsetMin = Vector2.zero;
        xpBtnRect.offsetMax = Vector2.zero;
        var xpBtnImage = xpBtnObj.AddComponent<Image>();
        xpBtnImage.color = new Color(0.1f, 0.15f, 0.2f, 0.9f);

        var xpTextObj = new GameObject("Text");
        xpTextObj.transform.SetParent(xpBtnObj.transform, false);
        var xpTextRect = xpTextObj.AddComponent<RectTransform>();
        xpTextRect.anchorMin = Vector2.zero;
        xpTextRect.anchorMax = Vector2.one;
        xpTextRect.offsetMin = new Vector2(10, 0);
        xpTextRect.offsetMax = new Vector2(-10, 0);
        lootXPText = xpTextObj.AddComponent<TextMeshProUGUI>();
        lootXPText.alignment = TextAlignmentOptions.MidlineLeft;
        lootXPText.fontSize = 22;
        lootXPText.color = new Color(0.4f, 0.9f, 1f);

        // Sigil reward button (for elite enemies - initially hidden)
        sigilRewardButton = new GameObject("SigilReward");
        sigilRewardButton.transform.SetParent(rewardContainer.transform, false);
        var sigilBtnRect = sigilRewardButton.AddComponent<RectTransform>();
        sigilBtnRect.anchorMin = new Vector2(0.05f, 0.1f);
        sigilBtnRect.anchorMax = new Vector2(0.95f, 0.35f);
        sigilBtnRect.offsetMin = Vector2.zero;
        sigilBtnRect.offsetMax = Vector2.zero;
        var sigilBtnImage = sigilRewardButton.AddComponent<Image>();
        sigilBtnImage.color = new Color(0.3f, 0.1f, 0.1f, 0.9f);
        var sigilBtn = sigilRewardButton.AddComponent<Button>();
        sigilBtn.targetGraphic = sigilBtnImage;
        sigilBtn.onClick.AddListener(OnSigilRewardClicked);

        var sigilTextObj = new GameObject("Text");
        sigilTextObj.transform.SetParent(sigilRewardButton.transform, false);
        var sigilTextRect = sigilTextObj.AddComponent<RectTransform>();
        sigilTextRect.anchorMin = Vector2.zero;
        sigilTextRect.anchorMax = Vector2.one;
        sigilTextRect.offsetMin = new Vector2(10, 0);
        sigilTextRect.offsetMax = new Vector2(-10, 0);
        var sigilText = sigilTextObj.AddComponent<TextMeshProUGUI>();
        sigilText.text = "Sigil"; // Will be updated with element
        sigilText.alignment = TextAlignmentOptions.MidlineLeft;
        sigilText.fontSize = 22;
        sigilText.color = Color.white;
        sigilRewardButton.SetActive(false);

        // Instruction text for sigil enchantment
        var instrObj = new GameObject("InstructionText");
        instrObj.transform.SetParent(panel.transform, false);
        var instrRect = instrObj.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0.05f, 0.15f);
        instrRect.anchorMax = new Vector2(0.95f, 0.24f);
        instrRect.offsetMin = Vector2.zero;
        instrRect.offsetMax = Vector2.zero;
        sigilInstructionText = instrObj.AddComponent<TextMeshProUGUI>();
        sigilInstructionText.text = "";
        sigilInstructionText.alignment = TextAlignmentOptions.Center;
        sigilInstructionText.fontSize = 18;
        sigilInstructionText.color = new Color(0.8f, 0.8f, 0.5f);

        // Continue button (shown after all rewards collected)
        var btnObj = new GameObject("ContinueButton");
        btnObj.transform.SetParent(panel.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.3f, 0.03f);
        btnRect.anchorMax = new Vector2(0.7f, 0.13f);
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
        btnText.text = "CONTINUE";
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.fontSize = 20;
        btnText.color = Color.white;

        return panel;
    }
    
    private void UpdateSkillButtonElementColor(Button button, Element element)
    {
        if (button == null) return;
        
        var image = button.GetComponent<Image>();
        if (image == null) return;
        
        if (element != Element.None)
        {
            // Tint the button with the element color (darker version for background)
            Color elemColor = GetElementColor(element);
            image.color = new Color(elemColor.r * 0.4f, elemColor.g * 0.4f, elemColor.b * 0.4f, 0.9f);
        }
        else
        {
            // Default dark gray for non-enchanted skills
            image.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        }
    }
    
    private void OnGoldRewardClicked()
    {
        if (goldCollected || pendingPlayer == null) return;
        
        pendingPlayer.AddGold(pendingGold);
        goldCollected = true;
        
        // Update visual to show collected
        if (lootGoldText != null)
        {
            lootGoldText.text = $"<color=#888888><s>Gold: +{pendingGold}</s> (Collected)</color>";
        }
        if (goldRewardButton != null)
        {
            goldRewardButton.interactable = false;
        }
    }
    
    private void OnSigilRewardClicked()
    {
        if (pendingSigil == Element.None) return;
        
        // Start enchantment mode - player needs to click a skill
        isEnchanting = true;
        if (sigilInstructionText != null)
        {
            sigilInstructionText.text = $"Click a skill to enchant with {pendingSigil}";
        }
        
        // Hide loot panel temporarily and show skill selection
        lootPanel.SetActive(false);
        ShowEnchantmentSkillSelection();
    }
    
    private void ShowEnchantmentSkillSelection()
    {
        // Show the bottom bar with skills for enchantment
        if (bottomBar != null) bottomBar.SetActive(true);
        
        // Create an overlay panel for skill selection
        var overlay = new GameObject("EnchantOverlay");
        overlay.transform.SetParent(lootPanel.transform.parent, false);
        var overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        
        // Semi-transparent background
        var overlayBg = overlay.AddComponent<Image>();
        overlayBg.color = new Color(0, 0, 0, 0.7f);
        
        // Instruction
        var instrObj = new GameObject("Instruction");
        instrObj.transform.SetParent(overlay.transform, false);
        var instrRect = instrObj.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0.2f, 0.6f);
        instrRect.anchorMax = new Vector2(0.8f, 0.8f);
        instrRect.offsetMin = Vector2.zero;
        instrRect.offsetMax = Vector2.zero;
        var instrText = instrObj.AddComponent<TextMeshProUGUI>();
        instrText.text = $"Select a skill to enchant with <color=#{ColorUtility.ToHtmlStringRGB(GetElementColor(pendingSigil))}>{pendingSigil}</color>";
        instrText.alignment = TextAlignmentOptions.Center;
        instrText.fontSize = 28;
        instrText.color = Color.white;
        
        // Create skill buttons for enchantment
        CreateEnchantmentSkillButtons(overlay.transform);
        
        enchantOverlay = overlay;
    }
    
    private GameObject enchantOverlay;
    
    private void CreateEnchantmentSkillButtons(Transform parent)
    {
        if (pendingPlayer == null) return;
        var character = pendingPlayer.GetCharacter();
        if (character == null) return;
        
        var buttonContainer = new GameObject("SkillButtons");
        buttonContainer.transform.SetParent(parent, false);
        var containerRect = buttonContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.25f);
        containerRect.anchorMax = new Vector2(0.9f, 0.55f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        string[] skillNames = { character.Skill1, character.Skill2, character.Skill3, character.Skill4, character.Skill5 };
        
        float buttonWidth = 0.18f;
        float spacing = 0.02f;
        float startX = 0.5f - (2.5f * buttonWidth + 2 * spacing);
        
        for (int i = 0; i < 5; i++)
        {
            int skillNum = i + 1;
            string skillName = skillNames[i];
            // Use runtime element from player (includes sigil enchantments)
            Element currElem = pendingPlayer.GetSkillElement(skillNum);
            
            var btnObj = new GameObject($"Skill{skillNum}Btn");
            btnObj.transform.SetParent(buttonContainer.transform, false);
            var btnRect = btnObj.AddComponent<RectTransform>();
            float xPos = startX + i * (buttonWidth + spacing);
            btnRect.anchorMin = new Vector2(xPos, 0.1f);
            btnRect.anchorMax = new Vector2(xPos + buttonWidth, 0.9f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;
            
            var btnImage = btnObj.AddComponent<Image>();
            // Color based on current element enchantment
            btnImage.color = currElem != Element.None ? GetElementColor(currElem) * 0.5f : new Color(0.25f, 0.25f, 0.3f);
            
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImage;
            btn.onClick.AddListener(() => OnEnchantSkillClicked(skillNum));
            
            // Skill name and element text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(3, 3);
            textRect.offsetMax = new Vector2(-3, -3);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            
            // Show skill name with current element enchantment
            if (currElem != Element.None)
            {
                string elemColorHex = ColorUtility.ToHtmlStringRGB(GetElementColor(currElem));
                text.text = $"<size=12>{skillNum}</size>\n{skillName}\n<color=#{elemColorHex}><b>[{currElem}]</b></color>";
            }
            else
            {
                text.text = $"<size=12>{skillNum}</size>\n{skillName}\n<color=#666666>[None]</color>";
            }
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 12;
            text.color = Color.white;
        }
        
        // Cancel button
        var cancelObj = new GameObject("CancelBtn");
        cancelObj.transform.SetParent(parent, false);
        var cancelRect = cancelObj.AddComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.4f, 0.08f);
        cancelRect.anchorMax = new Vector2(0.6f, 0.18f);
        cancelRect.offsetMin = Vector2.zero;
        cancelRect.offsetMax = Vector2.zero;
        var cancelImage = cancelObj.AddComponent<Image>();
        cancelImage.color = new Color(0.5f, 0.2f, 0.2f);
        var cancelBtn = cancelObj.AddComponent<Button>();
        cancelBtn.targetGraphic = cancelImage;
        cancelBtn.onClick.AddListener(OnCancelEnchantment);
        
        var cancelTextObj = new GameObject("Text");
        cancelTextObj.transform.SetParent(cancelObj.transform, false);
        var cancelTextRect = cancelTextObj.AddComponent<RectTransform>();
        cancelTextRect.anchorMin = Vector2.zero;
        cancelTextRect.anchorMax = Vector2.one;
        var cancelText = cancelTextObj.AddComponent<TextMeshProUGUI>();
        cancelText.text = "Cancel";
        cancelText.alignment = TextAlignmentOptions.Center;
        cancelText.fontSize = 16;
        cancelText.color = Color.white;
    }
    
    private Element ParseElement(string elementStr)
    {
        if (string.IsNullOrEmpty(elementStr) || elementStr.ToLower() == "none") return Element.None;
        if (System.Enum.TryParse<Element>(elementStr, true, out Element result)) return result;
        return Element.None;
    }
    
    private void OnEnchantSkillClicked(int skillNumber)
    {
        if (!isEnchanting || pendingSigil == Element.None || pendingPlayer == null) return;
        
        // Apply enchantment to the skill
        pendingPlayer.EnchantSkill(skillNumber, pendingSigil);
        
        // Clean up
        isEnchanting = false;
        pendingSigil = Element.None;
        
        if (enchantOverlay != null)
        {
            Destroy(enchantOverlay);
            enchantOverlay = null;
        }
        
        // Hide sigil button and show loot panel again
        if (sigilRewardButton != null)
        {
            var sigilText = sigilRewardButton.GetComponentInChildren<TextMeshProUGUI>();
            if (sigilText != null)
            {
                sigilText.text = "<color=#888888><s>Sigil</s> (Used)</color>";
            }
            sigilRewardButton.GetComponent<Button>().interactable = false;
        }
        
        lootPanel.SetActive(true);
        if (sigilInstructionText != null)
        {
            sigilInstructionText.text = $"Skill {skillNumber} enchanted!";
        }
    }
    
    private void OnCancelEnchantment()
    {
        isEnchanting = false;
        
        if (enchantOverlay != null)
        {
            Destroy(enchantOverlay);
            enchantOverlay = null;
        }
        
        lootPanel.SetActive(true);
        if (sigilInstructionText != null)
        {
            sigilInstructionText.text = "";
        }
    }

    public void ShowLootPanel(int gold, int xp, Player player, NodeBase node, string title = null, bool isElite = false)
    {
        pendingGold = gold;
        pendingXP = xp;
        pendingPlayer = player;
        pendingNode = node;
        goldCollected = false;
        pendingSigil = Element.None;
        isEnchanting = false;

        if (lootTitleText != null)
        {
            lootTitleText.text = "Reward";
        }
        
        // Reset gold button
        if (goldRewardButton != null)
        {
            goldRewardButton.interactable = true;
        }
        if (lootGoldText != null)
        {
            lootGoldText.text = $"<color=#FFD700>◆</color> Gold: +{gold} <size=16>(Click to collect)</size>";
        }
        
        // XP is auto-collected, show as already gained
        if (lootXPText != null)
        {
            lootXPText.text = $"<color=#66CCFF>★</color> Experience: +{xp} <size=16>(Auto)</size>";
        }
        
        // Sigil for elite enemies
        if (sigilRewardButton != null)
        {
            if (isElite)
            {
                // Random element sigil drop
                var elements = new Element[] { Element.Fire, Element.Ice, Element.Water, Element.Wind, Element.Rock };
                pendingSigil = elements[UnityEngine.Random.Range(0, elements.Length)];
                
                var sigilBtnImage = sigilRewardButton.GetComponent<Image>();
                if (sigilBtnImage != null)
                {
                    sigilBtnImage.color = GetElementColor(pendingSigil) * 0.4f;
                }
                
                var sigilText = sigilRewardButton.GetComponentInChildren<TextMeshProUGUI>();
                if (sigilText != null)
                {
                    sigilText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(GetElementColor(pendingSigil))}>◈</color> {pendingSigil} Sigil <size=16>(Click to enchant a skill)</size>";
                    sigilText.color = GetElementColor(pendingSigil);
                }
                
                sigilRewardButton.GetComponent<Button>().interactable = true;
                sigilRewardButton.SetActive(true);
            }
            else
            {
                sigilRewardButton.SetActive(false);
            }
        }
        
        if (sigilInstructionText != null)
        {
            sigilInstructionText.text = "";
        }

        combatPanel.SetActive(false);
        lootPanel.SetActive(true);
    }

    private void OnCollectLootClicked()
    {
        // Auto-collect gold if not already collected
        if (!goldCollected && pendingPlayer != null)
        {
            pendingPlayer.AddGold(pendingGold);
            goldCollected = true;
        }

        lootPanel.SetActive(false);
        
        // Clean up any enchant overlay
        if (enchantOverlay != null)
        {
            Destroy(enchantOverlay);
            enchantOverlay = null;
        }

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
