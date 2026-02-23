using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages the in-world combat arena - positions player on left, enemies on right.
/// Handles seamless transition from exploration to combat.
/// Includes skills UI, AP display, and end turn button.
/// </summary>
public class CombatArena : MonoBehaviour
{
    [Header("=== COMBAT POSITIONS (EDIT THESE) ===")]
    [Tooltip("Absolute world position for player during combat. Adjust X to move left/right.")]
    [SerializeField] private Vector3 playerCombatPosition = new Vector3(-5f, 0.75f, 0f);
    
    [Tooltip("Absolute world position for first enemy during combat. Adjust X to move left/right.")]
    [SerializeField] private Vector3 enemyCombatPosition = new Vector3(5f, 0.75f, 0f);
    
    [Tooltip("Z spacing between multiple enemies (stacked front to back).")]
    [SerializeField] private float enemyVerticalSpacing = 3f;
    
    [Header("=== PLAYER VISUAL SETTINGS ===")]
    [Tooltip("Player scale during combat. Should match enemy proportions.")]
    [SerializeField] private Vector3 playerCombatScale = new Vector3(1.5f, 2.5f, 1.5f);
    
    [Tooltip("Height of player nameplate above ground (match enemy healthBarYOffset = 4f).")]
    [SerializeField] private float playerNameplateHeight = 4f;
    
    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 0.5f;
    
    [Header("Enemy Visual Settings - 30% larger")]
    [SerializeField] private float enemyScale = 1.95f; // 1.5 * 1.3 = 1.95
    [SerializeField] private Color[] enemyColors = new Color[]
    {
        new Color(0.8f, 0.2f, 0.2f), // Red
        new Color(0.2f, 0.6f, 0.8f), // Blue  
        new Color(0.2f, 0.8f, 0.3f), // Green
        new Color(0.8f, 0.6f, 0.2f), // Orange
        new Color(0.6f, 0.2f, 0.8f), // Purple
    };
    
    [Header("Idle Animation - More visible bob")]
    [SerializeField] private float idleBobSpeed = 1.5f;
    [SerializeField] private float idleBobAmount = 0.25f; // Increased for visibility
    
    private Vector3 combatCenterPoint;
    private Vector3 playerOriginalPosition;
    private Quaternion playerOriginalRotation;
    private Vector3 playerOriginalScale;
    private Transform playerTransform;
    private List<EnemyWorldUnit> spawnedEnemies = new List<EnemyWorldUnit>();
    private bool inCombat = false;
    private bool isTransitioning = false; // Prevents new combat during exit transition
    
    // Idle animation tracking
    private Vector3 playerCombatBasePos;
    private Dictionary<EnemyWorldUnit, Vector3> enemyBasePosMap = new Dictionary<EnemyWorldUnit, Vector3>();
    
    // UI Elements
    private Canvas combatCanvas;
    private GameObject skillsContainer;
    private GameObject endTurnContainer;
    private Button endTurnButton;
    private TextMeshProUGUI apDisplayText;
    private List<SkillButtonData> skillButtonsData = new List<SkillButtonData>();
    
    // Hidden nodes during combat
    private List<GameObject> hiddenNodes = new List<GameObject>();
    
    // References
    private Player currentPlayer;
    private CombatManager combatManager;
    
    // Current AP tracking
    private int currentAP;
    private int maxAP;
    
    // Targeting system
    private int selectedEnemyIndex = 0;
    private GameObject targetIndicator;
    
    // Health and Energy UI containers
    private GameObject healthContainer;
    private Image healthBarFill;
    private Image shieldBarFill;
    private TextMeshProUGUI healthText;
    private GameObject energyContainer;
    private Image energyBarFill;
    private TextMeshProUGUI energyText;
    
    // Player nameplate
    private Canvas playerWorldCanvas;
    private TextMeshProUGUI playerNameText;
    
    // Camera manager for switching between freeroam and combat cameras
    private CinemachineCameraManager cameraManager;
    
    // Tooltip for enemy intent hover
    private static CombatArena _instance;
    private GameObject tooltipPanel;
    private TextMeshProUGUI tooltipText;
    private bool tooltipVisible;
    
    // Unified status display (buffs + debuffs) for the player
    private StatusDisplayUI playerStatusDisplay;
    
    public bool InCombat => inCombat;
    public List<EnemyWorldUnit> SpawnedEnemies => spawnedEnemies;
    
    private class SkillButtonData
    {
        public GameObject buttonObj;
        public Button button;
        public Image bgImage;
        public Image borderImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI apText;
        public TextMeshProUGUI cooldownText;
        public int skillNumber;
        public string baseElement; // Original element from character
        public int baseAPCost; // Original AP cost from character data
        public bool isUltimate;
    }

    private void Awake()
    {
        _instance = this;
        CreateCombatUI();
    }
    
    private void Update()
    {
        if (!inCombat) return;
        
        // Idle bob animation for player
        if (playerTransform != null && playerCombatBasePos != Vector3.zero)
        {
            float bobOffset = Mathf.Sin(Time.time * idleBobSpeed) * idleBobAmount;
            playerTransform.position = playerCombatBasePos + new Vector3(0f, bobOffset, 0f);
        }
        
        // Idle bob animation for enemies (offset phase for variety)
        foreach (var kvp in enemyBasePosMap)
        {
            if (kvp.Key != null && kvp.Key.gameObject != null)
            {
                float phaseOffset = kvp.Key.GetHashCode() * 0.1f;
                float bobOffset = Mathf.Sin((Time.time + phaseOffset) * idleBobSpeed) * idleBobAmount;
                kvp.Key.transform.position = kvp.Value + new Vector3(0f, bobOffset, 0f);
            }
        }
        
        // Handle target switching with Tab, arrow up/down
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            SelectNextEnemy();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            SelectPreviousEnemy();
        }
        
        // Handle keyboard input for skills (1-5) — executes on currently selected target
        if (Input.GetKeyDown(KeyCode.Alpha1)) OnSkillClicked(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) OnSkillClicked(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) OnSkillClicked(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) OnSkillClicked(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) OnSkillClicked(5);
        
        // Update target indicator position
        UpdateTargetIndicator();
        
        // Update health and energy displays
        UpdateHealthEnergyDisplays();
        
        // Update player status display (unified buffs + debuffs)
        if (playerStatusDisplay != null && currentPlayer != null)
            playerStatusDisplay.UpdateStatuses(currentPlayer.GetActiveStatuses());
        
        // X key toggles expanded status view for all entities
        if (Input.GetKeyDown(KeyCode.X))
        {
            StatusDisplayUI.GlobalExpanded = !StatusDisplayUI.GlobalExpanded;
            if (playerStatusDisplay != null)
                playerStatusDisplay.SetExpanded(StatusDisplayUI.GlobalExpanded);
            foreach (var unit in spawnedEnemies)
            {
                if (unit != null) unit.SetStatusExpanded(StatusDisplayUI.GlobalExpanded);
            }
            // Hide tooltip when collapsing (SetExpanded already calls hideTooltip,
            // but also clear it here for enemy world-space chips that use static tooltip)
            if (!StatusDisplayUI.GlobalExpanded)
                HideTooltipInternal();
        }
        
