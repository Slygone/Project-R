using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Character Loadout UI showing full stats, skills, and talent tier selection.
/// Responsive design for 1080p/1440p/4K resolutions.
/// </summary>
public class CharacterLoadoutUI : MonoBehaviour
{
    private GameObject loadoutPanel;
    private CharacterData currentCharacter;
    private CharacterProgressData currentProgress;
    private Action onStartRun;
    private Action onBack;
    private bool isActive = false;
    
    // References to tier rows for updating selections
    private GameObject[] tierRows;
    private Button[][] tierButtons; // [tier][0=A, 1=B]
    
    void Awake()
    {
        SetupUI();
    }
    
    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[CharacterLoadoutUI] Canvas not found");
            return;
        }
        
        loadoutPanel = CreateLoadoutPanel(canvas.transform);
        loadoutPanel.SetActive(false);
    }
    
    private GameObject CreateLoadoutPanel(Transform parent)
    {
        var panel = new GameObject("CharacterLoadoutPanel");
        panel.transform.SetParent(parent, false);
        
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.09f, 0.98f);
        
        return panel;
    }
    
    public void Show(CharacterData character, Action startRunCallback, Action backCallback)
    {
        currentCharacter = character;
        currentProgress = MetaProgressionManager.Instance.GetProgress(character.CharacterID);
        onStartRun = startRunCallback;
        onBack = backCallback;
        
        ClearContent();
        CreateContent();
        
        loadoutPanel.SetActive(true);
        isActive = true;
    }
    
    public void Hide()
    {
        loadoutPanel.SetActive(false);
        isActive = false;
    }
    
    private void ClearContent()
    {
        // Clear all children
        for (int i = loadoutPanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(loadoutPanel.transform.GetChild(i).gameObject);
        }
    }
    
    private void CreateContent()
    {
        // Fixed position approach - more reliable than layout groups for complex UIs
        
        // ===== Header Area (top 8%) =====
        CreateHeader();
        
        // ===== Core Stats Row (next 8%) =====
        CreateCoreStatsRow();
        
        // ===== Skills Panel (left side, middle area) =====
        CreateSkillsPanel();
        
        // ===== Talents Panel (right side, middle area) =====
        CreateTalentsPanel();
        
        // ===== Start Run Button (bottom 10%) =====
        CreateStartRunButton();
    }
    
    private void CreateHeader()
    {
        // Back button
        var backBtn = new GameObject("BackButton");
        backBtn.transform.SetParent(loadoutPanel.transform, false);
        var backRect = backBtn.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.02f, 0.92f);
        backRect.anchorMax = new Vector2(0.15f, 0.98f);
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;
        
        var backImg = backBtn.AddComponent<Image>();
        backImg.color = new Color(0.3f, 0.3f, 0.35f);
        
        var backButton = backBtn.AddComponent<Button>();
        backButton.targetGraphic = backImg;
        backButton.onClick.AddListener(OnBackClicked);
        
        var backTextObj = new GameObject("Text");
        backTextObj.transform.SetParent(backBtn.transform, false);
        var backTextRect = backTextObj.AddComponent<RectTransform>();
        backTextRect.anchorMin = Vector2.zero;
        backTextRect.anchorMax = Vector2.one;
        backTextRect.offsetMin = Vector2.zero;
        backTextRect.offsetMax = Vector2.zero;
        var backText = backTextObj.AddComponent<TextMeshProUGUI>();
        backText.text = "< BACK";
        backText.alignment = TextAlignmentOptions.Center;
        backText.fontSize = 16;
        backText.color = Color.white;
        
        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(loadoutPanel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.2f, 0.92f);
        titleRect.anchorMax = new Vector2(0.8f, 0.98f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = $"{currentCharacter.DisplayName.ToUpper()} - LOADOUT";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.2f);
        
        // Ascension level subtitle
        var levelObj = new GameObject("Level");
        levelObj.transform.SetParent(loadoutPanel.transform, false);
        var levelRect = levelObj.AddComponent<RectTransform>();
        levelRect.anchorMin = new Vector2(0.2f, 0.87f);
        levelRect.anchorMax = new Vector2(0.8f, 0.92f);
        levelRect.offsetMin = Vector2.zero;
        levelRect.offsetMax = Vector2.zero;
        var levelText = levelObj.AddComponent<TextMeshProUGUI>();
        levelText.text = $"Ascension Level {currentProgress.AscensionLevel}";
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.fontSize = 16;
        levelText.color = new Color(0.7f, 0.85f, 1f);
    }
    
    private void CreateCoreStatsRow()
    {
        var statsRow = new GameObject("CoreStatsRow");
        statsRow.transform.SetParent(loadoutPanel.transform, false);
        var statsRect = statsRow.AddComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0.05f, 0.78f);
        statsRect.anchorMax = new Vector2(0.95f, 0.86f);
        statsRect.offsetMin = Vector2.zero;
        statsRect.offsetMax = Vector2.zero;
        
        var statsBg = statsRow.AddComponent<Image>();
        statsBg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        
        var hLayout = statsRow.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing = 15;
        hLayout.padding = new RectOffset(20, 20, 5, 5);
        hLayout.childAlignment = TextAnchor.MiddleCenter;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = true;
        hLayout.childForceExpandHeight = true;
        
        // Stats displayed horizontally
        AddCompactStat(statsRow.transform, "HP", currentCharacter.MaxHealth.ToString(), new Color(0.4f, 0.9f, 0.4f));
        AddCompactStat(statsRow.transform, "DMG", currentCharacter.DamageRangeLabel, new Color(1f, 0.4f, 0.4f));
        AddCompactStat(statsRow.transform, "Energy", currentCharacter.MaxEnergy.ToString(), new Color(0.4f, 0.7f, 1f));
        AddCompactStat(statsRow.transform, "Crit", $"{currentCharacter.CritChance}%", new Color(1f, 0.7f, 0.3f));
        AddCompactStat(statsRow.transform, "CritDMG", $"{currentCharacter.CritDamage}x", new Color(1f, 0.7f, 0.3f));
        AddCompactStat(statsRow.transform, "Resist", $"{currentCharacter.BaseResistance}%", new Color(0.6f, 0.6f, 0.8f));
    }
    
    private void AddCompactStat(Transform parent, string label, string value, Color valueColor)
    {
        var statObj = new GameObject($"Stat_{label}");
        statObj.transform.SetParent(parent, false);
        
        var vLayout = statObj.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 0;
        vLayout.childAlignment = TextAnchor.MiddleCenter;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(statObj.transform, false);
        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 11;
        labelText.color = new Color(0.6f, 0.6f, 0.6f);
        
        var valueObj = new GameObject("Value");
        valueObj.transform.SetParent(statObj.transform, false);
        var valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = value;
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.fontSize = 16;
        valueText.fontStyle = FontStyles.Bold;
        valueText.color = valueColor;
    }
    
    private void CreateSkillsPanel()
    {
        // Left panel for skills
        var skillsPanel = new GameObject("SkillsPanel");
        skillsPanel.transform.SetParent(loadoutPanel.transform, false);
        var skillsRect = skillsPanel.AddComponent<RectTransform>();
        skillsRect.anchorMin = new Vector2(0.03f, 0.15f);
        skillsRect.anchorMax = new Vector2(0.48f, 0.76f);
        skillsRect.offsetMin = Vector2.zero;
        skillsRect.offsetMax = Vector2.zero;
        
        var skillsBg = skillsPanel.AddComponent<Image>();
        skillsBg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        
        // Header
        var headerObj = new GameObject("Header");
        headerObj.transform.SetParent(skillsPanel.transform, false);
        var headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0.9f);
        headerRect.anchorMax = new Vector2(1, 1f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;
        var headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "SKILLS";
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.fontSize = 18;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = new Color(1f, 0.85f, 0.2f);
        
        // Skills content area
        var contentArea = new GameObject("SkillsContent");
        contentArea.transform.SetParent(skillsPanel.transform, false);
        var contentRect = contentArea.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.02f, 0.02f);
        contentRect.anchorMax = new Vector2(0.98f, 0.88f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        
        var vLayout = contentArea.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 8;
        vLayout.padding = new RectOffset(5, 5, 5, 5);
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        
        // Add skills (1-4 gain energy, 5 is ultimate that costs energy)
        AddSkillRow(contentArea.transform, currentCharacter.Skill1, 
            $"{currentCharacter.Skill1DamagePercent}% | CD:{currentCharacter.Skill1Cooldown} | +{currentCharacter.Skill1EnergyGain}E",
            currentCharacter.Skill1Effect);
        
        AddSkillRow(contentArea.transform, currentCharacter.Skill2,
            $"{currentCharacter.Skill2DamagePercent}% | CD:{currentCharacter.Skill2Cooldown} | +{currentCharacter.Skill2EnergyGain}E",
            currentCharacter.Skill2Effect);
        
        AddSkillRow(contentArea.transform, currentCharacter.Skill3,
            $"{currentCharacter.Skill3DamagePercent}% | CD:{currentCharacter.Skill3Cooldown} | +{currentCharacter.Skill3EnergyGain}E",
            currentCharacter.Skill3Effect);
        
        AddSkillRow(contentArea.transform, currentCharacter.Skill4,
            $"{currentCharacter.Skill4DamagePercent}% | CD:{currentCharacter.Skill4Cooldown} | +{currentCharacter.Skill4EnergyGain}E",
            currentCharacter.Skill4Effect);
        
        AddSkillRow(contentArea.transform, currentCharacter.Skill5 + " (ULT)",
            $"{currentCharacter.Skill5DamagePercent}% | CD:{currentCharacter.Skill5Cooldown} | -{currentCharacter.Skill5EnergyCost}E",
            currentCharacter.Skill5Effect);
    }
    
    private void AddSkillRow(Transform parent, string skillName, string stats, string effect)
    {
        bool hasEffect = effect != null && effect.ToLower() != "null" && !string.IsNullOrEmpty(effect);
        
        var rowObj = new GameObject($"Skill_{skillName}");
        rowObj.transform.SetParent(parent, false);
        
        var rowBg = rowObj.AddComponent<Image>();
        rowBg.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
        
        var rowLayout = rowObj.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = hasEffect ? 55 : 35;
        
        // Skill name - positioned at top
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(rowObj.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.55f);
        nameRect.anchorMax = new Vector2(1, 1f);
        nameRect.offsetMin = new Vector2(10, 0);
        nameRect.offsetMax = new Vector2(-10, -5);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = $"{skillName}  <size=10><color=#888899>{stats}</color></size>";
        nameText.fontSize = 14;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(0.9f, 0.9f, 1f);
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.verticalAlignment = VerticalAlignmentOptions.Middle;
        
        // Effect description in green - positioned at bottom
        if (hasEffect)
        {
            var effectObj = new GameObject("Effect");
            effectObj.transform.SetParent(rowObj.transform, false);
            var effectRect = effectObj.AddComponent<RectTransform>();
            effectRect.anchorMin = new Vector2(0, 0);
            effectRect.anchorMax = new Vector2(1, 0.5f);
            effectRect.offsetMin = new Vector2(10, 5);
            effectRect.offsetMax = new Vector2(-10, 0);
            var effectText = effectObj.AddComponent<TextMeshProUGUI>();
            effectText.text = effect;
            effectText.fontSize = 11;
            effectText.color = new Color(0.4f, 0.85f, 0.5f); // Nice green
            effectText.alignment = TextAlignmentOptions.Left;
            effectText.verticalAlignment = VerticalAlignmentOptions.Top;
            effectText.textWrappingMode = TextWrappingModes.Normal;
            effectText.overflowMode = TextOverflowModes.Ellipsis;
        }
    }
    
    private void CreateTalentsPanel()
    {
        // Right panel for talents
        var talentsPanel = new GameObject("TalentsPanel");
        talentsPanel.transform.SetParent(loadoutPanel.transform, false);
        var talentsRect = talentsPanel.AddComponent<RectTransform>();
        talentsRect.anchorMin = new Vector2(0.52f, 0.15f);
        talentsRect.anchorMax = new Vector2(0.97f, 0.76f);
        talentsRect.offsetMin = Vector2.zero;
        talentsRect.offsetMax = Vector2.zero;
        
        var talentsBg = talentsPanel.AddComponent<Image>();
        talentsBg.color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        
        // Header
        var headerObj = new GameObject("Header");
        headerObj.transform.SetParent(talentsPanel.transform, false);
        var headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0.9f);
        headerRect.anchorMax = new Vector2(1, 1f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;
        var headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.text = "TALENT TIERS";
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.fontSize = 18;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = new Color(1f, 0.85f, 0.2f);
        
        // Talents content area
        var contentArea = new GameObject("TalentsContent");
        contentArea.transform.SetParent(talentsPanel.transform, false);
        var contentRect = contentArea.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.02f, 0.02f);
        contentRect.anchorMax = new Vector2(0.98f, 0.88f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        
        var vLayout = contentArea.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 8;
        vLayout.padding = new RectOffset(5, 5, 5, 5);
        vLayout.childAlignment = TextAnchor.UpperCenter;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = false;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        
        // Create tier rows
        int tierCount = currentCharacter.GetTierCount();
        tierRows = new GameObject[tierCount];
        tierButtons = new Button[tierCount][];
        
        for (int i = 0; i < tierCount; i++)
        {
            CreateTierRow(contentArea.transform, i);
        }
    }
    
    private void CreateTierRow(Transform parent, int tierIndex)
    {
        bool isUnlocked = currentProgress.IsTierUnlocked(tierIndex);
        int currentChoice = currentProgress.GetTalentChoice(tierIndex);
        int requiredLevel = tierIndex + 2;
        
        string perkA = currentCharacter.GetPerkOption(tierIndex, true);
        string perkB = currentCharacter.GetPerkOption(tierIndex, false);
        
        var rowObj = new GameObject($"Tier_{tierIndex}");
        rowObj.transform.SetParent(parent, false);
        tierRows[tierIndex] = rowObj;
        
        var rowLayout = rowObj.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 50;
        
        var rowBg = rowObj.AddComponent<Image>();
        rowBg.color = isUnlocked ? new Color(0.12f, 0.12f, 0.18f, 1f) : new Color(0.06f, 0.06f, 0.08f, 0.8f);
        
        var layout = rowObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 5, 5);
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        
        // Tier label
        var tierLabelObj = new GameObject("TierLabel");
        tierLabelObj.transform.SetParent(rowObj.transform, false);
        var tierLabelLayout = tierLabelObj.AddComponent<LayoutElement>();
        tierLabelLayout.preferredWidth = 35;
        tierLabelLayout.flexibleWidth = 0;
        var tierLabelText = tierLabelObj.AddComponent<TextMeshProUGUI>();
        tierLabelText.text = $"T{tierIndex + 1}";
        tierLabelText.alignment = TextAlignmentOptions.Center;
        tierLabelText.fontSize = 16;
        tierLabelText.fontStyle = FontStyles.Bold;
        tierLabelText.color = isUnlocked ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.4f, 0.4f, 0.4f);
        
        tierButtons[tierIndex] = new Button[2];
        
        if (isUnlocked)
        {
            // Option A button
            tierButtons[tierIndex][0] = CreateTalentButton(rowObj.transform, tierIndex, true, perkA, currentChoice == 1);
            
            // OR label
            var orObj = new GameObject("Or");
            orObj.transform.SetParent(rowObj.transform, false);
            var orLayout = orObj.AddComponent<LayoutElement>();
            orLayout.preferredWidth = 20;
            orLayout.flexibleWidth = 0;
            var orText = orObj.AddComponent<TextMeshProUGUI>();
            orText.text = "OR";
            orText.alignment = TextAlignmentOptions.Center;
            orText.fontSize = 10;
            orText.color = new Color(0.5f, 0.5f, 0.5f);
            
            // Option B button
            tierButtons[tierIndex][1] = CreateTalentButton(rowObj.transform, tierIndex, false, perkB, currentChoice == 2);
        }
        else
        {
            // Locked message
            var lockedObj = new GameObject("Locked");
            lockedObj.transform.SetParent(rowObj.transform, false);
            var lockedText = lockedObj.AddComponent<TextMeshProUGUI>();
            lockedText.text = $"Unlock at Ascension {requiredLevel}";
            lockedText.alignment = TextAlignmentOptions.Center;
            lockedText.fontSize = 13;
            lockedText.color = new Color(0.45f, 0.45f, 0.45f);
        }
    }
    
    private Button CreateTalentButton(Transform parent, int tierIndex, bool isOptionA, string perkId, bool isSelected)
    {
        var btnObj = new GameObject($"Option_{(isOptionA ? "A" : "B")}");
        btnObj.transform.SetParent(parent, false);
        
        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = isSelected 
            ? new Color(0.2f, 0.5f, 0.3f, 1f)  // Selected green
            : new Color(0.2f, 0.2f, 0.25f, 1f); // Unselected gray
        
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        
        int capturedTier = tierIndex;
        bool capturedIsA = isOptionA;
        string capturedPerkId = perkId;
        btn.onClick.AddListener(() => OnTalentSelected(capturedTier, capturedIsA, capturedPerkId));
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(3, 3);
        textRect.offsetMax = new Vector2(-3, -3);
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"<b>{(isOptionA ? "A" : "B")}</b> - <size=10>{FormatPerkIdCompact(perkId)}</size>";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 12;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        
        return btn;
    }
    
    private string FormatPerkIdCompact(string perkId)
    {
        if (string.IsNullOrEmpty(perkId)) return "???";
        var parts = perkId.Split('_');
        if (parts.Length >= 2)
        {
            return parts[0];
        }
        return perkId;
    }
    
    private void OnTalentSelected(int tierIndex, bool isOptionA, string perkId)
    {
        int choice = isOptionA ? 1 : 2;
        
        MetaProgressionManager.Instance.SetTalentChoice(
            currentCharacter.CharacterID,
            currentCharacter.DisplayName,
            tierIndex,
            choice,
            perkId
        );
        
        currentProgress = MetaProgressionManager.Instance.GetProgress(currentCharacter.CharacterID);
        UpdateTierButtonVisuals(tierIndex);
    }
    
    private void UpdateTierButtonVisuals(int tierIndex)
    {
        if (tierButtons == null || tierIndex >= tierButtons.Length) return;
        
        int currentChoice = currentProgress.GetTalentChoice(tierIndex);
        
        if (tierButtons[tierIndex][0] != null)
        {
            var imgA = tierButtons[tierIndex][0].GetComponent<Image>();
            imgA.color = (currentChoice == 1) 
                ? new Color(0.2f, 0.5f, 0.3f, 1f)
                : new Color(0.2f, 0.2f, 0.25f, 1f);
        }
        
        if (tierButtons[tierIndex][1] != null)
        {
            var imgB = tierButtons[tierIndex][1].GetComponent<Image>();
            imgB.color = (currentChoice == 2)
                ? new Color(0.2f, 0.5f, 0.3f, 1f)
                : new Color(0.2f, 0.2f, 0.25f, 1f);
        }
    }
    
    private void CreateStartRunButton()
    {
        var btnObj = new GameObject("StartRunButton");
        btnObj.transform.SetParent(loadoutPanel.transform, false);
        
        var rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.35f, 0.03f);
        rect.anchorMax = new Vector2(0.65f, 0.12f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.55f, 0.3f, 1f);
        
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OnStartRunClicked);
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "START RUN";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
    }
    
    private void OnBackClicked()
    {
        Debug.Log("[CharacterLoadoutUI] Back clicked");
        Hide();
        onBack?.Invoke();
    }
    
    private void OnStartRunClicked()
    {
        Debug.Log($"[CharacterLoadoutUI] Starting run with {currentCharacter.DisplayName}");
        Hide();
        onStartRun?.Invoke();
    }
    
    public CharacterData GetSelectedCharacter() => currentCharacter;
    
    public bool IsActive() => isActive;
}
