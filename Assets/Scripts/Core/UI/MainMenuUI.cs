using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main Menu UI with New Run, Continue, Options, Exit buttons.
/// Options submenu includes Clear Save functionality.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private GameObject menuPanel;
    private GameObject optionsPanel;
    private Button continueButton;
    private Action onNewRun;
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
            Debug.LogError("[MainMenuUI] Canvas not found");
            return;
        }
        
        menuPanel = CreateMenuPanel(canvas.transform);
        optionsPanel = CreateOptionsPanel(canvas.transform);
        
        menuPanel.SetActive(false);
        optionsPanel.SetActive(false);
    }
    
    private GameObject CreateMenuPanel(Transform parent)
    {
        var panel = new GameObject("MainMenuPanel");
        panel.transform.SetParent(parent, false);
        
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Background
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.03f, 0.08f, 1f);
        
        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.7f);
        titleRect.anchorMax = new Vector2(1, 0.9f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "PROJECT R";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 72;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.2f);
        
        // Button container
        var buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(panel.transform, false);
        var containerRect = buttonContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.35f, 0.2f);
        containerRect.anchorMax = new Vector2(0.65f, 0.6f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        var layout = buttonContainer.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        // New Run button
        CreateMenuButton(buttonContainer.transform, "New Run", OnNewRunClicked, new Color(0.2f, 0.5f, 0.3f));
        
        // Continue button (disabled)
        continueButton = CreateMenuButton(buttonContainer.transform, "Continue", OnContinueClicked, new Color(0.3f, 0.3f, 0.35f));
        continueButton.interactable = false; // No run save system yet
        var continueColors = continueButton.colors;
        continueColors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        continueButton.colors = continueColors;
        
        // Options button
        CreateMenuButton(buttonContainer.transform, "Options", OnOptionsClicked, new Color(0.25f, 0.25f, 0.35f));
        
        // Exit button
        CreateMenuButton(buttonContainer.transform, "Exit", OnExitClicked, new Color(0.4f, 0.2f, 0.2f));
        
        return panel;
    }
    
    private GameObject CreateOptionsPanel(Transform parent)
    {
        var panel = new GameObject("OptionsPanel");
        panel.transform.SetParent(parent, false);
        
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Semi-transparent background
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.06f, 0.98f);
        
        // Title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 0.9f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "OPTIONS";
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontSize = 48;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.2f);
        
        // Options container
        var optionsContainer = new GameObject("OptionsContainer");
        optionsContainer.transform.SetParent(panel.transform, false);
        var containerRect = optionsContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.3f, 0.3f);
        containerRect.anchorMax = new Vector2(0.7f, 0.75f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        var layout = optionsContainer.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        // Clear Save button (red/dangerous color)
        CreateMenuButton(optionsContainer.transform, "Clear All Progress", OnClearSaveClicked, new Color(0.6f, 0.15f, 0.15f));
        
        // Spacer
        var spacer = new GameObject("Spacer");
        spacer.transform.SetParent(optionsContainer.transform, false);
        var spacerLayout = spacer.AddComponent<LayoutElement>();
        spacerLayout.preferredHeight = 30;
        
        // Back button
        CreateMenuButton(optionsContainer.transform, "Back", OnOptionsBackClicked, new Color(0.3f, 0.3f, 0.35f));
        
        return panel;
    }
    
    private Button CreateMenuButton(Transform parent, string label, Action onClick, Color bgColor)
    {
        var btnObj = new GameObject($"Btn_{label}");
        btnObj.transform.SetParent(parent, false);
        
        var layoutElement = btnObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 60;
        
        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = bgColor;
        
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        
        var colors = btn.colors;
        colors.highlightedColor = new Color(bgColor.r + 0.1f, bgColor.g + 0.1f, bgColor.b + 0.1f, 1f);
        colors.pressedColor = new Color(bgColor.r - 0.05f, bgColor.g - 0.05f, bgColor.b - 0.05f, 1f);
        btn.colors = colors;
        
        btn.onClick.AddListener(() => onClick?.Invoke());
        
        var textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = label.ToUpper();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 28;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        
        return btn;
    }
    
    public void Show(Action newRunCallback)
    {
        onNewRun = newRunCallback;
        menuPanel.SetActive(true);
        optionsPanel.SetActive(false);
        isActive = true;
    }
    
    public void Hide()
    {
        menuPanel.SetActive(false);
        optionsPanel.SetActive(false);
        isActive = false;
    }
    
    private void OnNewRunClicked()
    {
        Debug.Log("[MainMenuUI] New Run clicked");
        Hide();
        onNewRun?.Invoke();
    }
    
    private void OnContinueClicked()
    {
        // Not implemented - no run save system yet
        Debug.Log("[MainMenuUI] Continue clicked (not implemented)");
    }
    
    private void OnOptionsClicked()
    {
        Debug.Log("[MainMenuUI] Options clicked");
        menuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }
    
    private void OnOptionsBackClicked()
    {
        Debug.Log("[MainMenuUI] Options Back clicked");
        optionsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }
    
    private void OnClearSaveClicked()
    {
        Debug.Log("[MainMenuUI] Clear Save clicked");
        
        // Clear all meta progression
        if (MetaProgressionManager.Instance != null)
        {
            MetaProgressionManager.Instance.ClearAllProgress();
        }
        
        // Show confirmation feedback by briefly changing button color
        // (In a full implementation, you'd show a confirmation dialog first)
        Debug.Log("[MainMenuUI] All progress cleared! Restart recommended.");
        
        // Return to main menu
        optionsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }
    
    private void OnExitClicked()
    {
        Debug.Log("[MainMenuUI] Exit clicked");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    public bool IsActive() => isActive;
}
