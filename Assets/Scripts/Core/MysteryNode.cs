using UnityEngine;

public class MysteryNode : NodeBase
{
    protected override void Start()
    {
        nodeType = NodeType.Mystery;
        base.Start();
    }

    protected override string GetNodeTitle()
    {
        return "Mystery - Unknown Event";
    }
}
