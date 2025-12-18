using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    private GameObject tooltipPanel;
    private TextMeshProUGUI tooltipText;
    private RectTransform tooltipRect;
    private Canvas canvas;

    void Awake()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                canvas = canvasObj.GetComponent<Canvas>();
            }
        }

        if (canvas == null)
        {
            Debug.LogError("[TooltipUI] Canvas not found");
            return;
        }

        tooltipPanel = new GameObject("TooltipPanel");
        tooltipPanel.transform.SetParent(canvas.transform, false);

        tooltipRect = tooltipPanel.AddComponent<RectTransform>();
        tooltipRect.pivot = new Vector2(0, 1);
        tooltipRect.sizeDelta = new Vector2(250, 80);

        var bg = tooltipPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        var outline = tooltipPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.4f, 0.5f, 1f);
        outline.effectDistance = new Vector2(2, -2);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(tooltipPanel.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.fontSize = 14;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAlignmentOptions.TopLeft;

        var fitter = tooltipPanel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layoutGroup = tooltipPanel.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(12, 12, 8, 8);
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;

        tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            UpdatePosition(Input.mousePosition);
        }
    }

    public void Show(string text, Vector3 position)
    {
        if (tooltipPanel == null) return;

        tooltipText.text = text;
        tooltipPanel.SetActive(true);
        UpdatePosition(position);
    }

    public void Hide()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    private void UpdatePosition(Vector3 mousePos)
    {
        if (tooltipRect == null || canvas == null) return;

        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            mousePos,
            canvas.worldCamera,
            out pos
        );

        float offsetX = 15;
        float offsetY = -15;

        tooltipRect.anchoredPosition = new Vector2(pos.x + offsetX, pos.y + offsetY);

        Vector3[] corners = new Vector3[4];
        tooltipRect.GetWorldCorners(corners);

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        if (corners[2].x > screenWidth)
        {
            tooltipRect.anchoredPosition = new Vector2(pos.x - tooltipRect.sizeDelta.x - offsetX, pos.y + offsetY);
        }

        if (corners[0].y < 0)
        {
            tooltipRect.anchoredPosition = new Vector2(tooltipRect.anchoredPosition.x, pos.y + tooltipRect.sizeDelta.y + Mathf.Abs(offsetY));
        }
    }
}
