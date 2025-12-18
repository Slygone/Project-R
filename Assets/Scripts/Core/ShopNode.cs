using UnityEngine;

public class ShopNode : NodeBase
{
    protected override void Start()
    {
        nodeType = NodeType.Shop;
        base.Start();
    }

    protected override void OnNodeTriggered()
    {
        if (refs == null) return;

        if (refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }

        if (refs.shopUI != null && refs.player != null)
        {
            refs.shopUI.Show(refs.player, this, OnShopClosed);
        }
        else
        {
            Debug.LogError("[ShopNode] ShopUI or Player not found in Referencer");
        }
    }

    private void OnShopClosed()
    {
        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(true);
        }
    }

    protected override string GetNodeTitle()
    {
        return "Shop - Buy Items Here!";
    }
}
