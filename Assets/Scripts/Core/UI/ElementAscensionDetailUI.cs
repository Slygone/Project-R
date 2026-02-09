using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Element Ascension detail screen showing level ladder and level-up button.
/// "Genshin Impact-like ascension" screen for element progression.
/// </summary>
public class ElementAscensionDetailUI : MonoBehaviour
{
    private static ElementAscensionDetailUI _instance;
    public static ElementAscensionDetailUI Instance => _instance;
    
    private GameObject detailPanel;
    private string currentElement;
    private Action onClose;
    private Button levelUpButton;
    private TextMeshProUGUI currentLevelText;
    private TextMeshProUGUI currentXPText;
    private Transform tierListContent;
    
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
        bg.color = new Color(0.02f, 0.02f, 0.06f, 0.98f);
        
        // Back button
        CreateBackButton(panel.transform);
        
        return panel;
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
        img.color = new Color(0.25f, 0.25f, 0.3f);
        
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
    
    public void Show(string elementName, Action closeCallback)
    {
        currentElement = elementName;
        onClose = closeCallback;
        
        Debug.Log($"[Meta] ElementAscensionOpen | element={elementName} level={MetaProgressionManager.Instance.GetElementProgress(elementName).AscensionLevel} xp={MetaProgressionManager.Instance.GetElementProgress(elementName).AscensionXP}");
        
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
        // Clear dynamic content (keep back button)
        for (int i = detailPanel.transform.childCount - 1; i >= 0; i--)
        {
            var child = detailPanel.transform.GetChild(i);
            if (child.name != "BackBtn")
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    private void CreateContent()
    {
        var progress = MetaProgressionManager.Instance.GetElementProgress(currentElement);
        int maxLevel = DataCache.GetElementMaxLevel(currentElement);
        int nextLevelThreshold = DataCache.GetElementXPThreshold(currentElement, progress.AscensionLevel + 1);
        
        // Element Title
        var titleObj = new GameObject("ElementTitle");
        titleObj.transform.SetParent(detailPanel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.85f);
        titleRect.anchorMax = new Vector2(1, 0.92f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = $"{currentElement.ToUpper()} ASCENSION";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = GetElementColor(currentElement);
        
        // Current Level/XP display
        var statusObj = new GameObject("Status");
        statusObj.transform.SetParent(detailPanel.transform, false);
        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.3f, 0.75f);
        statusRect.anchorMax = new Vector2(0.7f, 0.84f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
        
        var vLayout = statusObj.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.MiddleCenter;
        vLayout.spacing = 5;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        
        // Level text
        var levelObj = new GameObject("Level");
        levelObj.transform.SetParent(statusObj.transform, false);
        var levelLe = levelObj.AddComponent<LayoutElement>();
        levelLe.preferredHeight = 30;
        currentLevelText = levelObj.AddComponent<TextMeshProUGUI>();
        currentLevelText.text = $"Tier {progress.AscensionLevel}";
        currentLevelText.fontSize = 24;
        currentLevelText.alignment = TextAlignmentOptions.Center;
        currentLevelText.color = Color.white;
        
        // XP text
        var xpObj = new GameObject("XP");
        xpObj.transform.SetParent(statusObj.transform, false);
        var xpLe = xpObj.AddComponent<LayoutElement>();
        xpLe.preferredHeight = 24;
        currentXPText = xpObj.AddComponent<TextMeshProUGUI>();
        string xpDisplay = progress.AscensionLevel >= maxLevel 
            ? $"XP: {progress.AscensionXP} (MAX LEVEL)" 
            : $"XP: {progress.AscensionXP} / {nextLevelThreshold}";
        currentXPText.text = xpDisplay;
        currentXPText.fontSize = 16;
        currentXPText.alignment = TextAlignmentOptions.Center;
        currentXPText.color = new Color(0.7f, 0.7f, 0.7f);
        
        // Level Up Button
        CreateLevelUpButton();
        
        // Tier Ladder (scrollable)
        CreateTierLadder();
    }
    
    private void CreateLevelUpButton()
    {
        var progress = MetaProgressionManager.Instance.GetElementProgress(currentElement);
        bool canLevelUp = MetaProgressionManager.Instance.CanLevelUpElement(currentElement);
        
        var btnObj = new GameObject("LevelUpBtn");
        btnObj.transform.SetParent(detailPanel.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.35f, 0.65f);
        btnRect.anchorMax = new Vector2(0.65f, 0.73f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        
        var img = btnObj.AddComponent<Image>();
        img.color = canLevelUp ? new Color(0.2f, 0.5f, 0.3f) : new Color(0.3f, 0.3f, 0.35f);
        
        levelUpButton = btnObj.AddComponent<Button>();
        levelUpButton.targetGraphic = img;
        levelUpButton.interactable = canLevelUp;
        levelUpButton.onClick.AddListener(OnLevelUpClicked);
        
        if (!canLevelUp)
        {
            var colors = levelUpButton.colors;
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            levelUpButton.colors = colors;
        }
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "LEVEL UP";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 20;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
    }
    
    private void CreateTierLadder()
    {
        var progress = MetaProgressionManager.Instance.GetElementProgress(currentElement);
        var tiers = DataCache.ElementalTiers.ContainsKey(currentElement) 
            ? DataCache.ElementalTiers[currentElement] 
            : new List<ElementalTierData>();
        
        // Scroll container
        var scrollObj = new GameObject("TierScroll");
        scrollObj.transform.SetParent(detailPanel.transform, false);
        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.1f, 0.08f);
        scrollRect.anchorMax = new Vector2(0.9f, 0.62f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;
        
        var scrollView = scrollObj.AddComponent<ScrollRect>();
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.movementType = ScrollRect.MovementType.Clamped;
        
        // Viewport
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        
        var viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        var viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = Color.white;
        
        scrollView.viewport = viewportRect;
        
        // Content
        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        
        var vLayout = content.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 8;
        vLayout.padding = new RectOffset(10, 10, 10, 10);
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollView.content = contentRect;
        tierListContent = content.transform;
        
        // Add tier rows
        foreach (var tier in tiers)
        {
            CreateTierRow(tier, progress.AscensionLevel);
        }
    }
    
    private void CreateTierRow(ElementalTierData tier, int currentLevel)
    {
        bool isUnlocked = currentLevel >= tier.Level;
        bool isCurrent = currentLevel == tier.Level;
        
        var rowObj = new GameObject($"Tier_{tier.Level}");
        rowObj.transform.SetParent(tierListContent, false);
        
        var rowBg = rowObj.AddComponent<Image>();
        if (isCurrent)
            rowBg.color = new Color(0.15f, 0.25f, 0.15f, 0.9f);
        else if (isUnlocked)
            rowBg.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        else
            rowBg.color = new Color(0.08f, 0.08f, 0.1f, 0.6f);
        
        var rowLayout = rowObj.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 50;
        
        // Level number - left
        var levelObj = new GameObject("Level");
        levelObj.transform.SetParent(rowObj.transform, false);
        var levelRect = levelObj.AddComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.02f, 0);
        levelRect.anchorMax = new Vector2(0.12f, 1);
        levelRect.offsetMin = Vector2.zero;
        levelRect.offsetMax = Vector2.zero;
        var levelText = levelObj.AddComponent<TextMeshProUGUI>();
        levelText.text = $"{tier.Level}";
        levelText.fontSize = 18;
        levelText.fontStyle = FontStyles.Bold;
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        
        // XP threshold - middle-left
        var xpObj = new GameObject("XP");
        xpObj.transform.SetParent(rowObj.transform, false);
        var xpRect = xpObj.AddComponent<RectTransform>();
        xpRect.anchorMin = new Vector2(0.14f, 0);
        xpRect.anchorMax = new Vector2(0.28f, 1);
        xpRect.offsetMin = Vector2.zero;
        xpRect.offsetMax = Vector2.zero;
        var xpText = xpObj.AddComponent<TextMeshProUGUI>();
        xpText.text = $"{tier.XPRequiredToReachLevel} XP";
        xpText.fontSize = 13;
        xpText.alignment = TextAlignmentOptions.Left;
        xpText.color = isUnlocked ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f);
        
        // Bonus text - right
        var bonusObj = new GameObject("Bonus");
        bonusObj.transform.SetParent(rowObj.transform, false);
        var bonusRect = bonusObj.AddComponent<RectTransform>();
        bonusRect.anchorMin = new Vector2(0.30f, 0);
        bonusRect.anchorMax = new Vector2(0.98f, 1);
        bonusRect.offsetMin = Vector2.zero;
        bonusRect.offsetMax = Vector2.zero;
        var bonusText = bonusObj.AddComponent<TextMeshProUGUI>();
        bonusText.text = tier.Bonus;
        bonusText.fontSize = 13;
        bonusText.alignment = TextAlignmentOptions.Left;
        bonusText.color = isUnlocked ? new Color(0.5f, 0.85f, 0.5f) : new Color(0.35f, 0.45f, 0.35f);
        
        // Current level indicator
        if (isCurrent)
        {
            var indicatorObj = new GameObject("CurrentIndicator");
            indicatorObj.transform.SetParent(rowObj.transform, false);
            var indRect = indicatorObj.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(0, 0.4f);
            indRect.anchorMax = new Vector2(0.01f, 0.6f);
            indRect.offsetMin = Vector2.zero;
            indRect.offsetMax = Vector2.zero;
            var indImg = indicatorObj.AddComponent<Image>();
            indImg.color = new Color(0.3f, 0.9f, 0.4f);
        }
    }
    
    private void OnLevelUpClicked()
    {
        if (MetaProgressionManager.Instance.TryLevelUpElement(currentElement))
        {
            // Refresh the UI
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
