using System.Collections.Generic;
using UnityEngine;

public class MysteryNode : NodeBase
{
    private bool isEliteFight = false;
    private MysteryUI mysteryUI;
    private const int NORMAL_GOLD_REWARD = 5;
    private const int ELITE_GOLD_MULTIPLIER = 2;
    
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

        if (mysteryUI == null)
        {
            mysteryUI = FindFirstObjectByType<MysteryUI>();
        }

        if (mysteryUI != null)
        {
            mysteryUI.Show(refs.player, this, OnMysteryCompleted);
        }
        else
        {
            Debug.LogWarning("[MysteryNode] MysteryUI not found, giving random reward");
            GiveRandomUpgradeFromUI();
        }
    }

    public void StartEliteFightFromUI()
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

    public void GiveRandomUpgradeFromUI()
    {
        isEliteFight = false;
        
        var relic = GetRandomRelicByRarity("Common");
        if (relic != null)
        {
            refs.player.AddRelic(relic);
            refs.player.AddGold(NORMAL_GOLD_REWARD);
            
            Debug.Log($"[MysteryNode] Player received random upgrade: {relic.DisplayName} + {NORMAL_GOLD_REWARD} gold");
            
            if (refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
            {
                refs.playerStatsUI.UpdateStats();
            }
            
            if (refs.ui != null)
            {
                refs.ui.ShowNodePopup($"Mystery Reward!\n\nRelic: {relic.DisplayName}\n{relic.Description}\n\n+{NORMAL_GOLD_REWARD} Gold", this);
            }
        }
        else
        {
            refs.player.AddGold(NORMAL_GOLD_REWARD);
            if (refs.ui != null)
            {
                refs.ui.ShowNodePopup($"Mystery Reward!\n\n+{NORMAL_GOLD_REWARD} Gold", this);
            }
        }
    }

    public void GiveEliteRewards()
    {
        int goldReward = NORMAL_GOLD_REWARD * ELITE_GOLD_MULTIPLIER;
        refs.player.AddGold(goldReward);
        
        var relicsGiven = new List<RelicData>();
        
        // Elite rewards: 1 Common + 1 Common/Legendary (50/50)
        var commonRelic = GetRandomRelicByRarity("Common");
        if (commonRelic != null)
        {
            refs.player.AddRelic(commonRelic);
            relicsGiven.Add(commonRelic);
            Debug.Log($"[MysteryNode] Elite reward - Common relic: {commonRelic.DisplayName}");
        }
        
        string bonusRarity = Random.value < 0.5f ? "Legendary" : "Common";
        var bonusRelic = GetRandomRelicByRarity(bonusRarity);
        if (bonusRelic == null) bonusRelic = GetRandomRelicByRarity("Common");
        if (bonusRelic != null)
        {
            refs.player.AddRelic(bonusRelic);
            relicsGiven.Add(bonusRelic);
            Debug.Log($"[MysteryNode] Elite reward - {bonusRelic.Rarity} relic: {bonusRelic.DisplayName}");
        }
        
        if (refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
        {
            refs.playerStatsUI.UpdateStats();
        }
        
        string rewardText = "Elite Victory!\n\n";
        foreach (var relic in relicsGiven)
        {
            rewardText += $"• {relic.DisplayName}\n  {relic.Description}\n";
        }
        rewardText += $"\n+{goldReward} Gold";
        
        Debug.Log($"[MysteryNode] Elite rewards given: {relicsGiven.Count} relics, {goldReward} gold");
    }

    private static RelicData GetRandomRelicByRarity(string rarity)
    {
        if (DataCache.Relics == null || DataCache.Relics.Count == 0) return null;
        
        var pool = new List<RelicData>();
        foreach (var r in DataCache.Relics)
        {
            if (r.Rarity == rarity) pool.Add(r);
        }
        
        if (pool.Count == 0) return null;
        return pool[Random.Range(0, pool.Count)];
    }

    private void OnMysteryCompleted()
    {
        OnNodeCompleted();
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
