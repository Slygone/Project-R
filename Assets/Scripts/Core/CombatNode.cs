using UnityEngine;

public class CombatNode : NodeBase
{
    private CombatManager combatManager;

    protected override void Start()
    {
        nodeType = NodeType.Combat;
        base.Start();
        combatManager = FindFirstObjectByType<CombatManager>();
    }

    protected override void OnNodeTriggered()
    {
        if (refs == null) return;

        if (refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }

        if (combatManager != null && refs.player != null)
        {
            combatManager.StartCombat(this, refs.player);
        }
        else
        {
            Debug.LogError("[CombatNode] CombatManager or Player not found");
        }
    }

    public override void OnNodeCompleted()
    {
        base.OnNodeCompleted();

        if (refs != null && refs.playerController != null)
        {
            refs.playerController.SetCanMove(true);
        }
    }

    protected override string GetNodeTitle()
    {
        return "Combat - Fight an Enemy!";
    }
}
