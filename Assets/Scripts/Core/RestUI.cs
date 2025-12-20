using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RestUI : MonoBehaviour
{
    private GameObject restPanel;
    private GameObject mainChoiceContainer;
    private GameObject ascendChoiceContainer;
    private Action onRestClosed;
    private bool isActive = false;
    private Player player;
    private NodeBase currentNode;
    
    private int healthUpgradePercent;
    private int damageUpgradePercent;

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
        bg.color = new Color(0.05f, 0.1f, 0.08f, 0.97f);

        CreateTitle(panel.transform);
        CreateMainChoices(panel.transform);
        CreateAscendChoices(panel.transform);

        return panel;
    }

    private void CreateTitle(Transform parent)
    {
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "REST SITE";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.4f, 1f, 0.5f);
    }

    private void CreateMainChoices(Transform parent)
    {
        mainChoiceContainer = new GameObject("MainChoiceContainer");
        mainChoiceContainer.transform.SetParent(parent, false);

        var containerRect = mainChoiceContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.15f, 0.25f);
        containerRect.anchorMax = new Vector2(0.85f, 0.75f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        var layout = mainChoiceContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 40;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        CreateHealButton(mainChoiceContainer.transform);
        CreateAscendButton(mainChoiceContainer.transform);
    }

    private void CreateHealButton(Transform parent)
    {
        var btnObj = new GameObject("HealButton");
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.15f, 0.3f, 0.2f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(OnHealClicked);

        var layout = btnObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(20, 20, 30, 30);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(btnObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "HEAL";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.4f, 1f, 0.4f);
        var titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 50;

        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = "Restore 30% of\nyour maximum health";
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 20;
        descText.color = new Color(0.7f, 0.9f, 0.7f);
        var descLayout = descObj.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 60;
    }

    private void CreateAscendButton(Transform parent)
    {
        var btnObj = new GameObject("AscendButton");
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.25f, 0.2f, 0.35f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(OnAscendClicked);

        var layout = btnObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(20, 20, 30, 30);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(btnObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "ASCEND";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.8f, 0.6f, 1f);
        var titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 50;

        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = "Permanently upgrade\nyour stats";
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 20;
        descText.color = new Color(0.8f, 0.7f, 0.9f);
        var descLayout = descObj.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 60;
    }

    private void CreateAscendChoices(Transform parent)
    {
        ascendChoiceContainer = new GameObject("AscendChoiceContainer");
        ascendChoiceContainer.transform.SetParent(parent, false);

        var containerRect = ascendChoiceContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.2f);
        containerRect.anchorMax = new Vector2(0.9f, 0.75f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        ascendChoiceContainer.SetActive(false);
    }

    private void PopulateAscendChoices()
    {
        foreach (Transform child in ascendChoiceContainer.transform)
        {
            Destroy(child.gameObject);
        }

        healthUpgradePercent = UnityEngine.Random.Range(5, 11);
        damageUpgradePercent = UnityEngine.Random.Range(5, 11);

        var layout = ascendChoiceContainer.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = ascendChoiceContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        var subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(ascendChoiceContainer.transform, false);
        var subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "Choose your ascension:";
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontSize = 28;
        subtitleText.color = new Color(0.9f, 0.8f, 1f);
        var subtitleLayout = subtitleObj.AddComponent<LayoutElement>();
        subtitleLayout.preferredHeight = 50;

        var choicesContainer = new GameObject("ChoicesRow");
        choicesContainer.transform.SetParent(ascendChoiceContainer.transform, false);
        var choicesRect = choicesContainer.AddComponent<RectTransform>();
        var choicesLayout = choicesContainer.AddComponent<HorizontalLayoutGroup>();
        choicesLayout.spacing = 40;
        choicesLayout.childAlignment = TextAnchor.MiddleCenter;
        choicesLayout.childControlWidth = true;
        choicesLayout.childControlHeight = true;
        choicesLayout.childForceExpandWidth = true;
        choicesLayout.childForceExpandHeight = true;
        var choicesLayoutElement = choicesContainer.AddComponent<LayoutElement>();
        choicesLayoutElement.preferredHeight = 200;

        CreateHealthUpgradeButton(choicesContainer.transform);
        CreateDamageUpgradeButton(choicesContainer.transform);

        CreateBackButton(ascendChoiceContainer.transform);
    }

    private void CreateHealthUpgradeButton(Transform parent)
    {
        var btnObj = new GameObject("HealthUpgradeButton");
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.15f, 0.15f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(OnHealthUpgradeClicked);

        var layout = btnObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(btnObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "MAX HEALTH";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 26;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.5f, 0.5f);
        var titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 40;

        int currentMaxHealth = player != null ? player.GetMaxHealth() : 100;
        int healthGain = Mathf.RoundToInt(currentMaxHealth * (healthUpgradePercent / 100f));

        var valueObj = new GameObject("Value");
        valueObj.transform.SetParent(btnObj.transform, false);
        var valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = $"+{healthUpgradePercent}%";
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.fontSize = 36;
        valueText.fontStyle = FontStyles.Bold;
        valueText.color = new Color(1f, 0.8f, 0.8f);
        var valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.preferredHeight = 50;

        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = $"(+{healthGain} HP)";
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 18;
        descText.color = new Color(0.7f, 0.6f, 0.6f);
        var descLayout = descObj.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 30;
    }

    private void CreateDamageUpgradeButton(Transform parent)
    {
        var btnObj = new GameObject("DamageUpgradeButton");
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.15f, 0.3f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(OnDamageUpgradeClicked);

        var layout = btnObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(btnObj.transform, false);
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "CHARACTER DAMAGE";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 26;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.7f, 0.5f, 1f);
        var titleLayout = titleObj.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 40;

        int currentDamage = player != null ? player.GetCharacterDamage() : 10;
        int damageGain = Mathf.RoundToInt(currentDamage * (damageUpgradePercent / 100f));
        if (damageGain < 1) damageGain = 1;

        var valueObj = new GameObject("Value");
        valueObj.transform.SetParent(btnObj.transform, false);
        var valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = $"+{damageUpgradePercent}%";
        valueText.alignment = TextAlignmentOptions.Center;
        valueText.fontSize = 36;
        valueText.fontStyle = FontStyles.Bold;
        valueText.color = new Color(0.9f, 0.8f, 1f);
        var valueLayout = valueObj.AddComponent<LayoutElement>();
        valueLayout.preferredHeight = 50;

        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = $"(+{damageGain} Damage)";
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 18;
        descText.color = new Color(0.6f, 0.6f, 0.7f);
        var descLayout = descObj.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 30;
    }

    private void CreateBackButton(Transform parent)
    {
        var btnObj = new GameObject("BackButton");
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.3f, 0.25f, 0.2f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(OnBackFromAscendClicked);

        var layoutElement = btnObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 50;
        layoutElement.preferredWidth = 150;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Back";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22;
        text.color = Color.white;
    }

    public void Show(Player p, NodeBase node, Action onClosed)
    {
        player = p;
        currentNode = node;
        onRestClosed = onClosed;

        mainChoiceContainer.SetActive(true);
        ascendChoiceContainer.SetActive(false);

        restPanel.SetActive(true);
        isActive = true;

        Debug.Log("[RestUI] Rest site opened");
    }

    private void OnHealClicked()
    {
        if (player == null) return;

        int healAmount = Mathf.RoundToInt(player.GetMaxHealth() * 0.30f);
        player.Heal(healAmount);

        Debug.Log($"[RestUI] Player healed for {healAmount} HP (30% of max)");

        var refs = FindFirstObjectByType<Referencer>();
        if (refs != null && refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
        {
            refs.playerStatsUI.UpdateStats();
        }

        CloseRest();
    }

    private void OnAscendClicked()
    {
        mainChoiceContainer.SetActive(false);
        PopulateAscendChoices();
        ascendChoiceContainer.SetActive(true);

        Debug.Log("[RestUI] Showing ascend choices");
    }

    private void OnBackFromAscendClicked()
    {
        ascendChoiceContainer.SetActive(false);
        mainChoiceContainer.SetActive(true);
    }

    private void OnHealthUpgradeClicked()
    {
        if (player == null) return;

        int currentMaxHealth = player.GetMaxHealth();
        int healthGain = Mathf.RoundToInt(currentMaxHealth * (healthUpgradePercent / 100f));
        
        player.IncreaseMaxHealth(healthGain);

        Debug.Log($"[RestUI] Player ascended: +{healthUpgradePercent}% Max Health (+{healthGain} HP)");

        var refs = FindFirstObjectByType<Referencer>();
        if (refs != null && refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
        {
            refs.playerStatsUI.UpdateStats();
        }

        CloseRest();
    }

    private void OnDamageUpgradeClicked()
    {
        if (player == null) return;

        int currentDamage = player.GetCharacterDamage();
        int damageGain = Mathf.RoundToInt(currentDamage * (damageUpgradePercent / 100f));
        if (damageGain < 1) damageGain = 1;

        player.IncreaseCharacterDamage(damageGain);

        Debug.Log($"[RestUI] Player ascended: +{damageUpgradePercent}% Character Damage (+{damageGain})");

        var refs = FindFirstObjectByType<Referencer>();
        if (refs != null && refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
        {
            refs.playerStatsUI.UpdateStats();
        }

        CloseRest();
    }

    private void CloseRest()
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
}