        // Tooltip is positioned once in ShowTooltipInternal — no mouse follow
    }
    
    /// <summary>
    /// Enter combat mode - move player to left, spawn enemies on right
    /// </summary>
    public void EnterCombat(Transform player, List<CombatEnemy> enemies, Vector3 combatCenter)
    {
        if (inCombat) return;
        
        playerTransform = player;
        playerOriginalPosition = player.position;
        playerOriginalRotation = player.rotation;
        playerOriginalScale = player.localScale;
        combatCenterPoint = combatCenter;
        inCombat = true;
        
        // Get references
        currentPlayer = Object.FindFirstObjectByType<Player>();
        combatManager = Object.FindFirstObjectByType<CombatManager>();
        
        // Switch to combat camera — target the actual arena midpoint, not the node position
        if (cameraManager == null)
        {
            cameraManager = Object.FindFirstObjectByType<CinemachineCameraManager>();
        }
        if (cameraManager != null)
        {
            Vector3 arenaCenter = (playerCombatPosition + enemyCombatPosition) / 2f;
            cameraManager.EnterCombat(arenaCenter);
        }
        
        // Initialize AP from character data (read actual AP — may differ from max due to relics)
        if (currentPlayer != null && currentPlayer.HasCharacter())
        {
            maxAP = currentPlayer.GetCharacter().MaxActionPoints;
            currentAP = currentPlayer.GetCurrentAP();
        }
        
        // Hide world nodes
        HideWorldNodes();
        
        // Show combat UI
        ShowCombatUI();
        
        // Start transition
        StartCoroutine(TransitionToCombat(enemies));
    }
    
    /// <summary>
    /// Exit combat mode - return player to original position, despawn enemies
    /// </summary>
    public void ExitCombat()
    {
        if (!inCombat) return;
        
        // Hide combat UI
        HideCombatUI();
        
        // Show world nodes again
        ShowWorldNodes();
        
        StartCoroutine(TransitionFromCombat());
    }
    
    private IEnumerator TransitionToCombat(List<CombatEnemy> enemies)
    {
        // Use ABSOLUTE world positions - ignoring node position for combat layout
        // The serialized playerCombatPosition and enemyCombatPosition are the exact world coords
        Vector3 playerTargetPos = playerCombatPosition;
        Vector3 startPos = playerTransform.position;
        
        Debug.Log($"[CombatArena] Entering combat - Player moving to {playerTargetPos}, Enemy at {enemyCombatPosition}");
        
        // Use configured combat scale to match enemy proportions
        Vector3 combatScale = playerCombatScale;
        
        // Spawn enemies immediately at target positions
        SpawnEnemyUnits(enemies);
        
        // Smoothly move player to combat position
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            playerTransform.position = Vector3.Lerp(startPos, playerTargetPos, t);
            playerTransform.localScale = Vector3.Lerp(playerOriginalScale, combatScale, t);
            yield return null;
        }
        
        playerTransform.position = playerTargetPos;
        playerTransform.localScale = combatScale;
        playerCombatBasePos = playerTargetPos; // Store for idle animation
        
        // Make player face enemies (rotate to face right/positive X)
        playerTransform.rotation = Quaternion.Euler(0f, 90f, 0f);
        
        // Create player nameplate
        CreatePlayerNameplate();
    }
    
    private IEnumerator TransitionFromCombat()
    {
        Debug.Log("[CombatArena] Exiting combat - transitioning back to free roam");
        isTransitioning = true;
        
        // Destroy player nameplate
        DestroyPlayerNameplate();
        
        // Despawn all enemies
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        spawnedEnemies.Clear();
        enemyBasePosMap.Clear();
        
        // Move player back and restore scale
        Vector3 startPos = playerTransform.position;
        Vector3 startScale = playerTransform.localScale;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            playerTransform.position = Vector3.Lerp(startPos, playerOriginalPosition, t);
            playerTransform.localScale = Vector3.Lerp(startScale, playerOriginalScale, t);
            yield return null;
        }
        
        playerTransform.position = playerOriginalPosition;
        playerTransform.rotation = playerOriginalRotation;
        playerTransform.localScale = playerOriginalScale;
        inCombat = false;
        isTransitioning = false;
        
        // Switch back to freeroam camera
        if (cameraManager != null)
        {
            cameraManager.ExitCombat();
        }
        
        // Re-enable movement AFTER transition completes (not before)
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.SetCanMove(true);
            Debug.Log("[CombatArena] Transition complete - movement re-enabled");
        }
    }
    
    /// <summary>
    /// Returns true if currently in combat or transitioning out of combat.
    /// Use this to prevent triggering new combat during exit transition.
    /// </summary>
    public bool IsInCombatOrTransitioning()
    {
        return inCombat || isTransitioning;
    }
    
    private void SpawnEnemyUnits(List<CombatEnemy> enemies)
    {
        spawnedEnemies.Clear();
        enemyBasePosMap.Clear();
        
        int count = enemies.Count;
        
        // Stack enemies vertically (front to back) like in reference image
        for (int i = 0; i < count; i++)
        {
            // Use ABSOLUTE world position for enemies
            // First enemy at enemyCombatPosition, subsequent ones offset in Z
            float zOffset = i * enemyVerticalSpacing;
            Vector3 spawnPos = enemyCombatPosition + new Vector3(0f, 0f, zOffset);
            
            // Create enemy visual unit
            var enemyUnit = CreateEnemyUnit(enemies[i], spawnPos, i);
            spawnedEnemies.Add(enemyUnit);
            
            // Store base position for idle animation
            enemyBasePosMap[enemyUnit] = spawnPos;
        }
        
        // Select first enemy by default
        selectedEnemyIndex = 0;
        UpdateEnemySelection();
        
        // Create target indicator if not exists
        CreateTargetIndicator();
    }
    
    /// <summary>
    /// Called when the enemy list changes mid-combat (e.g., SlimeBoss split).
    /// Spawns visuals for any new enemies that don't already have a world unit.
    /// </summary>
    public void OnEnemiesChanged(List<CombatEnemy> allEnemies)
    {
        // Find enemies that don't have a visual unit yet
        foreach (var enemy in allEnemies)
        {
            if (!enemy.IsAlive()) continue;
            
            bool hasUnit = false;
            foreach (var unit in spawnedEnemies)
            {
                if (unit != null && unit.CombatEnemy == enemy)
                {
                    hasUnit = true;
                    break;
                }
            }
            
            if (!hasUnit)
            {
                int index = spawnedEnemies.Count;
                float zOffset = index * enemyVerticalSpacing;
                Vector3 spawnPos = enemyCombatPosition + new Vector3(0f, 0f, zOffset);
                var newUnit = CreateEnemyUnit(enemy, spawnPos, index);
                spawnedEnemies.Add(newUnit);
                enemyBasePosMap[newUnit] = spawnPos;
            }
        }
        
        UpdateEnemySelection();
    }
    
    private EnemyWorldUnit CreateEnemyUnit(CombatEnemy enemyData, Vector3 position, int index)
    {
        // Create enemy game object with visual representation
        GameObject enemyObj = new GameObject($"Enemy_{enemyData.Name}");
        enemyObj.transform.position = position;
        enemyObj.transform.rotation = Quaternion.Euler(0f, -90f, 0f); // Face player (left)
        
        // Add the EnemyWorldUnit component
        var worldUnit = enemyObj.AddComponent<EnemyWorldUnit>();
        
        // Create visual mesh (cube placeholder - can be replaced with actual models later)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(enemyObj.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = new Vector3(enemyScale, enemyScale * 1.5f, enemyScale);
        
        // Set color based on enemy damage element or index
        var renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color color = GetColorForElement(enemyData.DamageElement);
            if (color == Color.white && index < enemyColors.Length)
            {
                color = enemyColors[index];
            }
            renderer.material.color = color;
        }
        
        // Remove collider from visual (we'll add one to parent if needed)
        var collider = visual.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        
        // Initialize the world unit with enemy data
        worldUnit.Initialize(enemyData);
        
        return worldUnit;
    }
    
    private Color GetColorForElement(Element element)
    {
        switch (element)
        {
            case Element.Fire: return new Color(1f, 0.3f, 0.1f);
            case Element.Ice: return new Color(0.5f, 0.8f, 1f);
            case Element.Water: return new Color(0.2f, 0.4f, 0.9f);
            case Element.Wind: return new Color(0.6f, 0.9f, 0.6f);
            case Element.Rock: return new Color(0.6f, 0.4f, 0.2f);
            case Element.Lightning: return new Color(0.9f, 0.9f, 0.2f);
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// Get the enemy unit for a specific CombatEnemy
    /// </summary>
    public EnemyWorldUnit GetEnemyUnit(CombatEnemy enemy)
    {
        foreach (var unit in spawnedEnemies)
        {
            if (unit != null && unit.CombatEnemy == enemy)
            {
                return unit;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Remove a specific enemy unit (when killed)
    /// </summary>
    public void RemoveEnemyUnit(CombatEnemy enemy)
    {
        var unit = GetEnemyUnit(enemy);
        if (unit != null)
        {
            spawnedEnemies.Remove(unit);
            StartCoroutine(DeathAnimation(unit));
        }
    }
    
    private IEnumerator DeathAnimation(EnemyWorldUnit unit)
    {
        // Simple death animation - shrink and fade
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = unit.transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            unit.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        
        Destroy(unit.gameObject);
    }
    
    #region UI Creation and Management
    
    private void CreateCombatUI()
    {
        // Create canvas for combat UI
        GameObject canvasObj = new GameObject("CombatArenaCanvas");
        canvasObj.transform.SetParent(transform);
        combatCanvas = canvasObj.AddComponent<Canvas>();
        combatCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        combatCanvas.sortingOrder = 100;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create End Turn container (center, above skills)
        CreateEndTurnButton();
        
        // Create Skills container (bottom of screen)
        CreateSkillsContainer();
        
        // Create Health container (bottom left - red circle)
        CreateHealthContainer();
        
        // Create unified status display (buffs + debuffs) above health bar
        // Health container is at (20, 20) with height 36, so position chips above it
        playerStatusDisplay = new StatusDisplayUI(
            combatCanvas.transform, false, new Vector2(20, 64),
            ShowTooltipInternal, HideTooltipInternal);
        
        // Create Energy container (bottom right - dark circle)
        CreateEnergyContainer();
        
        // Create tooltip for enemy intent hover
        CreateTooltip();
        
        // Initially hide the UI
        combatCanvas.gameObject.SetActive(false);
    }
    
    private void CreateEndTurnButton()
    {
        // Container for end turn button and AP display
        endTurnContainer = new GameObject("EndTurnContainer");
        endTurnContainer.transform.SetParent(combatCanvas.transform);
        
        var containerRect = endTurnContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.2f);
        containerRect.anchorMax = new Vector2(0.5f, 0.2f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(220, 100);
        
        // AP Display text (above the button)
        var apObj = new GameObject("APDisplay");
        apObj.transform.SetParent(endTurnContainer.transform);
        var apRect = apObj.AddComponent<RectTransform>();
        apRect.anchorMin = new Vector2(0, 0.6f);
        apRect.anchorMax = new Vector2(1, 1);
        apRect.offsetMin = Vector2.zero;
        apRect.offsetMax = Vector2.zero;
        
        apDisplayText = apObj.AddComponent<TextMeshProUGUI>();
        apDisplayText.text = "AP: 10 / 10";
        apDisplayText.fontSize = 28;
        apDisplayText.fontStyle = FontStyles.Bold;
        apDisplayText.alignment = TextAlignmentOptions.Center;
        apDisplayText.color = new Color(0.9f, 0.75f, 0.2f); // Gold/yellow color
        
        // Button background - dark red like reference image
        var buttonObj = new GameObject("EndTurnButton");
        buttonObj.transform.SetParent(endTurnContainer.transform);
        
        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 0);
        buttonRect.anchorMax = new Vector2(1, 0.55f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;
        
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.5f, 0.15f, 0.15f); // Dark red
        
        endTurnButton = buttonObj.AddComponent<Button>();
        endTurnButton.targetGraphic = buttonImage;
        
        // Set button colors for hover/press states
        var colors = endTurnButton.colors;
        colors.normalColor = new Color(0.5f, 0.15f, 0.15f);
        colors.highlightedColor = new Color(0.6f, 0.2f, 0.2f);
        colors.pressedColor = new Color(0.4f, 0.1f, 0.1f);
        endTurnButton.colors = colors;
        
        // Button text
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "END TURN";
        text.fontSize = 22;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        
        // Wire up button click
        endTurnButton.onClick.AddListener(OnEndTurnClicked);
    }
    
    private void CreateSkillsContainer()
    {
        // ========== SKILL CONTAINER SIZE ==========
        // EDIT containerHeight to change the overall skills bar height
        float containerHeight = 95f; // 20% larger (was 70)
        
        skillsContainer = new GameObject("SkillsContainer");
        skillsContainer.transform.SetParent(combatCanvas.transform);
        
        var containerRect = skillsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.2f, 0f);
        containerRect.anchorMax = new Vector2(0.8f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = new Vector2(0, 15);
        containerRect.sizeDelta = new Vector2(0, containerHeight);
        
        // Background panel - dark with slight transparency
        var bgImage = skillsContainer.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        
        // Border
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(skillsContainer.transform);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2, -2);
        borderRect.offsetMax = new Vector2(2, 2);
        var borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(0.3f, 0.3f, 0.3f);
        borderObj.transform.SetAsFirstSibling();
        
        // Horizontal layout for skills
        var layout = skillsContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.padding = new RectOffset(15, 15, 8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }
    
    private void PopulateSkillButtons()
    {
        // Clear existing skill buttons
        foreach (var data in skillButtonsData)
        {
            if (data.buttonObj != null) Destroy(data.buttonObj);
        }
        skillButtonsData.Clear();
        
        if (currentPlayer == null || !currentPlayer.HasCharacter()) return;
        
        var character = currentPlayer.GetCharacter();
        
        // Create buttons for skills 1-5 (skill 5 is ultimate)
        // Use player.GetSkillElement() to get runtime infused element (from sigils), not base element
        CreateSkillButton(1, character.Skill1, character.Skill1APCost, GetElementString(currentPlayer.GetSkillElement(1)), false);
        CreateSkillButton(2, character.Skill2, character.Skill2APCost, GetElementString(currentPlayer.GetSkillElement(2)), false);
        CreateSkillButton(3, character.Skill3, character.Skill3APCost, GetElementString(currentPlayer.GetSkillElement(3)), false);
        CreateSkillButton(4, character.Skill4, character.Skill4APCost, GetElementString(currentPlayer.GetSkillElement(4)), false);
        CreateSkillButton(5, character.Skill5, character.Skill5APCost, GetElementString(currentPlayer.GetSkillElement(5)), true);
        
        // Update AP display
        UpdateAPDisplay();
        
        // Update skill button states
        UpdateSkillButtonStates();
    }
    
    private void CreateSkillButton(int skillNumber, string skillName, int apCost, string element, bool isUltimate)
    {
        if (string.IsNullOrEmpty(skillName)) return;
        
        // ========== SKILL BUTTON SIZE ==========
        // EDIT these values to change individual button dimensions
        float buttonWidth = 160f;  // 20% larger (was 90)
        float buttonHeight = 85f;  // 20% larger (was 54)
        
        var buttonObj = new GameObject($"Skill{skillNumber}");
        buttonObj.transform.SetParent(skillsContainer.transform);
        
        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        
        // Background
        var bgImage = buttonObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f);
        
        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = bgImage;
        
        // Border
        var borderObj = new GameObject("Border");
        borderObj.transform.SetParent(buttonObj.transform);
        var borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-1, -1);
        borderRect.offsetMax = new Vector2(1, 1);
        var borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(0.4f, 0.4f, 0.4f);
        borderObj.transform.SetAsFirstSibling();
        
        // Keybind number (top-left corner)
        var keybindObj = new GameObject("Keybind");
        keybindObj.transform.SetParent(buttonObj.transform);
        var keybindRect = keybindObj.AddComponent<RectTransform>();
        keybindRect.anchorMin = new Vector2(0, 0.65f);
        keybindRect.anchorMax = new Vector2(0.3f, 1);
        keybindRect.offsetMin = new Vector2(3, 0);
        keybindRect.offsetMax = new Vector2(0, -2);
        var keybindText = keybindObj.AddComponent<TextMeshProUGUI>();
        keybindText.text = skillNumber.ToString();
        // EDIT keybind font size here
        keybindText.fontSize = 16; // 20% larger (was 12)
        keybindText.alignment = TextAlignmentOptions.TopLeft;
        keybindText.color = new Color(0.8f, 0.7f, 0.2f);
        
        // AP cost (top-right corner)
        var apObj = new GameObject("APCost");
        apObj.transform.SetParent(buttonObj.transform);
        var apRect = apObj.AddComponent<RectTransform>();
        apRect.anchorMin = new Vector2(0.5f, 0.65f);
        apRect.anchorMax = new Vector2(1, 1);
        apRect.offsetMin = new Vector2(0, 0);
        apRect.offsetMax = new Vector2(-3, -2);
        var apText = apObj.AddComponent<TextMeshProUGUI>();
        // Show energy cost for ultimate instead of AP
        apText.text = isUltimate ? $"{currentPlayer.GetUltimateEnergyCost()}E" : $"{apCost} AP";
        // EDIT AP cost font size here
        apText.fontSize = 16; // 20% larger (was 11)
        apText.alignment = TextAlignmentOptions.TopRight;
        apText.color = new Color(0.8f, 0.7f, 0.2f);
        
        // Skill name (center-bottom)
        var nameObj = new GameObject("SkillName");
        nameObj.transform.SetParent(buttonObj.transform);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.15f);
        nameRect.anchorMax = new Vector2(1, 0.65f);
        nameRect.offsetMin = new Vector2(2, 0);
        nameRect.offsetMax = new Vector2(-2, 0);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = skillName.ToUpper();
        // EDIT skill name font size here
        nameText.fontSize = 16; // 20% larger (was 10)
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.white;
        
        // Element text (bottom, only if element is not "none")
        TextMeshProUGUI elemText = null;
        if (!string.IsNullOrEmpty(element))
        {
            var elemObj = new GameObject("Element");
            elemObj.transform.SetParent(buttonObj.transform);
            var elemRect = elemObj.AddComponent<RectTransform>();
            elemRect.anchorMin = new Vector2(0, 0);
            elemRect.anchorMax = new Vector2(1, 0.2f);
            elemRect.offsetMin = new Vector2(2, 2);
            elemRect.offsetMax = new Vector2(-2, 0);
            elemText = elemObj.AddComponent<TextMeshProUGUI>();
            elemText.text = element.ToUpper();
            elemText.fontSize = 14;
            elemText.alignment = TextAlignmentOptions.Center;
            elemText.color = GetElementColor(element);
        }
        
        // Cooldown text (overlay, hidden by default)
        var cdObj = new GameObject("Cooldown");
        cdObj.transform.SetParent(buttonObj.transform);
        var cdRect = cdObj.AddComponent<RectTransform>();
        cdRect.anchorMin = Vector2.zero;
        cdRect.anchorMax = Vector2.one;
        cdRect.offsetMin = Vector2.zero;
        cdRect.offsetMax = Vector2.zero;
        var cooldownText = cdObj.AddComponent<TextMeshProUGUI>();
        cooldownText.text = "";
        cooldownText.fontSize = 14;
        cooldownText.fontStyle = FontStyles.Bold;
        cooldownText.alignment = TextAlignmentOptions.Center;
        cooldownText.color = new Color(1f, 0.5f, 0.5f);
        cooldownText.gameObject.SetActive(false);
        
        // Wire up button click
        int capturedSkillNumber = skillNumber;
        button.onClick.AddListener(() => OnSkillClicked(capturedSkillNumber));
        
        // Store button data for state updates
        var data = new SkillButtonData
        {
            buttonObj = buttonObj,
            button = button,
            bgImage = bgImage,
            borderImage = borderImage,
            nameText = nameText,
            apText = apText,
            cooldownText = cooldownText,
            skillNumber = skillNumber,
            baseElement = element,
            baseAPCost = apCost,
            isUltimate = isUltimate
        };
        skillButtonsData.Add(data);
        
        // Apply element color to button background if skill has element
        if (!string.IsNullOrEmpty(element) && element.ToLower() != "physical")
        {
            Color elemColor = GetElementColor(element);
            bgImage.color = new Color(elemColor.r * 0.3f, elemColor.g * 0.3f, elemColor.b * 0.3f);
        }
    }
    
    private Color GetElementColor(string element) => ElementColors.Get(element);
    
    private string GetElementString(Element element)
    {
        switch (element)
        {
            case Element.Fire: return "fire";
            case Element.Ice: return "ice";
            case Element.Water: return "water";
            case Element.Wind: return "wind";
            case Element.Rock: return "rock";
            case Element.Lightning: return "lightning";
            default: return "Physical";
        }
    }
    
    private void UpdateSkillButtonStates()
    {
        if (currentPlayer == null) return;
        
        bool hasMaxEnergy = currentPlayer.GetEnergy() >= currentPlayer.GetMaxEnergy();
        
        foreach (var data in skillButtonsData)
        {
            // Get cooldown for this skill (skill number is 1-indexed, cooldown array is 0-indexed)
            int cooldown = currentPlayer.GetSkillCooldown(data.skillNumber - 1);
            bool isOnCooldown = cooldown > 0;
            
            // Get effective AP cost (accounts for Rhythm Discount, Cooldown Lottery, Elemental Fog)
            int effectiveCost = data.baseAPCost;
            if (!data.isUltimate && currentPlayer.Relics != null)
            {
                effectiveCost = currentPlayer.Relics.GetEffectiveAPCost(data.skillNumber - 1, data.baseAPCost);
            }
            
            // Update AP cost text dynamically
            if (data.apText != null && !data.isUltimate)
            {
                data.apText.text = $"{effectiveCost} AP";
                // Yellow text if discounted
                bool highlighted = currentPlayer.Relics != null && currentPlayer.Relics.IsSkillHighlighted(data.skillNumber - 1);
                data.apText.color = highlighted ? new Color(1f, 0.85f, 0.2f) : new Color(0.8f, 0.7f, 0.2f);
            }
            
            // Yellow border for highlighted skills
            if (data.borderImage != null && !data.isUltimate)
            {
                bool highlighted = currentPlayer.Relics != null && currentPlayer.Relics.IsSkillHighlighted(data.skillNumber - 1);
                data.borderImage.color = highlighted ? new Color(0.9f, 0.75f, 0.1f) : new Color(0.4f, 0.4f, 0.4f);
            }
            
            bool canAfford = currentAP >= effectiveCost;
            bool canUse = canAfford && !isOnCooldown;
            
            // Ultimate requires max energy
            if (data.isUltimate)
            {
                canUse = hasMaxEnergy && canAfford && !isOnCooldown;
            }
            
            // Update button interactability
            data.button.interactable = canUse;
            
            // Handle cooldown display
            if (isOnCooldown)
            {
                // Show cooldown overlay
                data.cooldownText.gameObject.SetActive(true);
                data.cooldownText.text = $"CD:{cooldown}";
                data.bgImage.color = new Color(0.08f, 0.08f, 0.08f);
                data.nameText.color = new Color(0.3f, 0.3f, 0.3f);
            }
            else
            {
                // Hide cooldown overlay
                data.cooldownText.gameObject.SetActive(false);
                
                if (data.isUltimate && !hasMaxEnergy)
                {
                    // Gray out ultimate when not enough energy
                    data.bgImage.color = new Color(0.1f, 0.1f, 0.1f);
                    data.nameText.color = new Color(0.5f, 0.5f, 0.5f);
                }
                else if (!canAfford)
                {
                    // Darken when can't afford AP
                    data.bgImage.color = new Color(0.12f, 0.12f, 0.12f);
                    data.nameText.color = new Color(0.5f, 0.5f, 0.5f);
                }
                else
                {
                    // Normal state - use element color if applicable
                    if (!string.IsNullOrEmpty(data.baseElement) && data.baseElement.ToLower() != "physical")
                    {
                        Color elemColor = GetElementColor(data.baseElement);
                        data.bgImage.color = new Color(elemColor.r * 0.3f, elemColor.g * 0.3f, elemColor.b * 0.3f);
                    }
                    else
                    {
                        data.bgImage.color = new Color(0.15f, 0.15f, 0.15f);
                    }
                    data.nameText.color = Color.white;
                }
            }
        }
    }
    
    private void UpdateAPDisplay()
    {
        // Always sync from Player — authoritative source for AP (relics may modify mid-turn)
        if (currentPlayer != null)
        {
            maxAP = currentPlayer.GetMaxAP();
            currentAP = currentPlayer.GetCurrentAP();
        }
        
        if (apDisplayText != null)
        {
            apDisplayText.text = $"AP: {currentAP} / {maxAP}";
        }
    }
    
    private void OnEndTurnClicked()
    {
        if (combatManager != null)
        {
            combatManager.EndPlayerTurn();
        }
    }
    
    private void OnSkillClicked(int skillNumber)
    {
        if (combatManager == null || currentPlayer == null) return;
        if (combatManager.IsQTEActive()) return; // Block skills during QTE
        
        // Find the skill data to get AP cost
        SkillButtonData skillData = null;
        foreach (var data in skillButtonsData)
        {
            if (data.skillNumber == skillNumber)
            {
                skillData = data;
                break;
            }
        }
        
        if (skillData == null) return;
        
        // Get effective AP cost (accounts for relic modifiers)
        int effectiveCost = skillData.baseAPCost;
        if (!skillData.isUltimate && currentPlayer.Relics != null)
        {
            effectiveCost = currentPlayer.Relics.GetEffectiveAPCost(skillData.skillNumber - 1, skillData.baseAPCost);
        }
        
        // Check if we can afford the skill
        if (currentAP < effectiveCost) return;
        
        // For ultimate, also check energy
        if (skillData.isUltimate && !currentPlayer.CanUseUltimate()) return;
        
        // Check cooldown
        int cooldown = currentPlayer.GetSkillCooldown(skillNumber - 1);
        if (cooldown > 0) return;
        
        // Execute skill on currently selected target (no confirmation needed)
        // Ensure selected target is valid and alive
        EnsureValidTarget();
        
        if (selectedEnemyIndex >= 0 && selectedEnemyIndex < spawnedEnemies.Count)
        {
            var selectedUnit = spawnedEnemies[selectedEnemyIndex];
            if (selectedUnit != null && selectedUnit.IsAlive)
            {
                CombatEnemy target = selectedUnit.CombatEnemy;
                if (target != null && combatManager != null)
                {
                    currentAP -= effectiveCost;
                    combatManager.OnPlayerSkillTarget(skillNumber, target);
                }
            }
        }
        
        // Update UI
        UpdateAPDisplay();
        UpdateSkillButtonStates();
    }
    
    /// <summary>
    /// Called after mid-turn effects (e.g. reaction buffs) change AP values.
    /// Syncs AP from Player and refreshes display + skill states.
    /// </summary>
    public void RefreshAPDisplay()
    {
        UpdateAPDisplay();
        UpdateSkillButtonStates();
    }
    
    /// <summary>
    /// Called at the start of player's turn to reset AP
    /// </summary>
    public void OnPlayerTurnStart()
    {
        if (currentPlayer != null && currentPlayer.HasCharacter())
        {
            // Sync from Player (currentAP may differ from max due to relics like First Pulse, Cracked Battery)
            maxAP = currentPlayer.GetMaxAP();
            currentAP = currentPlayer.GetCurrentAP();
            UpdateAPDisplay();
            UpdateSkillButtonStates();
        }
    }
    
    /// <summary>
    /// Called when player energy changes (for ultimate availability)
    /// </summary>
    public void OnPlayerEnergyChanged()
    {
        UpdateSkillButtonStates();
    }
    
    private void ShowCombatUI()
    {
        if (combatCanvas != null)
        {
            combatCanvas.gameObject.SetActive(true);
            PopulateSkillButtons();
            
            // Recreate player status display (HideCombatUI destroys it each combat)
            if (playerStatusDisplay == null)
            {
                playerStatusDisplay = new StatusDisplayUI(
                    combatCanvas.transform, false, new Vector2(20, 64),
                    ShowTooltipInternal, HideTooltipInternal);
            }
        }
    }
    
    private void HideCombatUI()
    {
        if (combatCanvas != null)
        {
            combatCanvas.gameObject.SetActive(false);
        }
        
        // Hide target indicator
        if (targetIndicator != null)
        {
            targetIndicator.SetActive(false);
        }
        
        // Hide tooltip
        HideTooltipInternal();
        
        // Clear status display
        if (playerStatusDisplay != null)
        {
            playerStatusDisplay.Destroy();
            playerStatusDisplay = null;
        }
        StatusDisplayUI.GlobalExpanded = false;
    }
    
    /// <summary>
    /// Hide combat UI before showing loot panel (to prevent blocking input)
    /// Called when combat victory is achieved but before loot collection
    /// </summary>
    public void HideCombatUIForLoot()
    {
        HideCombatUI();
        
        // Also destroy player nameplate immediately
        DestroyPlayerNameplate();
        
        // Destroy all spawned enemies immediately to prevent them from blocking input
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        spawnedEnemies.Clear();
        enemyBasePosMap.Clear();
        
        // Hide target indicator
        if (targetIndicator != null)
        {
            Destroy(targetIndicator);
            targetIndicator = null;
        }
    }
    
    private void CreateHealthContainer()
    {
        // Horizontal bar for player health - bottom left
        healthContainer = new GameObject("HealthContainer");
        healthContainer.transform.SetParent(combatCanvas.transform);
        
        var containerRect = healthContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(0f, 0f);
        containerRect.pivot = new Vector2(0f, 0f);
        containerRect.anchoredPosition = new Vector2(20, 20);
        containerRect.sizeDelta = new Vector2(260, 36);
        
        // Dark background bar
        var bgImage = healthContainer.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.05f, 0.05f);
        
        // Border outline
        var outline = healthContainer.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.1f, 0.1f);
        outline.effectDistance = new Vector2(2, 2);
        
        // Red fill bar (fillAmount tracks current/max HP)
        var fillObj = new GameObject("HealthFill");
        fillObj.transform.SetParent(healthContainer.transform, false);
        
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        
        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = new Color(0.8f, 0.2f, 0.15f);
        
        // Light blue shield overlay (rendered on top of health fill)
        var shieldObj = new GameObject("ShieldFill");
        shieldObj.transform.SetParent(healthContainer.transform, false);
        
        var shieldRect = shieldObj.AddComponent<RectTransform>();
        shieldRect.anchorMin = Vector2.zero;
        shieldRect.anchorMax = new Vector2(0f, 1f); // starts hidden (zero width)
        shieldRect.offsetMin = new Vector2(2, 2);
        shieldRect.offsetMax = new Vector2(-2, -2);
        
        shieldBarFill = shieldObj.AddComponent<Image>();
        shieldBarFill.color = new Color(0.4f, 0.7f, 1f); // Light blue
        
        // Health text overlay
        var textObj = new GameObject("HealthText");
        textObj.transform.SetParent(healthContainer.transform, false);
        
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        healthText = textObj.AddComponent<TextMeshProUGUI>();
        healthText.text = "100 / 100";
        healthText.fontSize = 20;
        healthText.fontStyle = FontStyles.Bold;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;
        healthText.outlineWidth = 0.3f;
        healthText.outlineColor = Color.black;
    }
    
    private void CreateEnergyContainer()
    {
        // Horizontal bar for player energy - bottom right
        energyContainer = new GameObject("EnergyContainer");
        energyContainer.transform.SetParent(combatCanvas.transform);
        
        var containerRect = energyContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1f, 0f);
        containerRect.anchorMax = new Vector2(1f, 0f);
        containerRect.pivot = new Vector2(1f, 0f);
        containerRect.anchoredPosition = new Vector2(-20, 20);
        containerRect.sizeDelta = new Vector2(220, 36);
        
        // Dark background bar
        var bgImage = energyContainer.AddComponent<Image>();
        bgImage.color = new Color(0.05f, 0.05f, 0.15f);
        
        // Border outline
        var outline = energyContainer.AddComponent<Outline>();
        outline.effectColor = new Color(0.15f, 0.15f, 0.4f);
        outline.effectDistance = new Vector2(2, 2);
        
        // Blue fill bar (fillAmount tracks current/max energy)
        var fillObj = new GameObject("EnergyFill");
        fillObj.transform.SetParent(energyContainer.transform, false);
        
        var fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        
        energyBarFill = fillObj.AddComponent<Image>();
        energyBarFill.color = new Color(0.2f, 0.5f, 0.9f);
        
        // Energy text overlay
        var textObj = new GameObject("EnergyText");
        textObj.transform.SetParent(energyContainer.transform, false);
        
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        energyText = textObj.AddComponent<TextMeshProUGUI>();
        energyText.text = "0 / 100";
        energyText.fontSize = 20;
        energyText.fontStyle = FontStyles.Bold;
        energyText.alignment = TextAlignmentOptions.Center;
        energyText.color = Color.white;
        energyText.outlineWidth = 0.3f;
        energyText.outlineColor = Color.black;
    }
    
    private void UpdateHealthEnergyDisplays()
    {
        if (currentPlayer == null) return;
        
        int hp = currentPlayer.GetHealth();
        int maxHp = currentPlayer.GetMaxHealth();
        int energy = currentPlayer.GetEnergy();
        int maxEnergy = currentPlayer.GetMaxEnergy();
        
        // Update health bar fill via RectTransform anchor width
        int shield = currentPlayer.GetShield();
        if (healthBarFill != null)
        {
            float hpPercent = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            var rt = healthBarFill.rectTransform;
            rt.anchorMax = new Vector2(hpPercent, rt.anchorMax.y);
        }
        // Shield overlay: fills from left, width = shield / maxHP
        if (shieldBarFill != null)
        {
            float shieldPercent = maxHp > 0 ? Mathf.Clamp01((float)shield / maxHp) : 0f;
            var rt = shieldBarFill.rectTransform;
            rt.anchorMax = new Vector2(shieldPercent, rt.anchorMax.y);
        }
        if (healthText != null)
        {
            if (shield > 0)
                healthText.text = $"{hp} / {maxHp}  (+{shield})";
            else
                healthText.text = $"{hp} / {maxHp}";
        }
        
        // Update energy bar fill via RectTransform anchor width
        if (energyBarFill != null)
        {
            float energyPercent = maxEnergy > 0 ? Mathf.Clamp01((float)energy / maxEnergy) : 0f;
            var rt = energyBarFill.rectTransform;
            rt.anchorMax = new Vector2(energyPercent, rt.anchorMax.y);
        }
        if (energyText != null)
        {
            energyText.text = $"{energy} / {maxEnergy}";
        }
    }
    
    private void CreateTooltip()
    {
        // Tooltip panel on screen-space overlay canvas (always visible, not clipped by 3D)
        tooltipPanel = new GameObject("IntentTooltip");
        tooltipPanel.transform.SetParent(combatCanvas.transform, false);
        
        var panelRect = tooltipPanel.AddComponent<RectTransform>();
        panelRect.pivot = new Vector2(1f, 0f); // Top-right pivot so it expands left+up from cursor
        panelRect.sizeDelta = new Vector2(320, 0); // Width fixed, height auto from layout
        
        // Force tooltip to render in front of everything (above details panels at 500)
        var tooltipCanvas = tooltipPanel.AddComponent<Canvas>();
        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = 1000;
        tooltipPanel.AddComponent<GraphicRaycaster>();
        
        // Dark background
        var panelImage = tooltipPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        panelImage.raycastTarget = false;
        
        // Border
        var outline = tooltipPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.6f, 0.5f, 0.2f, 0.9f);
        outline.effectDistance = new Vector2(2, 2);
        
        // Auto-size height to fit content
        var layout = tooltipPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 8, 8);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        var fitter = tooltipPanel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Description text
        var textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipPanel.transform, false);
        
        tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.text = "";
        tooltipText.fontSize = 18;
        tooltipText.color = new Color(0.9f, 0.85f, 0.7f);
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.raycastTarget = false;
        
        // LayoutElement to let text drive the panel height
        var layoutElem = textObj.AddComponent<LayoutElement>();
        layoutElem.preferredWidth = 300;
        
        tooltipPanel.SetActive(false);
        tooltipVisible = false;
    }
    
    /// <summary>
    /// Show the intent tooltip with the given description text. Called by EnemyWorldUnit on hover.
    /// </summary>
    public static void ShowTooltip(string text)
    {
        if (_instance != null) _instance.ShowTooltipInternal(text);
    }
    
    /// <summary>
    /// Hide the intent tooltip. Called by EnemyWorldUnit on hover exit.
    /// </summary>
    public static void HideTooltip()
    {
        if (_instance != null) _instance.HideTooltipInternal();
    }
    
    private void ShowTooltipInternal(string text)
    {
        if (tooltipPanel == null || string.IsNullOrEmpty(text)) return;
        
        tooltipText.text = text;
        tooltipPanel.SetActive(true);
        tooltipVisible = true;
        
        // Position tooltip at current mouse position (stays fixed, no follow)
        var rt = tooltipPanel.GetComponent<RectTransform>();
        Vector2 mousePos = Input.mousePosition;
        float x = mousePos.x - 16f;
        float y = mousePos.y + 16f;
        float tooltipWidth = rt.sizeDelta.x;
        float tooltipHeight = rt.sizeDelta.y > 0 ? rt.sizeDelta.y : 100f;
        if (x - tooltipWidth < 0) x = tooltipWidth;
        if (y + tooltipHeight > Screen.height) y = Screen.height - tooltipHeight;
        rt.position = new Vector3(x, y, 0f);
    }
    
    private void HideTooltipInternal()
    {
        if (tooltipPanel == null) return;
        
        tooltipPanel.SetActive(false);
        tooltipVisible = false;
    }
    
    #endregion
    
    #region Targeting System
    
    private void CreateTargetIndicator()
    {
        if (targetIndicator != null) return;
        
        // Create yellow triangle indicator
        targetIndicator = new GameObject("TargetIndicator");
        
        // Create a simple triangle mesh
        var meshFilter = targetIndicator.AddComponent<MeshFilter>();
        var meshRenderer = targetIndicator.AddComponent<MeshRenderer>();
        
        // Create triangle mesh (pointing down)
        Mesh triangleMesh = new Mesh();
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.3f, 0f, 0f),   // Left
            new Vector3(0.3f, 0f, 0f),    // Right
            new Vector3(0f, -0.5f, 0f)    // Bottom point
        };
        int[] triangles = new int[] { 0, 1, 2 };
        triangleMesh.vertices = vertices;
        triangleMesh.triangles = triangles;
        triangleMesh.RecalculateNormals();
        
        meshFilter.mesh = triangleMesh;
        
        // Yellow material
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(1f, 0.9f, 0.2f); // Yellow
        meshRenderer.material = mat;
        
        // Render behind enemy world-space canvas (sortingOrder=100)
        meshRenderer.sortingOrder = 90;
        
        targetIndicator.transform.localScale = Vector3.one * 1.5f;
        targetIndicator.SetActive(false);
    }
    
    private void UpdateTargetIndicator()
    {
        if (targetIndicator == null || spawnedEnemies.Count == 0) return;
        
        // Only show when in combat
        if (!inCombat)
        {
            targetIndicator.SetActive(false);
            return;
        }
        
        // Get current selected enemy
        if (selectedEnemyIndex >= 0 && selectedEnemyIndex < spawnedEnemies.Count)
        {
            var selectedEnemy = spawnedEnemies[selectedEnemyIndex];
            if (selectedEnemy != null && selectedEnemy.IsAlive)
            {
                targetIndicator.SetActive(true);
                
                // Position above the enemy nameplate using dynamic height
                Vector3 enemyPos = selectedEnemy.transform.position;
                float indicatorY = selectedEnemy.GetIndicatorWorldY();
                targetIndicator.transform.position = new Vector3(enemyPos.x, indicatorY, enemyPos.z);
                
                // Make it face the camera
                if (Camera.main != null)
                {
                    targetIndicator.transform.LookAt(targetIndicator.transform.position + Camera.main.transform.forward);
                }
            }
            else
            {
                targetIndicator.SetActive(false);
            }
        }
    }
    
    private void SelectNextEnemy()
    {
        if (spawnedEnemies.Count == 0) return;
        
        int startIndex = selectedEnemyIndex;
        do
        {
            selectedEnemyIndex = (selectedEnemyIndex + 1) % spawnedEnemies.Count;
            if (spawnedEnemies[selectedEnemyIndex] != null && spawnedEnemies[selectedEnemyIndex].IsAlive)
            {
                UpdateEnemySelection();
                return;
            }
        } while (selectedEnemyIndex != startIndex);
    }
    
    private void SelectPreviousEnemy()
    {
        if (spawnedEnemies.Count == 0) return;
        
        int startIndex = selectedEnemyIndex;
        do
        {
            selectedEnemyIndex--;
            if (selectedEnemyIndex < 0) selectedEnemyIndex = spawnedEnemies.Count - 1;
            if (spawnedEnemies[selectedEnemyIndex] != null && spawnedEnemies[selectedEnemyIndex].IsAlive)
            {
                UpdateEnemySelection();
                return;
            }
        } while (selectedEnemyIndex != startIndex);
    }
    
    private void UpdateEnemySelection()
    {
        // Update visual selection on all enemies
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null)
            {
                spawnedEnemies[i].SetSelected(i == selectedEnemyIndex);
            }
        }
    }
    
    /// <summary>
    /// Called when an enemy dies. If the dead enemy was the selected target,
    /// auto-advance to the next alive enemy so the player isn't stuck.
    /// </summary>
    public void OnEnemyDied(CombatEnemy deadEnemy)
    {
        // Find the index of the dead enemy
        int deadIndex = -1;
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null && spawnedEnemies[i].CombatEnemy == deadEnemy)
            {
                deadIndex = i;
                break;
            }
        }
        
        // If the dead enemy was the selected target, advance to next alive
        if (deadIndex >= 0 && deadIndex == selectedEnemyIndex)
        {
            SelectNextAliveEnemy();
        }
    }
    
    /// <summary>
    /// Select the first alive enemy starting from the current index.
    /// </summary>
    private void SelectNextAliveEnemy()
    {
        for (int i = 0; i < spawnedEnemies.Count; i++)
        {
            if (spawnedEnemies[i] != null && spawnedEnemies[i].IsAlive)
            {
                selectedEnemyIndex = i;
                UpdateEnemySelection();
                return;
            }
        }
    }
    
    /// <summary>
    /// Ensures the currently selected enemy index points to a valid alive enemy.
    /// If the current selection is invalid or dead, auto-selects the next alive enemy.
    /// </summary>
    private void EnsureValidTarget()
    {
        if (selectedEnemyIndex >= 0 && selectedEnemyIndex < spawnedEnemies.Count &&
            spawnedEnemies[selectedEnemyIndex] != null && spawnedEnemies[selectedEnemyIndex].IsAlive)
        {
            return; // Current selection is valid
        }
        
        SelectNextAliveEnemy();
    }
    
    /// <summary>
    /// Get the currently selected enemy for targeting
    /// </summary>
    public CombatEnemy GetSelectedEnemy()
    {
        if (selectedEnemyIndex >= 0 && selectedEnemyIndex < spawnedEnemies.Count)
        {
            var unit = spawnedEnemies[selectedEnemyIndex];
            if (unit != null && unit.IsAlive)
            {
                return unit.CombatEnemy;
            }
        }
        return null;
    }
    
    #endregion
    
    #region Floating Text Support
    
    /// <summary>
    /// Get the transform of the player for floating text
    /// </summary>
    public Transform GetPlayerTransform()
    {
        return playerTransform;
    }
    
    /// <summary>
    /// Get the transform of an enemy for floating text
    /// </summary>
    public Transform GetEnemyTransform(CombatEnemy enemy)
    {
        var unit = GetEnemyUnit(enemy);
        return unit != null ? unit.transform : null;
    }
    
    #endregion
    
    #region Player Nameplate
    
    private void CreatePlayerNameplate()
    {
        if (playerTransform == null || currentPlayer == null) return;
        
        // Create world-space canvas for player nameplate
        GameObject canvasObj = new GameObject("PlayerUI");
        canvasObj.transform.SetParent(playerTransform);
        
        float localY = playerNameplateHeight / playerCombatScale.y;
        canvasObj.transform.localPosition = new Vector3(0f, localY, 0f);
        
        playerWorldCanvas = canvasObj.AddComponent<Canvas>();
        playerWorldCanvas.renderMode = RenderMode.WorldSpace;
        playerWorldCanvas.sortingOrder = 100;
        
        var canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(325f, 120f);
        float scaleCompensation = 0.01f / playerCombatScale.x;
        canvasRect.localScale = Vector3.one * scaleCompensation;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;
        
        // GraphicRaycaster required for pointer events (tooltip hover)
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Chip container with background
        GameObject chipObj = new GameObject("NameplateChip");
        chipObj.transform.SetParent(canvasObj.transform, false);
        
        var chipRect = chipObj.AddComponent<RectTransform>();
        chipRect.anchorMin = new Vector2(0.5f, 0.5f);
        chipRect.anchorMax = new Vector2(0.5f, 0.5f);
        chipRect.pivot = new Vector2(0.5f, 0.5f);
        chipRect.anchoredPosition = Vector2.zero;
        chipRect.sizeDelta = new Vector2(220f, 36f);
        
        // Dark background
        var chipBg = chipObj.AddComponent<UnityEngine.UI.Image>();
        chipBg.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);
        chipBg.raycastTarget = true;
        
        // Green border for player
        var chipOutline = chipObj.AddComponent<UnityEngine.UI.Outline>();
        chipOutline.effectColor = new Color(0.3f, 0.9f, 0.4f, 1f);
        chipOutline.effectDistance = new Vector2(2, 2);
        
        // Name text inside chip
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(chipObj.transform, false);
        
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = new Vector2(6, 0);
        nameRect.offsetMax = new Vector2(-6, 0);
        
        playerNameText = nameObj.AddComponent<TextMeshProUGUI>();
        playerNameText.text = currentPlayer.HasCharacter() ? currentPlayer.GetCharacter().DisplayName : "Player";
        playerNameText.fontSize = 28f;
        playerNameText.fontStyle = FontStyles.Bold;
        playerNameText.color = Color.white;
        playerNameText.alignment = TextAlignmentOptions.Center;
        playerNameText.textWrappingMode = TextWrappingModes.NoWrap;
        playerNameText.raycastTarget = false;
        
        playerNameText.outlineWidth = 0.3f;
        playerNameText.outlineColor = Color.black;
        
        // Add hover events via EventTrigger (tooltip via screen-space)
        var trigger = chipObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        
        var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => ShowPlayerTooltip());
        trigger.triggers.Add(pointerEnter);
        
        var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => HideTooltipInternal());
        trigger.triggers.Add(pointerExit);
    }
    
    private void ShowPlayerTooltip()
    {
        if (currentPlayer == null) return;
        
        string dmgRange = currentPlayer.HasCharacter() ? currentPlayer.GetCharacter().DamageRangeLabel : "?";
        
        string content = 
            $"<color=#ff8888>DMG:</color> {dmgRange}\n" +
            $"<color=#cccccc>Phys Resist:</color> {currentPlayer.GetPhysicalResist()}%\n" +
            $"<color=#88bbff>Elem Resist:</color> {currentPlayer.GetElementalResist()}%";
        
        ShowTooltipInternal(content);
    }
    
    private void DestroyPlayerNameplate()
    {
        if (playerWorldCanvas != null)
        {
            Destroy(playerWorldCanvas.gameObject);
            playerWorldCanvas = null;
            playerNameText = null;
        }
    }
    
    #endregion
    
    #region Node Visibility Management
    
    private void HideWorldNodes()
    {
        hiddenNodes.Clear();
        
        // Find all NodeBase objects in the scene
        var allNodes = Object.FindObjectsByType<NodeBase>(FindObjectsSortMode.None);
        foreach (var node in allNodes)
        {
            if (node != null && node.gameObject.activeSelf)
            {
                node.gameObject.SetActive(false);
                hiddenNodes.Add(node.gameObject);
            }
        }
    }
    
    private void ShowWorldNodes()
    {
        foreach (var nodeObj in hiddenNodes)
        {
            if (nodeObj != null)
            {
                nodeObj.SetActive(true);
            }
        }
        hiddenNodes.Clear();
    }
    
    #endregion
}
