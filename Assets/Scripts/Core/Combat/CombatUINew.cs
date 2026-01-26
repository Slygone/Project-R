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
public class CombatUINew : MonoBehaviour
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
