using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Phase 3 Combat UI - Complete rework with:
/// - Player on left (exploration block style with character color)
/// - Enemies on right
/// - Nameplates above units
/// - HP: Green base + red missing overlay with smooth animation
/// - Energy: Gray base + blue gained overlay with smooth animation
/// - No top bar (character name/health/gold removed)
/// - Potions & Relics container preserved
/// </summary>
public class CombatUI : MonoBehaviour
{
    // Animation settings
    private const float BAR_ANIMATION_SPEED = 2.5f;
    
    // Core panels
    private GameObject combatPanel;
    private GameObject lootPanel;
    
    // Player area (left side)
    private GameObject playerBlock;
    private Image playerBlockImage;
    private TextMeshProUGUI playerNameplate;
    private GameObject playerHealthBarContainer;
    private Image playerHealthGreenFill;
    private Image playerHealthRedOverlay;
    private TextMeshProUGUI playerHealthText;
    private GameObject playerEnergyBarContainer;
    private Image playerEnergyGrayBase;
    private Image playerEnergyBlueFill;
    private TextMeshProUGUI playerEnergyText;
    private Image playerShieldOverlay;
    
    // Animation state
    private float targetHealthPercent = 1f;
    private float currentHealthPercent = 1f;
    private float targetEnergyPercent = 0f;
    private float currentEnergyPercent = 0f;
    
    // Enemy area (right side)
    private Transform enemyContainer;
    private Dictionary<CombatEnemy, EnemyUISlot> enemySlots = new Dictionary<CombatEnemy, EnemyUISlot>();
    
    // Bottom action bar
    private GameObject bottomBar;
    private Button skill1Button, skill2Button, skill3Button, skill4Button, skill5Button;
    private TextMeshProUGUI skill1Text, skill2Text, skill3Text, skill4Text, skill5Text;
    private TooltipTrigger skill1Tooltip, skill2Tooltip, skill3Tooltip, skill4Tooltip, skill5Tooltip;
    private TextMeshProUGUI apText;
    private Button endTurnButton;
    private Button backButton;
    private GameObject actionButtonContainer;
    
    // Potion container
    private GameObject potionContainer;
    private List<Button> potionButtons = new List<Button>();
    private List<TextMeshProUGUI> potionTexts = new List<TextMeshProUGUI>();
    private GameObject potionTooltipPanel;
    private TextMeshProUGUI potionTooltipText;
    private int selectedPotionIndex = -1;
    private Element selectedPotionElement = Element.None;
    private bool isPotionTargeting = false;
    
    // Relics panel
    private GameObject relicsPanel;
    private GameObject relicsContainer;
    private Button relicScrollLeftButton;
    private Button relicScrollRightButton;
    private List<GameObject> relicIcons = new List<GameObject>();
    private int relicScrollIndex = 0;
    private const int MAX_VISIBLE_RELICS = 5;
    
    // Combat state
    private CombatManager combatManager;
    private Player currentPlayer;
    private List<CombatEnemy> currentEnemies = new List<CombatEnemy>();
    private bool isTargeting = false;
    private int selectedSkillNumber = 0;
    private int selectedTargetIndex = 0;
    private bool isAoESkill = false;
    
    // Loot panel
    private TextMeshProUGUI lootGoldText;
    private TextMeshProUGUI lootXPText;
    private TextMeshProUGUI lootTitleText;
    private Button collectLootButton;
    private Button goldRewardButton;
    private GameObject sigilRewardButton;
    private GameObject relicRewardButton;
    private TextMeshProUGUI sigilInstructionText;
    private int pendingGold;
    private int pendingXP;
    private Player pendingPlayer;
    private NodeBase pendingNode;
    private Element pendingSigil = Element.None;
    private RelicData pendingRelic = null;
    private bool isEnchanting = false;
    private bool goldCollected = false;
    private bool relicCollected = false;
    private GameObject enchantOverlay;
    private GameObject rewardContainer;

    void Awake()
    {
        combatManager = FindFirstObjectByType<CombatManager>();
        SetupUI();
    }

    void Update()
    {
        // Animate HP and Energy bars
        AnimateBars();
        
        // Handle input
        if (combatPanel != null && combatPanel.activeSelf && !isTargeting)
        {
            HandleSkillHotkeys();
            return;
        }

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
    
    private void AnimateBars()
    {
        // Smoothly animate health bar
        if (!Mathf.Approximately(currentHealthPercent, targetHealthPercent))
        {
            currentHealthPercent = Mathf.MoveTowards(currentHealthPercent, targetHealthPercent, BAR_ANIMATION_SPEED * Time.deltaTime);
            if (playerHealthGreenFill != null)
            {
                playerHealthGreenFill.fillAmount = currentHealthPercent;
            }
            if (playerHealthRedOverlay != null)
            {
                playerHealthRedOverlay.fillAmount = 1f - currentHealthPercent;
            }
        }
        
        // Smoothly animate energy bar
        if (!Mathf.Approximately(currentEnergyPercent, targetEnergyPercent))
        {
            currentEnergyPercent = Mathf.MoveTowards(currentEnergyPercent, targetEnergyPercent, BAR_ANIMATION_SPEED * Time.deltaTime);
            if (playerEnergyBlueFill != null)
            {
                playerEnergyBlueFill.fillAmount = currentEnergyPercent;
            }
        }
    }

    private void HandleSkillHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            if (skill1Button != null && skill1Button.interactable) OnSkill1Clicked();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            if (skill2Button != null && skill2Button.interactable) OnSkill2Clicked();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            if (skill3Button != null && skill3Button.interactable) OnSkill3Clicked();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            if (skill4Button != null && skill4Button.interactable) OnSkill4Clicked();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
        {
            if (skill5Button != null && skill5Button.interactable) OnSkill5Clicked();
        }
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            GameLog.Error(GameLogCategory.System, "[CombatUI]", "SetupFail | reason=CanvasNotFound");
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

        // Dark background
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 1f);

        // Create UI sections - NO TOP BAR (removed per requirements)
        CreatePlayerArea(panel.transform);      // Left side - player block
        CreateEnemyContainer(panel.transform);  // Right side - enemies
        CreatePotionContainer(panel.transform); // Top-left potions
        CreateRelicsPanel(panel.transform);     // Top-right relics
        CreateBottomActionBar(panel.transform); // Bottom - skills, AP, end turn

