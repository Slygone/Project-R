using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RestUI : MonoBehaviour
{
    // Heal amount
    private const float HEAL_PERCENT = 0.40f;

    private GameObject restPanel;
    private TextMeshProUGUI titleText;
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
        CreateHealButton(panel.transform);

        return panel;
    }

    private void CreateTitle(Transform parent)
    {
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.75f);
        titleRect.anchorMax = new Vector2(1, 0.92f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "REST SITE";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.9f, 0.8f, 0.5f);
    }

    private void CreateHealButton(Transform parent)
    {
        var btnObj = new GameObject("HealButton");
        btnObj.transform.SetParent(parent, false);

        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.30f, 0.35f);
        btnRect.anchorMax = new Vector2(0.70f, 0.65f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.5f, 0.25f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(OnHealClicked);

        // Title
        var healTitleObj = new GameObject("Title");
        healTitleObj.transform.SetParent(btnObj.transform, false);
        var htRect = healTitleObj.AddComponent<RectTransform>();
        htRect.anchorMin = new Vector2(0.05f, 0.50f);
        htRect.anchorMax = new Vector2(0.95f, 0.90f);
        htRect.offsetMin = Vector2.zero;
        htRect.offsetMax = Vector2.zero;
        var htText = healTitleObj.AddComponent<TextMeshProUGUI>();
        htText.text = "HEAL";
        htText.alignment = TextAlignmentOptions.Center;
        htText.fontSize = 36;
        htText.fontStyle = FontStyles.Bold;
        htText.color = Color.white;

        // Description
        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.10f);
        descRect.anchorMax = new Vector2(0.95f, 0.50f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = "Restore 40% Max HP\n& Remove Debuffs";
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 18;
        descText.color = new Color(0.85f, 0.9f, 0.85f);
    }

    public void Show(Player playerRef, NodeBase node, Action onClosed)
    {
        player = playerRef;
        currentNode = node;
        onRestClosed = onClosed;

        restPanel.SetActive(true);
        isActive = true;
    }

    private void OnHealClicked()
    {
        if (player == null) return;

        int healAmount = Mathf.RoundToInt(player.GetMaxHealth() * HEAL_PERCENT);
        player.ClearWounds();
        player.Heal(healAmount);
        
        // Remove debuffs/counters
        player.ClearDebuffs();
        
        Debug.Log($"[RestUI] Healed {healAmount} HP (40% of {player.GetMaxHealth()}) and cleared debuffs");
        
        // Auto-close after heal
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
            CloseRestSite();
        }
    }
}
