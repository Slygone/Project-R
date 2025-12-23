using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfusionUI : MonoBehaviour
{
    private GameObject infusionPanel;
    private GameObject orbAButton;
    private GameObject orbBButton;
    private TextMeshProUGUI orbAText;
    private TextMeshProUGUI orbBText;
    private Image orbAImage;
    private Image orbBImage;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI reactionIndicator;
    
    private Action<bool> onInfusionSelected;
    private bool isActive = false;
    private Player player;
    private string pendingActionName;

    void Awake()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[InfusionUI] Canvas not found");
            return;
        }

        infusionPanel = CreateInfusionPanel(canvas.transform);
        infusionPanel.SetActive(false);
    }

    private GameObject CreateInfusionPanel(Transform parent)
    {
        var panel = new GameObject("InfusionPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.25f, 0.3f);
        rect.anchorMax = new Vector2(0.75f, 0.7f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.98f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.78f);
        titleRect.anchorMax = new Vector2(1, 0.95f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "CHOOSE INFUSION";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.2f);

        var subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(panel.transform, false);
        var subtitleRect = subtitleObj.AddComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0, 0.68f);
        subtitleRect.anchorMax = new Vector2(1, 0.78f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;
        var subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "Select which elemental orb to infuse into your attack";
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontSize = 16;
        subtitleText.color = new Color(0.7f, 0.7f, 0.7f);

        var reactionObj = new GameObject("ReactionIndicator");
        reactionObj.transform.SetParent(panel.transform, false);
        var reactionRect = reactionObj.AddComponent<RectTransform>();
        reactionRect.anchorMin = new Vector2(0, 0.05f);
        reactionRect.anchorMax = new Vector2(1, 0.18f);
        reactionRect.offsetMin = Vector2.zero;
        reactionRect.offsetMax = Vector2.zero;
        reactionIndicator = reactionObj.AddComponent<TextMeshProUGUI>();
        reactionIndicator.text = "";
        reactionIndicator.alignment = TextAlignmentOptions.Center;
        reactionIndicator.fontSize = 18;
        reactionIndicator.color = new Color(1f, 0.6f, 0.2f);

        CreateOrbButtons(panel.transform);

        return panel;
    }

    private void CreateOrbButtons(Transform parent)
    {
        var container = new GameObject("OrbContainer");
        container.transform.SetParent(parent, false);

        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.05f, 0.20f);
        containerRect.anchorMax = new Vector2(0.95f, 0.68f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        orbAButton = CreateOrbButton(container.transform, "OrbA", true);
        orbBButton = CreateOrbButton(container.transform, "OrbB", false);
    }

    private GameObject CreateOrbButton(Transform parent, string name, bool isOrbA)
    {
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        if (isOrbA)
            orbAImage = btnImage;
        else
            orbBImage = btnImage;

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        
        bool capturedIsOrbA = isOrbA;
        btn.onClick.AddListener(() => OnOrbClicked(capturedIsOrbA));

        // Use LayoutElement for flexible sizing instead of fixed padding
        var btnLayout = btnObj.AddComponent<LayoutElement>();
        btnLayout.flexibleWidth = 1;
        btnLayout.flexibleHeight = 1;
        btnLayout.minWidth = 120;
        btnLayout.minHeight = 80;

        // Create inner container with vertical layout for text elements
        var innerContainer = new GameObject("Inner");
        innerContainer.transform.SetParent(btnObj.transform, false);
        var innerRect = innerContainer.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(10, 10);
        innerRect.offsetMax = new Vector2(-10, -10);

        var layout = innerContainer.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 5;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // Label (ORB A / ORB B)
        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(innerContainer.transform, false);
        var labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = isOrbA ? "ORB A" : "ORB B";
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 16;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 10;
        labelText.fontSizeMax = 18;
        labelText.color = new Color(0.7f, 0.7f, 0.7f);
        var labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.flexibleHeight = 0.2f;
        labelLayout.minHeight = 18;

        // Element name (FIRE, ICE, etc)
        var elementObj = new GameObject("Element");
        elementObj.transform.SetParent(innerContainer.transform, false);
        var elementText = elementObj.AddComponent<TextMeshProUGUI>();
        elementText.text = "---";
        elementText.alignment = TextAlignmentOptions.Center;
        elementText.fontSize = 28;
        elementText.enableAutoSizing = true;
        elementText.fontSizeMin = 16;
        elementText.fontSizeMax = 32;
        elementText.fontStyle = FontStyles.Bold;
        elementText.color = Color.white;
        var elementLayout = elementObj.AddComponent<LayoutElement>();
        elementLayout.flexibleHeight = 0.5f;
        elementLayout.minHeight = 30;

        if (isOrbA)
            orbAText = elementText;
        else
            orbBText = elementText;

        // Status text (MARKED info)
        var statusObj = new GameObject("Status");
        statusObj.transform.SetParent(innerContainer.transform, false);
        var statusText = statusObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "";
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = 12;
        statusText.enableAutoSizing = true;
        statusText.fontSizeMin = 8;
        statusText.fontSizeMax = 14;
        statusText.color = new Color(0.5f, 0.8f, 0.5f);
        var statusLayout = statusObj.AddComponent<LayoutElement>();
        statusLayout.flexibleHeight = 0.2f;
        statusLayout.minHeight = 15;

        return btnObj;
    }

    public void Show(Player playerRef, string actionName, Action<bool> callback)
    {
        player = playerRef;
        pendingActionName = actionName;
        onInfusionSelected = callback;

        UpdateOrbDisplay();
        UpdateReactionIndicator();

        titleText.text = $"INFUSE: {actionName.ToUpper()}";
        
        infusionPanel.SetActive(true);
        isActive = true;

        Debug.Log($"[InfusionUI] Showing infusion selection for {actionName}");
    }

    private void UpdateOrbDisplay()
    {
        if (player == null) return;

        var orbSystem = player.GetOrbSystem();
        
        Element orbA = orbSystem.OrbAElement;
        Element orbB = orbSystem.OrbBElement;

        orbAText.text = orbA.ToString().ToUpper();
        orbBText.text = orbB.ToString().ToUpper();

        orbAImage.color = GetElementColor(orbA);
        orbBImage.color = GetElementColor(orbB);

        var statusA = orbAButton.transform.Find("Inner/Status")?.GetComponent<TextMeshProUGUI>();
        var statusB = orbBButton.transform.Find("Inner/Status")?.GetComponent<TextMeshProUGUI>();

        if (statusA != null)
        {
            if (orbSystem.IsOrbAActive)
            {
                statusA.text = $"<color=#ffaa55>MARKED: {orbSystem.OrbAMark}</color>";
            }
            else
            {
                statusA.text = "";
            }
        }

        if (statusB != null)
        {
            if (orbSystem.IsOrbBActive)
            {
                statusB.text = $"<color=#ffaa55>MARKED: {orbSystem.OrbBMark}</color>";
            }
            else
            {
                statusB.text = "";
            }
        }
    }

    private void UpdateReactionIndicator()
    {
        if (player == null) return;

        var orbSystem = player.GetOrbSystem();
        
        if (orbSystem.IsOrbAActive && orbSystem.IsOrbBActive)
        {
            var (a, b) = orbSystem.GetReactionPair();
            float multiplier = orbSystem.GetReactionDamageMultiplier();
            reactionIndicator.text = $"⚡ REACTION READY: {a} + {b} = {multiplier:F1}x damage! ⚡";
            reactionIndicator.color = new Color(1f, 0.6f, 0.2f);
        }
        else if (orbSystem.IsOrbAActive)
        {
            reactionIndicator.text = $"Orb A marked with {orbSystem.OrbAMark}. Use Orb B next for reaction!";
            reactionIndicator.color = new Color(0.6f, 0.8f, 0.6f);
        }
        else if (orbSystem.IsOrbBActive)
        {
            reactionIndicator.text = $"Orb B marked with {orbSystem.OrbBMark}. Use Orb A next for reaction!";
            reactionIndicator.color = new Color(0.6f, 0.8f, 0.6f);
        }
        else
        {
            reactionIndicator.text = "No marks active. Infuse to start building a reaction!";
            reactionIndicator.color = new Color(0.5f, 0.5f, 0.5f);
        }
    }

    private void OnOrbClicked(bool useOrbA)
    {
        infusionPanel.SetActive(false);
        isActive = false;

        string orbName = useOrbA ? "Orb A" : "Orb B";
        Element element = useOrbA ? player.GetOrbAElement() : player.GetOrbBElement();
        Debug.Log($"[InfusionUI] Player chose {orbName} ({element}) for {pendingActionName}");

        onInfusionSelected?.Invoke(useOrbA);
    }

    private Color GetElementColor(Element element)
    {
        return element switch
        {
            Element.Fire => new Color(0.6f, 0.15f, 0.1f, 1f),
            Element.Ice => new Color(0.15f, 0.4f, 0.6f, 1f),
            Element.Water => new Color(0.1f, 0.25f, 0.5f, 1f),
            Element.Wind => new Color(0.2f, 0.5f, 0.25f, 1f),
            Element.Rock => new Color(0.4f, 0.3f, 0.2f, 1f),
            _ => new Color(0.3f, 0.3f, 0.3f, 1f)
        };
    }

    public void Hide()
    {
        infusionPanel.SetActive(false);
        isActive = false;
    }

    public bool IsActive() => isActive;
}
