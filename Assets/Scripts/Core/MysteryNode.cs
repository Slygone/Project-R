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
        
        if (DataCache.Relics.Count > 0)
        {
            var relic = DataCache.Relics[Random.Range(0, DataCache.Relics.Count)];
            refs.player.AddRelic(relic);
            refs.player.AddGold(NORMAL_GOLD_REWARD);
            
            Debug.Log($"[MysteryNode] Player received random upgrade: {relic.DisplayName} + {NORMAL_GOLD_REWARD} gold");
            
            if (refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
            {
                refs.playerStatsUI.UpdateStats();
            }
            
            if (refs.ui != null)
            {
                refs.ui.ShowNodePopup($"Mystery Reward!\n\nRelic: {relic.DisplayName}\n({relic.StatAffected} +{relic.Amount})\n\n+{NORMAL_GOLD_REWARD} Gold", this);
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
        
        var elementMatchingRelic = GetElementMatchingRelic();
        if (elementMatchingRelic != null)
        {
            refs.player.AddRelic(elementMatchingRelic);
            relicsGiven.Add(elementMatchingRelic);
            Debug.Log($"[MysteryNode] Elite reward - Element matching relic: {elementMatchingRelic.DisplayName}");
        }
        
        if (DataCache.Relics.Count > 0)
        {
            var randomRelic = DataCache.Relics[Random.Range(0, DataCache.Relics.Count)];
            refs.player.AddRelic(randomRelic);
            relicsGiven.Add(randomRelic);
            Debug.Log($"[MysteryNode] Elite reward - Random relic: {randomRelic.DisplayName}");
        }
        
        if (refs.playerStatsUI != null && refs.playerStatsUI.IsOpen())
        {
            refs.playerStatsUI.UpdateStats();
        }
        
        string rewardText = "Elite Victory!\n\n";
        foreach (var relic in relicsGiven)
        {
            rewardText += $"• {relic.DisplayName}\n  ({relic.StatAffected} +{relic.Amount})\n";
        }
        rewardText += $"\n+{goldReward} Gold";
        
        Debug.Log($"[MysteryNode] Elite rewards given: {relicsGiven.Count} relics, {goldReward} gold");
    }

    private RelicData GetElementMatchingRelic()
    {
        if (DataCache.Relics == null || DataCache.Relics.Count == 0) return null;
        
        Element playerElement = Element.None;
        
        if (refs.player.HasAffinity())
        {
            playerElement = refs.player.GetAffinity();
        }
        
        if (playerElement == Element.None)
        {
            return DataCache.Relics[Random.Range(0, DataCache.Relics.Count)];
        }
        
        string elementName = playerElement.ToString().ToLower();
        var matchingRelics = new List<RelicData>();
        
        foreach (var relic in DataCache.Relics)
        {
            if (relic.StatAffected.ToLower().Contains(elementName))
            {
                matchingRelics.Add(relic);
            }
        }
        
        if (matchingRelics.Count > 0)
        {
            return matchingRelics[Random.Range(0, matchingRelics.Count)];
        }
        
        return DataCache.Relics[Random.Range(0, DataCache.Relics.Count)];
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