        return panel;
    }

    #region Player Area (Left Side)
    
    private void CreatePlayerArea(Transform parent)
    {
        // Player container on the left side
        var playerArea = new GameObject("PlayerArea");
        playerArea.transform.SetParent(parent, false);
        var areaRect = playerArea.AddComponent<RectTransform>();
        areaRect.anchorMin = new Vector2(0.02f, 0.25f);
        areaRect.anchorMax = new Vector2(0.28f, 0.88f);
        areaRect.offsetMin = Vector2.zero;
        areaRect.offsetMax = Vector2.zero;

        // Player block (exploration style)
        playerBlock = new GameObject("PlayerBlock");
        playerBlock.transform.SetParent(playerArea.transform, false);
        var blockRect = playerBlock.AddComponent<RectTransform>();
        blockRect.anchorMin = new Vector2(0.1f, 0.25f);
        blockRect.anchorMax = new Vector2(0.9f, 0.85f);
        blockRect.offsetMin = Vector2.zero;
        blockRect.offsetMax = Vector2.zero;

        playerBlockImage = playerBlock.AddComponent<Image>();
        playerBlockImage.color = new Color(0.3f, 0.5f, 0.7f, 1f); // Default blue, will be updated with character color
        
        // Add outline
        var outline = playerBlock.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.3f, 0.4f, 1f);
        outline.effectDistance = new Vector2(3, 3);

        // Player nameplate (above block)
        var nameplateObj = new GameObject("PlayerNameplate");
        nameplateObj.transform.SetParent(playerArea.transform, false);
        var nameplateRect = nameplateObj.AddComponent<RectTransform>();
        nameplateRect.anchorMin = new Vector2(0f, 0.88f);
        nameplateRect.anchorMax = new Vector2(1f, 0.98f);
        nameplateRect.offsetMin = Vector2.zero;
        nameplateRect.offsetMax = Vector2.zero;

        playerNameplate = nameplateObj.AddComponent<TextMeshProUGUI>();
        playerNameplate.text = "PLAYER";
        playerNameplate.fontSize = 24;
        playerNameplate.fontStyle = FontStyles.Bold;
        playerNameplate.alignment = TextAlignmentOptions.Center;
        playerNameplate.color = Color.white;

        // Health bar container (below block)
        CreatePlayerHealthBar(playerArea.transform);
        
        // Energy bar container (below health)
        CreatePlayerEnergyBar(playerArea.transform);
    }

    private void CreatePlayerHealthBar(Transform parent)
    {
        playerHealthBarContainer = new GameObject("HealthBarContainer");
        playerHealthBarContainer.transform.SetParent(parent, false);
        var containerRect = playerHealthBarContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.05f, 0.12f);
        containerRect.anchorMax = new Vector2(0.95f, 0.22f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // Background (dark)
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(playerHealthBarContainer.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Green fill (current health) - fills from left
        var greenFillObj = new GameObject("GreenFill");
        greenFillObj.transform.SetParent(playerHealthBarContainer.transform, false);
        var greenRect = greenFillObj.AddComponent<RectTransform>();
        greenRect.anchorMin = new Vector2(0.02f, 0.1f);
        greenRect.anchorMax = new Vector2(0.98f, 0.9f);
        greenRect.offsetMin = Vector2.zero;
        greenRect.offsetMax = Vector2.zero;
        playerHealthGreenFill = greenFillObj.AddComponent<Image>();
        playerHealthGreenFill.color = new Color(0.2f, 0.7f, 0.2f, 1f); // Green
        playerHealthGreenFill.type = Image.Type.Filled;
        playerHealthGreenFill.fillMethod = Image.FillMethod.Horizontal;
        playerHealthGreenFill.fillOrigin = 0; // Left
        playerHealthGreenFill.fillAmount = 1f;

        // Red overlay (missing health) - fills from right
        var redOverlayObj = new GameObject("RedOverlay");
        redOverlayObj.transform.SetParent(playerHealthBarContainer.transform, false);
        var redRect = redOverlayObj.AddComponent<RectTransform>();
        redRect.anchorMin = new Vector2(0.02f, 0.1f);
        redRect.anchorMax = new Vector2(0.98f, 0.9f);
        redRect.offsetMin = Vector2.zero;
        redRect.offsetMax = Vector2.zero;
        playerHealthRedOverlay = redOverlayObj.AddComponent<Image>();
        playerHealthRedOverlay.color = new Color(0.7f, 0.15f, 0.15f, 0.8f); // Red
        playerHealthRedOverlay.type = Image.Type.Filled;
        playerHealthRedOverlay.fillMethod = Image.FillMethod.Horizontal;
        playerHealthRedOverlay.fillOrigin = 1; // Right (fills missing portion from right)
        playerHealthRedOverlay.fillAmount = 0f;

        // Shield overlay (light blue, on top of health)
        var shieldObj = new GameObject("ShieldOverlay");
        shieldObj.transform.SetParent(playerHealthBarContainer.transform, false);
        var shieldRect = shieldObj.AddComponent<RectTransform>();
        shieldRect.anchorMin = new Vector2(0.02f, 0.1f);
        shieldRect.anchorMax = new Vector2(0.98f, 0.9f);
        shieldRect.offsetMin = Vector2.zero;
        shieldRect.offsetMax = Vector2.zero;
        playerShieldOverlay = shieldObj.AddComponent<Image>();
        playerShieldOverlay.color = new Color(0.4f, 0.7f, 1f, 0.5f);
        playerShieldOverlay.type = Image.Type.Filled;
        playerShieldOverlay.fillMethod = Image.FillMethod.Horizontal;
        playerShieldOverlay.fillOrigin = 0;
        playerShieldOverlay.fillAmount = 0f;

        // Health text
        var textObj = new GameObject("HealthText");
        textObj.transform.SetParent(playerHealthBarContainer.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        playerHealthText = textObj.AddComponent<TextMeshProUGUI>();
        playerHealthText.text = "100 / 100";
        playerHealthText.fontSize = 16;
        playerHealthText.fontStyle = FontStyles.Bold;
        playerHealthText.alignment = TextAlignmentOptions.Center;
        playerHealthText.color = Color.white;
    }

    private void CreatePlayerEnergyBar(Transform parent)
    {
        playerEnergyBarContainer = new GameObject("EnergyBarContainer");
        playerEnergyBarContainer.transform.SetParent(parent, false);
        var containerRect = playerEnergyBarContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.05f, 0.01f);
        containerRect.anchorMax = new Vector2(0.95f, 0.10f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // Gray base (empty energy)
        var grayBaseObj = new GameObject("GrayBase");
        grayBaseObj.transform.SetParent(playerEnergyBarContainer.transform, false);
        var grayRect = grayBaseObj.AddComponent<RectTransform>();
        grayRect.anchorMin = Vector2.zero;
        grayRect.anchorMax = Vector2.one;
        grayRect.offsetMin = Vector2.zero;
        grayRect.offsetMax = Vector2.zero;
        playerEnergyGrayBase = grayBaseObj.AddComponent<Image>();
        playerEnergyGrayBase.color = new Color(0.25f, 0.25f, 0.25f, 1f); // Gray

        // Blue fill (current energy) - fills from left
        var blueFillObj = new GameObject("BlueFill");
        blueFillObj.transform.SetParent(playerEnergyBarContainer.transform, false);
        var blueRect = blueFillObj.AddComponent<RectTransform>();
        blueRect.anchorMin = new Vector2(0.02f, 0.1f);
        blueRect.anchorMax = new Vector2(0.98f, 0.9f);
        blueRect.offsetMin = Vector2.zero;
        blueRect.offsetMax = Vector2.zero;
        playerEnergyBlueFill = blueFillObj.AddComponent<Image>();
        playerEnergyBlueFill.color = new Color(0.2f, 0.5f, 0.9f, 1f); // Blue
        playerEnergyBlueFill.type = Image.Type.Filled;
        playerEnergyBlueFill.fillMethod = Image.FillMethod.Horizontal;
        playerEnergyBlueFill.fillOrigin = 0; // Left
        playerEnergyBlueFill.fillAmount = 0f; // Starts empty

        // Energy text
        var textObj = new GameObject("EnergyText");
        textObj.transform.SetParent(playerEnergyBarContainer.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        playerEnergyText = textObj.AddComponent<TextMeshProUGUI>();
        playerEnergyText.text = "0 / 100";
        playerEnergyText.fontSize = 14;
        playerEnergyText.fontStyle = FontStyles.Bold;
        playerEnergyText.alignment = TextAlignmentOptions.Center;
        playerEnergyText.color = Color.white;
    }
    
    #endregion

    #region Enemy Area (Right Side)
    
    private void CreateEnemyContainer(Transform parent)
    {
        var container = new GameObject("EnemyContainer");
        container.transform.SetParent(parent, false);

        var rect = container.AddComponent<RectTransform>();
        // Right side of screen
        rect.anchorMin = new Vector2(0.32f, 0.25f);
        rect.anchorMax = new Vector2(0.98f, 0.88f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        enemyContainer = container.transform;
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
        
        var eventTrigger = slot.AddComponent<EventTrigger>();
        var pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => OnEnemyHover(capturedEnemy));
        eventTrigger.triggers.Add(pointerEnter);

        var layout = slot.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        // Target arrow (hidden by default)
        var arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(slot.transform, false);
        var arrowLayout = arrowObj.AddComponent<LayoutElement>();
        arrowLayout.preferredHeight = 30;
        arrowLayout.preferredWidth = 40;
        var arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
        arrowText.text = "▼";
        arrowText.alignment = TextAlignmentOptions.Center;
        arrowText.fontSize = 28;
        arrowText.color = new Color(1f, 0.8f, 0.2f);
        arrowObj.SetActive(false);

        // Nameplate (above enemy)
        var nameObj = new GameObject("Nameplate");
        nameObj.transform.SetParent(slot.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = enemy.Name;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 22;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;
        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 30;

        // Element indicator
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

        // Health bar background
        var healthBarBg = new GameObject("HealthBarBg");
        healthBarBg.transform.SetParent(slot.transform, false);
        var healthBgImage = healthBarBg.AddComponent<Image>();
        healthBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var healthBgLayout = healthBarBg.AddComponent<LayoutElement>();
        healthBgLayout.preferredHeight = 20;

        // Health fill
        var healthFill = new GameObject("Fill");
        healthFill.transform.SetParent(healthBarBg.transform, false);
        var fillRect = healthFill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        var fillImage = healthFill.AddComponent<Image>();
        fillImage.color = Color.red;

        // Health text
        var healthTextObj = new GameObject("HealthText");
        healthTextObj.transform.SetParent(slot.transform, false);
        var healthText = healthTextObj.AddComponent<TextMeshProUGUI>();
        healthText.text = $"{enemy.Health}/{enemy.MaxHealth}";
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.fontSize = 16;
        healthText.color = Color.white;
        var healthTextLayout = healthTextObj.AddComponent<LayoutElement>();
        healthTextLayout.preferredHeight = 25;

        // Damage display
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
    
    #endregion

    #region Potions & Relics (Preserved)
    
    private void CreatePotionContainer(Transform parent)
    {
        potionContainer = new GameObject("PotionContainer");
        potionContainer.transform.SetParent(parent, false);
        var containerRect = potionContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.01f, 0.90f);
        containerRect.anchorMax = new Vector2(0.15f, 0.98f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        var layout = potionContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.padding = new RectOffset(2, 2, 1, 1);

        var bg = potionContainer.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.5f);
        
        CreatePotionTooltip(potionContainer.transform);
    }

    private void CreatePotionTooltip(Transform parent)
    {
        potionTooltipPanel = new GameObject("PotionTooltip");
        potionTooltipPanel.transform.SetParent(parent, false);
        var rect = potionTooltipPanel.AddComponent<RectTransform>();
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

    private void CreateRelicsPanel(Transform parent)
    {
        relicsPanel = new GameObject("RelicsPanel");
        relicsPanel.transform.SetParent(parent, false);
        var panelRect = relicsPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.sizeDelta = new Vector2(340, 60);
        panelRect.anchoredPosition = new Vector2(-15, -10);
        
        var panelBg = relicsPanel.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
        
        var outline = relicsPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.4f, 0.5f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);
        
        // Left scroll button
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
        
        // Relic icons container
        relicsContainer = new GameObject("RelicsContainer");
        relicsContainer.transform.SetParent(relicsPanel.transform, false);
        var containerRect = relicsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.offsetMin = new Vector2(32, 5);
        containerRect.offsetMax = new Vector2(-32, -5);
        
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
        
        // Right scroll button
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
    
    private void OnRelicScrollLeft()
    {
        if (relicScrollIndex > 0)
        {
            relicScrollIndex--;
            if (currentPlayer != null) RefreshRelicsDisplay(currentPlayer);
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
    
    #endregion

    #region Bottom Action Bar
    
    private void CreateBottomActionBar(Transform parent)
    {
        bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(parent, false);
        var bottomRect = bottomBar.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0.02f, 0.02f);
        bottomRect.anchorMax = new Vector2(0.98f, 0.22f);
        bottomRect.offsetMin = Vector2.zero;
        bottomRect.offsetMax = Vector2.zero;

        // AP Display (top center)
        var apDisplayObj = new GameObject("APDisplay");
        apDisplayObj.transform.SetParent(bottomBar.transform, false);
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
        apText.color = new Color(0.9f, 0.75f, 0.3f);

        // End Turn Button
        var endTurnObj = new GameObject("EndTurnButton");
        endTurnObj.transform.SetParent(bottomBar.transform, false);
        var btnRect = endTurnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.35f, 0.52f);
        btnRect.anchorMax = new Vector2(0.65f, 0.76f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnBg = endTurnObj.AddComponent<Image>();
        btnBg.color = new Color(0.5f, 0.15f, 0.15f, 1f);

        endTurnButton = endTurnObj.AddComponent<Button>();
        endTurnButton.targetGraphic = btnBg;
        endTurnButton.onClick.AddListener(OnEndTurnClicked);

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

        // Skills Container
        var skillsContainer = new GameObject("SkillsContainer");
        skillsContainer.transform.SetParent(bottomBar.transform, false);
        actionButtonContainer = skillsContainer;
        var skillsRect = skillsContainer.AddComponent<RectTransform>();
        skillsRect.anchorMin = new Vector2(0.02f, 0.02f);
        skillsRect.anchorMax = new Vector2(0.98f, 0.50f);
        skillsRect.offsetMin = Vector2.zero;
        skillsRect.offsetMax = Vector2.zero;

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

        skill1Button = CreateSkillCard(skillsContainer.transform, "Skill1", "1", new Color(0.25f, 0.25f, 0.3f, 1f), OnSkill1Clicked, out skill1Text, out skill1Tooltip);
        skill2Button = CreateSkillCard(skillsContainer.transform, "Skill2", "2", new Color(0.25f, 0.25f, 0.3f, 1f), OnSkill2Clicked, out skill2Text, out skill2Tooltip);
        skill3Button = CreateSkillCard(skillsContainer.transform, "Skill3", "3", new Color(0.25f, 0.25f, 0.3f, 1f), OnSkill3Clicked, out skill3Text, out skill3Tooltip);
        skill4Button = CreateSkillCard(skillsContainer.transform, "Skill4", "4", new Color(0.25f, 0.25f, 0.3f, 1f), OnSkill4Clicked, out skill4Text, out skill4Tooltip);
        skill5Button = CreateSkillCard(skillsContainer.transform, "Skill5", "5", new Color(0.25f, 0.25f, 0.3f, 1f), OnSkill5Clicked, out skill5Text, out skill5Tooltip);

        backButton = CreateSkillCard(skillsContainer.transform, "Back", "ESC", new Color(0.4f, 0.4f, 0.4f, 1f), OnBackClicked, out _, out _);
        backButton.gameObject.SetActive(false);
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

        // Hotkey indicator
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

        // Skill name text
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
        textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        textComponent.overflowMode = TextOverflowModes.Ellipsis;

        // Energy cost
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
    
    #endregion

    #region Skill Button Events
    
    private void OnSkill1Clicked() { EnterTargetingMode(1, IsSkillAoE(1)); }
    private void OnSkill2Clicked() { EnterTargetingMode(2, IsSkillAoE(2)); }
    private void OnSkill3Clicked() { EnterTargetingMode(3, IsSkillAoE(3)); }
    private void OnSkill4Clicked() { EnterTargetingMode(4, IsSkillAoE(4)); }
    private void OnSkill5Clicked() { EnterTargetingMode(5, IsSkillAoE(5)); }
    private void OnBackClicked()
    {
        if (isPotionTargeting) ExitPotionTargetingMode();
        else ExitTargetingMode();
    }
    private void OnEndTurnClicked()
    {
        if (combatManager != null) combatManager.EndPlayerTurn();
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
        
        string skillId = $"skill_{skillName.ToLower()}";
        var skillDef = GameDataLoader.GetSkill(skillId);
        if (skillDef?.executions != null)
        {
            foreach (var exec in skillDef.executions)
            {
                if (exec.target?.selector == "AllEnemies" && exec.effectId == "eff_deal_damage")
                    return true;
            }
        }
        return false;
    }
    
    #endregion

    #region Targeting System
    
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
                ConfirmPotionTarget(refs.player);
            return;
        }
        
        var aliveEnemies = GetAliveEnemies();
        if (aliveEnemies.Count == 0 || selectedTargetIndex >= aliveEnemies.Count) return;
        
        var target = aliveEnemies[selectedTargetIndex];
        
        if (combatManager == null)
            combatManager = FindFirstObjectByType<CombatManager>();
        
        if (combatManager != null)
        {
            if (selectedSkillNumber == 0)
                combatManager.OnPlayerAttackTarget(target);
            else
                combatManager.OnPlayerSkillTarget(selectedSkillNumber, target);
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
                    if (arrowImage != null) arrowImage.color = new Color(1f, 0.8f, 0.2f);
                    slot.Arrow.transform.localScale = Vector3.one;
                }
            }
        }
    }

    private void HideAllArrows()
    {
        foreach (var slot in enemySlots.Values)
        {
            if (slot.Arrow != null) slot.Arrow.SetActive(false);
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
    
    #endregion

    #region Public API
    
    public void ShowCombat(List<CombatEnemy> enemies, Player player, string title = null)
    {
        if (combatPanel == null) SetupUI();
        currentEnemies = new List<CombatEnemy>(enemies);
        currentPlayer = player;
        isTargeting = false;
        ClearEnemySlots();
        foreach (var enemy in enemies) CreateEnemySlot(enemy);

        var character = player.GetCharacter();
        if (character != null)
        {
            if (playerNameplate != null) playerNameplate.text = character.DisplayName.ToUpper();
            if (playerBlockImage != null) playerBlockImage.color = GetCharacterColor(character.DisplayName);
        }

        float healthPct = (float)player.GetHealth() / player.GetMaxHealth();
        currentHealthPercent = healthPct; targetHealthPercent = healthPct;
        if (playerHealthGreenFill != null) playerHealthGreenFill.fillAmount = healthPct;
        if (playerHealthRedOverlay != null) playerHealthRedOverlay.fillAmount = 1f - healthPct;
        
        float energyPct = player.GetMaxEnergy() > 0 ? (float)player.GetEnergy() / player.GetMaxEnergy() : 0f;
        currentEnergyPercent = energyPct; targetEnergyPercent = energyPct;
        if (playerEnergyBlueFill != null) playerEnergyBlueFill.fillAmount = energyPct;

        UpdateSkillButtons(player); UpdatePlayerHealth(player); UpdateEnergyDisplay(player); UpdateAPDisplay(player);
        RefreshPotionButtons(player); RefreshRelicsDisplay(player);
        combatPanel.SetActive(true); SetPlayerTurn(true);
        isPotionTargeting = false; selectedPotionIndex = -1; selectedPotionElement = Element.None;
        if (backButton != null) backButton.gameObject.SetActive(false);
    }

    public void HideCombat() { var tip = FindFirstObjectByType<TooltipUI>(); if (tip != null) tip.Hide(); combatPanel.SetActive(false); ClearEnemySlots(); }
    private void ClearEnemySlots() { foreach (var s in enemySlots.Values) if (s.Root != null) Destroy(s.Root); enemySlots.Clear(); }

    public void UpdatePlayerHealth(Player player)
    {
        targetHealthPercent = (float)player.GetHealth() / player.GetMaxHealth();
        int shield = player.GetShield(), maxShield = player.GetMaxShield();
        if (playerShieldOverlay != null) playerShieldOverlay.fillAmount = (shield > 0 && maxShield > 0) ? (float)shield / maxShield : 0f;
        if (playerHealthText != null) playerHealthText.text = $"{player.GetHealth()} / {player.GetMaxHealth()}" + (shield > 0 ? $" <color=#66ccff>[+{shield}]</color>" : "");
    }
    
    public void UpdateEnergyDisplay(Player player) { int e = player.GetEnergy(), m = player.GetMaxEnergy(); targetEnergyPercent = m > 0 ? (float)e / m : 0f; if (playerEnergyText != null) playerEnergyText.text = $"{e} / {m}"; }
    public void UpdatePlayerEnergy(Player player) { UpdateEnergyDisplay(player); UpdateSkillButtons(player); }
    public void UpdateAPDisplay(Player player) { if (apText != null) apText.text = $"AP: {player.GetCurrentAP()} / {player.GetMaxAP()}"; }

    public void UpdateEnemyHealth(CombatEnemy enemy)
    {
        if (!enemySlots.ContainsKey(enemy)) return;
        var slot = enemySlots[enemy];
        slot.HealthFill.fillAmount = (float)enemy.Health / enemy.MaxHealth;
        string hp = $"{enemy.Health}/{enemy.MaxHealth}";
        foreach (var ef in enemy.GetStatusEffects())
            hp += ef.Type == StatusEffectType.DoT ? $" <color=#ff6600>DoT({ef.Duration}t)</color>" : ef.Type == StatusEffectType.Stun ? $" <color=#ffff00>STUN({ef.Duration}t)</color>" : "";
        slot.HealthText.text = hp;
        if (slot.MarksText != null) { var m = enemy.GetMarks(); slot.MarksText.text = m.Count > 0 ? string.Join(" ", new List<string>(System.Linq.Enumerable.Select(m, kv => $"<color=#{ColorUtility.ToHtmlStringRGB(GetElementColor(kv.Key))}>{kv.Key}:{kv.Value}</color>"))) : ""; }
        if (!enemy.IsAlive()) { slot.Root.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.5f); slot.NameText.color = Color.gray; }
    }

    public void SetPlayerTurn(bool pt) { if (pt) { var r = FindFirstObjectByType<Referencer>(); if (r?.player != null) UpdateSkillButtons(r.player); } else { if (skill1Button != null) skill1Button.interactable = false; if (skill2Button != null) skill2Button.interactable = false; if (skill3Button != null) skill3Button.interactable = false; if (skill4Button != null) skill4Button.interactable = false; if (skill5Button != null) skill5Button.interactable = false; } }
    public void UpdateReactionIndicator() { var r = FindFirstObjectByType<Referencer>(); if (r?.player == null) return; var p = r.player; UpdateSkillButtonElementColor(skill1Button, p.GetSkillElement(1)); UpdateSkillButtonElementColor(skill2Button, p.GetSkillElement(2)); UpdateSkillButtonElementColor(skill3Button, p.GetSkillElement(3)); UpdateSkillButtonElementColor(skill4Button, p.GetSkillElement(4)); UpdateSkillButtonElementColor(skill5Button, p.GetSkillElement(5)); }
    #endregion

    #region Skills
    public void UpdateSkillButtons(Player player)
    {
        var ch = player.GetCharacter(); if (ch == null) { DisableAllSkillButtons(); return; }
        UpdateSingleSkill(1, ch.Skill1, ch.Skill1APCost, ch.Skill1EnergyGain, ch.Skill1Cooldown, player, skill1Button, skill1Text, skill1Tooltip);
        UpdateSingleSkill(2, ch.Skill2, ch.Skill2APCost, ch.Skill2EnergyGain, ch.Skill2Cooldown, player, skill2Button, skill2Text, skill2Tooltip);
        UpdateSingleSkill(3, ch.Skill3, ch.Skill3APCost, ch.Skill3EnergyGain, ch.Skill3Cooldown, player, skill3Button, skill3Text, skill3Tooltip);
        UpdateSingleSkill(4, ch.Skill4, ch.Skill4APCost, ch.Skill4EnergyGain, ch.Skill4Cooldown, player, skill4Button, skill4Text, skill4Tooltip);
        int cd5 = player.GetSkillCooldown(4), ap5 = ch.Skill5APCost, ec = ch.Skill5EnergyCost, ce = player.GetEnergy();
        bool can5 = player.CanUseUltimate() && player.HasEnoughAP(ap5);
        if (cd5 > 0) { if (skill5Text != null) skill5Text.text = $"{ch.Skill5}\n<size=10><color=#888>CD:{cd5}</color></size>"; if (skill5Button != null) skill5Button.interactable = false; }
        else { if (skill5Text != null) skill5Text.text = $"<size=10><color={(player.HasEnoughAP(ap5)?"#aaa":"#f44")}>{ap5}AP</color></size>\n{ch.Skill5}\n<size=10><color={(ce>=ec?"#4f4":"#f44")}>{ce}E</color></size>"; if (skill5Button != null) skill5Button.interactable = can5; }
        if (skill5Tooltip != null) skill5Tooltip.SetTooltip($"<b>{ch.Skill5}</b>\n{PlayerStatsUI.GetSkillDescription(ch.Skill5)}\nAP:{ap5}|EnergyCost:{ec}|CD:{ch.Skill5Cooldown}");
        UpdateSkillButtonElementColor(skill1Button, player.GetSkillElement(1)); UpdateSkillButtonElementColor(skill2Button, player.GetSkillElement(2)); UpdateSkillButtonElementColor(skill3Button, player.GetSkillElement(3)); UpdateSkillButtonElementColor(skill4Button, player.GetSkillElement(4)); UpdateSkillButtonElementColor(skill5Button, player.GetSkillElement(5));
    }
    private void UpdateSingleSkill(int n, string nm, int ap, int eg, int cd, Player p, Button b, TextMeshProUGUI t, TooltipTrigger tip)
    {
        int c = p.GetSkillCooldown(n-1); bool can = c<=0 && p.HasEnoughAP(ap);
        string ex = nm.ToLower()=="dirtystab" && p.GetDirtyStabStacks()>0 ? $" <color=#fa0>[+{p.GetDirtyStabStacks()*20}%]</color>" : "";
        if (c>0) { if(t!=null) t.text=$"{nm}{ex}\n<size=10><color=#888>CD:{c}</color></size>"; if(b!=null) b.interactable=false; }
        else { if(t!=null) t.text=$"<size=10><color={(p.HasEnoughAP(ap)?"#aaa":"#f44")}>{ap}AP</color></size>\n{nm}{ex}"; if(b!=null) b.interactable=can; }
        if(tip!=null) tip.SetTooltip($"<b>{nm}</b>\n{PlayerStatsUI.GetSkillDescription(nm)}\nAP:{ap}|Energy:+{eg}|CD:{cd}");
    }
    private void DisableAllSkillButtons() { if(skill1Text!=null)skill1Text.text="---";if(skill2Text!=null)skill2Text.text="---";if(skill3Text!=null)skill3Text.text="---";if(skill4Text!=null)skill4Text.text="---";if(skill5Text!=null)skill5Text.text="---";if(skill1Button!=null)skill1Button.interactable=false;if(skill2Button!=null)skill2Button.interactable=false;if(skill3Button!=null)skill3Button.interactable=false;if(skill4Button!=null)skill4Button.interactable=false;if(skill5Button!=null)skill5Button.interactable=false; }
    private void UpdateSkillButtonElementColor(Button b, Element e) { if(b==null)return; var i=b.GetComponent<Image>(); if(i==null)return; var c=GetElementColor(e); i.color=e!=Element.None?new Color(c.r*0.4f,c.g*0.4f,c.b*0.4f,0.9f):new Color(0.2f,0.2f,0.25f,0.9f); }
    #endregion

    #region FloatingText
    public void ShowDamageToEnemy(CombatEnemy e,int d,bool cr=false){var f=FloatingTextManager.Instance;if(f!=null){var t=GetEnemyTransform(e);if(t!=null)f.ShowDamage(t,d,cr);}}
    public void ShowDamageToPlayer(int d){var f=FloatingTextManager.Instance;if(f!=null&&playerBlock!=null)f.ShowDamageTaken(playerBlock.transform,d);}
    public void ShowHealToPlayer(int a){var f=FloatingTextManager.Instance;if(f!=null&&playerBlock!=null)f.ShowHeal(playerBlock.transform,a);}
    public void ShowShieldToPlayer(int a,FloatingTextType ty){var f=FloatingTextManager.Instance;if(f!=null&&playerBlock!=null)f.ShowShield(playerBlock.transform,a,ty);}
    public void ShowShieldGainToPlayer(int a){ShowShieldToPlayer(a,FloatingTextType.ShieldGain);}
    public void ShowStatusToEnemy(CombatEnemy e,string n,bool g,int s=0){var f=FloatingTextManager.Instance;if(f!=null){var t=GetEnemyTransform(e);if(t!=null)f.ShowStatus(t,n,g,s);}}
    public void ShowDoTTickToEnemy(CombatEnemy e,int d,string n="DoT"){var f=FloatingTextManager.Instance;if(f!=null){var t=GetEnemyTransform(e);if(t!=null)f.ShowDoTTick(t,d,n);}}
    public void ShowTurnSkippedToEnemy(CombatEnemy e,string r="Stunned!"){var f=FloatingTextManager.Instance;if(f!=null){var t=GetEnemyTransform(e);if(t!=null)f.ShowTurnSkipped(t,r);}}
    public void ShowReactionToEnemy(CombatEnemy e,string n,int v=0){var f=FloatingTextManager.Instance;if(f!=null){var t=GetEnemyTransform(e);if(t!=null)f.ShowReaction(t,n,0f,v);}}
    public Transform GetEnemyTransform(CombatEnemy e)=>enemySlots.ContainsKey(e)?enemySlots[e].Root.transform:null;
    public Transform GetPlayerTransform()=>playerBlock?.transform;
    #endregion

    #region Potions
    private void RefreshPotionButtons(Player p){if(potionTooltipPanel!=null)potionTooltipPanel.SetActive(false);if(potionContainer!=null)for(int i=potionContainer.transform.childCount-1;i>=0;i--){var c=potionContainer.transform.GetChild(i);if(c.gameObject!=potionTooltipPanel)Destroy(c.gameObject);}potionButtons.Clear();potionTexts.Clear();var ps=p.GetPotions();for(int i=0;i<p.GetMaxPotions();i++)CreatePotionButton(i,i<ps.Count?ps[i]:null,p);}
    private void CreatePotionButton(int idx,PotionData pot,Player p){var o=new GameObject($"Pot_{idx}");o.transform.SetParent(potionContainer.transform,false);var le=o.AddComponent<LayoutElement>();le.preferredWidth=55;le.preferredHeight=55;le.minWidth=55;le.minHeight=55;var im=o.AddComponent<Image>();im.sprite=CreateCircleSprite();im.type=Image.Type.Simple;im.preserveAspect=true;if(pot!=null){im.color=GetPotionColor(pot);var bt=o.AddComponent<Button>();bt.targetGraphic=im;potionButtons.Add(bt);int ci=idx;bt.onClick.AddListener(()=>OnPotionClicked(ci,p));var et=o.AddComponent<EventTrigger>();var pe=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};PotionData cp=pot;pe.callback.AddListener((d)=>ShowPotionTooltip(cp));et.triggers.Add(pe);var px=new EventTrigger.Entry{eventID=EventTriggerType.PointerExit};px.callback.AddListener((d)=>HidePotionTooltip());et.triggers.Add(px);}else{im.color=new Color(0.25f,0.25f,0.3f,0.6f);potionButtons.Add(null);}var to=new GameObject("Txt");to.transform.SetParent(o.transform,false);var tr=to.AddComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=Vector2.zero;tr.offsetMax=Vector2.zero;var tx=to.AddComponent<TextMeshProUGUI>();tx.text=pot!=null?GetPotionLabel(pot):$"{idx+1}";tx.color=pot!=null?Color.white:new Color(0.5f,0.5f,0.5f);tx.alignment=TextAlignmentOptions.Center;tx.fontSize=10;potionTexts.Add(tx);}
    private void OnPotionClicked(int i,Player p){if(isTargeting)return;var pot=p.GetPotion(i);if(pot==null)return;if(pot.EffectId=="eff_potion_elemental_boost"){selectedPotionIndex=i;selectedPotionElement=RollPotionElement();isPotionTargeting=true;EnterPotionTargetingMode(pot);}else{if(p.UsePotion(i,null)){RefreshPotionButtons(p);var r=FindFirstObjectByType<Referencer>();if(r?.playerStatsUI!=null)r.playerStatsUI.UpdateStats();UpdatePlayerHealth(p);}}}
    private Element RollPotionElement(){var e=new Element[]{Element.Fire,Element.Ice,Element.Water,Element.Wind,Element.Rock};return e[Random.Range(0,e.Length)];}
    private void EnterPotionTargetingMode(PotionData pot){isTargeting=true;isAoESkill=false;var ae=GetAliveEnemies();if(ae.Count==0){ExitPotionTargetingMode();return;}selectedTargetIndex=0;skill1Button.gameObject.SetActive(false);skill2Button.gameObject.SetActive(false);skill3Button.gameObject.SetActive(false);skill4Button.gameObject.SetActive(false);skill5Button.gameObject.SetActive(false);backButton.gameObject.SetActive(true);var bt=backButton.GetComponentInChildren<TextMeshProUGUI>();if(bt!=null)bt.text=$"BACK\n({selectedPotionElement})";foreach(var b in potionButtons)if(b!=null)b.gameObject.SetActive(false);UpdateTargetArrows();}
    private void ExitPotionTargetingMode(){isPotionTargeting=false;selectedPotionIndex=-1;selectedPotionElement=Element.None;var bt=backButton.GetComponentInChildren<TextMeshProUGUI>();if(bt!=null)bt.text="BACK";ExitTargetingMode();foreach(var b in potionButtons)if(b!=null)b.gameObject.SetActive(true);}
    private void ConfirmPotionTarget(Player p){var ae=GetAliveEnemies();if(ae.Count==0||selectedTargetIndex>=ae.Count)return;if(p.UsePotion(selectedPotionIndex,ae[selectedTargetIndex],selectedPotionElement)){UpdateEnemyHealth(ae[selectedTargetIndex]);RefreshPotionButtons(p);}ExitPotionTargetingMode();}
    private void ShowPotionTooltip(PotionData p){if(potionTooltipPanel==null)return;potionTooltipText.text=p.EffectId switch{"eff_potion_heal"=>$"<color=#f55>{p.DisplayName}</color>\nRestores {p.EffectValue} HP","eff_potion_elemental_boost"=>$"<color=#a7f>{p.DisplayName}</color>\nDeals {p.EffectValue} elemental dmg",_=>p.DisplayName};potionTooltipPanel.SetActive(true);}
    private void HidePotionTooltip(){if(potionTooltipPanel!=null)potionTooltipPanel.SetActive(false);}
    private string GetPotionLabel(PotionData p)=>p.EffectId switch{"eff_potion_heal"=>$"HP\n+{p.EffectValue}","eff_potion_elemental_boost"=>$"DMG\n{p.EffectValue}","eff_potion_crit_rate"=>$"CR\n+{p.EffectValue}%","eff_potion_crit_damage"=>$"CD\n+{p.EffectValue}%",_=>p.DisplayName};
    private Color GetPotionColor(PotionData p)=>p.EffectId switch{"eff_potion_heal"=>new Color(0.4f,0.2f,0.2f),"eff_potion_elemental_boost"=>new Color(0.3f,0.25f,0.4f),"eff_potion_crit_rate"=>new Color(0.4f,0.35f,0.2f),"eff_potion_crit_damage"=>new Color(0.35f,0.2f,0.35f),_=>new Color(0.25f,0.3f,0.25f)};
    #endregion

    #region Relics
    private void RefreshRelicsDisplay(Player p){if(relicsContainer==null)return;for(int i=relicsContainer.transform.childCount-1;i>=0;i--)Destroy(relicsContainer.transform.GetChild(i).gameObject);relicIcons.Clear();var rs=p.GetRelics();int mx=Mathf.Max(0,rs.Count-MAX_VISIBLE_RELICS);relicScrollIndex=Mathf.Clamp(relicScrollIndex,0,mx);for(int i=relicScrollIndex;i<Mathf.Min(relicScrollIndex+MAX_VISIBLE_RELICS,rs.Count);i++)CreateRelicIcon(rs[i]);if(relicScrollLeftButton!=null)relicScrollLeftButton.gameObject.SetActive(relicScrollIndex>0);if(relicScrollRightButton!=null)relicScrollRightButton.gameObject.SetActive(relicScrollIndex+MAX_VISIBLE_RELICS<rs.Count);}
    private void CreateRelicIcon(RelicData r){var o=new GameObject($"Relic_{r.Id}");o.transform.SetParent(relicsContainer.transform,false);relicIcons.Add(o);var le=o.AddComponent<LayoutElement>();le.preferredWidth=46;le.preferredHeight=46;le.minWidth=46;le.minHeight=46;var im=o.AddComponent<Image>();im.sprite=CreateCircleSprite();im.type=Image.Type.Simple;im.preserveAspect=true;im.color=GetRelicRarityColor(r.Rarity);var tip=o.AddComponent<TooltipTrigger>();tip.SetTooltip($"<b>{r.DisplayName}</b>\n<i>{r.Rarity}</i>\n{r.Description}");}
    private Color GetRelicRarityColor(string r)=>r?.ToLower()switch{"common"=>new Color(0.5f,0.5f,0.5f),"uncommon"=>new Color(0.3f,0.6f,0.3f),"rare"=>new Color(0.3f,0.4f,0.7f),"epic"=>new Color(0.6f,0.3f,0.6f),"legendary"=>new Color(0.8f,0.6f,0.2f),_=>new Color(0.4f,0.4f,0.4f)};
    #endregion

    #region Helpers
    private Color GetElementColor(Element e)=>e switch{Element.Fire=>new Color(1f,0.4f,0.2f),Element.Ice=>new Color(0.4f,0.8f,1f),Element.Water=>new Color(0.2f,0.5f,1f),Element.Wind=>new Color(0.6f,1f,0.6f),Element.Rock=>new Color(0.7f,0.5f,0.3f),Element.Lightning=>new Color(0.9f,0.8f,0.2f),_=>Color.white};
    private Color GetCharacterColor(string n)=>n?.ToLower()switch{"fighter"=>new Color(0.7f,0.3f,0.3f),"rogue"=>new Color(0.3f,0.3f,0.7f),"mage"=>new Color(0.5f,0.3f,0.7f),"ranger"=>new Color(0.3f,0.6f,0.3f),"paladin"=>new Color(0.7f,0.6f,0.3f),_=>new Color(0.4f,0.4f,0.5f)};
    private static Sprite cachedCircleSprite;
    private Sprite CreateCircleSprite(){if(cachedCircleSprite!=null)return cachedCircleSprite;int s=55;var tex=new Texture2D(s,s,TextureFormat.RGBA32,false);float r=s/2f;for(int y=0;y<s;y++)for(int x=0;x<s;x++){float d=Mathf.Sqrt((x-r)*(x-r)+(y-r)*(y-r));tex.SetPixel(x,y,d<=r-1?Color.white:(d<=r?new Color(1,1,1,r-d):Color.clear));}tex.Apply();cachedCircleSprite=Sprite.Create(tex,new Rect(0,0,s,s),new Vector2(0.5f,0.5f),100);return cachedCircleSprite;}
    private class EnemyUISlot{public GameObject Root;public Image HealthFill;public TextMeshProUGUI HealthText;public TextMeshProUGUI NameText;public GameObject Arrow;public Button ClickArea;public TextMeshProUGUI MarksText;}
    #endregion

    #region LootPanel
    private GameObject CreateLootPanel(Transform parent){var pn=new GameObject("LootPanel");pn.transform.SetParent(parent,false);var rc=pn.AddComponent<RectTransform>();rc.anchorMin=new Vector2(0.25f,0.2f);rc.anchorMax=new Vector2(0.75f,0.8f);rc.offsetMin=Vector2.zero;rc.offsetMax=Vector2.zero;var bg=pn.AddComponent<Image>();bg.color=new Color(0.1f,0.1f,0.15f,0.95f);var to=new GameObject("Title");to.transform.SetParent(pn.transform,false);var tr=to.AddComponent<RectTransform>();tr.anchorMin=new Vector2(0,0.85f);tr.anchorMax=new Vector2(1,0.98f);tr.offsetMin=Vector2.zero;tr.offsetMax=Vector2.zero;lootTitleText=to.AddComponent<TextMeshProUGUI>();lootTitleText.text="Reward";lootTitleText.alignment=TextAlignmentOptions.Center;lootTitleText.fontSize=36;lootTitleText.fontStyle=FontStyles.Bold;lootTitleText.color=new Color(1f,0.85f,0.2f);rewardContainer=new GameObject("RewardContainer");rewardContainer.transform.SetParent(pn.transform,false);var cr=rewardContainer.AddComponent<RectTransform>();cr.anchorMin=new Vector2(0.05f,0.25f);cr.anchorMax=new Vector2(0.95f,0.82f);cr.offsetMin=Vector2.zero;cr.offsetMax=Vector2.zero;var go=new GameObject("GoldReward");go.transform.SetParent(rewardContainer.transform,false);var gr=go.AddComponent<RectTransform>();gr.anchorMin=new Vector2(0.05f,0.7f);gr.anchorMax=new Vector2(0.95f,0.95f);gr.offsetMin=Vector2.zero;gr.offsetMax=Vector2.zero;var gi=go.AddComponent<Image>();gi.color=new Color(0.2f,0.18f,0.1f,0.9f);goldRewardButton=go.AddComponent<Button>();goldRewardButton.targetGraphic=gi;goldRewardButton.onClick.AddListener(OnGoldRewardClicked);var gto=new GameObject("Text");gto.transform.SetParent(go.transform,false);var gtr=gto.AddComponent<RectTransform>();gtr.anchorMin=Vector2.zero;gtr.anchorMax=Vector2.one;gtr.offsetMin=new Vector2(10,0);gtr.offsetMax=new Vector2(-10,0);lootGoldText=gto.AddComponent<TextMeshProUGUI>();lootGoldText.alignment=TextAlignmentOptions.MidlineLeft;lootGoldText.fontSize=22;lootGoldText.color=new Color(1f,0.85f,0.2f);var xo=new GameObject("XPReward");xo.transform.SetParent(rewardContainer.transform,false);var xr=xo.AddComponent<RectTransform>();xr.anchorMin=new Vector2(0.05f,0.4f);xr.anchorMax=new Vector2(0.95f,0.65f);xr.offsetMin=Vector2.zero;xr.offsetMax=Vector2.zero;var xi=xo.AddComponent<Image>();xi.color=new Color(0.1f,0.15f,0.2f,0.9f);var xto=new GameObject("Text");xto.transform.SetParent(xo.transform,false);var xtr=xto.AddComponent<RectTransform>();xtr.anchorMin=Vector2.zero;xtr.anchorMax=Vector2.one;xtr.offsetMin=new Vector2(10,0);xtr.offsetMax=new Vector2(-10,0);lootXPText=xto.AddComponent<TextMeshProUGUI>();lootXPText.alignment=TextAlignmentOptions.MidlineLeft;lootXPText.fontSize=22;lootXPText.color=new Color(0.4f,0.9f,1f);sigilRewardButton=new GameObject("SigilReward");sigilRewardButton.transform.SetParent(rewardContainer.transform,false);var sr=sigilRewardButton.AddComponent<RectTransform>();sr.anchorMin=new Vector2(0.05f,0.1f);sr.anchorMax=new Vector2(0.95f,0.35f);sr.offsetMin=Vector2.zero;sr.offsetMax=Vector2.zero;var si=sigilRewardButton.AddComponent<Image>();si.color=new Color(0.3f,0.1f,0.1f,0.9f);var sb=sigilRewardButton.AddComponent<Button>();sb.targetGraphic=si;sb.onClick.AddListener(OnSigilRewardClicked);var sto=new GameObject("Text");sto.transform.SetParent(sigilRewardButton.transform,false);var str=sto.AddComponent<RectTransform>();str.anchorMin=Vector2.zero;str.anchorMax=Vector2.one;str.offsetMin=new Vector2(10,0);str.offsetMax=new Vector2(-10,0);var st=sto.AddComponent<TextMeshProUGUI>();st.text="Sigil";st.alignment=TextAlignmentOptions.MidlineLeft;st.fontSize=22;st.color=Color.white;sigilRewardButton.SetActive(false);relicRewardButton=new GameObject("RelicReward");relicRewardButton.transform.SetParent(rewardContainer.transform,false);var rr=relicRewardButton.AddComponent<RectTransform>();rr.anchorMin=new Vector2(0.05f,-0.20f);rr.anchorMax=new Vector2(0.95f,0.05f);rr.offsetMin=Vector2.zero;rr.offsetMax=Vector2.zero;var ri=relicRewardButton.AddComponent<Image>();ri.color=new Color(0.3f,0.2f,0.4f,0.9f);var rb=relicRewardButton.AddComponent<Button>();rb.targetGraphic=ri;rb.onClick.AddListener(OnRelicRewardClicked);var rto=new GameObject("Text");rto.transform.SetParent(relicRewardButton.transform,false);var rtr=rto.AddComponent<RectTransform>();rtr.anchorMin=Vector2.zero;rtr.anchorMax=Vector2.one;rtr.offsetMin=new Vector2(10,0);rtr.offsetMax=new Vector2(-10,0);var rt=rto.AddComponent<TextMeshProUGUI>();rt.text="Relic";rt.alignment=TextAlignmentOptions.MidlineLeft;rt.fontSize=22;rt.color=Color.white;rt.raycastTarget=false;relicRewardButton.SetActive(false);var io=new GameObject("InstructionText");io.transform.SetParent(pn.transform,false);var ir=io.AddComponent<RectTransform>();ir.anchorMin=new Vector2(0.05f,0.15f);ir.anchorMax=new Vector2(0.95f,0.24f);ir.offsetMin=Vector2.zero;ir.offsetMax=Vector2.zero;sigilInstructionText=io.AddComponent<TextMeshProUGUI>();sigilInstructionText.text="";sigilInstructionText.alignment=TextAlignmentOptions.Center;sigilInstructionText.fontSize=18;sigilInstructionText.color=new Color(0.8f,0.8f,0.5f);var bo=new GameObject("ContinueButton");bo.transform.SetParent(pn.transform,false);var br=bo.AddComponent<RectTransform>();br.anchorMin=new Vector2(0.3f,0.03f);br.anchorMax=new Vector2(0.7f,0.13f);br.offsetMin=Vector2.zero;br.offsetMax=Vector2.zero;var bi=bo.AddComponent<Image>();bi.color=new Color(0.2f,0.6f,0.2f,1f);collectLootButton=bo.AddComponent<Button>();collectLootButton.targetGraphic=bi;collectLootButton.onClick.AddListener(OnCollectLootClicked);var bto=new GameObject("Text");bto.transform.SetParent(bo.transform,false);var btr=bto.AddComponent<RectTransform>();btr.anchorMin=Vector2.zero;btr.anchorMax=Vector2.one;btr.offsetMin=Vector2.zero;btr.offsetMax=Vector2.zero;var bt=bto.AddComponent<TextMeshProUGUI>();bt.text="CONTINUE";bt.alignment=TextAlignmentOptions.Center;bt.fontSize=20;bt.color=Color.white;return pn;}
    private void OnGoldRewardClicked(){if(goldCollected||pendingPlayer==null)return;pendingPlayer.AddGold(pendingGold);goldCollected=true;if(lootGoldText!=null)lootGoldText.text=$"<color=#888><s>Gold:+{pendingGold}</s>(Collected)</color>";if(goldRewardButton!=null)goldRewardButton.interactable=false;}
    private void OnSigilRewardClicked(){if(pendingSigil==Element.None)return;isEnchanting=true;if(sigilInstructionText!=null)sigilInstructionText.text=$"Click a skill to enchant with {pendingSigil}";lootPanel.SetActive(false);ShowEnchantmentSkillSelection();}
    private void OnRelicRewardClicked(){if(pendingRelic==null||relicCollected||pendingPlayer==null)return;pendingPlayer.AddRelic(pendingRelic);relicCollected=true;if(relicRewardButton!=null){relicRewardButton.GetComponent<Button>().interactable=false;var t=relicRewardButton.GetComponentInChildren<TextMeshProUGUI>();if(t!=null)t.text=$"<color=#96f>✦</color>{pendingRelic.DisplayName}<size=16>(Collected!)</size>";}}
    private void ShowEnchantmentSkillSelection(){if(bottomBar!=null)bottomBar.SetActive(true);var ov=new GameObject("EnchantOverlay");ov.transform.SetParent(lootPanel.transform.parent,false);var or=ov.AddComponent<RectTransform>();or.anchorMin=Vector2.zero;or.anchorMax=Vector2.one;or.offsetMin=Vector2.zero;or.offsetMax=Vector2.zero;var ob=ov.AddComponent<Image>();ob.color=new Color(0,0,0,0.7f);var io=new GameObject("Instr");io.transform.SetParent(ov.transform,false);var ir=io.AddComponent<RectTransform>();ir.anchorMin=new Vector2(0.2f,0.6f);ir.anchorMax=new Vector2(0.8f,0.8f);ir.offsetMin=Vector2.zero;ir.offsetMax=Vector2.zero;var it=io.AddComponent<TextMeshProUGUI>();it.text=$"Select skill to enchant with <color=#{ColorUtility.ToHtmlStringRGB(GetElementColor(pendingSigil))}>{pendingSigil}</color>";it.alignment=TextAlignmentOptions.Center;it.fontSize=28;it.color=Color.white;CreateEnchantmentSkillButtons(ov.transform);enchantOverlay=ov;}
    private void CreateEnchantmentSkillButtons(Transform parent){if(pendingPlayer==null)return;var ch=pendingPlayer.GetCharacter();if(ch==null)return;var bc=new GameObject("SkillBtns");bc.transform.SetParent(parent,false);var bcr=bc.AddComponent<RectTransform>();bcr.anchorMin=new Vector2(0.1f,0.25f);bcr.anchorMax=new Vector2(0.9f,0.55f);bcr.offsetMin=Vector2.zero;bcr.offsetMax=Vector2.zero;string[]nms={ch.Skill1,ch.Skill2,ch.Skill3,ch.Skill4,ch.Skill5};float bw=0.18f,sp=0.02f,sx=0.5f-(2.5f*bw+2*sp);for(int i=0;i<5;i++){int sn=i+1;string nm=nms[i];Element ce=pendingPlayer.GetSkillElement(sn);var bo=new GameObject($"Skill{sn}Btn");bo.transform.SetParent(bc.transform,false);var br=bo.AddComponent<RectTransform>();float xp=sx+i*(bw+sp);br.anchorMin=new Vector2(xp,0.1f);br.anchorMax=new Vector2(xp+bw,0.9f);br.offsetMin=Vector2.zero;br.offsetMax=Vector2.zero;var bi=bo.AddComponent<Image>();bi.color=ce!=Element.None?GetElementColor(ce)*0.5f:new Color(0.25f,0.25f,0.3f);var bt=bo.AddComponent<Button>();bt.targetGraphic=bi;bt.onClick.AddListener(()=>OnEnchantSkillClicked(sn));var to=new GameObject("Text");to.transform.SetParent(bo.transform,false);var tr=to.AddComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(3,3);tr.offsetMax=new Vector2(-3,-3);var tx=to.AddComponent<TextMeshProUGUI>();tx.text=ce!=Element.None?$"<size=12>{sn}</size>\n{nm}\n<color=#{ColorUtility.ToHtmlStringRGB(GetElementColor(ce))}><b>[{ce}]</b></color>":$"<size=12>{sn}</size>\n{nm}\n<color=#666>[None]</color>";tx.alignment=TextAlignmentOptions.Center;tx.fontSize=12;tx.color=Color.white;}var co=new GameObject("CancelBtn");co.transform.SetParent(parent,false);var cr=co.AddComponent<RectTransform>();cr.anchorMin=new Vector2(0.4f,0.08f);cr.anchorMax=new Vector2(0.6f,0.18f);cr.offsetMin=Vector2.zero;cr.offsetMax=Vector2.zero;var ci=co.AddComponent<Image>();ci.color=new Color(0.5f,0.2f,0.2f);var cb=co.AddComponent<Button>();cb.targetGraphic=ci;cb.onClick.AddListener(OnCancelEnchantment);var cto=new GameObject("Text");cto.transform.SetParent(co.transform,false);var ctr=cto.AddComponent<RectTransform>();ctr.anchorMin=Vector2.zero;ctr.anchorMax=Vector2.one;var ctx=cto.AddComponent<TextMeshProUGUI>();ctx.text="Cancel";ctx.alignment=TextAlignmentOptions.Center;ctx.fontSize=16;ctx.color=Color.white;}
    private void OnEnchantSkillClicked(int sn){if(!isEnchanting||pendingSigil==Element.None||pendingPlayer==null)return;pendingPlayer.EnchantSkill(sn,pendingSigil);isEnchanting=false;pendingSigil=Element.None;if(enchantOverlay!=null){Destroy(enchantOverlay);enchantOverlay=null;}if(sigilRewardButton!=null){var t=sigilRewardButton.GetComponentInChildren<TextMeshProUGUI>();if(t!=null)t.text="<color=#888><s>Sigil</s>(Used)</color>";sigilRewardButton.GetComponent<Button>().interactable=false;}lootPanel.SetActive(true);if(sigilInstructionText!=null)sigilInstructionText.text=$"Skill {sn} enchanted!";}
    private void OnCancelEnchantment(){isEnchanting=false;if(enchantOverlay!=null){Destroy(enchantOverlay);enchantOverlay=null;}lootPanel.SetActive(true);if(sigilInstructionText!=null)sigilInstructionText.text="";}
    public void ShowLootPanel(int gold,int xp,Player p,NodeBase node,string title=null,bool isElite=false,bool isBoss=false,bool dropsSigil=false,bool dropsRelic=false){pendingGold=gold;pendingXP=xp;pendingPlayer=p;pendingNode=node;goldCollected=false;relicCollected=false;pendingSigil=Element.None;pendingRelic=null;isEnchanting=false;if(lootTitleText!=null)lootTitleText.text=isBoss?"BOSS DEFEATED!":"Reward";if(goldRewardButton!=null)goldRewardButton.interactable=true;if(lootGoldText!=null)lootGoldText.text=$"<color=#FFD700>◆</color>Gold:+{gold}<size=16>(Click to collect)</size>";if(lootXPText!=null)lootXPText.text=$"<color=#6CF>★</color>Experience:+{xp}<size=16>(Auto)</size>";bool showSigil=dropsSigil||isElite;if(sigilRewardButton!=null){if(showSigil){var els=new Element[]{Element.Fire,Element.Ice,Element.Water,Element.Wind,Element.Rock};pendingSigil=els[Random.Range(0,els.Length)];var si=sigilRewardButton.GetComponent<Image>();if(si!=null)si.color=GetElementColor(pendingSigil)*0.4f;var st=sigilRewardButton.GetComponentInChildren<TextMeshProUGUI>();if(st!=null){st.text=$"<color=#{ColorUtility.ToHtmlStringRGB(GetElementColor(pendingSigil))}>◈</color>{pendingSigil} Sigil<size=16>(Click to enchant)</size>";st.color=GetElementColor(pendingSigil);}sigilRewardButton.GetComponent<Button>().interactable=true;sigilRewardButton.SetActive(true);}else sigilRewardButton.SetActive(false);}if(relicRewardButton!=null){if(dropsRelic&&DataCache.Relics!=null&&DataCache.Relics.Count>0){pendingRelic=DataCache.Relics[Random.Range(0,DataCache.Relics.Count)];var ri=relicRewardButton.GetComponent<Image>();if(ri!=null)ri.color=new Color(0.6f,0.4f,0.8f,1f);var rt=relicRewardButton.GetComponentInChildren<TextMeshProUGUI>();if(rt!=null)rt.text=$"<color=#96f>✦</color>{pendingRelic.DisplayName}<size=16>(Click to collect)</size>";relicRewardButton.GetComponent<Button>().interactable=true;relicRewardButton.SetActive(true);}else relicRewardButton.SetActive(false);}if(sigilInstructionText!=null)sigilInstructionText.text="";combatPanel.SetActive(false);lootPanel.SetActive(true);}
    private void OnCollectLootClicked(){if(!goldCollected&&pendingPlayer!=null){pendingPlayer.AddGold(pendingGold);goldCollected=true;}lootPanel.SetActive(false);if(enchantOverlay!=null){Destroy(enchantOverlay);enchantOverlay=null;}if(combatManager==null)combatManager=FindFirstObjectByType<CombatManager>();if(combatManager!=null)combatManager.OnLootCollected();}
    #endregion
}
