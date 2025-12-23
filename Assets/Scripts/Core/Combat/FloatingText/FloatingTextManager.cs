using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Singleton manager for spawning and managing floating combat text.
/// Provides simple API for showing damage, heals, status effects, and reactions.
/// </summary>
public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance { get; private set; }
    
    [Header("Configuration")]
    [SerializeField] private FloatingTextConfig config;
    
    [Header("Prefab")]
    [SerializeField] private GameObject floatingTextPrefab;
    
    [Header("Container")]
    [SerializeField] private Canvas floatingTextCanvas;
    
    // Object pool for floating texts
    private Queue<FloatingText> pool = new Queue<FloatingText>();
    private List<FloatingText> activeTexts = new List<FloatingText>();
    
    // Queue for delayed text spawning
    private Queue<QueuedText> textQueue = new Queue<QueuedText>();
    private float lastSpawnTime;
    private bool isProcessingQueue;
    
    // Track vertical offsets per target to stack texts
    private Dictionary<Transform, int> targetTextCounts = new Dictionary<Transform, int>();
    
    private struct QueuedText
    {
        public string text;
        public Transform target;
        public Vector3 position;
        public FloatingTextType type;
        public bool usePosition;
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Create canvas if not assigned
        if (floatingTextCanvas == null)
        {
            CreateFloatingTextCanvas();
        }
        
        // Create default config if not assigned
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<FloatingTextConfig>();
            Debug.LogWarning("[FloatingTextManager] No config assigned, using default values");
        }
        
        // Pre-populate pool
        for (int i = 0; i < 10; i++)
        {
            CreatePooledText();
        }
        
        Debug.Log($"[FloatingTextManager] Initialized! Config: {(config != null ? "assigned" : "default")}, Canvas: {floatingTextCanvas.name}, Pool size: {pool.Count}");
    }
    
    private void Update()
    {
        // Process queued texts with delay
        if (textQueue.Count > 0 && Time.time - lastSpawnTime >= config.globalDelay)
        {
            ProcessNextQueuedText();
        }
        
        // Clean up target text counts for completed texts
        CleanupTargetCounts();
    }
    
    private void CreateFloatingTextCanvas()
    {
        GameObject canvasObj = new GameObject("FloatingTextCanvas");
        canvasObj.transform.SetParent(transform);
        
        floatingTextCanvas = canvasObj.AddComponent<Canvas>();
        floatingTextCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        floatingTextCanvas.sortingOrder = 100;
        
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }
    
    private FloatingText CreatePooledText()
    {
        GameObject textObj;
        
        if (floatingTextPrefab != null)
        {
            textObj = Instantiate(floatingTextPrefab, floatingTextCanvas.transform);
        }
        else
        {
            // Create default floating text object
            textObj = new GameObject("FloatingText");
            textObj.transform.SetParent(floatingTextCanvas.transform);
            
            var rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 50);
            
            var canvasGroup = textObj.AddComponent<CanvasGroup>();
            
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
        }
        
        textObj.SetActive(false);
        
        var floatingText = textObj.GetComponent<FloatingText>();
        if (floatingText == null)
        {
            floatingText = textObj.AddComponent<FloatingText>();
        }
        
        floatingText.OnComplete = OnTextComplete;
        pool.Enqueue(floatingText);
        
        return floatingText;
    }
    
    private FloatingText GetFromPool()
    {
        if (pool.Count == 0)
        {
            CreatePooledText();
        }
        
        // Check max concurrent texts
        if (activeTexts.Count >= config.maxConcurrentTexts)
        {
            // Force complete oldest text
            if (activeTexts.Count > 0)
            {
                activeTexts[0].ForceComplete();
            }
        }
        
        var text = pool.Dequeue();
        activeTexts.Add(text);
        return text;
    }
    
    private void OnTextComplete(FloatingText text)
    {
        activeTexts.Remove(text);
        pool.Enqueue(text);
    }
    
    private void CleanupTargetCounts()
    {
        // Decrease counts over time
        var keys = new List<Transform>(targetTextCounts.Keys);
        foreach (var key in keys)
        {
            if (key == null)
            {
                targetTextCounts.Remove(key);
            }
        }
    }
    
    private int GetAndIncrementTargetCount(Transform target)
    {
        if (target == null) return 0;
        
        if (!targetTextCounts.ContainsKey(target))
        {
            targetTextCounts[target] = 0;
        }
        
        int count = targetTextCounts[target];
        targetTextCounts[target] = (count + 1) % 5; // Cycle through 5 positions
        return count;
    }
    
    private void ProcessNextQueuedText()
    {
        if (textQueue.Count == 0) return;
        
        var queued = textQueue.Dequeue();
        SpawnTextImmediate(queued.text, queued.target, queued.position, queued.type, queued.usePosition);
        lastSpawnTime = Time.time;
    }
    
    private void SpawnTextImmediate(string text, Transform target, Vector3 position, FloatingTextType type, bool usePosition)
    {
        var style = config.GetStyle(type);
        if (!style.enabled) 
        {
            Debug.Log($"[FloatingTextManager] Style {type} is disabled, skipping");
            return;
        }
        
        var floatingText = GetFromPool();
        
        // Calculate offset with stacking
        int stackIndex = usePosition ? 0 : GetAndIncrementTargetCount(target);
        Vector3 offset = Vector3.up * (config.verticalOffset + stackIndex * config.stackOffset);
        
        Debug.Log($"[FloatingTextManager] Spawning text '{text}' at offset {offset}, target: {(target != null ? target.name : "null")}");
        
        if (usePosition)
        {
            floatingText.ShowAtPosition(text, position + offset, style);
        }
        else
        {
            floatingText.Show(text, target, offset, style);
        }
    }
    
    private void QueueText(string text, Transform target, Vector3 position, FloatingTextType type, bool usePosition, bool immediate = false)
    {
        if (immediate || textQueue.Count == 0)
        {
            SpawnTextImmediate(text, target, position, type, usePosition);
            lastSpawnTime = Time.time;
        }
        else
        {
            textQueue.Enqueue(new QueuedText
            {
                text = text,
                target = target,
                position = position,
                type = type,
                usePosition = usePosition
            });
        }
    }
    
    // ==================== PUBLIC API ====================
    
    /// <summary>
    /// Show damage dealt to a target.
    /// </summary>
    public void ShowDamage(Transform target, int amount, bool isCrit = false, string sourceType = "")
    {
        Debug.Log($"[FloatingTextManager] ShowDamage called: {amount} (crit: {isCrit}) on {(target != null ? target.name : "null")}");
        
        if (!config.IsEnabled(isCrit ? FloatingTextType.CriticalHit : FloatingTextType.DamageDealt)) 
        {
            Debug.Log("[FloatingTextManager] Text type is disabled in config");
            return;
        }
        
        var type = isCrit ? FloatingTextType.CriticalHit : FloatingTextType.DamageDealt;
        string text = isCrit ? amount.ToString() : amount.ToString();
        QueueText(text, target, Vector3.zero, type, false);
    }
    
    /// <summary>
    /// Show damage taken by a target.
    /// </summary>
    public void ShowDamageTaken(Transform target, int amount)
    {
        if (!config.IsEnabled(FloatingTextType.DamageTaken)) return;
        QueueText("-" + amount.ToString(), target, Vector3.zero, FloatingTextType.DamageTaken, false);
    }
    
    /// <summary>
    /// Show DoT tick damage.
    /// </summary>
    public void ShowDoTTick(Transform target, int amount, string dotName = "DoT")
    {
        if (!config.IsEnabled(FloatingTextType.DoTTick)) return;
        QueueText($"{dotName} {amount}", target, Vector3.zero, FloatingTextType.DoTTick, false);
    }
    
    /// <summary>
    /// Show healing received.
    /// </summary>
    public void ShowHeal(Transform target, int amount)
    {
        if (!config.IsEnabled(FloatingTextType.Heal)) return;
        QueueText(amount.ToString(), target, Vector3.zero, FloatingTextType.Heal, false);
    }
    
    /// <summary>
    /// Show shield events.
    /// </summary>
    public void ShowShield(Transform target, int amount, FloatingTextType shieldType)
    {
        if (!config.IsEnabled(shieldType)) return;
        
        string text;
        switch (shieldType)
        {
            case FloatingTextType.ShieldGain:
                text = amount.ToString();
                break;
            case FloatingTextType.ShieldAbsorb:
                text = amount.ToString();
                break;
            case FloatingTextType.ShieldBroken:
                text = "Shield Broken!";
                break;
            default:
                text = amount.ToString();
                break;
        }
        
        QueueText(text, target, Vector3.zero, shieldType, false);
    }
    
    /// <summary>
    /// Show status effect gained or lost.
    /// </summary>
    public void ShowStatus(Transform target, string statusName, bool gained, int stacksDelta = 0)
    {
        FloatingTextType type;
        string text;
        
        if (stacksDelta != 0)
        {
            type = FloatingTextType.StatusStack;
            text = $"{statusName} {(stacksDelta > 0 ? "+" : "")}{stacksDelta}";
        }
        else if (gained)
        {
            type = FloatingTextType.StatusGain;
            text = statusName;
        }
        else
        {
            type = FloatingTextType.StatusLost;
            text = $"{statusName} Ended";
        }
        
        if (!config.IsEnabled(type)) return;
        QueueText(text, target, Vector3.zero, type, false);
    }
    
    /// <summary>
    /// Show immune/resist feedback.
    /// </summary>
    public void ShowImmune(Transform target, bool isImmune = true)
    {
        var type = isImmune ? FloatingTextType.Immune : FloatingTextType.Resisted;
        if (!config.IsEnabled(type)) return;
        
        string text = isImmune ? "Immune" : "Resisted";
        QueueText(text, target, Vector3.zero, type, false);
    }
    
    /// <summary>
    /// Show elemental orb activation.
    /// </summary>
    public void ShowOrbActivated(Transform target, string elementName)
    {
        if (!config.IsEnabled(FloatingTextType.OrbActivated)) return;
        QueueText($"{elementName} Orb", target, Vector3.zero, FloatingTextType.OrbActivated, false);
    }
    
    /// <summary>
    /// Show elemental reaction triggered.
    /// </summary>
    public void ShowReaction(Transform target, string reactionName, float multiplier = 0f, int bonusDamage = 0)
    {
        if (!config.IsEnabled(FloatingTextType.ReactionTriggered)) return;
        
        string text = multiplier > 0 ? $"{reactionName} x{multiplier:F2}" : reactionName;
        QueueText(text, target, Vector3.zero, FloatingTextType.ReactionTriggered, false);
        
        // Show bonus damage separately if provided
        if (bonusDamage > 0 && config.IsEnabled(FloatingTextType.ReactionDamage))
        {
            QueueText($"+{bonusDamage}", target, Vector3.zero, FloatingTextType.ReactionDamage, false);
        }
    }
    
    /// <summary>
    /// Show turn skipped due to stun.
    /// </summary>
    public void ShowTurnSkipped(Transform target, string reason = "Stunned!")
    {
        if (!config.IsEnabled(FloatingTextType.TurnSkipped)) return;
        QueueText($"{reason} Turn Skipped", target, Vector3.zero, FloatingTextType.TurnSkipped, false);
    }
    
    /// <summary>
    /// Show energy change (subtle, optional).
    /// </summary>
    public void ShowEnergyChange(Transform target, int amount)
    {
        if (!config.IsEnabled(FloatingTextType.EnergyChange)) return;
        string text = amount > 0 ? $"+{amount} Energy" : $"{amount} Energy";
        QueueText(text, target, Vector3.zero, FloatingTextType.EnergyChange, false);
    }
    
    /// <summary>
    /// Show generic text with custom type.
    /// </summary>
    public void ShowText(Transform target, string text, FloatingTextType type = FloatingTextType.Generic)
    {
        if (!config.IsEnabled(type)) return;
        QueueText(text, target, Vector3.zero, type, false);
    }
    
    /// <summary>
    /// Show text at a world position (no target tracking).
    /// </summary>
    public void ShowTextAtPosition(Vector3 worldPosition, string text, FloatingTextType type = FloatingTextType.Generic)
    {
        if (!config.IsEnabled(type)) return;
        QueueText(text, null, worldPosition, type, true);
    }
    
    /// <summary>
    /// Clear all active floating texts.
    /// </summary>
    public void ClearAllTexts()
    {
        foreach (var text in activeTexts.ToArray())
        {
            text.ForceComplete();
        }
        textQueue.Clear();
        targetTextCounts.Clear();
    }
    
    /// <summary>
    /// Get the configured pause duration after player action.
    /// </summary>
    public float GetPlayerActionPause() => config?.combatPauseAfterPlayerAction ?? 0.5f;
    
    /// <summary>
    /// Get the configured pause duration after enemy action.
    /// </summary>
    public float GetEnemyActionPause() => config?.combatPauseAfterEnemyAction ?? 0.4f;
    
    /// <summary>
    /// Wait for all queued texts to finish displaying.
    /// </summary>
    public IEnumerator WaitForQueuedTexts()
    {
        while (textQueue.Count > 0)
        {
            yield return null;
        }
        // Small additional delay for last text to be readable
        yield return new WaitForSeconds(0.2f);
    }
}
