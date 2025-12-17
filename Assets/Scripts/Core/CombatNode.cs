using UnityEngine;

public class CombatNode : NodeBase
{
    protected override void Start()
    {
        nodeType = NodeType.Combat;
        base.Start();
    }

    protected override string GetNodeTitle()
    {
        return "Combat - Fight an Enemy!";
    }
}
