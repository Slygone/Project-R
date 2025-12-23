using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public enum DefensiveQTEResult { Bad, Good, Perfect }

public class DefensiveQTE : MonoBehaviour
{
    private GameObject qtePanel;
    private RectTransform indicator;
    private RectTransform targetZone;
    private RectTransform perfectZone;
    private TextMeshProUGUI resultText;
    private TextMeshProUGUI instructionText;
    
    private bool isActive = false;
    private bool hasPressed = false;
    private float indicatorPosition = 0f;
    private float indicatorSpeed = 2f;
    private int indicatorDirection = 1;
    
    private Action<DefensiveQTEResult> onComplete;
    
    // Zone boundaries (0-1 normalized)
    private const float GOOD_ZONE_START = 0.3f;
    private const float GOOD_ZONE_END = 0.7f;
    private const float PERFECT_ZONE_START = 0.45f;
    private const float PERFECT_ZONE_END = 0.55f;
    
    void Awake()
    {
        CreateQTEPanel();
    }
    
    void Update()
    {
        if (!isActive || hasPressed) return;
        
        // Move indicator back and forth
        indicatorPosition += indicatorDirection * indicatorSpeed * Time.deltaTime;
        
        if (indicatorPosition >= 1f)
        {
            indicatorPosition = 1f;
            indicatorDirection = -1;
        }
        else if (indicatorPosition <= 0f)
        {
            indicatorPosition = 0f;
            indicatorDirection = 1;
        }
        
        UpdateIndicatorPosition();
        
        // Check for input
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            ResolveQTE();
        }
    }
    
    private void CreateQTEPanel()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        
        // Main panel
        qtePanel = new GameObject("DefensiveQTEPanel");
        qtePanel.transform.SetParent(canvas.transform, false);
        
        var panelRect = qtePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.4f);
        panelRect.anchorMax = new Vector2(0.7f, 0.6f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        var panelBg = qtePanel.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        
        // Instruction text
        var instructObj = new GameObject("Instruction");
        instructObj.transform.SetParent(qtePanel.transform, false);
        var instructRect = instructObj.AddComponent<RectTransform>();
        instructRect.anchorMin = new Vector2(0f, 0.7f);
        instructRect.anchorMax = new Vector2(1f, 1f);
        instructRect.offsetMin = Vector2.zero;
        instructRect.offsetMax = Vector2.zero;
        instructionText = instructObj.AddComponent<TextMeshProUGUI>();
        instructionText.text = "DEFEND! Press SPACE";
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.fontSize = 24;
        instructionText.color = Color.white;
        
        // QTE bar background
        var barBg = new GameObject("BarBackground");
        barBg.transform.SetParent(qtePanel.transform, false);
        var barBgRect = barBg.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.05f, 0.3f);
        barBgRect.anchorMax = new Vector2(0.95f, 0.5f);
        barBgRect.offsetMin = Vector2.zero;
        barBgRect.offsetMax = Vector2.zero;
        var barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.2f, 0.2f);
        
        // Good zone (green)
        var goodZone = new GameObject("GoodZone");
        goodZone.transform.SetParent(barBg.transform, false);
        targetZone = goodZone.AddComponent<RectTransform>();
        targetZone.anchorMin = new Vector2(GOOD_ZONE_START, 0f);
        targetZone.anchorMax = new Vector2(GOOD_ZONE_END, 1f);
        targetZone.offsetMin = Vector2.zero;
        targetZone.offsetMax = Vector2.zero;
        var goodImg = goodZone.AddComponent<Image>();
        goodImg.color = new Color(0.2f, 0.6f, 0.2f);
        
        // Perfect zone (gold)
        var perfectZoneObj = new GameObject("PerfectZone");
        perfectZoneObj.transform.SetParent(barBg.transform, false);
        perfectZone = perfectZoneObj.AddComponent<RectTransform>();
        perfectZone.anchorMin = new Vector2(PERFECT_ZONE_START, 0f);
        perfectZone.anchorMax = new Vector2(PERFECT_ZONE_END, 1f);
        perfectZone.offsetMin = Vector2.zero;
        perfectZone.offsetMax = Vector2.zero;
        var perfectImg = perfectZoneObj.AddComponent<Image>();
        perfectImg.color = new Color(1f, 0.85f, 0.2f);
        
        // Indicator
        var indicatorObj = new GameObject("Indicator");
        indicatorObj.transform.SetParent(barBg.transform, false);
        indicator = indicatorObj.AddComponent<RectTransform>();
        indicator.anchorMin = new Vector2(0f, 0f);
        indicator.anchorMax = new Vector2(0f, 1f);
        indicator.pivot = new Vector2(0.5f, 0.5f);
        indicator.sizeDelta = new Vector2(8f, 0f);
        var indicatorImg = indicatorObj.AddComponent<Image>();
        indicatorImg.color = Color.white;
        
        // Result text
        var resultObj = new GameObject("Result");
        resultObj.transform.SetParent(qtePanel.transform, false);
        var resultRect = resultObj.AddComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0f, 0f);
        resultRect.anchorMax = new Vector2(1f, 0.25f);
        resultRect.offsetMin = Vector2.zero;
        resultRect.offsetMax = Vector2.zero;
        resultText = resultObj.AddComponent<TextMeshProUGUI>();
        resultText.text = "";
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.fontSize = 28;
        
        qtePanel.SetActive(false);
    }
    
    private void UpdateIndicatorPosition()
    {
        if (indicator == null) return;
        indicator.anchorMin = new Vector2(indicatorPosition, 0f);
        indicator.anchorMax = new Vector2(indicatorPosition, 1f);
    }
    
    public void StartQTE(Action<DefensiveQTEResult> callback, string attackerName = "Enemy")
    {
        onComplete = callback;
        isActive = true;
        hasPressed = false;
        indicatorPosition = 0f;
        indicatorDirection = 1;
        
        if (instructionText != null)
            instructionText.text = $"{attackerName} attacks! Press SPACE";
        if (resultText != null)
            resultText.text = "";
        
        qtePanel.SetActive(true);
    }
    
    private void ResolveQTE()
    {
        hasPressed = true;
        
        DefensiveQTEResult result;
        string resultStr;
        Color resultColor;
        
        if (indicatorPosition >= PERFECT_ZONE_START && indicatorPosition <= PERFECT_ZONE_END)
        {
            result = DefensiveQTEResult.Perfect;
            resultStr = "PERFECT!";
            resultColor = new Color(1f, 0.85f, 0.2f);
        }
        else if (indicatorPosition >= GOOD_ZONE_START && indicatorPosition <= GOOD_ZONE_END)
        {
            result = DefensiveQTEResult.Good;
            resultStr = "GOOD!";
            resultColor = new Color(0.2f, 0.8f, 0.2f);
        }
        else
        {
            result = DefensiveQTEResult.Bad;
            resultStr = "BAD";
            resultColor = new Color(0.8f, 0.2f, 0.2f);
        }
        
        if (resultText != null)
        {
            resultText.text = resultStr;
            resultText.color = resultColor;
        }
        
        // Update debug overlay
        GameManager.SetLastQTEResult($"Def: {resultStr}");
        
        Debug.Log($"[DefensiveQTE] Result: {result} (position: {indicatorPosition:F2})");
        
        // Delay before completing to show result
        Invoke(nameof(CompleteQTE), 0.5f);
    }
    
    private void CompleteQTE()
    {
        isActive = false;
        qtePanel.SetActive(false);
        
        var callback = onComplete;
        onComplete = null;
        callback?.Invoke(hasPressed ? GetResultFromPosition() : DefensiveQTEResult.Bad);
    }
    
    private DefensiveQTEResult GetResultFromPosition()
    {
        if (indicatorPosition >= PERFECT_ZONE_START && indicatorPosition <= PERFECT_ZONE_END)
            return DefensiveQTEResult.Perfect;
        if (indicatorPosition >= GOOD_ZONE_START && indicatorPosition <= GOOD_ZONE_END)
            return DefensiveQTEResult.Good;
        return DefensiveQTEResult.Bad;
    }
    
    public bool IsActive() => isActive;
    
    // Healing percentages based on result
    public static float GetHealPercent(DefensiveQTEResult result)
    {
        return result switch
        {
            DefensiveQTEResult.Perfect => 0.05f,
            DefensiveQTEResult.Good => 0.03f,
            _ => 0.01f
        };
    }
}
