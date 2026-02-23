using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Visual representation of an enemy in world-space combat.
/// Displays health bar and nameplate above the enemy.
/// </summary>
public class EnemyWorldUnit : MonoBehaviour
{
    [Header("Health Bar Settings - 30% larger for readability")]
    [SerializeField] private float healthBarWidth = 3.25f;     // 2.5 * 1.3 = 3.25
    [SerializeField] private float healthBarHeight = 0.325f;   // 0.25 * 1.3 = 0.325
    [SerializeField] private float healthBarYOffset = 4f;      // Raised higher
    [SerializeField] private float healthAnimationSpeed = 2f;
    
    [Header("Colors")]
    [SerializeField] private Color healthBarBackground = new Color(0.2f, 0.2f, 0.2f, 0.9f);
    [SerializeField] private Color healthBarFill = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color healthBarDamage = new Color(0.8f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color nameplateColor = Color.white;
    
    private CombatEnemy combatEnemy;
    private Canvas worldCanvas;
    private Image healthBarBg;
    private Image healthBarGreen;
    private Image healthBarRed;
    private Image shieldBarFill;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI healthText;
    
    // Intent display (next skill preview)
    private TextMeshProUGUI intentText;
    private Image intentBg;
    private string cachedIntentDescription = "";
    private string lastIntentSkillId = "";
    
    // Elemental mark display
    private GameObject markContainer;
    private Dictionary<Element, MarkChipData> markChips = new Dictionary<Element, MarkChipData>();
    
    // Unified status display (buffs + debuffs)
    private StatusDisplayUI enemyStatusDisplay;
    
    private class MarkChipData
    {
        public GameObject chipObj;
        public TextMeshProUGUI labelText;
        public Element element;
        public string tooltipText;
    }
    
    private float displayedHealthPercent = 1f;
    private float targetHealthPercent = 1f;
    private int lastDisplayedHealth;
    private int lastDisplayedShield;
    
    // Nameplate tooltip content (rendered via CombatArena screen-space tooltip)
    
    private bool isSelected = false;
    // Selection indicator removed - using targetIndicator in CombatArena instead
    
    public CombatEnemy CombatEnemy => combatEnemy;
    public bool IsAlive => combatEnemy != null && combatEnemy.IsAlive();

    public void Initialize(CombatEnemy enemy)
    {
        combatEnemy = enemy;
        lastDisplayedHealth = enemy.Health;
        lastDisplayedShield = enemy.Shield;
        displayedHealthPercent = 1f;
        targetHealthPercent = 1f;
        
        CreateWorldSpaceUI();
        // Selection indicator removed - CombatArena.targetIndicator handles targeting
        UpdateHealthDisplay();
    }
    
    private void Update()
    {
        if (combatEnemy == null) return;
        
        // Check if health changed
        float currentPercent = (float)combatEnemy.Health / combatEnemy.MaxHealth;
        if (!Mathf.Approximately(targetHealthPercent, currentPercent))
        {
            targetHealthPercent = currentPercent;
        }
        
        // Animate health bar
        if (!Mathf.Approximately(displayedHealthPercent, targetHealthPercent))
        {
            displayedHealthPercent = Mathf.MoveTowards(displayedHealthPercent, targetHealthPercent, healthAnimationSpeed * Time.deltaTime);
            UpdateHealthBarVisuals();
        }
        
        // Update health text and shield bar if health or shield changed
        if (lastDisplayedHealth != combatEnemy.Health || lastDisplayedShield != combatEnemy.Shield)
        {
            bool shieldChanged = lastDisplayedShield != combatEnemy.Shield;
            lastDisplayedHealth = combatEnemy.Health;
            lastDisplayedShield = combatEnemy.Shield;
            UpdateHealthText();
            if (shieldChanged) UpdateHealthBarVisuals();
        }
        
        // Update intent display
        UpdateIntentDisplay();
        
        // Update elemental mark display
        UpdateMarkDisplay();
        
        // Update unified status display (buffs + debuffs)
        if (enemyStatusDisplay != null)
            enemyStatusDisplay.UpdateStatuses(combatEnemy.GetActiveStatuses());
        
        // Make canvas face camera
        if (worldCanvas != null && Camera.main != null)
        {
            worldCanvas.transform.LookAt(worldCanvas.transform.position + Camera.main.transform.forward);
        }
    }
    
    private void CreateWorldSpaceUI()
    {
        // Create world-space canvas for health bar and nameplate
        GameObject canvasObj = new GameObject("EnemyUI");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = new Vector3(0f, healthBarYOffset, 0f);
        
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 100;
        worldCanvas.worldCamera = Camera.main;
        
        var canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(healthBarWidth * 100f + 500f, 250f);
        canvasRect.localScale = Vector3.one * 0.01f;
        
        // Add canvas scaler for proper sizing
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;
        
        // GraphicRaycaster required for pointer events (tooltip hover)
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create nameplate text
        CreateNameplate(canvasObj.transform);
        
        // Create health bar
        CreateHealthBar(canvasObj.transform);
        
        // Create intent display (next skill preview)
        CreateIntentDisplay(canvasObj.transform);
        
        // Create elemental mark display (left of enemy body)
        CreateMarkDisplay(canvasObj.transform);
        
        // Create unified status display (centered on enemy body)
        enemyStatusDisplay = new StatusDisplayUI(
            canvasObj.transform, true, new Vector2(0f, -80f),
            (text) => CombatArena.ShowTooltip(text),
            () => CombatArena.HideTooltip());
    }
    
    private void CreateNameplate(Transform parent)
    {
        // Chip container with background
        GameObject chipObj = new GameObject("NameplateChip");
        chipObj.transform.SetParent(parent, false);
        
        var chipRect = chipObj.AddComponent<RectTransform>();
        chipRect.anchorMin = new Vector2(0.5f, 0.5f);
        chipRect.anchorMax = new Vector2(0.5f, 0.5f);
        chipRect.pivot = new Vector2(0.5f, 0f);
        chipRect.anchoredPosition = new Vector2(0f, 22f);
        chipRect.sizeDelta = new Vector2(220f, 36f);
        
        // Dark background
        var chipBg = chipObj.AddComponent<Image>();
        chipBg.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);
        chipBg.raycastTarget = true;
        
        // Colored border based on enemy type
        var chipOutline = chipObj.AddComponent<Outline>();
        chipOutline.effectColor = GetNameplateBorderColor();
        chipOutline.effectDistance = new Vector2(2, 2);
        
        // Name text inside chip
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(chipObj.transform, false);
        
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = new Vector2(6, 0);
        nameRect.offsetMax = new Vector2(-6, 0);
        
        nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = combatEnemy.Name;
        nameText.fontSize = 28f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = nameplateColor;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.raycastTarget = false;
        
        nameText.outlineWidth = 0.3f;
        nameText.outlineColor = Color.black;
        
        // Add hover events via EventTrigger (tooltip via CombatArena screen-space)
        var trigger = chipObj.AddComponent<EventTrigger>();
        
        var pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => ShowTooltip());
        trigger.triggers.Add(pointerEnter);
        
