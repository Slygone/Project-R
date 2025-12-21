using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MysteryUI : MonoBehaviour
{
    private GameObject mysteryPanel;
    private Action onMysteryCompleted;
    private bool isActive = false;
    private Player player;
    private MysteryNode currentNode;

    void Awake()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[MysteryUI] Canvas not found");
            return;
        }

        mysteryPanel = CreateMysteryPanel(canvas.transform);
        mysteryPanel.SetActive(false);
    }

    private GameObject CreateMysteryPanel(Transform parent)
    {
        var panel = new GameObject("MysteryPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.05f, 0.12f, 0.97f);

        CreateTitle(panel.transform);
        CreateChoices(panel.transform);

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
        titleText.text = "MYSTERY EVENT";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.9f, 0.7f, 1f);
    }

    private void CreateChoices(Transform parent)
    {
        var choiceContainer = new GameObject("ChoiceContainer");
        choiceContainer.transform.SetParent(parent, false);

        var containerRect = choiceContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.2f);
        containerRect.anchorMax = new Vector2(0.9f, 0.75f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        CreateEliteFightButton(choiceContainer.transform);
        CreateRandomUpgradeButton(choiceContainer.transform);
    }

    private void CreateEliteFightButton(Transform parent)
    {
        float buttonWidth = 0.42f;
        float xMin = 0.04f;
        float xMax = xMin + buttonWidth;

        var btnObj = new GameObject("EliteFightButton");
        btnObj.transform.SetParent(parent, false);

        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(xMin, 0.1f);
        btnRect.anchorMax = new Vector2(xMax, 0.9f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.4f, 0.15f, 0.15f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(OnEliteFightClicked);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(btnObj.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.7f);
        titleRect.anchorMax = new Vector2(0.95f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "ELITE FIGHT";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.6f, 0.6f);

        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.25f);
        descRect.anchorMax = new Vector2(0.95f, 0.7f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = "Fight a powerful elite enemy\n\n<color=#FFD700>Rewards:</color>\n• 2 Relics (1 matching element)\n• Double Gold";
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 16;
        descText.color = new Color(0.9f, 0.8f, 0.8f);
        descText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        var warningObj = new GameObject("Warning");
        warningObj.transform.SetParent(btnObj.transform, false);
        var warningRect = warningObj.AddComponent<RectTransform>();
        warningRect.anchorMin = new Vector2(0.05f, 0.05f);
        warningRect.anchorMax = new Vector2(0.95f, 0.25f);
        warningRect.offsetMin = Vector2.zero;
        warningRect.offsetMax = Vector2.zero;
        var warningText = warningObj.AddComponent<TextMeshProUGUI>();
        warningText.text = "<color=#FF6666>High Risk / High Reward</color>";
        warningText.alignment = TextAlignmentOptions.Center;
        warningText.fontSize = 14;
        warningText.fontStyle = FontStyles.Italic;
        warningText.color = Color.white;
    }

    private void CreateRandomUpgradeButton(Transform parent)
    {
        float buttonWidth = 0.42f;
        float xMin = 0.54f;
        float xMax = xMin + buttonWidth;

        var btnObj = new GameObject("RandomUpgradeButton");
        btnObj.transform.SetParent(parent, false);

        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(xMin, 0.1f);
        btnRect.anchorMax = new Vector2(xMax, 0.9f);
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.15f, 0.25f, 0.4f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        btn.onClick.AddListener(OnRandomUpgradeClicked);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(btnObj.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.7f);
        titleRect.anchorMax = new Vector2(0.95f, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "RANDOM UPGRADE";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.6f, 0.8f, 1f);

        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.05f, 0.25f);
        descRect.anchorMax = new Vector2(0.95f, 0.7f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = "Receive a mysterious gift\n\n<color=#FFD700>Rewards:</color>\n• 1 Random Relic\n• Normal Gold";
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 16;
        descText.color = new Color(0.8f, 0.85f, 0.95f);
        descText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        var safeObj = new GameObject("Safe");
        safeObj.transform.SetParent(btnObj.transform, false);
        var safeRect = safeObj.AddComponent<RectTransform>();
        safeRect.anchorMin = new Vector2(0.05f, 0.05f);
        safeRect.anchorMax = new Vector2(0.95f, 0.25f);
        safeRect.offsetMin = Vector2.zero;
        safeRect.offsetMax = Vector2.zero;
        var safeText = safeObj.AddComponent<TextMeshProUGUI>();
        safeText.text = "<color=#66FF66>Safe Choice</color>";
        safeText.alignment = TextAlignmentOptions.Center;
        safeText.fontSize = 14;
        safeText.fontStyle = FontStyles.Italic;
        safeText.color = Color.white;
    }

    public void Show(Player playerRef, MysteryNode node, Action onCompleted)
    {
        player = playerRef;
        currentNode = node;
        onMysteryCompleted = onCompleted;

        mysteryPanel.SetActive(true);
        isActive = true;
    }

    public void Hide()
    {
        mysteryPanel.SetActive(false);
        isActive = false;
    }

    private void OnEliteFightClicked()
    {
        Debug.Log("[MysteryUI] Player chose Elite Fight");
        Hide();
        
        if (currentNode != null)
        {
            currentNode.StartEliteFightFromUI();
        }
    }

    private void OnRandomUpgradeClicked()
    {
        Debug.Log("[MysteryUI] Player chose Random Upgrade");
        Hide();
        
        if (currentNode != null)
        {
            currentNode.GiveRandomUpgradeFromUI();
        }
    }

    public bool IsActive() => isActive;
}
