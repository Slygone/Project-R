using UnityEngine;

public class ShopNode : NodeBase
{
    protected override void Start()
    {
        nodeType = NodeType.Shop;
        base.Start();
    }

    protected override string GetNodeTitle()
    {
        return "Shop - Buy Items Here!";
    }
}