        var pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => HideTooltip());
        trigger.triggers.Add(pointerExit);
    }
    
    private Color GetNameplateBorderColor()
    {
        if (combatEnemy.IsBoss) return new Color(1f, 0.3f, 0.3f, 1f);    // Red for bosses
        if (combatEnemy.IsElite) return new Color(1f, 0.7f, 0.2f, 1f);   // Gold for elites
        return new Color(0.5f, 0.7f, 0.9f, 1f);                          // Blue-grey for regular
    }
    
    private string GetTooltipContent()
    {
        if (combatEnemy == null) return "";
        
        string dmgType = combatEnemy.DamageElement == Element.None ? "Physical" : combatEnemy.DamageElement.ToString();
        string dmgColor = combatEnemy.DamageElement == Element.None ? "#cccccc" : "#88bbff";
        
        return 
            $"<color=#ff8888>DMG:</color> {combatEnemy.DamageMin}-{combatEnemy.DamageMax} <color={dmgColor}>({dmgType})</color>\n" +
            $"<color=#cccccc>Phys Resist:</color> {combatEnemy.PhysicalResist}%\n" +
            $"<color=#88bbff>Elem Resist:</color> {combatEnemy.ElementalResist}%";
    }
    
    private void ShowTooltip()
    {
        CombatArena.ShowTooltip(GetTooltipContent());
    }
    
    private void HideTooltip()
    {
        CombatArena.HideTooltip();
    }
    
    private void CreateHealthBar(Transform parent)
    {
        // Container
        GameObject barContainer = new GameObject("HealthBarContainer");
        barContainer.transform.SetParent(parent, false);
        
        var containerRect = barContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(healthBarWidth * 100f, healthBarHeight * 100f);
        
        // Background (dark)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(barContainer.transform, false);
        
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        healthBarBg = bgObj.AddComponent<Image>();
        healthBarBg.color = healthBarBackground;
        
        // Red overlay (shows damage taken - retroactive decrease via anchor width)
        GameObject redObj = new GameObject("DamageOverlay");
        redObj.transform.SetParent(barContainer.transform, false);
        
        var redRect = redObj.AddComponent<RectTransform>();
        redRect.anchorMin = Vector2.zero;
        redRect.anchorMax = Vector2.one;
        redRect.offsetMin = Vector2.zero;
        redRect.offsetMax = Vector2.zero;
        
        healthBarRed = redObj.AddComponent<Image>();
        healthBarRed.color = healthBarDamage;
        
        // Green fill (current health via anchor width)
        GameObject greenObj = new GameObject("HealthFill");
        greenObj.transform.SetParent(barContainer.transform, false);
        
        var greenRect = greenObj.AddComponent<RectTransform>();
        greenRect.anchorMin = Vector2.zero;
        greenRect.anchorMax = Vector2.one;
        greenRect.offsetMin = Vector2.zero;
        greenRect.offsetMax = Vector2.zero;
        
        healthBarGreen = greenObj.AddComponent<Image>();
        healthBarGreen.color = healthBarFill;
        
        // Light blue shield overlay (rendered on top of green health fill)
        GameObject shieldObj = new GameObject("ShieldFill");
        shieldObj.transform.SetParent(barContainer.transform, false);
        
        var shieldRect = shieldObj.AddComponent<RectTransform>();
        shieldRect.anchorMin = Vector2.zero;
        shieldRect.anchorMax = new Vector2(0f, 1f); // starts hidden
        shieldRect.offsetMin = Vector2.zero;
        shieldRect.offsetMax = Vector2.zero;
        
        shieldBarFill = shieldObj.AddComponent<Image>();
        shieldBarFill.color = new Color(0.4f, 0.7f, 1f); // Light blue
        
        // Health text overlay
        GameObject healthTextObj = new GameObject("HealthText");
        healthTextObj.transform.SetParent(barContainer.transform, false);
        
        var textRect = healthTextObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        healthText = healthTextObj.AddComponent<TextMeshProUGUI>();
        healthText.fontSize = 26f;  // 20 * 1.3 = 26 for 30% larger
        healthText.color = Color.white;
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.outlineWidth = 0.3f;
        healthText.outlineColor = Color.black;
    }
    
    private void CreateIntentDisplay(Transform parent)
    {
        // Container positioned below health bar
        GameObject intentObj = new GameObject("IntentContainer");
        intentObj.transform.SetParent(parent, false);
        
        var intentRect = intentObj.AddComponent<RectTransform>();
        intentRect.anchorMin = new Vector2(0.5f, 0.5f);
        intentRect.anchorMax = new Vector2(0.5f, 0.5f);
        intentRect.pivot = new Vector2(0.5f, 1f);
        intentRect.anchoredPosition = new Vector2(0f, -(healthBarHeight * 100f / 2f) - 5f);
        intentRect.sizeDelta = new Vector2(healthBarWidth * 100f, 28f);
        
        // Dark background with gold border
        intentBg = intentObj.AddComponent<Image>();
        intentBg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
        intentBg.raycastTarget = true;
        
        var outline = intentObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.6f, 0.2f, 0.8f);
        outline.effectDistance = new Vector2(1, 1);
        
        // Skill name text
        var textObj = new GameObject("IntentText");
        textObj.transform.SetParent(intentObj.transform, false);
        
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4, 0);
        textRect.offsetMax = new Vector2(-4, 0);
        
        intentText = textObj.AddComponent<TextMeshProUGUI>();
        intentText.text = "";
        intentText.fontSize = 18f;
        intentText.color = new Color(1f, 0.85f, 0.4f);
        intentText.alignment = TextAlignmentOptions.Center;
        intentText.outlineWidth = 0.2f;
        intentText.outlineColor = Color.black;
        intentText.raycastTarget = false;
        
        // EventTrigger for tooltip hover
        var trigger = intentObj.AddComponent<EventTrigger>();
        
        var enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { CombatArena.ShowTooltip(cachedIntentDescription); });
        trigger.triggers.Add(enterEntry);
        
        var exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { CombatArena.HideTooltip(); });
        trigger.triggers.Add(exitEntry);
    }
    
    private void UpdateIntentDisplay()
    {
        if (combatEnemy == null || intentText == null) return;
        
        string skillId = combatEnemy.PeekNextSkillId();
        
        // Only update when the peeked skill changes
        if (skillId == lastIntentSkillId) return;
        lastIntentSkillId = skillId;
        
        if (skillId == null)
        {
            // Reactive boss - next skill depends on player action
            intentText.text = "???";
            cachedIntentDescription = "This enemy reacts to your actions";
            return;
        }
        
        var skillDef = GameDataLoader.GetSkill(skillId);
        if (skillDef != null)
        {
            intentText.text = skillDef.name;
            cachedIntentDescription = skillDef.description;
        }
        else
        {
            intentText.text = skillId;
            cachedIntentDescription = "";
        }
    }
    
    private void UpdateHealthDisplay()
    {
        UpdateHealthBarVisuals();
        UpdateHealthText();
    }
    
    private void UpdateHealthBarVisuals()
    {
        if (healthBarGreen != null)
        {
            // Green bar shows actual current health via anchor width
            float actualPercent = (float)combatEnemy.Health / combatEnemy.MaxHealth;
            var rt = healthBarGreen.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(actualPercent), rt.anchorMax.y);
        }
        
        if (healthBarRed != null)
        {
            // Red bar shows the animated "damage taken" via anchor width
            var rt = healthBarRed.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(displayedHealthPercent), rt.anchorMax.y);
        }
        
        if (shieldBarFill != null)
        {
            // Shield overlay: width = shield / maxHealth
            float shieldPercent = combatEnemy.MaxHealth > 0 ? Mathf.Clamp01((float)combatEnemy.Shield / combatEnemy.MaxHealth) : 0f;
            var rt = shieldBarFill.rectTransform;
            rt.anchorMax = new Vector2(shieldPercent, rt.anchorMax.y);
        }
    }
    
    private void UpdateHealthText()
    {
        if (healthText != null && combatEnemy != null)
        {
            if (combatEnemy.Shield > 0)
                healthText.text = $"{combatEnemy.Health} / {combatEnemy.MaxHealth}  (+{combatEnemy.Shield})";
            else
                healthText.text = $"{combatEnemy.Health} / {combatEnemy.MaxHealth}";
        }
    }
    
    /// <summary>
    /// Returns the world-space Y position where the target indicator arrow should be placed
    /// (above the nameplate text with clearance).
    /// </summary>
    public float GetIndicatorWorldY()
    {
        if (nameText != null)
        {
            Vector3[] corners = new Vector3[4];
            nameText.rectTransform.GetWorldCorners(corners);
            // corners[1] = top-left, corners[2] = top-right
            float topY = Mathf.Max(corners[1].y, corners[2].y);
            return topY + 1.5f;
        }
        return transform.position.y + 8f;
    }
    
    /// <summary>
    /// Set this enemy as selected (for targeting)
    /// Note: Visual selection is handled by CombatArena.targetIndicator
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        // Visual indicator handled by CombatArena.targetIndicator
    }
    
    /// <summary>
    /// Flash the enemy when hit
    /// </summary>
    public void FlashDamage()
    {
        StartCoroutine(DamageFlashCoroutine());
    }
    
    private System.Collections.IEnumerator DamageFlashCoroutine()
    {
        var visual = transform.Find("Visual");
        if (visual == null) yield break;
        
        var renderer = visual.GetComponent<Renderer>();
        if (renderer == null) yield break;
        
        Color originalColor = renderer.material.color;
        
        // Flash white
        renderer.material.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        
        // Return to original
        renderer.material.color = originalColor;
    }
    
    /// <summary>
    /// Play death animation
    /// </summary>
    public void PlayDeathAnimation()
    {
        StartCoroutine(DeathCoroutine());
    }
    
    private System.Collections.IEnumerator DeathCoroutine()
    {
        // Disable health bar
        if (worldCanvas != null)
        {
            worldCanvas.gameObject.SetActive(false);
        }
        
        // Shrink and fade
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        
        var visual = transform.Find("Visual");
        Renderer renderer = visual != null ? visual.GetComponent<Renderer>() : null;
        Color startColor = renderer != null ? renderer.material.color : Color.white;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
            if (renderer != null)
            {
                Color c = startColor;
                c.a = 1f - t;
                renderer.material.color = c;
            }
            
            yield return null;
        }
    }
    
    // ========== ELEMENTAL MARK DISPLAY ==========
    
    private void CreateMarkDisplay(Transform parent)
    {
        markContainer = new GameObject("MarkContainer");
        markContainer.transform.SetParent(parent, false);
        
        var containerRect = markContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(1f, 0.5f);
        // Position to the left of the enemy body, below the health bar
        containerRect.anchoredPosition = new Vector2(-healthBarWidth * 100f / 2f - 8f, -150f);
        containerRect.sizeDelta = new Vector2(300f, 48f);
        
        // Horizontal layout: chips flow right-to-left (closest to health bar first)
        var layout = markContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 4;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.padding = new RectOffset(0, 0, 0, 0);
    }
    
    private void UpdateMarkDisplay()
    {
        if (combatEnemy == null || markContainer == null) return;
        
        var currentMarks = combatEnemy.GetMarks();
        
        // Remove chips for elements that no longer have marks
        var toRemove = new List<Element>();
        foreach (var kvp in markChips)
        {
            if (!currentMarks.ContainsKey(kvp.Key) || currentMarks[kvp.Key] <= 0)
            {
                Destroy(kvp.Value.chipObj);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var key in toRemove)
        {
            markChips.Remove(key);
        }
        
        // Add or update chips for active marks
        foreach (var kvp in currentMarks)
        {
            if (kvp.Value <= 0) continue;
            
            if (markChips.TryGetValue(kvp.Key, out var existing))
            {
                // Update label and tooltip
                existing.labelText.text = $"{GetElementAbbrev(kvp.Key)} {kvp.Value}";
                existing.tooltipText = $"Elemental {(kvp.Value == 1 ? "Mark" : "Marks")}: {kvp.Key} x {kvp.Value}";
            }
            else
            {
                // Create new chip
                var chip = CreateMarkChip(kvp.Key, kvp.Value);
                markChips[kvp.Key] = chip;
            }
        }
    }
    
    private MarkChipData CreateMarkChip(Element element, int count)
    {
        var data = new MarkChipData();
        data.element = element;
        data.tooltipText = $"Elemental {(count == 1 ? "Mark" : "Marks")}: {element} x {count}";
        
        data.chipObj = new GameObject($"Mark_{element}");
        data.chipObj.transform.SetParent(markContainer.transform, false);
        
        // Colored background box
        var bg = data.chipObj.AddComponent<Image>();
        Color elemColor = GetElementColor(element);
        bg.color = new Color(elemColor.r * 0.4f, elemColor.g * 0.4f, elemColor.b * 0.4f, 0.9f);
        bg.raycastTarget = true;
        
        // Border in element color
        var outline = data.chipObj.AddComponent<Outline>();
        outline.effectColor = new Color(elemColor.r, elemColor.g, elemColor.b, 0.8f);
        outline.effectDistance = new Vector2(1, 1);
        
        // Size via LayoutElement
        var layoutElem = data.chipObj.AddComponent<LayoutElement>();
        layoutElem.preferredHeight = 44;
        layoutElem.minWidth = 40;
        
        // Auto-width to fit text
        var fitter = data.chipObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        
        // Horizontal padding
        var hLayout = data.chipObj.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding = new RectOffset(12, 12, 2, 2);
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = true;
        
        // Label text: "FIR 3"
        var textObj = new GameObject("Label");
        textObj.transform.SetParent(data.chipObj.transform, false);
        
        data.labelText = textObj.AddComponent<TextMeshProUGUI>();
        data.labelText.text = $"{GetElementAbbrev(element)} {count}";
        data.labelText.fontSize = 28f;
        data.labelText.color = elemColor;
        data.labelText.fontStyle = FontStyles.Bold;
        data.labelText.alignment = TextAlignmentOptions.Center;
        data.labelText.textWrappingMode = TextWrappingModes.NoWrap;
        data.labelText.raycastTarget = false;
        
        // EventTrigger for tooltip on hover
        var trigger = data.chipObj.AddComponent<EventTrigger>();
        
        var enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((_) => { CombatArena.ShowTooltip(data.tooltipText); });
        trigger.triggers.Add(enterEntry);
        
        var exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((_) => { CombatArena.HideTooltip(); });
        trigger.triggers.Add(exitEntry);
        
        return data;
    }
    
    private static string GetElementAbbrev(Element element)
    {
        return element switch
        {
            Element.Fire      => "FIR",
            Element.Ice       => "ICE",
            Element.Water     => "WTR",
            Element.Wind      => "WND",
            Element.Rock      => "RCK",
            Element.Lightning => "LTN",
            _ => "???"
        };
    }
    
    private static Color GetElementColor(Element element)
    {
        return element switch
        {
            Element.Fire      => new Color(0.90f, 0.30f, 0.10f),
            Element.Ice       => new Color(0.50f, 0.85f, 1.00f),
            Element.Water     => new Color(0.20f, 0.40f, 0.90f),
            Element.Wind      => new Color(0.40f, 0.90f, 0.50f),
            Element.Rock      => new Color(0.70f, 0.55f, 0.30f),
            Element.Lightning => new Color(0.95f, 0.85f, 0.20f),
            _ => Color.gray
        };
    }
    
    // ========== STATUS DISPLAY CONTROL ==========
    
    /// <summary>
    /// Toggle expanded/compact mode on the enemy status display.
    /// Called by CombatArena when X key is pressed.
    /// </summary>
    public void SetStatusExpanded(bool expanded)
    {
        if (enemyStatusDisplay != null)
            enemyStatusDisplay.SetExpanded(expanded);
    }
}
