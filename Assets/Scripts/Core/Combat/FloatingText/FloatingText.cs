using UnityEngine;
using TMPro;

/// <summary>
/// Individual floating text instance. Handles animation, movement, and fade.
/// Attach to a Canvas with TextMeshProUGUI child.
/// </summary>
public class FloatingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private FloatingTextStyle style;
    private Transform targetTransform;
    private Vector3 worldOffset;
    private float elapsedTime;
    private float initialScale;
    private Vector3 shakeOffset;
    private bool isActive;
    
    // Arc animation
    private float arcDirection; // -1 = left, 1 = right
    private Vector3 startScreenPos;
    private bool useArcAnimation = true;
    
    // Callback when text finishes
    public System.Action<FloatingText> OnComplete;
    
    public bool IsActive => isActive;
    
    private void Awake()
    {
        if (textComponent == null)
            textComponent = GetComponentInChildren<TextMeshProUGUI>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
    
    /// <summary>
    /// Initialize and show the floating text.
    /// </summary>
    public void Show(string text, Transform target, Vector3 offset, FloatingTextStyle textStyle)
    {
        // Try to find text component if not set (fallback for dynamic creation)
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }
        if (textComponent == null)
        {
            textComponent = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (textComponent == null)
        {
            GameLog.Error(
                GameLogCategory.System,
                "[FloatingText]",
                GameLog.Join(
                    "ShowFail",
                    GameLog.KV("reason", "TextComponentNotFound")
                )
            );
            return;
        }
        
        // Ensure canvas group is set
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        
        style = textStyle;
        targetTransform = target;
        worldOffset = offset;
        elapsedTime = 0f;
        isActive = true;
        
        // Apply style
        string displayText = style.prefix + text + style.suffix;
        textComponent.text = displayText;
        textComponent.color = style.color;
        textComponent.fontSize = style.fontSize;
        textComponent.outlineColor = style.outlineColor;
        textComponent.outlineWidth = 0.2f;
        
        // Reset state
        canvasGroup.alpha = 1f;
        initialScale = style.scalePunch ? style.scaleMultiplier : 1f;
        transform.localScale = Vector3.one * initialScale;
        shakeOffset = Vector3.zero;
        
        // Randomize arc direction (-1 = left, 1 = right)
        arcDirection = Random.value > 0.5f ? 1f : -1f;
        
        // Initial position
        UpdatePosition();
        startScreenPos = transform.position;
        
        gameObject.SetActive(true);

        GameLog.System(GameLog.Join(
            "FloatingTextShow",
            GameLog.KV("text", displayText),
            GameLog.KV("target", target != null ? target.name : "null")
        ), GameLogVerbosity.Verbose);
    }
    
    /// <summary>
    /// Initialize for world position (no target tracking).
    /// </summary>
    public void ShowAtPosition(string text, Vector3 worldPosition, FloatingTextStyle textStyle)
    {
        // Try to find text component if not set (fallback for dynamic creation)
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }
        if (textComponent == null)
        {
            textComponent = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (textComponent == null)
        {
            GameLog.Error(
                GameLogCategory.System,
                "[FloatingText]",
                GameLog.Join(
                    "ShowAtPositionFail",
                    GameLog.KV("reason", "TextComponentNotFound")
                )
            );
            return;
        }
        
        // Ensure canvas group is set
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        
        style = textStyle;
        targetTransform = null;
        worldOffset = worldPosition;
        elapsedTime = 0f;
        isActive = true;
        
        // Apply style
        string displayText = style.prefix + text + style.suffix;
        textComponent.text = displayText;
        textComponent.color = style.color;
        textComponent.fontSize = style.fontSize;
        textComponent.outlineColor = style.outlineColor;
        textComponent.outlineWidth = 0.2f;
        
        // Reset state
        canvasGroup.alpha = 1f;
        initialScale = style.scalePunch ? style.scaleMultiplier : 1f;
        transform.localScale = Vector3.one * initialScale;
        shakeOffset = Vector3.zero;
        
        // Randomize arc direction (-1 = left, 1 = right)
        arcDirection = Random.value > 0.5f ? 1f : -1f;
        
        // Set position
        transform.position = Camera.main.WorldToScreenPoint(worldPosition);
        startScreenPos = transform.position;
        
        gameObject.SetActive(true);
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        elapsedTime += Time.deltaTime;
        float normalizedTime = elapsedTime / style.duration;
        
        // Check completion
        if (normalizedTime >= 1f)
        {
            Complete();
            return;
        }
        
        // Update base position from target
        UpdatePosition();
        
        // Apply arc animation - horizontal arc from center to left/right
        if (useArcAnimation)
        {
            // Arc parameters
            float arcWidth = 150f;  // Horizontal distance of arc
            float arcHeight = 80f;  // Maximum height of arc
            
            // Calculate arc position using parabolic motion
            // x moves linearly in arc direction
            float horizontalOffset = normalizedTime * arcWidth * arcDirection;
            
            // y follows a parabola: peaks at middle (t=0.5), returns near start at end
            float verticalOffset = 4f * arcHeight * normalizedTime * (1f - normalizedTime);
            
            // Apply arc offset from start position
            transform.position = startScreenPos + new Vector3(horizontalOffset, verticalOffset, 0f);
        }
        else
        {
            // Original linear float behavior
            float floatOffset = elapsedTime * style.floatSpeed * style.floatDirection;
            transform.position += Vector3.up * floatOffset * Time.deltaTime;
        }
        
        // Apply shake
        if (style.shake)
        {
            float shakeDecay = 1f - normalizedTime;
            shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * style.shakeIntensity * shakeDecay,
                Random.Range(-1f, 1f) * style.shakeIntensity * shakeDecay,
                0f
            );
            transform.position += shakeOffset;
        }
        
        // Scale punch (shrink back to normal)
        if (style.scalePunch)
        {
            float scaleProgress = Mathf.Min(normalizedTime * 3f, 1f); // Fast scale down
            float currentScale = Mathf.Lerp(initialScale, 1f, scaleProgress);
            transform.localScale = Vector3.one * currentScale;
        }
        
        // Fade out
        if (normalizedTime >= style.fadeStartTime)
        {
            float fadeProgress = (normalizedTime - style.fadeStartTime) / (1f - style.fadeStartTime);
            canvasGroup.alpha = 1f - fadeProgress;
        }
    }
    
    private void UpdatePosition()
    {
        if (targetTransform != null)
        {
            // Check if target is a UI element (RectTransform) or world object
            RectTransform targetRect = targetTransform as RectTransform;
            if (targetRect != null)
            {
                // Target is UI - use its screen position directly
                // worldOffset is used as pixel offset for UI targets
                transform.position = targetTransform.position + worldOffset * 30f; // Scale offset for screen space
            }
            else if (Camera.main != null)
            {
                // Target is world object - convert to screen space
                Vector3 worldPos = targetTransform.position + worldOffset;
                transform.position = Camera.main.WorldToScreenPoint(worldPos);
            }
        }
    }
    
    private void Complete()
    {
        isActive = false;
        gameObject.SetActive(false);
        OnComplete?.Invoke(this);
    }
    
    /// <summary>
    /// Force complete and return to pool.
    /// </summary>
    public void ForceComplete()
    {
        Complete();
    }
}
