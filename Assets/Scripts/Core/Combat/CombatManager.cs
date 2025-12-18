using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    private List<CombatEnemy> enemies = new List<CombatEnemy>();
    private Player player;
    private CombatUI combatUI;
    private bool isPlayerTurn = true;
    private bool combatActive = false;
    private CombatNode currentNode;

    public void StartCombat(CombatNode node, Player playerRef)
    {
        if (combatUI == null)
        {
            combatUI = FindFirstObjectByType<CombatUI>();
        }

        currentNode = node;
        player = playerRef;
        enemies.Clear();

        int enemyCount = Random.Range(1, 4);
        var availableEnemies = DataCache.Enemies;

        for (int i = 0; i < enemyCount; i++)
        {
            int randomIndex = Random.Range(0, availableEnemies.Count);
            var enemyData = availableEnemies[randomIndex];
            enemies.Add(new CombatEnemy(enemyData));
        }

        combatActive = true;
        isPlayerTurn = true;

        Debug.Log($"[CombatManager] Combat started with {enemies.Count} enemies");

        if (combatUI != null)
        {
            Debug.Log("[CombatManager] Showing combat UI");
            combatUI.ShowCombat(enemies, player);
        }
        else
        {
            Debug.LogError("[CombatManager] CombatUI is null - cannot show combat screen");
        }
    }

    public void OnPlayerAttack()
    {
        if (!combatActive || !isPlayerTurn) return;

        var target = GetFirstAliveEnemy();
        if (target == null) return;

        Element attackElement = player.GetAffinity();
        int damage = player.GetTotalDamage();
        bool isCrit = false;
        
        if (Random.Range(0, 100) < player.GetCritChance())
        {
            damage = Mathf.RoundToInt(damage * player.GetCritDamage());
            isCrit = true;
        }

        target.TakeDamage(damage);
        string critText = isCrit ? " (CRIT!)" : "";
        Debug.Log($"[CombatManager] Player dealt {damage} {attackElement} damage to {target.Name}{critText}. Enemy HP: {target.Health}/{target.MaxHealth}");

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
        if (!combatActive || !isPlayerTurn) return;

        var weapon = player.GetWeapon();
        if (weapon == null)
        {
            Debug.Log("[CombatManager] No weapon equipped");
            return;
        }

        string skillName = skillNumber switch
        {
            1 => weapon.Skill1,
            2 => weapon.Skill2,
            3 => weapon.Skill3,
            _ => "Unknown"
        };

        var target = GetFirstAliveEnemy();
        if (target == null) return;

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

        target.TakeDamage(damage);
        Debug.Log($"[CombatManager] Player {effectText} {target.Name} for {damage} damage! ({skillName})");

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

    private void DamageAllEnemies(int damage)
    {
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive())
            {
                enemy.TakeDamage(damage);
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
                totalDamage += enemy.Damage;
                Debug.Log($"[CombatManager] {enemy.Name} attacks for {enemy.Damage} damage");
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
        Debug.Log($"[CombatManager] Combat ended. Victory: {victory}");

        if (victory)
        {
            int goldReward = Random.Range(1, 11);
            var availableRelics = DataCache.Relics;
            RelicData relicReward = availableRelics[Random.Range(0, availableRelics.Count)];

            if (combatUI != null)
            {
                combatUI.ShowLootPanel(goldReward, relicReward, player, currentNode);
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
    public bool IsPlayerTurn() => isPlayerTurn;
    public List<CombatEnemy> GetEnemies() => enemies;
}
