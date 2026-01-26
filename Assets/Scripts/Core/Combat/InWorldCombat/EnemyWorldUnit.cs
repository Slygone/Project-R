using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI healthText;
    
    private float displayedHealthPercent = 1f;
    private float targetHealthPercent = 1f;
    private int lastDisplayedHealth;
    
    private bool isSelected = false;
    // Selection indicator removed - using targetIndicator in CombatArena instead
    
    public CombatEnemy CombatEnemy => combatEnemy;
    public bool IsAlive => combatEnemy != null && combatEnemy.IsAlive();

    public void Initialize(CombatEnemy enemy)
    {
        combatEnemy = enemy;
        lastDisplayedHealth = enemy.Health;
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
        
        // Update health text if changed
        if (lastDisplayedHealth != combatEnemy.Health)
        {
            lastDisplayedHealth = combatEnemy.Health;
            UpdateHealthText();
        }
        
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
        
        var canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(healthBarWidth * 100f, 50f);
        canvasRect.localScale = Vector3.one * 0.01f;
        
        // Add canvas scaler for proper sizing
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;
        
        // Create nameplate text
        CreateNameplate(canvasObj.transform);
        
        // Create health bar
        CreateHealthBar(canvasObj.transform);
    }
    
    private void CreateNameplate(Transform parent)
    {
        GameObject nameObj = new GameObject("Nameplate");
        nameObj.transform.SetParent(parent, false);
        
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 1f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 0f);
        nameRect.anchoredPosition = new Vector2(0f, 5f);
        nameRect.sizeDelta = new Vector2(200f, 30f);
        
        nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = combatEnemy.Name;
        nameText.fontSize = 36f;  // 28 * 1.3 = ~36 for 30% larger
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = nameplateColor;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.enableWordWrapping = false;
        
        // Add outline for readability
        nameText.outlineWidth = 0.4f;
        nameText.outlineColor = Color.black;
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
        
        // Red overlay (shows damage taken - retroactive decrease)
        GameObject redObj = new GameObject("DamageOverlay");
        redObj.transform.SetParent(barContainer.transform, false);
        
        var redRect = redObj.AddComponent<RectTransform>();
        redRect.anchorMin = Vector2.zero;
        redRect.anchorMax = Vector2.one;
        redRect.pivot = new Vector2(0f, 0.5f);
        redRect.offsetMin = Vector2.zero;
        redRect.offsetMax = Vector2.zero;
        
        healthBarRed = redObj.AddComponent<Image>();
        healthBarRed.color = healthBarDamage;
        healthBarRed.type = Image.Type.Filled;
        healthBarRed.fillMethod = Image.FillMethod.Horizontal;
        healthBarRed.fillOrigin = 0;
        healthBarRed.fillAmount = 1f;
        
        // Green fill (current health)
        GameObject greenObj = new GameObject("HealthFill");
        greenObj.transform.SetParent(barContainer.transform, false);
        
        var greenRect = greenObj.AddComponent<RectTransform>();
        greenRect.anchorMin = Vector2.zero;
        greenRect.anchorMax = Vector2.one;
        greenRect.pivot = new Vector2(0f, 0.5f);
        greenRect.offsetMin = Vector2.zero;
        greenRect.offsetMax = Vector2.zero;
        
        healthBarGreen = greenObj.AddComponent<Image>();
        healthBarGreen.color = healthBarFill;
        healthBarGreen.type = Image.Type.Filled;
        healthBarGreen.fillMethod = Image.FillMethod.Horizontal;
        healthBarGreen.fillOrigin = 0;
        healthBarGreen.fillAmount = 1f;
        
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
    
    // CreateSelectionIndicator removed - CombatArena.targetIndicator handles all targeting visuals
    
    private void UpdateHealthDisplay()
    {
        UpdateHealthBarVisuals();
        UpdateHealthText();
    }
    
    private void UpdateHealthBarVisuals()
    {
        if (healthBarGreen != null)
        {
            // Green bar shows actual current health (instant update)
            float actualPercent = (float)combatEnemy.Health / combatEnemy.MaxHealth;
            healthBarGreen.fillAmount = actualPercent;
        }
        
        if (healthBarRed != null)
        {
            // Red bar shows the animated "damage taken" (retroactive decrease)
            healthBarRed.fillAmount = displayedHealthPercent;
        }
    }
    
    private void UpdateHealthText()
    {
        if (healthText != null && combatEnemy != null)
        {
            healthText.text = $"{combatEnemy.Health} / {combatEnemy.MaxHealth}";
        }
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
}
