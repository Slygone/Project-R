using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReactionQTEPanel : MonoBehaviour
{
    private GameObject qtePanel;
    private GameObject marker;
    private GameObject perfectZone;
    private GameObject goodZoneLeft;
    private GameObject goodZoneRight;
    private GameObject barBackground;
    private TextMeshProUGUI instructionText;
    private TextMeshProUGUI resultText;
    private TextMeshProUGUI reactionNameText;
    
    private Action<QTEResult> onQTEComplete;
    private bool isActive = false;
    private bool waitingForInput = false;
    private bool showingResult = false;
    
    private float markerPosition = 0f;
    private float markerSpeed = 1.25f; // try 1.50f also think about different visual. expedition 33 style button mby ?
    private float markerDirection = 1f;
    
    private const float BAR_WIDTH = 600f;
    private const float PERFECT_ZONE_WIDTH = 60f;
    private const float GOOD_ZONE_WIDTH = 40f;
    private const float PERFECT_ZONE_CENTER = 0.5f;
    
    private float resultDisplayTime = 0.8f;
    private float resultTimer = 0f;
    private QTEResult pendingResult;

    void Awake()
    {
        SetupUI();
    }

    void Update()
    {
        if (!isActive) return;

        if (waitingForInput)
        {
            UpdateMarker();
            
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                EvaluateQTE();
            }
        }
        else if (showingResult)
        {
            resultTimer -= Time.deltaTime;
            if (resultTimer <= 0f)
            {
                CompleteQTE();
            }
        }
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[ReactionQTEPanel] Canvas not found");
            return;
        }

        qtePanel = CreateQTEPanel(canvas.transform);
        qtePanel.SetActive(false);
    }

    private GameObject CreateQTEPanel(Transform parent)
    {
        var panel = new GameObject("ReactionQTEPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.35f);
        rect.anchorMax = new Vector2(0.85f, 0.65f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.98f);

        var reactionNameObj = new GameObject("ReactionName");
        reactionNameObj.transform.SetParent(panel.transform, false);
        var reactionNameRect = reactionNameObj.AddComponent<RectTransform>();
        reactionNameRect.anchorMin = new Vector2(0, 0.75f);
        reactionNameRect.anchorMax = new Vector2(1, 0.95f);
        reactionNameRect.offsetMin = Vector2.zero;
        reactionNameRect.offsetMax = Vector2.zero;
        reactionNameText = reactionNameObj.AddComponent<TextMeshProUGUI>();
        reactionNameText.text = "REACTION!";
        reactionNameText.alignment = TextAlignmentOptions.Center;
        reactionNameText.fontSize = 36;
        reactionNameText.fontStyle = FontStyles.Bold;
        reactionNameText.color = new Color(1f, 0.6f, 0.2f);

        var instructionObj = new GameObject("Instruction");
        instructionObj.transform.SetParent(panel.transform, false);
        var instructionRect = instructionObj.AddComponent<RectTransform>();
        instructionRect.anchorMin = new Vector2(0, 0.55f);
        instructionRect.anchorMax = new Vector2(1, 0.72f);
        instructionRect.offsetMin = Vector2.zero;
        instructionRect.offsetMax = Vector2.zero;
        instructionText = instructionObj.AddComponent<TextMeshProUGUI>();
        instructionText.text = "Press SPACE or CLICK when the marker is in the zone!";
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.fontSize = 18;
        instructionText.color = new Color(0.8f, 0.8f, 0.8f);

        var barContainer = new GameObject("BarContainer");
        barContainer.transform.SetParent(panel.transform, false);
        var barContainerRect = barContainer.AddComponent<RectTransform>();
        barContainerRect.anchorMin = new Vector2(0.05f, 0.25f);
        barContainerRect.anchorMax = new Vector2(0.95f, 0.45f);
        barContainerRect.offsetMin = Vector2.zero;
        barContainerRect.offsetMax = Vector2.zero;

        barBackground = new GameObject("BarBackground");
        barBackground.transform.SetParent(barContainer.transform, false);
        var barBgRect = barBackground.AddComponent<RectTransform>();
        barBgRect.anchorMin = Vector2.zero;
        barBgRect.anchorMax = Vector2.one;
        barBgRect.offsetMin = Vector2.zero;
        barBgRect.offsetMax = Vector2.zero;
        var barBgImage = barBackground.AddComponent<Image>();
        barBgImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        float perfectStart = PERFECT_ZONE_CENTER - (PERFECT_ZONE_WIDTH / BAR_WIDTH / 2f);
        float perfectEnd = PERFECT_ZONE_CENTER + (PERFECT_ZONE_WIDTH / BAR_WIDTH / 2f);
        
        float goodLeftStart = perfectStart - (GOOD_ZONE_WIDTH / BAR_WIDTH);
        float goodRightEnd = perfectEnd + (GOOD_ZONE_WIDTH / BAR_WIDTH);

        goodZoneLeft = new GameObject("GoodZoneLeft");
        goodZoneLeft.transform.SetParent(barContainer.transform, false);
        var goodLeftRect = goodZoneLeft.AddComponent<RectTransform>();
        goodLeftRect.anchorMin = new Vector2(goodLeftStart, 0f);
        goodLeftRect.anchorMax = new Vector2(perfectStart, 1f);
        goodLeftRect.offsetMin = Vector2.zero;
        goodLeftRect.offsetMax = Vector2.zero;
        var goodLeftImage = goodZoneLeft.AddComponent<Image>();
        goodLeftImage.color = new Color(0.3f, 0.6f, 0.3f, 0.8f);

        goodZoneRight = new GameObject("GoodZoneRight");
        goodZoneRight.transform.SetParent(barContainer.transform, false);
        var goodRightRect = goodZoneRight.AddComponent<RectTransform>();
        goodRightRect.anchorMin = new Vector2(perfectEnd, 0f);
        goodRightRect.anchorMax = new Vector2(goodRightEnd, 1f);
        goodRightRect.offsetMin = Vector2.zero;
        goodRightRect.offsetMax = Vector2.zero;
        var goodRightImage = goodZoneRight.AddComponent<Image>();
        goodRightImage.color = new Color(0.3f, 0.6f, 0.3f, 0.8f);

        perfectZone = new GameObject("PerfectZone");
        perfectZone.transform.SetParent(barContainer.transform, false);
        var perfectRect = perfectZone.AddComponent<RectTransform>();
        perfectRect.anchorMin = new Vector2(perfectStart, 0f);
        perfectRect.anchorMax = new Vector2(perfectEnd, 1f);
        perfectRect.offsetMin = Vector2.zero;
        perfectRect.offsetMax = Vector2.zero;
        var perfectImage = perfectZone.AddComponent<Image>();
        perfectImage.color = new Color(1f, 0.8f, 0.2f, 0.9f);

        marker = new GameObject("Marker");
        marker.transform.SetParent(barContainer.transform, false);
        var markerRect = marker.AddComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0f, -0.1f);
        markerRect.anchorMax = new Vector2(0f, 1.1f);
        markerRect.sizeDelta = new Vector2(8f, 0f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        var markerImage = marker.AddComponent<Image>();
        markerImage.color = Color.white;

        var resultObj = new GameObject("Result");
        resultObj.transform.SetParent(panel.transform, false);
        var resultObjRect = resultObj.AddComponent<RectTransform>();
        resultObjRect.anchorMin = new Vector2(0, 0.05f);
        resultObjRect.anchorMax = new Vector2(1, 0.22f);
        resultObjRect.offsetMin = Vector2.zero;
        resultObjRect.offsetMax = Vector2.zero;
        resultText = resultObj.AddComponent<TextMeshProUGUI>();
        resultText.text = "";
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.fontSize = 28;
        resultText.fontStyle = FontStyles.Bold;

        return panel;
    }

    public void Show(string reactionName, Action<QTEResult> callback)
    {
        onQTEComplete = callback;
        
        reactionNameText.text = $"{reactionName.ToUpper()} REACTION!";
        resultText.text = "";
        instructionText.gameObject.SetActive(true);
        
        markerPosition = 0f;
        markerDirection = 1f;
        UpdateMarkerVisual();
        
        qtePanel.SetActive(true);
        isActive = true;
        waitingForInput = true;
        showingResult = false;

        Debug.Log($"[ReactionQTEPanel] QTE started for {reactionName}");
    }

    private void UpdateMarker()
    {
        markerPosition += markerDirection * markerSpeed * Time.deltaTime;
        
        if (markerPosition >= 1f)
        {
            markerPosition = 1f;
            markerDirection = -1f;
        }
        else if (markerPosition <= 0f)
        {
            markerPosition = 0f;
            markerDirection = 1f;
        }
        
        UpdateMarkerVisual();
    }

    private void UpdateMarkerVisual()
    {
        var markerRect = marker.GetComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(markerPosition, -0.1f);
        markerRect.anchorMax = new Vector2(markerPosition, 1.1f);
    }

    private void EvaluateQTE()
    {
        waitingForInput = false;
        
        float perfectStart = PERFECT_ZONE_CENTER - (PERFECT_ZONE_WIDTH / BAR_WIDTH / 2f);
        float perfectEnd = PERFECT_ZONE_CENTER + (PERFECT_ZONE_WIDTH / BAR_WIDTH / 2f);
        float goodLeftStart = perfectStart - (GOOD_ZONE_WIDTH / BAR_WIDTH);
        float goodRightEnd = perfectEnd + (GOOD_ZONE_WIDTH / BAR_WIDTH);

        if (markerPosition >= perfectStart && markerPosition <= perfectEnd)
        {
            pendingResult = QTEResult.Perfect;
            resultText.text = "PERFECT!";
            resultText.color = new Color(1f, 0.85f, 0.2f);
        }
        else if ((markerPosition >= goodLeftStart && markerPosition < perfectStart) ||
                 (markerPosition > perfectEnd && markerPosition <= goodRightEnd))
        {
            pendingResult = QTEResult.Good;
            resultText.text = "GOOD";
            resultText.color = new Color(0.4f, 0.8f, 0.4f);
        }
        else
        {
            pendingResult = QTEResult.Bad;
            resultText.text = "BAD";
            resultText.color = new Color(0.8f, 0.3f, 0.3f);
        }

        instructionText.gameObject.SetActive(false);
        showingResult = true;
        resultTimer = resultDisplayTime;

        float multiplier = ReactionQTE.GetQteMultiplier(pendingResult);
        Debug.Log($"[ReactionQTEPanel] QTE Result: {pendingResult} (x{multiplier:F1}) at position {markerPosition:F3}");
    }

    private void CompleteQTE()
    {
        qtePanel.SetActive(false);
        isActive = false;
        showingResult = false;
        
        onQTEComplete?.Invoke(pendingResult);
    }

    public void Hide()
    {
        qtePanel.SetActive(false);
        isActive = false;
        waitingForInput = false;
        showingResult = false;
    }

    public bool IsActive() => isActive;
}
