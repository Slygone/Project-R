using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// New Run character selection UI showing ALL characters from JSON.
/// Displays character cards with ascension level and yellow "UP" badge.
/// </summary>
public class NewRunCharacterSelectUI : MonoBehaviour
{
    private GameObject selectionPanel;
    private Action<CharacterData> onCharacterSelected;
    private Action onBackClicked;
    private bool isActive = false;
    
    void Awake()
    {
        SetupUI();
    }
    
    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[NewRunCharacterSelectUI] Canvas not found");
            return;
        }
        
        selectionPanel = CreateSelectionPanel(canvas.transform);
        selectionPanel.SetActive(false);
    }
    
    private GameObject CreateSelectionPanel(Transform parent)
    {
        var panel = new GameObject("NewRunCharacterSelectPanel");
        panel.transform.SetParent(parent, false);
        
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.98f);
        
        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.85f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "SELECT YOUR CHARACTER";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.2f);
        
        // Subtitle
        var subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(panel.transform, false);
        var subtitleRect = subtitleObj.AddComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0, 0.78f);
        subtitleRect.anchorMax = new Vector2(1, 0.85f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;
        var subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "Complete runs to level up and unlock talent tiers";
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontSize = 18;
        subtitleText.color = new Color(0.7f, 0.7f, 0.7f);
        
        // Back button
        var backBtn = CreateBackButton(panel.transform);
        
        return panel;
    }
    
    private Button CreateBackButton(Transform parent)
    {
        var btnObj = new GameObject("BackButton");
        btnObj.transform.SetParent(parent, false);
        
        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.02f, 0.92f);
        rect.anchorMax = new Vector2(0.12f, 0.98f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.35f);
        
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OnBackButtonClicked);
        
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
        text.fontSize = 18;
        text.color = Color.white;
        
        return btn;
    }
    
    public void Show(Action<CharacterData> selectCallback, Action backCallback)
    {
        onCharacterSelected = selectCallback;
        onBackClicked = backCallback;
        
        // Ensure DataCache is loaded
        if (!DataCache.IsLoaded)
        {
            DataCache.LoadAll();
        }
        
        ClearDynamicContent();
        CreateCharacterCards();
        CreateElementalAscensionSection();
        
        selectionPanel.SetActive(true);
        isActive = true;
    }
    
    public void Hide()
    {
        selectionPanel.SetActive(false);
        isActive = false;
    }
    
    private void ClearDynamicContent()
    {
        // Clear character cards
        var container = selectionPanel.transform.Find("CharacterScroll");
        if (container != null)
        {
            Destroy(container.gameObject);
        }
        
        // Clear elemental ascension section
        var elemSection = selectionPanel.transform.Find("ElementalAscensionSection");
        if (elemSection != null)
        {
            Destroy(elemSection.gameObject);
        }
    }
    
    private void CreateCharacterCards()
    {
        // ScrollView wrapper for future character expansion
        var scrollObj = new GameObject("CharacterScroll");
        scrollObj.transform.SetParent(selectionPanel.transform, false);
        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.05f, 0.12f);
        scrollRect.anchorMax = new Vector2(0.95f, 0.76f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;
        
        var scrollView = scrollObj.AddComponent<ScrollRect>();
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.movementType = ScrollRect.MovementType.Clamped;
        scrollView.scrollSensitivity = 30;
        
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
        
        // Content container with grid
        var container = new GameObject("CharacterContainer");
        container.transform.SetParent(viewport.transform, false);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 1);
        containerRect.anchorMax = new Vector2(1, 1);
        containerRect.pivot = new Vector2(0.5f, 1);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        var gridLayout = container.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(140, 140);
        gridLayout.spacing = new Vector2(12, 12);
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
        gridLayout.padding = new RectOffset(10, 10, 10, 10);
        
        var contentFitter = container.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollView.content = containerRect;
        
        // Create card for each character
        foreach (var character in DataCache.Characters)
        {
            CreateCharacterCard(container.transform, character);
        }
    }
    
    private void CreateCharacterCard(Transform parent, CharacterData character)
    {
        var progress = MetaProgressionManager.Instance.GetProgress(character.CharacterID);
        
        var cardObj = new GameObject($"Card_{character.DisplayName}");
        cardObj.transform.SetParent(parent, false);
        
        var cardImage = cardObj.AddComponent<Image>();
        cardImage.color = new Color(0.12f, 0.12f, 0.18f, 1f);
        
        var btn = cardObj.AddComponent<Button>();
        btn.targetGraphic = cardImage;
        
        CharacterData capturedCharacter = character;
        btn.onClick.AddListener(() => OnCharacterCardClicked(capturedCharacter));
        
        // Calculate talent status
        int tierCount = character.GetTierCount();
        int unlockedTiers = 0;
        int selectedTiers = 0;
        for (int i = 0; i < tierCount; i++)
        {
            if (progress.IsTierUnlocked(i))
            {
                unlockedTiers++;
                if (progress.GetTalentChoice(i) != 0)
                {
                    selectedTiers++;
                }
            }
        }
        
        // Character name - top section (70-95%)
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(cardObj.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.05f, 0.72f);
        nameRect.anchorMax = new Vector2(0.95f, 0.95f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = character.DisplayName.ToUpper();
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 16;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(1f, 0.85f, 0.2f);
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = 12;
        nameText.fontSizeMax = 16;
        
        // Ascension level - (55-72%)
        var levelObj = new GameObject("Level");
        levelObj.transform.SetParent(cardObj.transform, false);
        var levelRect = levelObj.AddComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.05f, 0.55f);
        levelRect.anchorMax = new Vector2(0.95f, 0.72f);
        levelRect.offsetMin = Vector2.zero;
        levelRect.offsetMax = Vector2.zero;
        var levelText = levelObj.AddComponent<TextMeshProUGUI>();
        levelText.text = $"Ascension {progress.AscensionLevel}";
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.fontSize = 12;
        levelText.color = new Color(0.6f, 0.8f, 1f);
        
        // Stats - middle section (25-55%)
        var statsObj = new GameObject("Stats");
        statsObj.transform.SetParent(cardObj.transform, false);
        var statsRect = statsObj.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.05f, 0.25f);
        statsRect.anchorMax = new Vector2(0.95f, 0.55f);
        statsRect.offsetMin = Vector2.zero;
        statsRect.offsetMax = Vector2.zero;
        var statsText = statsObj.AddComponent<TextMeshProUGUI>();
        statsText.text = $"HP: {character.MaxHealth}\nDMG: {character.DamageRangeLabel}";
        statsText.alignment = TextAlignmentOptions.Center;
        statsText.fontSize = 11;
        statsText.color = new Color(0.55f, 0.55f, 0.55f);
        
        // Talents - bottom section (5-25%)
        var talentObj = new GameObject("TalentStatus");
        talentObj.transform.SetParent(cardObj.transform, false);
        var talentRect = talentObj.AddComponent<RectTransform>();
        talentRect.anchorMin = new Vector2(0.05f, 0.05f);
        talentRect.anchorMax = new Vector2(0.95f, 0.25f);
        talentRect.offsetMin = Vector2.zero;
        talentRect.offsetMax = Vector2.zero;
        var talentText = talentObj.AddComponent<TextMeshProUGUI>();
        talentText.text = $"Talents: {selectedTiers}/{unlockedTiers}";
        talentText.alignment = TextAlignmentOptions.Center;
        talentText.fontSize = 11;
        talentText.color = new Color(0.45f, 0.65f, 0.45f);
        
        // Yellow "UP" badge if there are unselected unlocked tiers
        if (progress.HasUnselectedUnlockedTier(tierCount))
        {
            CreateUpBadge(cardObj.transform);
        }
    }
    
    private void CreateUpBadge(Transform parent)
    {
        var badgeObj = new GameObject("UpBadge");
        badgeObj.transform.SetParent(parent.parent.parent, false); // Attach to card but position at corner
        badgeObj.transform.SetParent(parent, false);
        
        var badgeRect = badgeObj.AddComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.7f, 0.8f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.offsetMin = Vector2.zero;
        badgeRect.offsetMax = Vector2.zero;
        
        var badgeImage = badgeObj.AddComponent<Image>();
        badgeImage.color = new Color(1f, 0.85f, 0f, 1f); // Yellow
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(badgeObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "UP";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 16;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.black;
    }
    
    private void OnCharacterCardClicked(CharacterData character)
    {
        Debug.Log($"[NewRunCharacterSelectUI] Selected {character.DisplayName}");
        Hide();
        onCharacterSelected?.Invoke(character);
    }
    
    private void OnBackButtonClicked()
    {
        Debug.Log("[NewRunCharacterSelectUI] Back clicked");
        Hide();
        onBackClicked?.Invoke();
    }
    
    // ===== ELEMENTAL ASCENSION SECTION =====
    
    private void CreateElementalAscensionSection()
    {
        var section = new GameObject("ElementalAscensionSection");
        section.transform.SetParent(selectionPanel.transform, false);
        
        var sectionRect = section.AddComponent<RectTransform>();
        sectionRect.anchorMin = new Vector2(0.15f, 0.02f);
        sectionRect.anchorMax = new Vector2(0.85f, 0.10f);
        sectionRect.offsetMin = Vector2.zero;
        sectionRect.offsetMax = Vector2.zero;
        
        // Section background
        var sectionBg = section.AddComponent<Image>();
        sectionBg.color = new Color(0.08f, 0.08f, 0.12f, 0.8f);
        
        // Section title
        var titleObj = new GameObject("SectionTitle");
        titleObj.transform.SetParent(section.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "ELEMENTAL ASCENSION";
        titleText.fontSize = 14;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.8f, 0.7f, 0.4f);
        
        // Element cards container
        var cardsContainer = new GameObject("ElementCards");
        cardsContainer.transform.SetParent(section.transform, false);
        var cardsRect = cardsContainer.AddComponent<RectTransform>();
        cardsRect.anchorMin = new Vector2(0.02f, 0.05f);
        cardsRect.anchorMax = new Vector2(0.98f, 0.65f);
        cardsRect.offsetMin = Vector2.zero;
        cardsRect.offsetMax = Vector2.zero;
        
        var hLayout = cardsContainer.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 10;
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = true;
        
        // Create element cards from JSON data
        var elements = DataCache.GetAllElements();
        foreach (var element in elements)
        {
            CreateElementCard(cardsContainer.transform, element);
        }
    }
    
    private void CreateElementCard(Transform parent, string elementName)
    {
        var progress = MetaProgressionManager.Instance.GetElementProgress(elementName);
        int maxLevel = DataCache.GetElementMaxLevel(elementName);
        
        var cardObj = new GameObject($"Element_{elementName}");
        cardObj.transform.SetParent(parent, false);
        
        var cardBg = cardObj.AddComponent<Image>();
        cardBg.color = GetElementBgColor(elementName);
        
        var btn = cardObj.AddComponent<Button>();
        btn.targetGraphic = cardBg;
        
        string capturedElement = elementName;
        btn.onClick.AddListener(() => OnElementCardClicked(capturedElement));
        
        // Element name and level text
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(cardObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(3, 3);
        textRect.offsetMax = new Vector2(-3, -3);
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        string levelStr = progress.AscensionLevel >= maxLevel ? "MAX" : $"{progress.AscensionLevel}";
        text.text = $"<b>{elementName}</b>\nTier {levelStr}";
        text.fontSize = 11;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
    }
    
    private void OnElementCardClicked(string elementName)
    {
        Debug.Log($"[NewRunCharacterSelectUI] Element {elementName} clicked");
        
        // Find or create detail UI
        var detailUI = FindFirstObjectByType<ElementAscensionDetailUI>();
        if (detailUI == null)
        {
            var go = new GameObject("ElementAscensionDetailUI");
            detailUI = go.AddComponent<ElementAscensionDetailUI>();
        }
        
        selectionPanel.SetActive(false);
        detailUI.Show(elementName, () => {
            selectionPanel.SetActive(true);
            // Refresh element cards to show updated XP/level
            ClearDynamicContent();
            CreateCharacterCards();
            CreateElementalAscensionSection();
        });
    }
    
    private Color GetElementBgColor(string element)
    {
        return element.ToLower() switch
        {
            "fire" => new Color(0.4f, 0.15f, 0.1f, 0.9f),
            "ice" => new Color(0.15f, 0.25f, 0.4f, 0.9f),
            "water" => new Color(0.1f, 0.15f, 0.4f, 0.9f),
            "wind" => new Color(0.15f, 0.35f, 0.15f, 0.9f),
            "rock" => new Color(0.35f, 0.25f, 0.1f, 0.9f),
            _ => new Color(0.2f, 0.2f, 0.2f, 0.9f)
        };
    }
    
    public bool IsActive() => isActive;
}

