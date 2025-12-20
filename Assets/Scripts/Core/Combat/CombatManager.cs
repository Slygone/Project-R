using System.Collections.Generic;
using UnityEngine;

public enum CombatType { Normal, Elite, Boss }

public class CombatManager : MonoBehaviour
{
    private List<CombatEnemy> enemies = new List<CombatEnemy>();
    private Player player;
    private CombatUI combatUI;
    private bool isPlayerTurn = true;
    private bool combatActive = false;
    private NodeBase currentNode;
    private CombatType currentCombatType = CombatType.Normal;

    public void StartCombat(CombatNode node, Player playerRef)
    {
        currentCombatType = CombatType.Normal;
        StartCombatInternal(node, playerRef, DataCache.RegularEnemies, Random.Range(1, 4));
    }

    public void StartEliteCombat(NodeBase node, Player playerRef)
    {
        currentCombatType = CombatType.Elite;
        StartCombatInternal(node, playerRef, DataCache.EliteEnemies, 1);
    }

    public void StartBossCombat(Player playerRef, System.Action<bool> onBossComplete)
    {
        currentCombatType = CombatType.Boss;
        onBossCombatComplete = onBossComplete;
        currentNode = null;
        
        if (combatUI == null)
        {
            combatUI = FindFirstObjectByType<CombatUI>();
        }

        player = playerRef;
        enemies.Clear();

        if (DataCache.BossEnemies.Count > 0)
        {
            var bossData = DataCache.BossEnemies[Random.Range(0, DataCache.BossEnemies.Count)];
            enemies.Add(new CombatEnemy(bossData));
        }

        combatActive = true;
        isPlayerTurn = true;

        Debug.Log($"[CombatManager] BOSS FIGHT started: {enemies[0].Name}");

        if (combatUI != null)
        {
            combatUI.ShowCombat(enemies, player, "BOSS FIGHT!");
        }
    }

    private System.Action<bool> onBossCombatComplete;

    private void StartCombatInternal(NodeBase node, Player playerRef, List<EnemyData> enemyPool, int enemyCount)
    {
        if (combatUI == null)
        {
            combatUI = FindFirstObjectByType<CombatUI>();
        }

        currentNode = node;
        player = playerRef;
        enemies.Clear();

        if (enemyPool == null || enemyPool.Count == 0)
        {
            enemyPool = DataCache.RegularEnemies;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            int randomIndex = Random.Range(0, enemyPool.Count);
            var enemyData = enemyPool[randomIndex];
            enemies.Add(new CombatEnemy(enemyData));
        }

        combatActive = true;
        isPlayerTurn = true;

        string combatTypeLabel = currentCombatType == CombatType.Elite ? "ELITE " : "";
        Debug.Log($"[CombatManager] {combatTypeLabel}Combat started with {enemies.Count} enemies");

        if (combatUI != null)
        {
            string title = currentCombatType == CombatType.Elite ? "ELITE ENCOUNTER!" : null;
            combatUI.ShowCombat(enemies, player, title);
        }
        else
        {
            Debug.LogError("[CombatManager] CombatUI is null - cannot show combat screen");
        }
    }

    public void OnPlayerAttack()
    {
        var target = GetFirstAliveEnemy();
        if (target != null)
        {
            OnPlayerAttackTarget(target);
        }
    }

    public void OnPlayerAttackTarget(CombatEnemy target)
    {
        if (!combatActive || !isPlayerTurn) return;
        if (target == null || !target.IsAlive()) return;

        Element attackElement = player.GetAffinity();
        int baseDamage = player.GetTotalDamage();
        bool isCrit = false;
        
        if (Random.Range(0, 100) < player.GetCritChance())
        {
            baseDamage = Mathf.RoundToInt(baseDamage * player.GetCritDamage());
            isCrit = true;
        }

        int damage = target.ApplyResistance(baseDamage, attackElement);
        int resistPercent = target.CalculateResistance(attackElement);
        
        target.TakeDamage(damage);
        string critText = isCrit ? " (CRIT!)" : "";
        string resistText = resistPercent > 0 ? $" ({resistPercent}% resisted)" : "";
        Debug.Log($"[CombatManager] Player dealt {damage} {attackElement} damage to {target.Name}{critText}{resistText}. Enemy HP: {target.Health}/{target.MaxHealth}");

        if (combatUI != null)
        {
            combatUI.ShowDamageToEnemy(target, damage);
            combatUI.UpdateEnemyHealth(target);
        }

        if (!target.IsAlive())
        {
            Debug.Log($"[CombatManager] {target.Name} defeated!");
        }

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }

