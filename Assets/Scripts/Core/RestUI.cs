using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RestUI : MonoBehaviour
{
    // Heal amount
    private const float HEAL_PERCENT = 0.40f;
    
    // XP costs for upgrades
    private const int XP_COST_MAX_HP = 2;
    private const int XP_COST_CRIT = 3;
    private const int XP_COST_DAMAGE = 3;
    
    // Upgrade amounts
    private const int MAX_HP_UPGRADE_AMOUNT = 8;
    private const int CRIT_UPGRADE_AMOUNT = 4;
    private const int DAMAGE_UPGRADE_AMOUNT = 2;

    private GameObject restPanel;
    private GameObject mainChoiceContainer;
    private GameObject ascendContainer;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI xpDisplayText;
    private List<GameObject> upgradeButtons = new List<GameObject>();
    private Action onRestClosed;
    private bool isActive = false;
    private Player player;
    private NodeBase currentNode;

    void Awake()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[RestUI] Canvas not found");
            return;
        }

        restPanel = CreateRestPanel(canvas.transform);
        restPanel.SetActive(false);
    }

    private GameObject CreateRestPanel(Transform parent)
    {
        var panel = new GameObject("RestPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.97f);

        CreateTitle(panel.transform);
        CreateMainChoices(panel.transform);
        CreateAscendScreen(panel.transform);

        return panel;
    }

    private void CreateTitle(Transform parent)
    {
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.85f);
        titleRect.anchorMax = new Vector2(1, 0.98f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "REST SITE";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.9f, 0.8f, 0.5f);
    }

    private void CreateMainChoices(Transform parent)
    {
        mainChoiceContainer = new GameObject("MainChoices");
        mainChoiceContainer.transform.SetParent(parent, false);
        var containerRect = mainChoiceContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.25f);
        containerRect.anchorMax = new Vector2(0.9f, 0.80f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // Heal Button (left side)
        CreateMainChoiceButton(mainChoiceContainer.transform, 0, "HEAL", 
            "Restore 40% Max HP\n& Remove Debuffs", 
            new Color(0.3f, 0.6f, 0.3f), OnHealClicked);

        // Ascend Button (right side)
        CreateMainChoiceButton(mainChoiceContainer.transform, 1, "ASCEND", 
            "Spend XP to\nUpgrade Stats", 
            new Color(0.5f, 0.3f, 0.6f), OnAscendClicked);
    }

    private void CreateMainChoiceButton(Transform parent, int index, string title, string description, Color bgColor, Action onClick)
    {
        float buttonWidth = 0.42f;
        float gap = 0.08f;
        float startX = (1f - (2 * buttonWidth + gap)) / 2f;
        float xMin = startX + index * (buttonWidth + gap);
        float xMax = xMin + buttonWidth;

        var btnObj = new GameObject($"Choice_{title}");
        btnObj.transform.SetParent(parent, false);

        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(xMin, 0.1f);
        btnRect.anchorMax = new Vector2(xMax, 0.9f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = bgColor;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(() => onClick());

        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(btnObj.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.55f);
        titleRect.anchorMax = new Vector2(0.95f, 0.90f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 36;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;

        // Description
        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.10f);
        descRect.anchorMax = new Vector2(0.95f, 0.55f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = description;
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 20;
        descText.color = new Color(0.9f, 0.9f, 0.9f);
    }

    private void CreateAscendScreen(Transform parent)
    {
        ascendContainer = new GameObject("AscendScreen");
        ascendContainer.transform.SetParent(parent, false);
        var containerRect = ascendContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        ascendContainer.SetActive(false);

        // XP Display
        var xpObj = new GameObject("XPDisplay");
        xpObj.transform.SetParent(ascendContainer.transform, false);
        var xpRect = xpObj.AddComponent<RectTransform>();
        xpRect.anchorMin = new Vector2(0.3f, 0.78f);
        xpRect.anchorMax = new Vector2(0.7f, 0.85f);
        xpRect.offsetMin = Vector2.zero;
        xpRect.offsetMax = Vector2.zero;
        xpDisplayText = xpObj.AddComponent<TextMeshProUGUI>();
        xpDisplayText.text = "Available XP: 0";
        xpDisplayText.alignment = TextAlignmentOptions.Center;
        xpDisplayText.fontSize = 28;
        xpDisplayText.fontStyle = FontStyles.Bold;
        xpDisplayText.color = new Color(0.3f, 0.9f, 1f);

        // Upgrade container
        var upgradesObj = new GameObject("Upgrades");
        upgradesObj.transform.SetParent(ascendContainer.transform, false);
        var upgradesRect = upgradesObj.AddComponent<RectTransform>();
        upgradesRect.anchorMin = new Vector2(0.1f, 0.25f);
        upgradesRect.anchorMax = new Vector2(0.9f, 0.75f);
        upgradesRect.offsetMin = Vector2.zero;
        upgradesRect.offsetMax = Vector2.zero;

        // Back Button
        var backObj = new GameObject("BackButton");
        backObj.transform.SetParent(ascendContainer.transform, false);
        var backRect = backObj.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.35f, 0.08f);
        backRect.anchorMax = new Vector2(0.65f, 0.18f);
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;

        var backImage = backObj.AddComponent<Image>();
        backImage.color = new Color(0.5f, 0.3f, 0.2f);

        var backBtn = backObj.AddComponent<Button>();
        backBtn.targetGraphic = backImage;
        backBtn.onClick.AddListener(OnBackFromAscendClicked);

        var backTextObj = new GameObject("Text");
        backTextObj.transform.SetParent(backObj.transform, false);
        var backTextRect = backTextObj.AddComponent<RectTransform>();
        backTextRect.anchorMin = Vector2.zero;
        backTextRect.anchorMax = Vector2.one;
        backTextRect.offsetMin = Vector2.zero;
        backTextRect.offsetMax = Vector2.zero;
        var backText = backTextObj.AddComponent<TextMeshProUGUI>();
        backText.text = "Back";
        backText.alignment = TextAlignmentOptions.Center;
        backText.fontSize = 24;
        backText.color = Color.white;
    }

    public void Show(Player playerRef, NodeBase node, Action onClosed)
    {
        player = playerRef;
        currentNode = node;
        onRestClosed = onClosed;

        // Show main choices first
        ShowMainChoices();

        restPanel.SetActive(true);
        isActive = true;
    }

    private void ShowMainChoices()
    {
        titleText.text = "REST SITE";
        mainChoiceContainer.SetActive(true);
        ascendContainer.SetActive(false);
    }

    private void ShowAscendScreen()
    {
        titleText.text = "ASCENSION";
        mainChoiceContainer.SetActive(false);
        ascendContainer.SetActive(true);
        RefreshXPDisplay();
        PopulateUpgrades();
    }

    private void RefreshXPDisplay()
    {
        if (xpDisplayText != null)
        {
            xpDisplayText.text = $"Available XP: {GameManager.RunXP}";
        }
    }

    private void PopulateUpgrades()
    {
        // Clear existing buttons
        foreach (var btn in upgradeButtons)
        {
            if (btn != null) Destroy(btn);
        }
        upgradeButtons.Clear();

        var upgradesContainer = ascendContainer.transform.Find("Upgrades");
        if (upgradesContainer == null) return;

        // Get current values
        int currentMaxHP = player != null ? player.GetMaxHealth() : 100;
        int currentCrit = player != null ? player.GetCritChance() : 5;
        int currentDmgMin = player != null ? player.GetDamageMin() : 10;
        int currentDmgMax = player != null ? player.GetDamageMax() : 12;
        float currentCritDmg = player != null ? player.GetCritDamage() : 1.7f;

        // Create 4 upgrade buttons in a single row
        CreateUpgradeButton(upgradesContainer, 0, 4, "Max HP", $"{currentMaxHP}", $"+{MAX_HP_UPGRADE_AMOUNT}", 
            XP_COST_MAX_HP, OnMaxHPUpgradeClicked, new Color(0.4f, 0.8f, 0.4f));
        
        CreateUpgradeButton(upgradesContainer, 1, 4, "Crit Rating", $"{currentCrit}%", $"+{CRIT_UPGRADE_AMOUNT}%", 
            XP_COST_CRIT, OnCritUpgradeClicked, new Color(1f, 0.8f, 0.3f));

        CreateUpgradeButton(upgradesContainer, 2, 4, "Crit Damage", $"{currentCritDmg:F1}x", $"+0.1x", 
            XP_COST_CRIT, OnCritDamageUpgradeClicked, new Color(1f, 0.5f, 0.6f));
        
        CreateUpgradeButton(upgradesContainer, 3, 4, "Damage", $"{currentDmgMin}-{currentDmgMax}", $"+{DAMAGE_UPGRADE_AMOUNT}", 
            XP_COST_DAMAGE, OnDamageUpgradeClicked, new Color(1f, 0.5f, 0.4f));
    }

    private void CreateUpgradeButton(Transform parent, int index, int totalColumns, string statName, string currentValue, 
        string upgradeValue, int xpCost, Action onClick, Color accentColor)
    {
        // Layout math for N columns across the row
        float gap = 0.02f;
        float buttonWidth = (1f - ((totalColumns - 1) * gap)) / totalColumns;
        float xMin = index * (buttonWidth + gap);
        float xMax = xMin + buttonWidth;

        bool canAfford = GameManager.RunXP >= xpCost;

        var btnObj = new GameObject($"Upgrade_{statName}");
        btnObj.transform.SetParent(parent, false);
        upgradeButtons.Add(btnObj);

        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(xMin, 0.05f);
        btnRect.anchorMax = new Vector2(xMax, 0.95f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = canAfford ? new Color(0.15f, 0.18f, 0.22f, 0.95f) : new Color(0.08f, 0.08f, 0.1f, 0.7f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.interactable = canAfford;
        btn.onClick.AddListener(() => onClick());

        // Stat Name
        var nameObj = new GameObject("StatName");
        nameObj.transform.SetParent(btnObj.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.05f, 0.72f);
        nameRect.anchorMax = new Vector2(0.95f, 0.95f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = statName;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 22;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = canAfford ? accentColor : new Color(0.35f, 0.35f, 0.35f);

        // Current Value
        var currentObj = new GameObject("CurrentValue");
        currentObj.transform.SetParent(btnObj.transform, false);
        var currentRect = currentObj.AddComponent<RectTransform>();
        currentRect.anchorMin = new Vector2(0.05f, 0.50f);
        currentRect.anchorMax = new Vector2(0.95f, 0.72f);
        currentRect.offsetMin = Vector2.zero;
        currentRect.offsetMax = Vector2.zero;
        var currentText = currentObj.AddComponent<TextMeshProUGUI>();
        currentText.text = $"(<color=#BBBBBB>{currentValue}</color>)";
        currentText.alignment = TextAlignmentOptions.Center;
        currentText.fontSize = 18;
        currentText.color = canAfford ? new Color(0.8f, 0.85f, 0.9f) : new Color(0.5f, 0.5f, 0.5f);

        // Upgrade Amount
        var upgradeObj = new GameObject("UpgradeValue");
        upgradeObj.transform.SetParent(btnObj.transform, false);
        var upgradeRect = upgradeObj.AddComponent<RectTransform>();
        upgradeRect.anchorMin = new Vector2(0.05f, 0.28f);
        upgradeRect.anchorMax = new Vector2(0.95f, 0.50f);
        upgradeRect.offsetMin = Vector2.zero;
        upgradeRect.offsetMax = Vector2.zero;
        var upgradeText = upgradeObj.AddComponent<TextMeshProUGUI>();
        upgradeText.text = upgradeValue;
        upgradeText.alignment = TextAlignmentOptions.Center;
        upgradeText.fontSize = 26;
        upgradeText.fontStyle = FontStyles.Bold;
        upgradeText.color = canAfford ? new Color(0.35f, 1f, 0.55f) : new Color(0.25f, 0.45f, 0.3f);

        // Cost
        var costObj = new GameObject("Cost");
        costObj.transform.SetParent(btnObj.transform, false);
        var costRect = costObj.AddComponent<RectTransform>();
        costRect.anchorMin = new Vector2(0.05f, 0.05f);
        costRect.anchorMax = new Vector2(0.95f, 0.28f);
        costRect.offsetMin = Vector2.zero;
        costRect.offsetMax = Vector2.zero;
        var costText = costObj.AddComponent<TextMeshProUGUI>();
        costText.text = $"<color=#77D0FF>Cost:</color> {xpCost} XP";
        costText.alignment = TextAlignmentOptions.Center;
        costText.fontSize = 18;
        costText.fontStyle = FontStyles.Bold;
        costText.color = canAfford ? new Color(0.5f, 0.9f, 1f) : new Color(0.75f, 0.35f, 0.35f);
    }

    private void OnHealClicked()
    {
        if (player == null) return;

        int healAmount = Mathf.RoundToInt(player.GetMaxHealth() * HEAL_PERCENT);
        player.Heal(healAmount);
        
        // Remove debuffs/counters
        player.ClearDebuffs();
        
        Debug.Log($"[RestUI] Healed {healAmount} HP (40% of {player.GetMaxHealth()}) and cleared debuffs");
        
        // Auto-close after heal
        CloseRestSite();
    }

    private void OnAscendClicked()
    {
        ShowAscendScreen();
    }

    private void OnMaxHPUpgradeClicked()
    {
        if (player == null) return;
        if (!SpendXP(XP_COST_MAX_HP)) return;
        
        player.IncreaseMaxHealth(MAX_HP_UPGRADE_AMOUNT);
        Debug.Log($"[RestUI] Upgraded Max HP +{MAX_HP_UPGRADE_AMOUNT} for {XP_COST_MAX_HP} XP");
        RefreshAscendUI();
    }

    private void OnCritUpgradeClicked()
    {
        if (player == null) return;
        if (!SpendXP(XP_COST_CRIT)) return;
        
        player.AddCritChance(CRIT_UPGRADE_AMOUNT);
        Debug.Log($"[RestUI] Upgraded Crit Rating +{CRIT_UPGRADE_AMOUNT}% for {XP_COST_CRIT} XP");
        RefreshAscendUI();
    }

    private void OnCritDamageUpgradeClicked()
    {
        if (player == null) return;
        if (!SpendXP(XP_COST_CRIT)) return;

        player.AddCritDamage(0.1f);
        Debug.Log($"[RestUI] Upgraded Crit Damage +0.1x for {XP_COST_CRIT} XP (now {player.GetCritDamage():F1}x)");
        RefreshAscendUI();
    }

    private void OnDamageUpgradeClicked()
    {
        if (player == null) return;
        if (!SpendXP(XP_COST_DAMAGE)) return;
        
        // Increase both min and max damage
        player.IncreaseDamageRange(DAMAGE_UPGRADE_AMOUNT);
        Debug.Log($"[RestUI] Upgraded Damage +{DAMAGE_UPGRADE_AMOUNT} (both min and max) for {XP_COST_DAMAGE} XP");
        RefreshAscendUI();
    }

    private bool SpendXP(int amount)
    {
        if (GameManager.RunXP < amount) return false;
        GameManager.AddRunXP(-amount);
        return true;
    }

    private void RefreshAscendUI()
    {
        RefreshXPDisplay();
        PopulateUpgrades();
        
        var refs = FindFirstObjectByType<Referencer>();
        if (refs != null && refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
        {
            refs.playerStatsUI.UpdateStats();
        }
    }

    private void OnBackFromAscendClicked()
    {
        // Close the rest site completely when pressing back from ascend
        CloseRestSite();
    }

    private void CloseRestSite()
    {
        restPanel.SetActive(false);
        isActive = false;
        
        if (currentNode != null)
        {
            currentNode.OnNodeCompleted();
            currentNode = null;
        }
        
        onRestClosed?.Invoke();
        onRestClosed = null;
        Debug.Log("[RestUI] Rest site closed");
    }

    public bool IsActive() => isActive;

    void Update()
    {
        if (isActive && Input.GetKeyDown(KeyCode.Escape))
        {
            if (ascendContainer.activeSelf)
            {
                OnBackFromAscendClicked();
            }
            else
            {
                CloseRestSite();
            }
        }
    }
}
