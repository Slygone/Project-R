using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays earned relics as rarity-colored chips in the top-right corner of the screen.
/// Each chip shows the relic name, a counter (X/Y) for counter-based relics,
/// a highlight glow when the relic is ready, and a shake animation when it procs.
/// Colors: Common = pale green, Legendary = pale purple, Cursed = pale red.
/// </summary>
public class RelicDisplayUI : MonoBehaviour
{
    private Player player;
    private GameObject panelObj;
    private Transform chipContainer;
    private List<RelicChipData> chips = new List<RelicChipData>();
    
    // Tooltip (reuse CombatArena screen-space tooltip pattern)
    private GameObject tooltipPanel;
    private TextMeshProUGUI tooltipText;
    private bool tooltipVisible;
    
    // Rarity colors
    private static readonly Color CommonColor = new Color(0.6f, 0.9f, 0.6f, 1f);       // pale green
    private static readonly Color LegendaryColor = new Color(0.75f, 0.55f, 0.9f, 1f);   // pale purple
    private static readonly Color CursedColor = new Color(0.9f, 0.45f, 0.45f, 1f);      // pale red
    
    private static readonly Color CommonBg = new Color(0.15f, 0.25f, 0.15f, 0.9f);
    private static readonly Color LegendaryBg = new Color(0.2f, 0.12f, 0.3f, 0.9f);
    private static readonly Color CursedBg = new Color(0.3f, 0.1f, 0.1f, 0.9f);
    
    // Highlight colors (brighter versions for "ready" state)
    private static readonly Color HighlightBorder = new Color(1f, 0.95f, 0.5f, 1f);  // gold glow
    private static readonly Color HighlightBg = new Color(0.25f, 0.22f, 0.1f, 0.95f);
    
    private class RelicChipData
    {
        public string RelicId;
        public GameObject ChipObj;
        public Image BgImage;
        public Outline OutlineComp;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI CounterText;
        public Color NormalBorderColor;
        public Color NormalBgColor;
        public RectTransform ContentRect; // inner child that gets shaken
        // Shake state
        public float ShakeTimer;
    }
    
    void Start()
    {
        var refs = FindFirstObjectByType<Referencer>();
        if (refs != null) player = refs.player;
        
        CreateUI();
    }
    
    void Update()
    {
        // Follow mouse for tooltip
        if (tooltipVisible && tooltipPanel != null)
        {
            Vector2 pos = Input.mousePosition;
            pos.x += 16f;
            pos.y -= 16f;
            
            var rt = tooltipPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                float w = rt.rect.width * rt.lossyScale.x;
                float h = rt.rect.height * rt.lossyScale.y;
                if (pos.x + w > Screen.width) pos.x = Screen.width - w;
                if (pos.y - h < 0) pos.y = h;
            }
            
            tooltipPanel.transform.position = pos;
        }
        