        isPlayerTurn = false;
        EnemyTurn();
    }

    public void OnPlayerSkill(int skillNumber)
    {
        var target = GetFirstAliveEnemy();
        if (target != null)
        {
            OnPlayerSkillTarget(skillNumber, target);
        }
    }

    public void OnPlayerSkillTarget(int skillNumber, CombatEnemy target)
    {
        if (!combatActive || !isPlayerTurn) return;
        if (target == null || !target.IsAlive()) return;

        var character = player.GetCharacter();
        if (character == null)
        {
            Debug.Log("[CombatManager] No character selected");
            return;
        }

        string skillName = skillNumber switch
        {
            1 => character.Skill1,
            2 => character.Skill2,
            3 => character.Skill3,
            _ => "Unknown"
        };

        int damage = player.GetTotalDamage();
        string effectText = "";

        switch (skillName.ToLower())
        {
            case "slash":
                damage = Mathf.RoundToInt(damage * 1.0f);
                effectText = "slashes";
                break;
            case "riposte":
                damage = Mathf.RoundToInt(damage * 0.8f);
                effectText = "ripostes";
                break;
            case "bladestorm":
                damage = Mathf.RoundToInt(damage * 1.5f);
                effectText = "unleashes bladestorm on";
                DamageAllEnemies(Mathf.RoundToInt(damage * 0.5f));
                break;
            case "bolt":
                damage = Mathf.RoundToInt(damage * 0.9f);
                effectText = "fires a bolt at";
                break;
            case "ray":
                damage = Mathf.RoundToInt(damage * 1.2f);
                effectText = "fires a ray at";
                break;
            case "meteor":
                damage = Mathf.RoundToInt(damage * 2.0f);
                effectText = "calls down a meteor on";
                break;
            case "aimedshot":
                damage = Mathf.RoundToInt(damage * 1.3f);
                effectText = "takes an aimed shot at";
                break;
            case "tripleshot":
                damage = Mathf.RoundToInt(damage * 0.5f);
                effectText = "fires triple shot at";
                DamageAllEnemies(damage);
                break;
            case "doubleup":
                damage = Mathf.RoundToInt(damage * 2.0f);
                effectText = "doubles up on";
                break;
            case "dirtystab":
                damage = Mathf.RoundToInt(damage * 1.1f);
                effectText = "dirty stabs";
                break;
            case "cheapshot":
                damage = Mathf.RoundToInt(damage * 0.7f);
                effectText = "cheap shots";
                break;
            case "ambush":
                damage = Mathf.RoundToInt(damage * 1.8f);
                effectText = "ambushes";
                break;
            case "shock":
                damage = Mathf.RoundToInt(damage * 0.9f);
                effectText = "shocks";
                break;
            case "judgement":
                damage = Mathf.RoundToInt(damage * 1.4f);
                effectText = "judges";
                break;
            case "holynova":
                damage = Mathf.RoundToInt(damage * 1.2f);
                effectText = "unleashes holy nova on";
                DamageAllEnemies(damage);
                break;
            default:
                effectText = $"uses {skillName} on";
                break;
        }

        Element attackElement = player.GetAffinity();
        
        var (critDamage, isCrit) = player.CalculateDamageWithCrit(damage);
        int damageAfterCrit = critDamage;
        
        int finalDamage = target.ApplyResistance(damageAfterCrit, attackElement);
        int resistPercent = target.CalculateResistance(attackElement);
        
        target.TakeDamage(finalDamage);
        string critText = isCrit ? " <color=yellow>CRIT!</color>" : "";
        string resistText = resistPercent > 0 ? $" ({resistPercent}% resisted)" : "";
        Debug.Log($"[CombatManager] Player {effectText} {target.Name} for {finalDamage} damage! ({skillName}){critText}{resistText}");

        if (combatUI != null)
        {
            combatUI.ShowDamageToEnemy(target, finalDamage, isCrit);
            combatUI.UpdateEnemyHealth(target);
        }

        if (!target.IsAlive())
        {
            Debug.Log($"[CombatManager] {target.Name} defeated!");
        }

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }

        isPlayerTurn = false;
        EnemyTurn();
    }

    private void DamageAllEnemies(int damage)
    {
        Element attackElement = player.GetAffinity();
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive())
            {
                int finalDamage = enemy.ApplyResistance(damage, attackElement);
                enemy.TakeDamage(finalDamage);
                if (combatUI != null)
                {
                    combatUI.UpdateEnemyHealth(enemy);
                }
            }
        }
    }

    private void EnemyTurn()
    {
        int totalDamage = 0;

        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive())
            {
                int baseDamage = enemy.Damage;
                int finalDamage = player.ApplyResistance(baseDamage, enemy.Affinity);
                totalDamage += finalDamage;
                
                string elementText = enemy.Affinity != Element.None ? $" ({enemy.Affinity})" : "";
                Debug.Log($"[CombatManager] {enemy.Name}{elementText} attacks for {finalDamage} damage (base: {baseDamage})");
            }
        }

        if (totalDamage > 0)
        {
            player.TakeDamage(totalDamage);

            if (combatUI != null)
            {
                combatUI.ShowDamageToPlayer(totalDamage);
                combatUI.UpdatePlayerHealth(player);
            }
        }

        if (!player.IsAlive())
        {
            EndCombat(false);
            return;
        }

        isPlayerTurn = true;

        if (combatUI != null)
        {
            combatUI.SetPlayerTurn(true);
        }
    }

    private CombatEnemy GetFirstAliveEnemy()
    {
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive()) return enemy;
        }
        return null;
    }

    private bool AllEnemiesDead()
    {
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive()) return false;
        }
        return true;
    }

    private void EndCombat(bool victory)
    {
        combatActive = false;
        Debug.Log($"[CombatManager] Combat ended. Victory: {victory}, Type: {currentCombatType}");

        if (currentCombatType == CombatType.Boss)
        {
            if (combatUI != null)
            {
                combatUI.HideCombat();
            }
            onBossCombatComplete?.Invoke(victory);
            onBossCombatComplete = null;
            return;
        }

        if (victory)
        {
            int goldReward = Random.Range(1, 11);
            
            if (currentCombatType == CombatType.Elite)
            {
                goldReward *= 2;
            }
            
            var availableRelics = DataCache.Relics;
            RelicData relicReward = availableRelics[Random.Range(0, availableRelics.Count)];

            if (combatUI != null)
            {
                string lootTitle = currentCombatType == CombatType.Elite ? "ELITE VICTORY!\n(Double Rewards)" : null;
                combatUI.ShowLootPanel(goldReward, relicReward, player, currentNode, lootTitle);
            }
        }
        else
        {
            if (combatUI != null)
            {
                combatUI.HideCombat();
            }
            Debug.Log("[CombatManager] Player defeated!");
        }
    }

    public void OnLootCollected()
    {
        if (combatUI != null)
        {
            combatUI.HideCombat();
        }

        if (currentNode != null)
        {
            currentNode.OnNodeCompleted();
        }

        currentNode = null;
    }

    public bool IsCombatActive() => combatActive;
    public bool IsInCombat() => combatActive;
    public bool IsPlayerTurn() => isPlayerTurn;
    public List<CombatEnemy> GetEnemies() => enemies;
}
