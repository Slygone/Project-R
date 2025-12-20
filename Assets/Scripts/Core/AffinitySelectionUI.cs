using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AffinitySelectionUI : MonoBehaviour
{
    private GameObject selectionPanel;
    private List<Element> choices = new List<Element>();
    private List<ElementPair> pairChoices = new List<ElementPair>();
    private Action<Element> onAffinitySelected;
    private Action<ElementPair> onPairSelected;
    private bool isActive = false;
    private bool isPairMode = false;

    void Awake()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("[AffinitySelectionUI] Canvas not found");
            return;
        }

        selectionPanel = CreateSelectionPanel(canvas.transform);
        selectionPanel.SetActive(false);
    }

    private GameObject CreateSelectionPanel(Transform parent)
    {
        var panel = new GameObject("AffinitySelectionPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.75f);
        titleRect.anchorMax = new Vector2(1, 0.9f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "CHOOSE YOUR ELEMENTAL ORBS";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 42;
        titleText.color = new Color(1f, 0.85f, 0.2f);

        var subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(panel.transform, false);
        var subtitleRect = subtitleObj.AddComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0, 0.65f);
        subtitleRect.anchorMax = new Vector2(1, 0.75f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;
        var subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "Choose your elemental orb pair for this run. Infuse attacks with either element!";
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontSize = 20;
        subtitleText.color = new Color(0.7f, 0.7f, 0.7f);

        return panel;
    }

    public void Show(Action<Element> callback)
    {
        isPairMode = false;
        onAffinitySelected = callback;
        choices = GetRandomElements(3);
        
        ClearChoiceButtons();
        CreateChoiceButtons();
        
        selectionPanel.SetActive(true);
        isActive = true;
    }
    
    public void ShowPairSelection(Action<ElementPair> callback)
    {
        isPairMode = true;
        onPairSelected = callback;
        pairChoices = GetRandomPairs(3);
        
        ClearChoiceButtons();
        CreatePairButtons();
        
        selectionPanel.SetActive(true);
        isActive = true;
    }
    
    private List<ElementPair> GetRandomPairs(int count)
    {
        var allPairs = new List<ElementPair>(ElementPair.GetPredefinedPairs());
        var result = new List<ElementPair>();
        
        while (result.Count < count && allPairs.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, allPairs.Count);
            result.Add(allPairs[index]);
            allPairs.RemoveAt(index);
        }
        
        return result;
    }
    
    private void CreatePairButtons()
    {
        var container = new GameObject("ChoiceContainer");
        container.transform.SetParent(selectionPanel.transform, false);

        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.15f);
        containerRect.anchorMax = new Vector2(0.9f, 0.6f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        foreach (var pair in pairChoices)
        {
            CreatePairButton(container.transform, pair);
        }
    }
    
    private void CreatePairButton(Transform parent, ElementPair pair)
    {
        var btnObj = new GameObject($"Btn_{pair.DisplayName}");
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        
        ElementPair capturedPair = pair;
        btn.onClick.AddListener(() => OnPairClicked(capturedPair));

        var layout = btnObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.padding = new RectOffset(15, 15, 20, 20);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(btnObj.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = pair.DisplayName.ToUpper();
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 26;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;
        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 40;

        var orbContainer = new GameObject("OrbContainer");
        orbContainer.transform.SetParent(btnObj.transform, false);
        var orbLayout = orbContainer.AddComponent<HorizontalLayoutGroup>();
        orbLayout.spacing = 20;
        orbLayout.childAlignment = TextAnchor.MiddleCenter;
        orbLayout.childControlWidth = false;
        orbLayout.childControlHeight = false;
        var orbLayoutElement = orbContainer.AddComponent<LayoutElement>();
        orbLayoutElement.preferredHeight = 80;

        CreateOrbIcon(orbContainer.transform, pair.OrbA, "A");
        CreateOrbIcon(orbContainer.transform, pair.OrbB, "B");

        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = $"Orb A: {pair.OrbA}\nOrb B: {pair.OrbB}";
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 14;
        descText.color = new Color(0.8f, 0.8f, 0.8f);
        var descLayout = descObj.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 40;
    }
    
    private void CreateOrbIcon(Transform parent, Element element, string label)
    {
        var orbObj = new GameObject($"Orb_{label}");
        orbObj.transform.SetParent(parent, false);
        
        var orbRect = orbObj.AddComponent<RectTransform>();
        orbRect.sizeDelta = new Vector2(60, 60);
        
        var orbImage = orbObj.AddComponent<Image>();
        orbImage.color = GetElementBackgroundColor(element);
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(orbObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = element.ToString().Substring(0, 1);
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 28;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
    }
    
    private void OnPairClicked(ElementPair pair)
    {
        selectionPanel.SetActive(false);
        isActive = false;

        Debug.Log($"[AffinitySelectionUI] Player chose {pair.DisplayName} orb pair");
        onPairSelected?.Invoke(pair);
    }

    private List<Element> GetRandomElements(int count)
    {
        var allElements = new List<Element>
        {
            Element.Fire,
            Element.Ice,
            Element.Water,
            Element.Wind,
            Element.Rock
        };

        var result = new List<Element>();
        while (result.Count < count && allElements.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, allElements.Count);
            result.Add(allElements[index]);
            allElements.RemoveAt(index);
        }

        return result;
    }

    private void ClearChoiceButtons()
    {
        var container = selectionPanel.transform.Find("ChoiceContainer");
        if (container != null)
        {
            Destroy(container.gameObject);
        }
    }

    private void CreateChoiceButtons()
    {
        var container = new GameObject("ChoiceContainer");
        container.transform.SetParent(selectionPanel.transform, false);

        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.1f, 0.2f);
        containerRect.anchorMax = new Vector2(0.9f, 0.6f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        for (int i = 0; i < choices.Count; i++)
        {
            CreateAffinityButton(container.transform, choices[i]);
        }
    }

    private void CreateAffinityButton(Transform parent, Element element)
    {
        var btnObj = new GameObject($"Btn_{element}");
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = GetElementBackgroundColor(element);

        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        
        Element capturedElement = element;
        btn.onClick.AddListener(() => OnChoiceClicked(capturedElement));

        var layout = btnObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(20, 20, 30, 30);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(btnObj.transform, false);
        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = element.ToString().ToUpper();
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 32;
        nameText.color = Color.white;
        var nameLayout = nameObj.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 50;

        var descObj = new GameObject("Description");
        descObj.transform.SetParent(btnObj.transform, false);
        var descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.text = GetElementDescription(element);
        descText.alignment = TextAlignmentOptions.Center;
        descText.fontSize = 16;
        descText.color = new Color(0.9f, 0.9f, 0.9f);
        var descLayout = descObj.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 60;
    }

    private void OnChoiceClicked(Element element)
    {
        selectionPanel.SetActive(false);
        isActive = false;

        Debug.Log($"[AffinitySelectionUI] Player chose {element} affinity");
        onAffinitySelected?.Invoke(element);
    }

    private Color GetElementBackgroundColor(Element element)
    {
        switch (element)
        {
            case Element.Fire: return new Color(0.6f, 0.15f, 0.1f, 1f);
            case Element.Ice: return new Color(0.15f, 0.4f, 0.6f, 1f);
            case Element.Water: return new Color(0.1f, 0.25f, 0.5f, 1f);
            case Element.Wind: return new Color(0.2f, 0.5f, 0.25f, 1f);
            case Element.Rock: return new Color(0.4f, 0.3f, 0.2f, 1f);
            default: return new Color(0.3f, 0.3f, 0.3f, 1f);
        }
    }

    private string GetElementDescription(Element element)
    {
        switch (element)
        {
            case Element.Fire: return "Burn your enemies\nwith blazing flames";
            case Element.Ice: return "Freeze foes solid\nwith arctic cold";
            case Element.Water: return "Drown opposition\nwith crushing waves";
            case Element.Wind: return "Slice through all\nwith cutting gales";
            case Element.Rock: return "Crush resistance\nwith earthen might";
            default: return "";
        }
    }

    public bool IsActive() => isActive;
    public bool IsPairMode() => isPairMode;
}
