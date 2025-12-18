using UnityEngine;

public class MysteryNode : NodeBase
{
    private bool isEliteFight = false;
    
    protected override void Start()
    {
        nodeType = NodeType.Mystery;
        base.Start();
    }

    protected override void OnNodeTriggered()
    {
        if (refs == null) return;

        if (refs.playerController != null)
        {
            refs.playerController.SetCanMove(false);
        }

        float roll = Random.Range(0f, 1f);
        
        if (roll < 0.5f)
        {
            GiveRandomReward();
        }
        else
        {
            StartEliteFight();
        }
    }

    private void GiveRandomReward()
    {
        bool giveRelic = Random.Range(0, 2) == 0;
        
        if (giveRelic && DataCache.Relics.Count > 0)
        {
            var relic = DataCache.Relics[Random.Range(0, DataCache.Relics.Count)];
            refs.player.AddRelic(relic);
            Debug.Log($"[MysteryNode] Player found a relic: {relic.DisplayName}");
            
            if (refs.ui != null)
            {
                refs.ui.ShowNodePopup($"Mystery Reward!\nFound: {relic.DisplayName}\n({relic.StatAffected} +{relic.Amount})", this);
            }
        }
        else if (DataCache.Potions.Count > 0)
        {
            var potion = DataCache.Potions[Random.Range(0, DataCache.Potions.Count)];
            ApplyPotionEffect(potion);
            Debug.Log($"[MysteryNode] Player found a potion: {potion.DisplayName}");
            
            if (refs.ui != null)
            {
                refs.ui.ShowNodePopup($"Mystery Reward!\nFound: {potion.DisplayName}\n({potion.StatAffected} +{potion.Amount})", this);
            }
        }
    }

    private void ApplyPotionEffect(PotionData potion)
    {
        switch (potion.StatAffected.ToLower())
        {
            case "health":
                refs.player.Heal(potion.Amount);
                break;
            case "damage":
                refs.player.AddBaseDamage(potion.Amount);
                break;
            default:
                Debug.LogWarning($"[MysteryNode] Unknown potion stat: {potion.StatAffected}");
                break;
        }
    }

    private void StartEliteFight()
    {
        isEliteFight = true;
        
        if (refs.combatManager != null)
        {
            refs.combatManager.StartEliteCombat(this, refs.player);
        }
        else
        {
            Debug.LogError("[MysteryNode] CombatManager not found");
            OnNodeCompleted();
        }
    }

    public bool IsEliteFight() => isEliteFight;

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
        return "Mystery - Unknown Event";
    }
}
