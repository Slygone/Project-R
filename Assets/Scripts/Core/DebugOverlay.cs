using UnityEngine;
using TMPro;

public class DebugOverlay : MonoBehaviour
{
    private TextMeshProUGUI debugText;
    private Player player;
    private GameObject overlayPanel;
    private bool isVisible = true;

    void Start()
    {
        CreateOverlay();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            isVisible = !isVisible;
            if (overlayPanel != null)
                overlayPanel.SetActive(isVisible);
        }

        if (isVisible)
            UpdateDebugText();
    }

    private void CreateOverlay()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogWarning("[DebugOverlay] Canvas not found");
            return;
        }

        overlayPanel = new GameObject("DebugOverlay");
        overlayPanel.transform.SetParent(canvas.transform, false);

        var rect = overlayPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(10f, -10f);
        rect.sizeDelta = new Vector2(280f, 140f);

        var bg = overlayPanel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        var textObj = new GameObject("DebugText");
        textObj.transform.SetParent(overlayPanel.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 8f);
        textRect.offsetMax = new Vector2(-8f, -8f);

        debugText = textObj.AddComponent<TextMeshProUGUI>();
        debugText.fontSize = 14;
        debugText.color = Color.white;
        debugText.alignment = TextAlignmentOptions.TopLeft;

        player = FindFirstObjectByType<Player>();
    }

    private void UpdateDebugText()
    {
        if (debugText == null) return;

        if (player == null)
            player = FindFirstObjectByType<Player>();

        string hpText = "HP: --/--";
        string woundsText = "Wounds: --";
        
        if (player != null)
        {
            int currentHP = player.GetHealth();
            int maxHP = player.GetMaxHealth();
            int wounds = player.WoundCount;
            int maxWounds = maxHP / player.ThresholdSize;
            int maxRecoverable = player.GetMaxRecoverableHP();
            
            hpText = $"HP: {currentHP}/{maxHP} (Cap: {maxRecoverable})";
            woundsText = $"Wounds: {wounds}/{maxWounds}";
        }

        string xpText = $"XP: {GameManager.RunXP}";
        string worldText = $"World: {GameManager.CurrentWorld}";
        string qteText = $"Last QTE: {GameManager.LastQTEResult}";

        debugText.text = $"<b>=== DEBUG (F1 to hide) ===</b>\n{hpText}\n{woundsText}\n{xpText}\n{worldText}\n{qteText}";
    }
}
