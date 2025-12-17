using UnityEngine;

public class UIManager : MonoBehaviour
{
    private Referencer refs;
    private NodeBase currentNode;

    void Start()
    {
        refs = FindFirstObjectByType<Referencer>();
        
        if (refs != null && refs.continueButton != null)
        {
            refs.continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    public void ShowNodePopup(string title, NodeBase node)
    {
        currentNode = node;
        
        if (refs == null) return;
        
        if (refs.popupTitleText != null)
        {
            refs.popupTitleText.text = title;
        }
        
        if (refs.nodePopupPanel != null)
        {
            refs.nodePopupPanel.SetActive(true);
        }
        
        if (refs.player != null)
        {
            refs.player.SetCanMove(false);
        }
    }

    private void OnContinueClicked()
    {
        if (refs == null) return;
        
        if (refs.nodePopupPanel != null)
        {
            refs.nodePopupPanel.SetActive(false);
        }
        
        if (refs.player != null)
        {
            refs.player.SetCanMove(true);
        }
        
        if (currentNode != null)
        {
            currentNode.OnNodeCompleted();
            currentNode = null;
        }
    }
}
