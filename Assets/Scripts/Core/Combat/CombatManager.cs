using System.Collections.Generic;
using UnityEngine;

public enum CombatType { Normal, Elite, Boss }

public class CombatManager : MonoBehaviour
{
    private List<CombatEnemy> enemies = new List<CombatEnemy>();
    private Player player;
    private CombatUI combatUI;
    private InfusionUI infusionUI;
    private ReactionQTEPanel qtePanel;
    private bool isPlayerTurn = true;
    private bool combatActive = false;
    private NodeBase currentNode;
    private CombatType currentCombatType = CombatType.Normal;
    
    private CombatEnemy pendingTarget;
    private int pendingSkillNumber;
    private bool pendingIsAttack;
    private Element pendingInfusedElement = Element.None;
    private float pendingReactionMultiplier = 1f;
    private string pendingReactionName = "";
    private Element pendingReactionElementA = Element.None;
    private Element pendingReactionElementB = Element.None;

    public void StartCombat(CombatNode node, Player playerRef)
    {
        currentCombatType = CombatType.Normal;
        StartCombatInternal(node, playerRef, DataCache.RegularEnemies, Random.Range(1, 4));
    }

    public void StartEliteCombat(NodeBase node, Player playerRef)
    {
        currentCombatType = CombatType.Elite;
        // World 2 spawns 2 elites instead of 1
        int eliteCount = GameManager.CurrentWorld >= 2 ? 2 : 1;
        StartCombatInternal(node, playerRef, DataCache.EliteEnemies, eliteCount);
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

        int world = GameManager.CurrentWorld;
        int bossCount = world >= 2 ? 2 : 1;
        
        if (DataCache.BossEnemies.Count > 0)
        {
            for (int i = 0; i < bossCount; i++)
            {
                var bossData = DataCache.BossEnemies[Random.Range(0, DataCache.BossEnemies.Count)];
                enemies.Add(new CombatEnemy(bossData, world));
            }
        }

        combatActive = true;
        isPlayerTurn = true;

        string bossNames = string.Join(" & ", enemies.ConvertAll(e => e.Name));
        Debug.Log($"[CombatManager] BOSS FIGHT started: {bossNames} (World {world})");

        string title = world >= 2 ? "FINAL BOSS FIGHT!" : "BOSS FIGHT!";
        if (combatUI != null)
        {
            combatUI.ShowCombat(enemies, player, title);
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

        int world = GameManager.CurrentWorld;
        for (int i = 0; i < enemyCount; i++)
        {
            int randomIndex = Random.Range(0, enemyPool.Count);
            var enemyData = enemyPool[randomIndex];
            enemies.Add(new CombatEnemy(enemyData, world));
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

        if (player.HasElementPair())
        {
            if (player.HasReactionReady())
            {
                TriggerReactionAction(target, 0, true);
            }
            else
            {
                RequestInfusion(target, 0, true);
            }
        }
        else
        {
            ExecuteAttack(target);
        }
    }
    
    private void RequestInfusion(CombatEnemy target, int skillNumber, bool isAttack)
    {
        pendingTarget = target;
        pendingSkillNumber = skillNumber;
        pendingIsAttack = isAttack;
        
        if (infusionUI == null)
        {
            infusionUI = FindFirstObjectByType<InfusionUI>();
        }
        
        if (infusionUI != null)
        {
            string actionName = isAttack ? "Attack" : GetSkillName(skillNumber);
            infusionUI.Show(player, actionName, OnInfusionSelected);
        }
        else
        {
            Debug.LogWarning("[CombatManager] InfusionUI not found, executing without infusion");
            if (isAttack)
                ExecuteAttack(target);
            else
                ExecuteSkill(skillNumber, target);
        }
    }
    
    private string GetSkillName(int skillNumber)
    {
        var character = player.GetCharacter();
        if (character == null) return $"Skill {skillNumber}";
        
        return skillNumber switch
        {
            1 => character.Skill1,
            2 => character.Skill2,
            3 => character.Skill3,
            _ => $"Skill {skillNumber}"
        };
    }
    
    private void OnInfusionSelected(bool useOrbA)
    {
        pendingInfusedElement = player.GetInfusedElement(useOrbA);
        player.InfuseOrb(useOrbA);
        
        ExecuteActionWithoutReaction();
    }
    
    private void OnQTEComplete(QTEResult result)
    {
        float qteMultiplier = ReactionQTE.GetQteMultiplier(result);
        
        Debug.Log($"[CombatLog] QTEComplete | Result={result} | QTEMultiplier={qteMultiplier:F1}x");
        
        if (pendingIsAttack)
        {
            ExecuteAttackWithReaction(pendingTarget, pendingReactionMultiplier, qteMultiplier, pendingInfusedElement, result);
        }
        else
        {
            ExecuteSkillWithReaction(pendingSkillNumber, pendingTarget, pendingReactionMultiplier, qteMultiplier, pendingInfusedElement, result);
        }
        
        ClearPendingAction();
    }
    
    private void ExecuteActionWithoutReaction()
    {
        if (pendingIsAttack)
        {
            ExecuteAttack(pendingTarget, 1f, pendingInfusedElement);
        }
        else
        {
            ExecuteSkill(pendingSkillNumber, pendingTarget, 1f, pendingInfusedElement);
        }
        
        ClearPendingAction();
    }
    
    private void ClearPendingAction()
    {
        pendingTarget = null;
        pendingSkillNumber = 0;
        pendingIsAttack = false;
        pendingInfusedElement = Element.None;
        pendingReactionMultiplier = 1f;
        pendingReactionName = "";
        pendingReactionElementA = Element.None;
        pendingReactionElementB = Element.None;
    }
    
    private void TriggerReactionAction(CombatEnemy target, int skillNumber, bool isAttack)
    {
        pendingTarget = target;
        pendingSkillNumber = skillNumber;
        pendingIsAttack = isAttack;
        
        var orbSystem = player.GetOrbSystem();
        pendingReactionElementA = orbSystem.OrbAMark;
        pendingReactionElementB = orbSystem.OrbBMark;
        pendingInfusedElement = orbSystem.DetonatorElement;
        pendingReactionName = ReactionQTE.GetReactionName(pendingReactionElementA, pendingReactionElementB);
        pendingReactionMultiplier = ReactionQTE.GetReactionMultiplier(pendingReactionElementA, pendingReactionElementB);
        
        orbSystem.ClearMarks();
        
        Debug.Log($"[CombatLog] ReactionTriggered | Type={pendingReactionName} | Elements={pendingReactionElementA}+{pendingReactionElementB} | Detonator={pendingInfusedElement} | Multiplier={pendingReactionMultiplier:F2}x");
        
        if (qtePanel == null)
        {
            qtePanel = FindFirstObjectByType<ReactionQTEPanel>();
        }
        
        if (qtePanel != null)
        {
            qtePanel.Show(pendingReactionName, OnQTEComplete);
        }
        else
        {
            Debug.LogWarning("[CombatManager] ReactionQTEPanel not found, executing with Good QTE result");
            OnQTEComplete(QTEResult.Good);
        }
    }
    
    private void ExecuteAttack(CombatEnemy target, float reactionMultiplier = 1f, Element? forcedElement = null)
    {
        Element attackElement = forcedElement ?? player.GetAffinity();
        int baseDamage = player.GetTotalDamage();
        bool isCrit = false;
        
        if (Random.Range(0, 100) < player.GetCritChance())
        {
            baseDamage = Mathf.RoundToInt(baseDamage * player.GetCritDamage());
            isCrit = true;
        }
        
        baseDamage = Mathf.RoundToInt(baseDamage * reactionMultiplier);

        int damage = target.ApplyResistance(baseDamage, attackElement);
        int resistPercent = target.CalculateResistance(attackElement);
        
        target.TakeDamage(damage);
        string critText = isCrit ? " (CRIT!)" : "";
        string resistText = resistPercent > 0 ? $" ({resistPercent}% resisted)" : "";
        string reactionText = reactionMultiplier > 1f ? $" (REACTION x{reactionMultiplier:F1})" : "";
        Debug.Log($"[CombatManager] Player dealt {damage} {attackElement} damage to {target.Name}{critText}{resistText}{reactionText}. Enemy HP: {target.Health}/{target.MaxHealth}");

        string infusedSrc = "";
        if (player.HasElementPair())
        {
            if (attackElement == player.GetOrbAElement()) infusedSrc = "A";
            else if (attackElement == player.GetOrbBElement()) infusedSrc = "B";
        }
        Debug.Log($"[CombatLog] PlayerAttack | Element={attackElement} | InfusedFrom={infusedSrc} | Damage={damage} | Crit={isCrit} | Reaction={reactionMultiplier:F1}x | Resist={resistPercent}% | Target={target.Name}");

        if (combatUI != null)
        {
            combatUI.ShowDamageToEnemy(target, damage, isCrit);
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
    
    private void ExecuteAttackWithReaction(CombatEnemy target, float reactionMultiplier, float qteMultiplier, Element? forcedElement, QTEResult qteResult)
    {
        Element attackElement = forcedElement ?? player.GetAffinity();
        int characterDamage = player.GetCharacterDamage();
        int elementalBonus = player.HasElementPair() 
            ? player.GetElementalBonus(player.GetOrbAElement()) + player.GetElementalBonus(player.GetOrbBElement())
            : player.GetAffinityBonus();
        
        float damageAfterReaction = (characterDamage + elementalBonus) * reactionMultiplier;
        
        bool isCrit = Random.Range(0, 100) < player.GetCritChance();
        float critMultiplier = isCrit ? player.GetCritDamage() : 1f;
        float damageAfterCrit = damageAfterReaction * critMultiplier;
        
        float damageAfterQTE = damageAfterCrit * qteMultiplier;
        int baseDamageBeforeResist = Mathf.RoundToInt(damageAfterQTE);
        
        int damage = target.ApplyResistance(baseDamageBeforeResist, attackElement);
        int resistPercent = target.CalculateResistance(attackElement);
        
        target.TakeDamage(damage);
        
        string infusedSrc = "";
        if (player.HasElementPair())
        {
            if (attackElement == player.GetOrbAElement()) infusedSrc = "A";
            else if (attackElement == player.GetOrbBElement()) infusedSrc = "B";
        }
        
        Debug.Log($"[CombatLog] PlayerAttackReaction | Reaction={pendingReactionName} | ReactionMult={reactionMultiplier:F2}x | Crit={isCrit} | CritMult={critMultiplier:F1}x | QTE={qteResult} | QTEMult={qteMultiplier:F1}x | FinalDamage={damage} | Element={attackElement} | InfusedFrom={infusedSrc} | Resist={resistPercent}% | Target={target.Name}");
        Debug.Log($"[CombatManager] REACTION ATTACK! {pendingReactionName} ({pendingReactionElementA}+{pendingReactionElementB}) -> {damage} damage to {target.Name} [QTE: {qteResult}]");

        if (combatUI != null)
        {
            combatUI.ShowDamageToEnemy(target, damage, isCrit);
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

        if (player.HasElementPair())
        {
            if (player.HasReactionReady())
            {
                TriggerReactionAction(target, skillNumber, false);
            }
            else
            {
                RequestInfusion(target, skillNumber, false);
            }
        }
        else
        {
            ExecuteSkill(skillNumber, target);
        }
    }
    
    private void ExecuteSkill(int skillNumber, CombatEnemy target, float reactionMultiplier = 1f, Element? forcedElement = null)
    {
        var character = player.GetCharacter();
        if (character == null) return;

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
                DamageAllEnemies(Mathf.RoundToInt(damage * 0.5f), forcedElement);
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
                DamageAllEnemies(damage, forcedElement);
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
                DamageAllEnemies(damage, forcedElement);
                break;
            default:
                effectText = $"uses {skillName} on";
                break;
        }

        Element attackElement = forcedElement ?? player.GetAffinity();
        
        var (critDamage, isCrit) = player.CalculateDamageWithCrit(damage);
        int damageAfterCrit = Mathf.RoundToInt(critDamage * reactionMultiplier);
        
        int finalDamage = target.ApplyResistance(damageAfterCrit, attackElement);
        int resistPercent = target.CalculateResistance(attackElement);
        
        target.TakeDamage(finalDamage);
        string critText = isCrit ? " <color=yellow>CRIT!</color>" : "";
        string resistText = resistPercent > 0 ? $" ({resistPercent}% resisted)" : "";
        string reactionText = reactionMultiplier > 1f ? $" (REACTION x{reactionMultiplier:F1})" : "";
        Debug.Log($"[CombatManager] Player {effectText} {target.Name} for {finalDamage} damage! ({skillName}){critText}{resistText}{reactionText}");

        string infusedSrcSkill = "";
        if (player.HasElementPair())
        {
            if (attackElement == player.GetOrbAElement()) infusedSrcSkill = "A";
            else if (attackElement == player.GetOrbBElement()) infusedSrcSkill = "B";
        }
        Debug.Log($"[CombatLog] PlayerSkill | Skill={skillName} | Element={attackElement} | InfusedFrom={infusedSrcSkill} | Damage={finalDamage} | Crit={isCrit} | Reaction={reactionMultiplier:F1}x | Resist={resistPercent}% | Target={target.Name}");

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
    
    private void ExecuteSkillWithReaction(int skillNumber, CombatEnemy target, float reactionMultiplier, float qteMultiplier, Element? forcedElement, QTEResult qteResult)
    {
        var character = player.GetCharacter();
        if (character == null) return;

        string skillName = skillNumber switch
        {
            1 => character.Skill1,
            2 => character.Skill2,
            3 => character.Skill3,
            _ => "Unknown"
        };

        float skillMultiplier = GetSkillMultiplier(skillName);
        
        Element attackElement = forcedElement ?? player.GetAffinity();
        int characterDamage = player.GetCharacterDamage();
        int elementalBonus = player.HasElementPair() 
            ? player.GetElementalBonus(player.GetOrbAElement()) + player.GetElementalBonus(player.GetOrbBElement())
            : player.GetAffinityBonus();
        
        float baseDamage = (characterDamage + elementalBonus) * skillMultiplier;
        float damageAfterReaction = baseDamage * reactionMultiplier;
        
        bool isCrit = Random.Range(0, 100) < player.GetCritChance();
        float critMultiplier = isCrit ? player.GetCritDamage() : 1f;
        float damageAfterCrit = damageAfterReaction * critMultiplier;
        
        float damageAfterQTE = damageAfterCrit * qteMultiplier;
        int baseDamageBeforeResist = Mathf.RoundToInt(damageAfterQTE);
        
        int finalDamage = target.ApplyResistance(baseDamageBeforeResist, attackElement);
        int resistPercent = target.CalculateResistance(attackElement);
        
        target.TakeDamage(finalDamage);
        
        string infusedSrc = "";
        if (player.HasElementPair())
        {
            if (attackElement == player.GetOrbAElement()) infusedSrc = "A";
            else if (attackElement == player.GetOrbBElement()) infusedSrc = "B";
        }
        
        Debug.Log($"[CombatLog] PlayerSkillReaction | Skill={skillName} | Reaction={pendingReactionName} | ReactionMult={reactionMultiplier:F2}x | Crit={isCrit} | CritMult={critMultiplier:F1}x | QTE={qteResult} | QTEMult={qteMultiplier:F1}x | FinalDamage={finalDamage} | Element={attackElement} | InfusedFrom={infusedSrc} | Resist={resistPercent}% | Target={target.Name}");
        Debug.Log($"[CombatManager] REACTION SKILL! {skillName} + {pendingReactionName} ({pendingReactionElementA}+{pendingReactionElementB}) -> {finalDamage} damage to {target.Name} [QTE: {qteResult}]");

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
    
    private float GetSkillMultiplier(string skillName)
    {
        return skillName.ToLower() switch
        {
            "slash" => 1.0f,
            "riposte" => 0.8f,
            "bladestorm" => 1.5f,
            "bolt" => 0.9f,
            "ray" => 1.2f,
            "meteor" => 2.0f,
            "aimedshot" => 1.3f,
            "tripleshot" => 0.5f,
            "doubleup" => 2.0f,
            "dirtystab" => 1.1f,
            "cheapshot" => 0.7f,
            "ambush" => 1.8f,
            "shock" => 0.9f,
            "judgement" => 1.4f,
            "holynova" => 1.2f,
            _ => 1.0f
        };
    }

    private void DamageAllEnemies(int damage, Element? forcedElement = null)
    {
        Element attackElement = forcedElement ?? player.GetAffinity();
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive())
            {
                int finalDamage = enemy.ApplyResistance(damage, attackElement);
                enemy.TakeDamage(finalDamage);
                string infusedSrcAoE = "";
                if (player.HasElementPair())
                {
                    if (attackElement == player.GetOrbAElement()) infusedSrcAoE = "A";
                    else if (attackElement == player.GetOrbBElement()) infusedSrcAoE = "B";
                }
                Debug.Log($"[CombatLog] PlayerAoE | Element={attackElement} | InfusedFrom={infusedSrcAoE} | Damage={finalDamage} | Target={enemy.Name}");
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
                int resistPercent = player.CalculateResistance(enemy.Affinity);
                int finalDamage = player.ApplyResistance(baseDamage, enemy.Affinity);
                totalDamage += finalDamage;
                
                string elementText = enemy.Affinity != Element.None ? $" ({enemy.Affinity})" : "";
                Debug.Log($"[CombatManager] {enemy.Name}{elementText} attacks for {finalDamage} damage (base: {baseDamage})");

                // Detailed, filterable log for enemy attacks
                string resMarker = "";
                if (player.HasElementPair())
                {
                    bool isA = enemy.Affinity == player.GetOrbAElement();
                    bool isB = enemy.Affinity == player.GetOrbBElement();
                    if (isA && isB) resMarker = "A,B"; // edge case
                    else if (isA) resMarker = "A";
                    else if (isB) resMarker = "B";
                }
                else if (player.HasAffinity() && enemy.Affinity == player.GetAffinity())
                {
                    resMarker = "Affinity";
                }

                Debug.Log($"[CombatLog] EnemyAttack | Attacker={enemy.Name} | Element={enemy.Affinity} | Base={baseDamage} | ResistTotal={resistPercent}% | ResistBonusFrom={resMarker} | Final={finalDamage}");
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
                
                var mysteryNode = currentNode as MysteryNode;
                if (mysteryNode != null && mysteryNode.IsEliteFight())
                {
                    mysteryNode.GiveEliteRewards();
                    
                    if (combatUI != null)
                    {
                        combatUI.HideCombat();
                    }
                    
                    if (currentNode != null)
                    {
                        currentNode.OnNodeCompleted();
                    }
                    currentNode = null;
                    return;
                }
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
