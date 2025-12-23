using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    private Referencer refs;
    private NodeBase currentNode;
    private TextMeshProUGUI popupBodyText;

    void Start()
    {
        refs = FindFirstObjectByType<Referencer>();
        
        if (refs != null && refs.continueButton != null)
        {
            refs.continueButton.onClick.AddListener(OnContinueClicked);
        }

        EnsurePopupBody();
    }

    private void EnsurePopupBody()
    {
        if (refs == null || refs.nodePopupPanel == null) return;

        var existing = refs.nodePopupPanel.transform.Find("Body");
        if (existing != null)
        {
            popupBodyText = existing.GetComponent<TextMeshProUGUI>();
            return;
        }

        var bodyObj = new GameObject("Body");
        bodyObj.transform.SetParent(refs.nodePopupPanel.transform, false);

        var rect = bodyObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.08f, 0.25f);
        rect.anchorMax = new Vector2(0.92f, 0.78f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        popupBodyText = bodyObj.AddComponent<TextMeshProUGUI>();
        popupBodyText.alignment = TextAlignmentOptions.TopLeft;
        popupBodyText.fontSize = 22;
        popupBodyText.color = Color.white;
        popupBodyText.textWrappingMode = TextWrappingModes.Normal;
        popupBodyText.gameObject.SetActive(false);
    }

    public void ShowNodePopup(string title, NodeBase node)
    {
        currentNode = node;
        
        if (refs == null) return;

        EnsurePopupBody();

        string header = title;
        string body = "";
        if (!string.IsNullOrEmpty(title) && title.Contains("\n"))
        {
            var lines = title.Split('\n');
            if (lines.Length > 0) header = lines[0];

            var sb = new StringBuilder();
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                sb.AppendLine(lines[i]);
            }
            body = sb.ToString().Trim();
        }

        if (!string.IsNullOrEmpty(header) && header.ToLower().Contains("mystery reward"))
        {
            header = "MYSTERY REWARD";
            body = FormatMysteryRewardBody(body);
        }
        
        if (refs.popupTitleText != null)
        {
            refs.popupTitleText.text = header;
        }

        if (popupBodyText != null)
        {
            popupBodyText.text = body;
            popupBodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(body));
        }
        
        if (refs.nodePopupPanel != null)
        {
            refs.nodePopupPanel.SetActive(true);
        }
        
        if (refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }
    }

    private string FormatMysteryRewardBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "";

        var lines = body.Split('\n');
        var sb = new StringBuilder();
        
        sb.AppendLine("<size=90%>");

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // Relic found
            if (line.StartsWith("Found:") || line.Contains("Relic"))
            {
                string item = line.Replace("Found:", "").Trim();
                sb.AppendLine($"<color=#b388ff>★ RELIC</color>");
                sb.AppendLine($"<color=#ffdd55><b>{item}</b></color>");
                sb.AppendLine();
                continue;
            }

            // Gold gained
            if (line.ToLower().Contains("gold"))
            {
                string goldText = line;
                sb.AppendLine($"<color=#ffd700>◆ {goldText}</color>");
                continue;
            }

            // Relic effect description in parentheses
            if (line.StartsWith("(") && line.EndsWith(")"))
            {
                string details = line.Substring(1, line.Length - 2);
                sb.AppendLine($"<color=#88ddaa><i>{details}</i></color>");
                sb.AppendLine();
                continue;
            }

            // XP gained
            if (line.ToLower().Contains("xp"))
            {
                sb.AppendLine($"<color=#55ddff>✦ {line}</color>");
                continue;
            }

            sb.AppendLine(line);
        }
        
        sb.AppendLine("</size>");

        return sb.ToString().Trim();
    }

    private void OnContinueClicked()
    {
        if (refs == null) return;
        
        if (refs.nodePopupPanel != null)
        {
            refs.nodePopupPanel.SetActive(false);
        }

        if (popupBodyText != null)
        {
            popupBodyText.text = "";
            popupBodyText.gameObject.SetActive(false);
        }
        
        if (refs.playerController != null)
        {
            refs.playerController.SetCanMove(true);
        }
        
        if (currentNode != null)
        {
            currentNode.OnNodeCompleted();
            currentNode = null;
        }
    }
}
