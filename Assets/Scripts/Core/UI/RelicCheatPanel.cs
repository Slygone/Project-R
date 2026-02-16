using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Debug cheat panel to toggle relics on/off during gameplay.
/// Press K to open/close. Toggling a relic on calls Player.AddRelic,
/// toggling off calls Player.RemoveRelic. Fully data-driven from DataCache.Relics.
/// </summary>
public class RelicCheatPanel : MonoBehaviour
{
    private Player player;
    private GameObject panelObj;
    private Transform contentContainer;
    private ScrollRect scrollRect;
    private bool isOpen = false;
    
    private List<RelicToggleRow> rows = new List<RelicToggleRow>();
    
    private static readonly Color CommonColor = new Color(0.6f, 0.9f, 0.6f, 1f);
    private static readonly Color LegendaryColor = new Color(0.75f, 0.55f, 0.9f, 1f);
    private static readonly Color CursedColor = new Color(0.9f, 0.45f, 0.45f, 1f);
    
    private class RelicToggleRow
    {
        public RelicData Relic;
        public GameObject RowObj;
        public Image ToggleBg;
        public TextMeshProUGUI ToggleLabel;
        public bool IsActive;
    }
    
    void Start()
    {
        var refs = FindFirstObjectByType<Referencer>();
        if (refs != null) player = refs.player;
        
        CreatePanel();
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            TogglePanel();
        }
        