        UpdateChipStates();
    }
    
    void LateUpdate()
    {
        if (player == null) return;
        
        var relics = player.GetRelics();
        
        bool needsRebuild = false;
        if (chips.Count != relics.Count)
        {
            needsRebuild = true;
        }
        else
        {
            for (int i = 0; i < relics.Count; i++)
            {
                if (chips[i].RelicId != relics[i].Id)
                {
                    needsRebuild = true;
                    break;
                }
            }
        }
        
        if (needsRebuild)
        {
            RebuildChips(relics);
        }
    }
    
    private void UpdateChipStates()
    {
        if (player == null || chips.Count == 0) return;
        
        var states = player.GetRelicStates();
        
        // Build lookup
        var stateMap = new Dictionary<string, Player.RelicStateInfo>();
        foreach (var s in states)
            stateMap[s.RelicId] = s;
        
        bool anyTriggered = false;
        
        foreach (var chip in chips)
        {
            if (chip.ChipObj == null) continue;
            
            bool isReady = false;
            bool justTriggered = false;
            int currentCount = -1;
            int maxCount = -1;
            
            if (stateMap.TryGetValue(chip.RelicId, out var state))
            {
                isReady = state.IsReady;
                justTriggered = state.JustTriggered;
                if (state.MaxCount > 0)
                {
                    currentCount = state.CurrentCount;
                    maxCount = state.MaxCount;
                }
                if (justTriggered) anyTriggered = true;
            }
            
            // Update counter text
            if (chip.CounterText != null)
            {
                if (maxCount > 0)
                {
                    chip.CounterText.text = $"{currentCount}/{maxCount}";
                    chip.CounterText.gameObject.SetActive(true);
                }
                else
                {
                    chip.CounterText.gameObject.SetActive(false);
                }
            }
            
            // Update highlight
            if (isReady)
            {
                chip.BgImage.color = HighlightBg;
                chip.OutlineComp.effectColor = HighlightBorder;
                chip.OutlineComp.effectDistance = new Vector2(2, 2);
                chip.NameText.color = HighlightBorder;
            }
            else
            {
                chip.BgImage.color = chip.NormalBgColor;
                chip.OutlineComp.effectColor = chip.NormalBorderColor;
                chip.OutlineComp.effectDistance = new Vector2(1, 1);
                chip.NameText.color = chip.NormalBorderColor;
            }
            
            // Start shake on trigger
            if (justTriggered && chip.ShakeTimer <= 0f)
            {
                chip.ShakeTimer = 0.5f;
            }
            
            // Animate shake on inner content (grid controls outer position)
            if (chip.ShakeTimer > 0f && chip.ContentRect != null)
            {
                chip.ShakeTimer -= Time.deltaTime;
                float intensity = Mathf.Lerp(0f, 3f, chip.ShakeTimer / 0.5f);
                float offsetX = Mathf.Sin(Time.time * 60f) * intensity;
                float offsetY = Mathf.Cos(Time.time * 45f) * intensity * 0.5f;
                chip.ContentRect.anchoredPosition = new Vector2(offsetX, offsetY);
                
                if (chip.ShakeTimer <= 0f)
                {
                    chip.ContentRect.anchoredPosition = Vector2.zero;
                }
            }
        }
        
        // Clear triggered flags after processing
        if (anyTriggered)
        {
            player.ClearRelicJustTriggered();
        }
    }
    
    private void CreateUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        
        // Main panel (top-right, grid layout, grows downward)
        panelObj = new GameObject("RelicDisplayPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-10f, -10f);
        panelRect.sizeDelta = new Vector2(1076f, 0f);
        
        var glg = panelObj.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(130f, 24f);
        glg.spacing = new Vector2(4f, 4f);
        glg.startCorner = GridLayoutGroup.Corner.UpperRight;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperRight;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 8;
        glg.padding = new RectOffset(4, 4, 4, 4);
        
        var csf = panelObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        
        chipContainer = panelObj.transform;
        
        // Tooltip panel (screen-space, follows mouse)
        tooltipPanel = new GameObject("RelicTooltip");
        tooltipPanel.transform.SetParent(canvas.transform, false);
        
        var ttRect = tooltipPanel.AddComponent<RectTransform>();
        ttRect.sizeDelta = new Vector2(250f, 0f);
        ttRect.pivot = new Vector2(0f, 1f);
        
        var ttBg = tooltipPanel.AddComponent<Image>();
        ttBg.color = new Color(0.1f, 0.1f, 0.14f, 0.95f);
        ttBg.raycastTarget = false;
        
        var ttOutline = tooltipPanel.AddComponent<Outline>();
        ttOutline.effectColor = new Color(0.6f, 0.6f, 0.7f, 0.6f);
        ttOutline.effectDistance = new Vector2(1, 1);
        
        var ttVlg = tooltipPanel.AddComponent<VerticalLayoutGroup>();
        ttVlg.padding = new RectOffset(8, 8, 6, 6);
        ttVlg.childControlWidth = true;
        ttVlg.childControlHeight = true;
        ttVlg.childForceExpandWidth = true;
        
        var ttCsf = tooltipPanel.AddComponent<ContentSizeFitter>();
        ttCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        var ttTextObj = new GameObject("TooltipText");
        ttTextObj.transform.SetParent(tooltipPanel.transform, false);
        tooltipText = ttTextObj.AddComponent<TextMeshProUGUI>();
        tooltipText.fontSize = 14f;
        tooltipText.color = Color.white;
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.raycastTarget = false;
        
        tooltipPanel.SetActive(false);
        tooltipVisible = false;
    }
    
    private void RebuildChips(List<RelicData> relics)
    {
        foreach (var c in chips)
        {
            if (c.ChipObj != null) Destroy(c.ChipObj);
        }
        chips.Clear();
        
        if (chipContainer == null) return;
        
        foreach (var relic in relics)
        {
            var chip = CreateChip(relic);
            chips.Add(chip);
        }
    }
    
    private RelicChipData CreateChip(RelicData relic)
    {
        var data = new RelicChipData { RelicId = relic.Id };
        
        string rarity = (relic.Rarity ?? "Common").ToLower();
        Color borderColor = CommonColor;
        Color bgColor = CommonBg;
        if (rarity == "legendary") { borderColor = LegendaryColor; bgColor = LegendaryBg; }
        else if (rarity == "cursed") { borderColor = CursedColor; bgColor = CursedBg; }
        
        data.NormalBorderColor = borderColor;
        data.NormalBgColor = bgColor;
        
        // Outer grid cell (position controlled by GridLayoutGroup — never modify its anchoredPosition)
        data.ChipObj = new GameObject($"RelicChip_{relic.Id}");
        data.ChipObj.transform.SetParent(chipContainer, false);
        data.ChipObj.AddComponent<RectTransform>();
        
        // Inner content child (this gets shaken, holds all visuals)
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(data.ChipObj.transform, false);
        data.ContentRect = contentObj.AddComponent<RectTransform>();
        data.ContentRect.anchorMin = Vector2.zero;
        data.ContentRect.anchorMax = Vector2.one;
        data.ContentRect.offsetMin = Vector2.zero;
        data.ContentRect.offsetMax = Vector2.zero;
        
        data.BgImage = contentObj.AddComponent<Image>();
        data.BgImage.color = bgColor;
        data.BgImage.raycastTarget = true;
        
        data.OutlineComp = contentObj.AddComponent<Outline>();
        data.OutlineComp.effectColor = borderColor;
        data.OutlineComp.effectDistance = new Vector2(1, 1);
        
        // Name text (left-aligned, leave space for counter on right)
        var textObj = new GameObject("Name");
        textObj.transform.SetParent(contentObj.transform, false);
        
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(6f, 0f);
        textRect.offsetMax = new Vector2(-30f, 0f);
        
        data.NameText = textObj.AddComponent<TextMeshProUGUI>();
        data.NameText.text = relic.DisplayName;
        data.NameText.fontSize = 12f;
        data.NameText.color = borderColor;
        data.NameText.alignment = TextAlignmentOptions.MidlineLeft;
        data.NameText.textWrappingMode = TextWrappingModes.NoWrap;
        data.NameText.overflowMode = TextOverflowModes.Ellipsis;
        data.NameText.raycastTarget = false;
        
        // Counter text (right-aligned, small)
        var counterObj = new GameObject("Counter");
        counterObj.transform.SetParent(contentObj.transform, false);
        
        var counterRect = counterObj.AddComponent<RectTransform>();
        counterRect.anchorMin = new Vector2(1f, 0f);
        counterRect.anchorMax = new Vector2(1f, 1f);
        counterRect.pivot = new Vector2(1f, 0.5f);
        counterRect.anchoredPosition = new Vector2(-4f, 0f);
        counterRect.sizeDelta = new Vector2(30f, 24f);
        
        data.CounterText = counterObj.AddComponent<TextMeshProUGUI>();
        data.CounterText.text = "";
        data.CounterText.fontSize = 10f;
        data.CounterText.color = new Color(0.8f, 0.8f, 0.8f, 0.7f);
        data.CounterText.alignment = TextAlignmentOptions.MidlineRight;
        data.CounterText.textWrappingMode = TextWrappingModes.NoWrap;
        data.CounterText.raycastTarget = false;
        counterObj.SetActive(false);
        
        // Hover tooltip (on content so it captures pointer)
        var trigger = contentObj.AddComponent<EventTrigger>();
        
        var enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        string tooltipContent = $"<b><color=#{ColorUtility.ToHtmlStringRGB(borderColor)}>{relic.DisplayName}</color></b>\n<size=12>{relic.Description}</size>";
        enterEntry.callback.AddListener((d) => ShowTooltip(tooltipContent));
        trigger.triggers.Add(enterEntry);
        
        var exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((d) => HideTooltip());
        trigger.triggers.Add(exitEntry);
        
        return data;
    }
    
    private void ShowTooltip(string text)
    {
        if (tooltipPanel == null) return;
        tooltipText.text = text;
        tooltipPanel.SetActive(true);
        tooltipVisible = true;
        tooltipPanel.transform.SetAsLastSibling();
    }
    
    private void HideTooltip()
    {
        if (tooltipPanel == null) return;
        tooltipPanel.SetActive(false);
        tooltipVisible = false;
    }
}
