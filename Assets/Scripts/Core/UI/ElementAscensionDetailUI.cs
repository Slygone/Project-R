using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Element Ascension detail screen showing level ladder and level-up button.
/// Clean tier-list layout matching reference design.
/// </summary>
public class ElementAscensionDetailUI : MonoBehaviour
{
    private static ElementAscensionDetailUI _instance;
    public static ElementAscensionDetailUI Instance => _instance;
    
    private GameObject detailPanel;
    private string currentElement;
    private Action onClose;
    private Transform tierListContent;
    
    // Accent color for the current element (used for borders, highlights)
    private Color accentColor;
    
    void Awake()
    {
        _instance = this;
        SetupUI();
    }
    
    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[ElementAscensionDetailUI] Canvas not found");
            return;
        }
        
        detailPanel = CreateDetailPanel(canvas.transform);
        detailPanel.SetActive(false);
    }
    
    private GameObject CreateDetailPanel(Transform parent)
    {
        var panel = new GameObject("ElementAscensionDetailPanel");
        panel.transform.SetParent(parent, false);
        
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.10f, 0.98f);
        
        return panel;
    }
    
    public void Show(string elementName, Action closeCallback)
    {
        currentElement = elementName;
        onClose = closeCallback;
        accentColor = GetElementColor(currentElement);
        
        Debug.Log($"[Meta] ElementAscensionOpen | element={elementName} level={MetaProgressionManager.Instance.GetElementProgress(elementName).AscensionLevel}");
        
        ClearContent();
        CreateContent();
        
        detailPanel.SetActive(true);
    }
    
    public void Hide()
    {
        detailPanel.SetActive(false);
    }
    
    private void ClearContent()
    {
        for (int i = detailPanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(detailPanel.transform.GetChild(i).gameObject);
        }
    }
    
    private void CreateContent()
    {
        var progress = MetaProgressionManager.Instance.GetElementProgress(currentElement);
        int maxLevel = DataCache.GetElementMaxLevel(currentElement);
        
        // ── Back Button (top-left) ──
        CreateBackButton(detailPanel.transform);
        
        // ── Title: "WIND ASCENSION" ──
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(detailPanel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.88f);
        titleRect.anchorMax = new Vector2(1f, 0.96f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = $"{currentElement.ToUpper()} ASCENSION";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 34;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = accentColor;
        titleText.raycastTarget = false;
        
        // ── Subtitle: "Current Tier: X" ──
        var subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(detailPanel.transform, false);
        var subRect = subtitleObj.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0.83f);
        subRect.anchorMax = new Vector2(1f, 0.88f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;
        var subText = subtitleObj.AddComponent<TextMeshProUGUI>();
        subText.text = progress.AscensionLevel >= maxLevel
            ? $"Current Tier: {progress.AscensionLevel}  (MAX)"
            : $"Current Tier: {progress.AscensionLevel}";
        subText.alignment = TextAlignmentOptions.Center;
        subText.fontSize = 18;
        subText.color = new Color(0.8f, 0.8f, 0.8f);
        subText.raycastTarget = false;
        
        // ── Tier List (scrollable) ──
        CreateTierList(progress.AscensionLevel, maxLevel);
        
        // ── Level Up Button (bottom center) ──
        CreateLevelUpButton(progress.AscensionLevel, maxLevel);
    }
    
    private void CreateBackButton(Transform parent)
    {
        var btnObj = new GameObject("BackBtn");
        btnObj.transform.SetParent(parent, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.02f, 0.92f);
        btnRect.anchorMax = new Vector2(0.12f, 0.98f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        
        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.18f, 0.22f, 0.9f);
        
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OnBackClicked);
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "← BACK";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 16;
        text.color = Color.white;
    }
    
    private void CreateTierList(int currentLevel, int maxLevel)
    {
        var costs = DataCache.ElementalAscensionCosts.ContainsKey(currentElement) 
            ? DataCache.ElementalAscensionCosts[currentElement] 
            : new List<ElementalAscensionCost>();
        
        int regularCores = MetaProgressionManager.Instance.GetRegularCores();
        int ascendedCores = MetaProgressionManager.Instance.GetAscendedCores();
        
        // Scroll container
        var scrollObj = new GameObject("TierScroll");
        scrollObj.transform.SetParent(detailPanel.transform, false);
        var scrollRectTransform = scrollObj.AddComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.08f, 0.14f);
        scrollRectTransform.anchorMax = new Vector2(0.92f, 0.80f);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;
        
        var scrollView = scrollObj.AddComponent<ScrollRect>();
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.movementType = ScrollRect.MovementType.Clamped;
        scrollView.scrollSensitivity = 30f;
        
        // Viewport with mask
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        var viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        viewport.AddComponent<Image>().color = Color.white;
        scrollView.viewport = viewportRect;
        
        // Content container
        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        
        var vLayout = content.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 4;
        vLayout.padding = new RectOffset(0, 0, 6, 6);
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollView.content = contentRect;
        tierListContent = content.transform;
        
        // Build element display name (e.g. "Wind Flow Amplification")
        string elementDisplayName = $"{currentElement} Flow Amplification";
        
        // Create rows
        foreach (var cost in costs)
        {
            CreateTierRow(cost, currentLevel, elementDisplayName, regularCores, ascendedCores);
        }
    }
    
    private void CreateTierRow(ElementalAscensionCost cost, int currentLevel, string elementDisplayName, int ownedRegular, int ownedAscended)
    {
        bool isUnlocked = currentLevel >= cost.Level;
        bool isCurrent = currentLevel == cost.Level;
        bool isNext = currentLevel == cost.Level - 1;
        
        float rowHeight = 52f;
        
        // Row root
        var rowObj = new GameObject($"Tier_{cost.Level}");
        rowObj.transform.SetParent(tierListContent, false);
        var rowLe = rowObj.AddComponent<LayoutElement>();
        rowLe.preferredHeight = rowHeight;
        
        // Row background
        var rowBg = rowObj.AddComponent<Image>();
        if (isCurrent)
            rowBg.color = new Color(accentColor.r * 0.15f, accentColor.g * 0.15f, accentColor.b * 0.15f, 0.9f);
        else if (isNext)
            rowBg.color = new Color(0.10f, 0.12f, 0.16f, 0.9f);
        else if (isUnlocked)
            rowBg.color = new Color(0.08f, 0.09f, 0.12f, 0.85f);
        else
            rowBg.color = new Color(0.06f, 0.07f, 0.09f, 0.7f);
        
        // Left accent border (element color)
        var accentObj = new GameObject("Accent");
        accentObj.transform.SetParent(rowObj.transform, false);
        var accentRect = accentObj.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0.005f, 1f);
        accentRect.offsetMin = Vector2.zero;
        accentRect.offsetMax = Vector2.zero;
        var accentImg = accentObj.AddComponent<Image>();
        accentImg.color = isUnlocked || isNext ? accentColor : new Color(accentColor.r * 0.4f, accentColor.g * 0.4f, accentColor.b * 0.4f);
        
        // "Tier X" label (left column)
        var tierLabelObj = new GameObject("TierLabel");
        tierLabelObj.transform.SetParent(rowObj.transform, false);
        var tierLabelRect = tierLabelObj.AddComponent<RectTransform>();
        tierLabelRect.anchorMin = new Vector2(0.02f, 0f);
        tierLabelRect.anchorMax = new Vector2(0.12f, 1f);
        tierLabelRect.offsetMin = Vector2.zero;
        tierLabelRect.offsetMax = Vector2.zero;
        var tierLabel = tierLabelObj.AddComponent<TextMeshProUGUI>();
        tierLabel.text = $"Tier {cost.Level}";
        tierLabel.fontSize = 16;
        tierLabel.fontStyle = FontStyles.Bold;
        tierLabel.alignment = TextAlignmentOptions.MidlineLeft;
        tierLabel.color = isUnlocked ? Color.white : new Color(0.55f, 0.55f, 0.55f);
        
        // Element name + placeholder bonus (middle column)
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(rowObj.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.14f, 0f);
        nameRect.anchorMax = new Vector2(0.68f, 1f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = $"<b>{elementDisplayName}</b> <color=#888888><size=12>(Bonus description will be added)</size></color>";
        nameText.fontSize = 14;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.color = isUnlocked ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.5f, 0.5f, 0.5f);
        nameText.richText = true;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        
        // Core cost (right column) — e.g. "0/3 Regular Cores"
        var costObj = new GameObject("Cost");
        costObj.transform.SetParent(rowObj.transform, false);
        var costRect = costObj.AddComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0.70f, 0f);
        costRect.anchorMax = new Vector2(0.98f, 1f);
        costRect.offsetMin = Vector2.zero;
        costRect.offsetMax = Vector2.zero;
        var costText = costObj.AddComponent<TextMeshProUGUI>();
        costText.fontSize = 13;
        costText.alignment = TextAlignmentOptions.MidlineRight;
        costText.richText = true;
        
        if (isUnlocked)
        {
            costText.text = "<color=#55cc77>Unlocked</color>";
            costText.color = Color.white;
        }
        else
        {
            string costStr = BuildCostString(cost, ownedRegular, ownedAscended);
            costText.text = costStr;
            costText.color = new Color(0.75f, 0.75f, 0.75f);
        }
        
        // Current tier highlight bar (bottom)
        if (isCurrent)
        {
            var highlightObj = new GameObject("CurrentHighlight");
            highlightObj.transform.SetParent(rowObj.transform, false);
            var hlRect = highlightObj.AddComponent<RectTransform>();
            hlRect.anchorMin = new Vector2(0f, 0f);
            hlRect.anchorMax = new Vector2(1f, 0.04f);
            hlRect.offsetMin = Vector2.zero;
            hlRect.offsetMax = Vector2.zero;
            var hlImg = highlightObj.AddComponent<Image>();
            hlImg.color = accentColor;
        }
    }
    
    private string BuildCostString(ElementalAscensionCost cost, int ownedRegular, int ownedAscended)
    {
        string result = "";
        if (cost.RegularCost > 0)
        {
            string regColor = ownedRegular >= cost.RegularCost ? "#ccaa44" : "#ff5555";
            result += $"<color={regColor}>{ownedRegular}/{cost.RegularCost}</color> Regular Core";
            if (cost.RegularCost > 1) result += "s";
        }
        if (cost.RegularCost > 0 && cost.AscendedCost > 0)
        {
            result += "\n";
        }
        if (cost.AscendedCost > 0)
        {
            string ascColor = ownedAscended >= cost.AscendedCost ? "#aa66ff" : "#ff5555";
            result += $"<color={ascColor}>{ownedAscended}/{cost.AscendedCost}</color> Ascended Core";
            if (cost.AscendedCost > 1) result += "s";
        }
        if (cost.RegularCost == 0 && cost.AscendedCost == 0)
        {
            result = "<color=#55cc77>Free</color>";
        }
        return result;
    }
    
    private void CreateLevelUpButton(int currentLevel, int maxLevel)
    {
        bool canLevelUp = MetaProgressionManager.Instance.CanLevelUpElement(currentElement);
        bool isMaxed = currentLevel >= maxLevel;
        
        var btnObj = new GameObject("LevelUpBtn");
        btnObj.transform.SetParent(detailPanel.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.30f, 0.03f);
        btnRect.anchorMax = new Vector2(0.70f, 0.11f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        
        var img = btnObj.AddComponent<Image>();
        if (isMaxed)
            img.color = new Color(0.15f, 0.15f, 0.18f, 0.8f);
        else if (canLevelUp)
            img.color = accentColor;
        else
            img.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        
        // Border effect
        var outline = btnObj.AddComponent<Outline>();
        outline.effectColor = canLevelUp ? new Color(accentColor.r * 1.3f, accentColor.g * 1.3f, accentColor.b * 1.3f, 0.8f) : new Color(0.3f, 0.3f, 0.35f, 0.6f);
        outline.effectDistance = new Vector2(2, 2);
        
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = canLevelUp;
        btn.onClick.AddListener(OnLevelUpClicked);
        
        if (!canLevelUp)
        {
            var colors = btn.colors;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
            btn.colors = colors;
        }
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = isMaxed ? "MAX LEVEL" : "Level Up!";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24;
        text.fontStyle = FontStyles.Bold;
        text.color = isMaxed ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.05f, 0.05f, 0.08f);
    }
    
    private void OnLevelUpClicked()
    {
        if (MetaProgressionManager.Instance.TryLevelUpElement(currentElement))
        {
            ClearContent();
            CreateContent();
        }
    }
    
    private void OnBackClicked()
    {
        Hide();
        onClose?.Invoke();
    }
    
    private Color GetElementColor(string element) => ElementColors.Get(element);
}
