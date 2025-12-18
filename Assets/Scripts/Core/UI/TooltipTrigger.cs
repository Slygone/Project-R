using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string tooltipText = "";
    private static TooltipUI tooltipUI;

    public void SetTooltip(string text)
    {
        tooltipText = text;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(tooltipText)) return;
        
        if (tooltipUI == null)
        {
            tooltipUI = FindFirstObjectByType<TooltipUI>();
        }

        if (tooltipUI != null)
        {
            tooltipUI.Show(tooltipText, Input.mousePosition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipUI != null)
        {
            tooltipUI.Hide();
        }
    }
}
