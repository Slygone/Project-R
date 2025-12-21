using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUI : MonoBehaviour
{
    private const int POTION_PRICE = 2;
    private const int RELIC_PRICE = 3;
    private const int REROLL_PRICE = 1;

    private GameObject shopPanel;
    private GameObject potionContainer;
    private GameObject relicContainer;
    private TextMeshProUGUI goldText;
    private List<RelicData> currentRelicChoices = new List<RelicData>();
    private List<PotionData> currentPotionChoices = new List<PotionData>();
    private List<GameObject> relicButtons = new List<GameObject>();
    private List<GameObject> potionButtons = new List<GameObject>();
    private Action onShopClosed;
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
            Debug.LogError("[ShopUI] Canvas not found");
            return;
        }

        shopPanel = CreateShopPanel(canvas.transform);
        shopPanel.SetActive(false);
    }

    private GameObject CreateShopPanel(Transform parent)
    {
        var panel = new GameObject("ShopPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.12f, 0.97f);

        CreateTitle(panel.transform);
        CreateGoldDisplay(panel.transform);
        CreatePotionSection(panel.transform);
        CreateRelicSection(panel.transform);
        CreateBackButton(panel.transform);

        return panel;
    }

    private void CreateTitle(Transform parent)
    {
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.88f);
        titleRect.anchorMax = new Vector2(1, 0.98f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "SHOP";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.2f);
    }

    private void CreateGoldDisplay(Transform parent)
    {
        var goldObj = new GameObject("GoldDisplay");
        goldObj.transform.SetParent(parent, false);
        var goldRect = goldObj.AddComponent<RectTransform>();
        goldRect.anchorMin = new Vector2(0.4f, 0.82f);
        goldRect.anchorMax = new Vector2(0.6f, 0.88f);
        goldRect.offsetMin = Vector2.zero;
        goldRect.offsetMax = Vector2.zero;
        goldText = goldObj.AddComponent<TextMeshProUGUI>();
        goldText.text = "Gold: 0";
        goldText.alignment = TextAlignmentOptions.Center;
        goldText.fontSize = 24;
        goldText.color = new Color(1f, 0.84f, 0f);
    }

    private void CreatePotionSection(Transform parent)
    {
        var sectionObj = new GameObject("PotionSection");
        sectionObj.transform.SetParent(parent, false);
        var sectionRect = sectionObj.AddComponent<RectTransform>();
        sectionRect.anchorMin = new Vector2(0.05f, 0.55f);
        sectionRect.anchorMax = new Vector2(0.95f, 0.80f);
        sectionRect.offsetMin = Vector2.zero;
        sectionRect.offsetMax = Vector2.zero;

        var labelObj = new GameObject("PotionLabel");
        labelObj.transform.SetParent(sectionObj.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.75f);
        labelRect.anchorMax = new Vector2(1, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = $"POTIONS - {POTION_PRICE} Gold Each";
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 22;
        labelText.color = new Color(0.6f, 0.9f, 0.6f);

        potionContainer = new GameObject("PotionContainer");
        potionContainer.transform.SetParent(sectionObj.transform, false);
        var containerRect = potionContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(1f, 0.75f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
    }

    private void CreateRelicSection(Transform parent)
    {
        var sectionObj = new GameObject("RelicSection");
        sectionObj.transform.SetParent(parent, false);
        var sectionRect = sectionObj.AddComponent<RectTransform>();
        sectionRect.anchorMin = new Vector2(0.05f, 0.18f);
        sectionRect.anchorMax = new Vector2(0.95f, 0.52f);
        sectionRect.offsetMin = Vector2.zero;
        sectionRect.offsetMax = Vector2.zero;

        var labelObj = new GameObject("RelicLabel");
        labelObj.transform.SetParent(sectionObj.transform, false);
        var labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.82f);
        labelRect.anchorMax = new Vector2(1, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = $"RELICS - {RELIC_PRICE} Gold Each";
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 22;
        labelText.color = new Color(0.9f, 0.7f, 0.4f);

        relicContainer = new GameObject("RelicContainer");
        relicContainer.transform.SetParent(sectionObj.transform, false);
        var containerRect = relicContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.05f, 0.18f);
        containerRect.anchorMax = new Vector2(0.95f, 0.82f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        var rerollObj = new GameObject("RerollButton");
        rerollObj.transform.SetParent(sectionObj.transform, false);
        var rerollRect = rerollObj.AddComponent<RectTransform>();
        rerollRect.anchorMin = new Vector2(0.35f, 0f);
        rerollRect.anchorMax = new Vector2(0.65f, 0.15f);
        rerollRect.offsetMin = Vector2.zero;
        rerollRect.offsetMax = Vector2.zero;

        var rerollImage = rerollObj.AddComponent<Image>();
        rerollImage.color = new Color(0.3f, 0.25f, 0.4f);

        var rerollBtn = rerollObj.AddComponent<Button>();
        rerollBtn.targetGraphic = rerollImage;
        rerollBtn.onClick.AddListener(OnRerollClicked);

        var rerollTextObj = new GameObject("Text");
        rerollTextObj.transform.SetParent(rerollObj.transform, false);
        var rerollTextRect = rerollTextObj.AddComponent<RectTransform>();
        rerollTextRect.anchorMin = Vector2.zero;
        rerollTextRect.anchorMax = Vector2.one;
        rerollTextRect.offsetMin = Vector2.zero;
        rerollTextRect.offsetMax = Vector2.zero;
        var rerollText = rerollTextObj.AddComponent<TextMeshProUGUI>();
        rerollText.text = $"Re-roll Relics ({REROLL_PRICE} Gold)";
        rerollText.alignment = TextAlignmentOptions.Center;
        rerollText.fontSize = 18;
        rerollText.color = Color.white;
    }

    private void CreateBackButton(Transform parent)
    {
        var backObj = new GameObject("BackButton");
        backObj.transform.SetParent(parent, false);
        var backRect = backObj.AddComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.4f, 0.03f);
        backRect.anchorMax = new Vector2(0.6f, 0.12f);
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;

        var backImage = backObj.AddComponent<Image>();
        backImage.color = new Color(0.5f, 0.2f, 0.2f);

        var backBtn = backObj.AddComponent<Button>();
        backBtn.targetGraphic = backImage;
        backBtn.onClick.AddListener(OnBackClicked);

        var backTextObj = new GameObject("Text");
        backTextObj.transform.SetParent(backObj.transform, false);
        var backTextRect = backTextObj.AddComponent<RectTransform>();
        backTextRect.anchorMin = Vector2.zero;
        backTextRect.anchorMax = Vector2.one;
        backTextRect.offsetMin = Vector2.zero;
        backTextRect.offsetMax = Vector2.zero;
        var backText = backTextObj.AddComponent<TextMeshProUGUI>();
        backText.text = "Leave Shop";
        backText.alignment = TextAlignmentOptions.Center;
        backText.fontSize = 22;
        backText.color = Color.white;
    }

    public void Show(Player playerRef, NodeBase node, Action onClosed)
    {
        player = playerRef;
        currentNode = node;
        onShopClosed = onClosed;

        RefreshGoldDisplay();
        PopulatePotions();
        PopulateRelics();

        shopPanel.SetActive(true);
        isActive = true;
    }

    private void RefreshGoldDisplay()
    {
        if (goldText != null && player != null)
        {
            goldText.text = $"Gold: {player.GetGold()}";
        }
    }

    private void PopulatePotions()
    {
        foreach (var btn in potionButtons)
        {
            if (btn != null) Destroy(btn);
        }
        potionButtons.Clear();

        currentPotionChoices = GetRandomPotions(3);

        foreach (var potion in currentPotionChoices)
        {
            CreatePotionButton(potion);
        }
    }

    private List<PotionData> GetRandomPotions(int count)
    {
        var allPotions = DataCache.Potions;
        if (allPotions == null || allPotions.Count == 0)
        {
            Debug.LogWarning("[ShopUI] No potions loaded in DataCache");
            return new List<PotionData>();
        }

        var available = new List<PotionData>(allPotions);
        var result = new List<PotionData>();

        while (result.Count < count && available.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, available.Count);
            result.Add(available[index]);
            available.RemoveAt(index);
        }

        return result;
    }

    private void CreatePotionButton(PotionData potion)
    {
        int index = potionButtons.Count;
        float buttonWidth = 0.30f;
        float gap = 0.05f;
        float startX = (1f - (3 * buttonWidth + 2 * gap)) / 2f;
        float xMin = startX + index * (buttonWidth + gap);
        float xMax = xMin + buttonWidth;
        
        var btnObj = new GameObject($"Potion_{potion.DisplayName}");
        btnObj.transform.SetParent(potionContainer.transform, false);
        
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(xMin, 0.05f);
        btnRect.anchorMax = new Vector2(xMax, 0.95f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.35f, 0.2f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        potionButtons.Add(btnObj);

        PotionData capturedPotion = potion;
        GameObject capturedBtn = btnObj;
        btn.onClick.AddListener(() => OnPotionClicked(capturedPotion, capturedBtn));

        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(btnObj.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.05f, 0.65f);
        nameRect.anchorMax = new Vector2(0.95f, 0.95f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = potion.DisplayName;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 20;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;

        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.35f);
        descRect.anchorMax = new Vector2(0.95f, 0.65f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = $"+{potion.Amount} {potion.StatAffected}";
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 16;
        descText.color = new Color(0.8f, 0.8f, 0.8f);

        var priceObj = new GameObject("Price");
        priceObj.transform.SetParent(btnObj.transform, false);
        var priceRect = priceObj.AddComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(0.05f, 0.05f);
        priceRect.anchorMax = new Vector2(0.95f, 0.35f);
        priceRect.offsetMin = Vector2.zero;
        priceRect.offsetMax = Vector2.zero;
        var priceText = priceObj.AddComponent<TextMeshProUGUI>();
        priceText.text = $"{POTION_PRICE} Gold";
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.fontSize = 16;
        priceText.color = new Color(1f, 0.84f, 0f);
    }

    private void PopulateRelics()
    {
        foreach (var btn in relicButtons)
        {
            if (btn != null) Destroy(btn);
        }
        relicButtons.Clear();

        currentRelicChoices = GetRandomRelics(3);

        foreach (var relic in currentRelicChoices)
        {
            CreateRelicButton(relic);
        }
    }

    private List<RelicData> GetRandomRelics(int count)
    {
        var allRelics = DataCache.Relics;
        if (allRelics == null || allRelics.Count == 0)
        {
            Debug.LogWarning("[ShopUI] No relics loaded in DataCache");
            return new List<RelicData>();
        }

        var available = new List<RelicData>(allRelics);
        var result = new List<RelicData>();

        while (result.Count < count && available.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, available.Count);
            result.Add(available[index]);
            available.RemoveAt(index);
        }

        return result;
    }

    private void CreateRelicButton(RelicData relic)
    {
        int index = relicButtons.Count;
        float buttonWidth = 0.30f;
        float gap = 0.05f;
        float startX = (1f - (3 * buttonWidth + 2 * gap)) / 2f;
        float xMin = startX + index * (buttonWidth + gap);
        float xMax = xMin + buttonWidth;
        
        var btnObj = new GameObject($"Relic_{relic.DisplayName}");
        btnObj.transform.SetParent(relicContainer.transform, false);
        relicButtons.Add(btnObj);
        
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(xMin, 0.05f);
        btnRect.anchorMax = new Vector2(xMax, 0.95f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = GetRelicColor(relic);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;

        RelicData capturedRelic = relic;
        GameObject capturedBtn = btnObj;
        btn.onClick.AddListener(() => OnRelicClicked(capturedRelic, capturedBtn));

        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(btnObj.transform, false);
        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.05f, 0.65f);
        nameRect.anchorMax = new Vector2(0.95f, 0.95f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = relic.DisplayName;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 18;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;

        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.35f);
        descRect.anchorMax = new Vector2(0.95f, 0.65f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = $"+{relic.Amount} {relic.StatAffected}";
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 14;
        descText.color = new Color(0.8f, 0.8f, 0.8f);

        var priceObj = new GameObject("Price");
        priceObj.transform.SetParent(btnObj.transform, false);
        var priceRect = priceObj.AddComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(0.10f, 0.08f);
        priceRect.anchorMax = new Vector2(0.90f, 0.28f);
        priceRect.offsetMin = Vector2.zero;
        priceRect.offsetMax = Vector2.zero;
        var priceText = priceObj.AddComponent<TextMeshProUGUI>();
        priceText.text = $"{RELIC_PRICE} Gold";
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        priceText.overflowMode = TextOverflowModes.Truncate;
        priceText.fontSize = 14;
        priceText.color = new Color(1f, 0.84f, 0f);
        priceObj.transform.SetAsLastSibling();
    }

    private Color GetRelicColor(RelicData relic)
    {
        var stat = relic.StatAffected.ToLower().Trim();
        switch (stat)
        {
            case "fire": return new Color(0.5f, 0.2f, 0.15f);
            case "ice": return new Color(0.15f, 0.3f, 0.5f);
            case "water": return new Color(0.1f, 0.25f, 0.45f);
            case "wind": return new Color(0.2f, 0.4f, 0.25f);
            case "rock": return new Color(0.35f, 0.28f, 0.2f);
            case "health": return new Color(0.4f, 0.2f, 0.3f);
            default: return new Color(0.3f, 0.3f, 0.35f);
        }
    }

    private void OnPotionClicked(PotionData potion, GameObject btnObj)
    {
        if (player == null) return;

        if (player.GetGold() < POTION_PRICE)
        {
            Debug.Log($"[ShopUI] Not enough gold for {potion.DisplayName}. Need {POTION_PRICE}, have {player.GetGold()}");
            return;
        }

        if (!player.CanAddPotion())
        {
            Debug.Log($"[ShopUI] Potion inventory full (max 4)");
            return;
        }

        player.AddGold(-POTION_PRICE);
        player.AddPotionToInventory(potion);
        RefreshGoldDisplay();

        if (btnObj != null)
        {
            potionButtons.Remove(btnObj);
            Destroy(btnObj);
        }

        Debug.Log($"[ShopUI] Purchased {potion.DisplayName} for {POTION_PRICE} gold");
    }

    private void OnRelicClicked(RelicData relic, GameObject btnObj)
    {
        if (player == null) return;

        if (player.GetGold() < RELIC_PRICE)
        {
            Debug.Log($"[ShopUI] Not enough gold for {relic.DisplayName}. Need {RELIC_PRICE}, have {player.GetGold()}");
            return;
        }

        player.AddGold(-RELIC_PRICE);
        player.AddRelic(relic);
        RefreshGoldDisplay();

        var refs = FindFirstObjectByType<Referencer>();
        if (refs != null && refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
        {
            refs.playerStatsUI.UpdateStats();
        }

        if (btnObj != null)
        {
            relicButtons.Remove(btnObj);
            Destroy(btnObj);
        }

        Debug.Log($"[ShopUI] Purchased {relic.DisplayName} for {RELIC_PRICE} gold");
    }

    private void OnRerollClicked()
    {
        if (player == null) return;

        if (player.GetGold() < REROLL_PRICE)
        {
            Debug.Log($"[ShopUI] Not enough gold to re-roll. Need {REROLL_PRICE}, have {player.GetGold()}");
            return;
        }

        player.AddGold(-REROLL_PRICE);
        RefreshGoldDisplay();
        PopulateRelics();

        Debug.Log($"[ShopUI] Re-rolled relics for {REROLL_PRICE} gold");
    }

    private void OnBackClicked()
    {
        shopPanel.SetActive(false);
        isActive = false;

        if (currentNode != null)
        {
            currentNode.OnNodeCompleted();
            currentNode = null;
        }

        onShopClosed?.Invoke();
        onShopClosed = null;

        Debug.Log("[ShopUI] Shop closed");
    }

    public bool IsActive() => isActive;

    void Update()
    {
        if (isActive && Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackClicked();
        }
    }
}