        // Keep toggle states in sync with player relics
        if (isOpen && player != null)
        {
            var playerRelics = player.GetRelics();
            foreach (var row in rows)
            {
                bool hasIt = playerRelics.Any(r => r.Id == row.Relic.Id);
                if (hasIt != row.IsActive)
                {
                    row.IsActive = hasIt;
                    UpdateRowVisual(row);
                }
            }
        }
    }
    
    private void TogglePanel()
    {
        isOpen = !isOpen;
        if (panelObj != null)
        {
            panelObj.SetActive(isOpen);
        }
    }
    
    private void CreatePanel()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        
        // Dark overlay panel, centered, scrollable
        panelObj = new GameObject("RelicCheatPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        
        var panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.05f);
        panelRect.anchorMax = new Vector2(0.85f, 0.95f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        var panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.08f, 0.12f, 0.96f);
        panelBg.raycastTarget = true;
        
        var panelOutline = panelObj.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.5f, 0.5f, 0.6f, 0.5f);
        panelOutline.effectDistance = new Vector2(2, 2);
        
        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -8f);
        titleRect.sizeDelta = new Vector2(0f, 36f);
        
        var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "RELIC CHEAT PANEL (K to close)";
        titleTmp.fontSize = 20f;
        titleTmp.color = new Color(1f, 0.85f, 0.4f);
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.raycastTarget = false;
        
        // Scroll area
        var scrollObj = new GameObject("ScrollArea");
        scrollObj.transform.SetParent(panelObj.transform, false);
        
        var scrollRectTr = scrollObj.AddComponent<RectTransform>();
        scrollRectTr.anchorMin = new Vector2(0f, 0f);
        scrollRectTr.anchorMax = new Vector2(1f, 1f);
        scrollRectTr.offsetMin = new Vector2(10f, 10f);
        scrollRectTr.offsetMax = new Vector2(-10f, -50f);
        
        scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        
        var scrollMask = scrollObj.AddComponent<RectMask2D>();
        
        // Content container
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        
        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        
        var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 3f;
        vlg.padding = new RectOffset(8, 8, 4, 4);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;
        
        var csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollRect.content = contentRect;
        contentContainer = contentObj.transform;
        
        // Populate rows from DataCache
        PopulateRows();
        
        panelObj.SetActive(false);
    }
    
    private void PopulateRows()
    {
        if (!DataCache.IsLoaded) return;
        
        var allRelics = DataCache.Relics;
        if (allRelics == null) return;
        
        // Group by rarity
        string[] rarityOrder = { "Common", "Legendary", "Cursed" };
        
        foreach (string rarity in rarityOrder)
        {
            // Section header
            CreateSectionHeader(rarity);
            
            foreach (var relic in allRelics)
            {
                if (relic.Rarity != rarity) continue;
                CreateRelicRow(relic);
            }
        }
    }
    
    private void CreateSectionHeader(string rarity)
    {
        Color headerColor = CommonColor;
        if (rarity == "Legendary") headerColor = LegendaryColor;
        else if (rarity == "Cursed") headerColor = CursedColor;
        
        var headerObj = new GameObject($"Header_{rarity}");
        headerObj.transform.SetParent(contentContainer, false);
        
        var headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.sizeDelta = new Vector2(0f, 28f);
        
        var le = headerObj.AddComponent<LayoutElement>();
        le.preferredHeight = 28f;
        le.minHeight = 28f;
        
        var tmp = headerObj.AddComponent<TextMeshProUGUI>();
        tmp.text = $"── {rarity.ToUpper()} ──";
        tmp.fontSize = 16f;
        tmp.color = headerColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
    }
    
    private void CreateRelicRow(RelicData relic)
    {
        var row = new RelicToggleRow { Relic = relic, IsActive = false };
        
        string rarity = (relic.Rarity ?? "Common").ToLower();
        Color textColor = CommonColor;
        if (rarity == "legendary") textColor = LegendaryColor;
        else if (rarity == "cursed") textColor = CursedColor;
        
        // Row object
        row.RowObj = new GameObject($"Row_{relic.Id}");
        row.RowObj.transform.SetParent(contentContainer, false);
        
        var rowRect = row.RowObj.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, 30f);
        
        var le = row.RowObj.AddComponent<LayoutElement>();
        le.preferredHeight = 30f;
        le.minHeight = 30f;
        
        var hlg = row.RowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(4, 4, 2, 2);
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        
        // Toggle button
        var toggleObj = new GameObject("Toggle");
        toggleObj.transform.SetParent(row.RowObj.transform, false);
        
        var toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(50f, 26f);
        
        var toggleLe = toggleObj.AddComponent<LayoutElement>();
        toggleLe.preferredWidth = 50f;
        toggleLe.minWidth = 50f;
        
        row.ToggleBg = toggleObj.AddComponent<Image>();
        row.ToggleBg.color = new Color(0.3f, 0.15f, 0.15f, 0.9f); // Red = off
        row.ToggleBg.raycastTarget = true;
        
        var toggleTextObj = new GameObject("Label");
        toggleTextObj.transform.SetParent(toggleObj.transform, false);
        
        var toggleTextRect = toggleTextObj.AddComponent<RectTransform>();
        toggleTextRect.anchorMin = Vector2.zero;
        toggleTextRect.anchorMax = Vector2.one;
        toggleTextRect.offsetMin = Vector2.zero;
        toggleTextRect.offsetMax = Vector2.zero;
        
        row.ToggleLabel = toggleTextObj.AddComponent<TextMeshProUGUI>();
        row.ToggleLabel.text = "OFF";
        row.ToggleLabel.fontSize = 12f;
        row.ToggleLabel.color = new Color(0.9f, 0.4f, 0.4f);
        row.ToggleLabel.alignment = TextAlignmentOptions.Center;
        row.ToggleLabel.raycastTarget = false;
        
        var toggleBtn = toggleObj.AddComponent<Button>();
        toggleBtn.targetGraphic = row.ToggleBg;
        var capturedRow = row;
        toggleBtn.onClick.AddListener(() => OnToggleClicked(capturedRow));
        
        // Relic name
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(row.RowObj.transform, false);
        
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.sizeDelta = new Vector2(200f, 26f);
        
        var nameLe = nameObj.AddComponent<LayoutElement>();
        nameLe.preferredWidth = 200f;
        nameLe.flexibleWidth = 1f;
        
        var nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
        nameTmp.text = relic.DisplayName;
        nameTmp.fontSize = 14f;
        nameTmp.color = textColor;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
        nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
        nameTmp.overflowMode = TextOverflowModes.Ellipsis;
        nameTmp.raycastTarget = false;
        
        // Description
        var descObj = new GameObject("Desc");
        descObj.transform.SetParent(row.RowObj.transform, false);
        
        var descRect = descObj.AddComponent<RectTransform>();
        descRect.sizeDelta = new Vector2(300f, 26f);
        
        var descLe = descObj.AddComponent<LayoutElement>();
        descLe.preferredWidth = 300f;
        descLe.flexibleWidth = 2f;
        
        var descTmp = descObj.AddComponent<TextMeshProUGUI>();
        descTmp.text = relic.Description;
        descTmp.fontSize = 11f;
        descTmp.color = new Color(0.7f, 0.7f, 0.7f);
        descTmp.alignment = TextAlignmentOptions.MidlineLeft;
        descTmp.textWrappingMode = TextWrappingModes.NoWrap;
        descTmp.overflowMode = TextOverflowModes.Ellipsis;
        descTmp.raycastTarget = false;
        
        rows.Add(row);
    }
    
    private void OnToggleClicked(RelicToggleRow row)
    {
        if (player == null) return;
        
        if (row.IsActive)
        {
            // Turn off: remove relic
            var playerRelics = player.GetRelics();
            var match = playerRelics.FirstOrDefault(r => r.Id == row.Relic.Id);
            if (match != null)
            {
                player.RemoveRelic(match);
            }
            row.IsActive = false;
        }
        else
        {
            // Turn on: add relic (avoid duplicates)
            var playerRelics = player.GetRelics();
            if (!playerRelics.Any(r => r.Id == row.Relic.Id))
            {
                player.AddRelic(row.Relic);
            }
            row.IsActive = true;
        }
        
        UpdateRowVisual(row);
    }
    
    private void UpdateRowVisual(RelicToggleRow row)
    {
        if (row.IsActive)
        {
            row.ToggleBg.color = new Color(0.15f, 0.3f, 0.15f, 0.9f); // Green = on
            row.ToggleLabel.text = "ON";
            row.ToggleLabel.color = new Color(0.4f, 0.9f, 0.4f);
        }
        else
        {
            row.ToggleBg.color = new Color(0.3f, 0.15f, 0.15f, 0.9f); // Red = off
            row.ToggleLabel.text = "OFF";
            row.ToggleLabel.color = new Color(0.9f, 0.4f, 0.4f);
        }
    }
}
