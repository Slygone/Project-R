using UnityEngine;

public class RestNode : NodeBase
{
    protected override void Start()
    {
        nodeType = NodeType.Rest;
        base.Start();
    }

    protected override void OnNodeTriggered()
    {
        if (refs == null) return;

        if (refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }

        if (refs.restUI != null && refs.player != null)
        {
            refs.restUI.Show(refs.player, this, OnRestClosed);
        }
        else
        {
            Debug.LogError("[RestNode] RestUI or Player not found");
            OnNodeCompleted();
        }
    }

    private void OnRestClosed()
    {
        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(true);
        }
    }

    protected override string GetNodeTitle()
    {
        return "Rest Site";
    }
}
