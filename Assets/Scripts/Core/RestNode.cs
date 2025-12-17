using UnityEngine;

public class RestNode : NodeBase
{
    protected override void Start()
    {
        nodeType = NodeType.Rest;
        base.Start();
    }

    protected override string GetNodeTitle()
    {
        return "Rest - Heal or Gain +10 Damage";
    }
}
