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
        // Ensure DataCache is loaded
        if (!DataCache.IsLoaded)
        {
            DataCache.LoadAll();
        }
        
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
        
        if (enemyPool == null || enemyPool.Count == 0)
        {
            Debug.LogError("[CombatManager] No enemies loaded from DataCache!");
            return;
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
        
        // Reset player combat state (energy to 0, cooldowns cleared)
        player.ResetCombatState();

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
        
        // Update debug overlay
        GameManager.SetLastQTEResult($"Reaction: {result}");
        
        Debug.Log($"[CombatLog] ReactionQTEComplete | Result={result} | QTEMultiplier={qteMultiplier:F1}x");
        
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
        
        // Show reaction floating text on target
        if (combatUI != null && pendingTarget != null)
        {
            combatUI.ShowReactionToEnemy(pendingTarget, pendingReactionName, pendingReactionMultiplier);
        }
        
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
        
        // Player Attack Order:
        // 1. Base damage (character damage + elemental bonuses)
        int baseDamage = player.GetTotalDamage();
        
        // 2. Apply variance (0.90-1.10)
        int afterVariance = player.ApplyVariance(baseDamage);
        
        // 3. No OQTE for basic attack without reaction
        
        // 4. Apply crit
        bool isCrit = Random.Range(0, 100) < player.GetCritChance();
        int afterCrit = isCrit ? Mathf.RoundToInt(afterVariance * player.GetCritDamage()) : afterVariance;
        
        // 5. Apply reaction multiplier
        int totalDamage = Mathf.RoundToInt(afterCrit * reactionMultiplier);

        // 6. Apply enemy resistance
        int damage = target.ApplyResistance(totalDamage, attackElement);
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
        Debug.Log($"[CombatLog] PlayerAttack | Element={attackElement} | InfusedFrom={infusedSrc} | Base={baseDamage} | Variance={afterVariance} | Crit={isCrit} | AfterCrit={afterCrit} | Reaction={reactionMultiplier:F1}x | Resist={resistPercent}% | Final={damage} | Target={target.Name}");

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
        StartCoroutine(DelayedEnemyTurn());
    }
    
    private void ExecuteAttackWithReaction(CombatEnemy target, float reactionMultiplier, float qteMultiplier, Element? forcedElement, QTEResult qteResult)
    {
        Element attackElement = forcedElement ?? player.GetAffinity();
        
        // Player Attack Order with Reaction:
        // 1. Base damage (character damage + elemental bonuses)
        int baseDamage = player.GetTotalDamage();
        
        // 2. Apply variance (0.90-1.10)
        int afterVariance = player.ApplyVariance(baseDamage);
        
        // 3. Apply Offensive QTE multiplier
        float afterOQTE = afterVariance * qteMultiplier;
        
        // 4. Apply crit (direct hit only)
        bool isCrit = Random.Range(0, 100) < player.GetCritChance();
        float critMultiplier = isCrit ? player.GetCritDamage() : 1f;
        float directAfterCrit = afterOQTE * critMultiplier;
        
        // 5. Calculate reaction damage using afterOQTE (pre-crit, so crit doesn't inflate reactions)
        float reactionDamage = afterOQTE * (reactionMultiplier - 1f); // Only the bonus from reaction
        
        // 6. Total = DirectAfterCrit + ReactionDamage
        float totalDamage = directAfterCrit + reactionDamage;
        int damageBeforeResist = Mathf.RoundToInt(totalDamage);
        
        // 7. Apply enemy resistance
        int damage = target.ApplyResistance(damageBeforeResist, attackElement);
        int resistPercent = target.CalculateResistance(attackElement);
        
        target.TakeDamage(damage);
        
        string infusedSrc = "";
        if (player.HasElementPair())
        {
            if (attackElement == player.GetOrbAElement()) infusedSrc = "A";
            else if (attackElement == player.GetOrbBElement()) infusedSrc = "B";
        }
        
        Debug.Log($"[CombatLog] PlayerAttackReaction | Reaction={pendingReactionName} | Base={baseDamage} | Variance={afterVariance} | OQTE={afterOQTE:F0} | Crit={isCrit} | CritMult={critMultiplier:F1}x | ReactionBonus={reactionDamage:F0} | FinalDamage={damage} | Element={attackElement} | InfusedFrom={infusedSrc} | Resist={resistPercent}% | Target={target.Name}");
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
        StartCoroutine(DelayedEnemyTurn());
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
        
        // Check cooldown (skillNumber is 1-indexed, array is 0-indexed)
        int skillIndex = skillNumber - 1;
        if (player.IsSkillOnCooldown(skillIndex))
        {
            Debug.Log($"[CombatManager] Skill {skillNumber} is on cooldown ({player.GetSkillCooldown(skillIndex)} turns remaining)");
            return;
        }
        
        // Check energy for Skill 3
        if (skillNumber == 3 && !player.CanUseSkill3())
        {
            Debug.Log($"[CombatManager] Not enough energy for Skill 3. Need {player.GetSkill3EnergyCost()}, have {player.GetEnergy()}");
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

        // Get skill info from character data
        string skillName;
        float skillDamagePercent;
        string skillEffect;
        
        switch (skillNumber)
        {
            case 1:
                skillName = character.Skill1;
                skillDamagePercent = character.Skill1DamagePercent;
                skillEffect = character.Skill1Effect;
                break;
            case 2:
                skillName = character.Skill2;
                skillDamagePercent = character.Skill2DamagePercent;
                skillEffect = character.Skill2Effect;
                break;
            case 3:
                skillName = character.Skill3;
                skillDamagePercent = character.Skill3DamagePercent;
                skillEffect = character.Skill3Effect;
                break;
            default:
                skillName = "Unknown";
                skillDamagePercent = 100f;
                skillEffect = "null";
                break;
        }
        
        string skillLower = skillName.ToLower();
        Element attackElement = forcedElement ?? player.GetAffinity();
        
        // Special handling for skills with unique mechanics
        int hitCount = 1;
        bool isAoE = false;
        int tempCritBonus = 0;
        float tempCritDmgBonus = 0f;
        float dirtyStabBonus = 1f;
        
        // Parse skill effects
        if (skillLower == "tripleshot")
        {
            hitCount = 3;
        }
        else if (skillLower == "aimedshot")
        {
            tempCritBonus = 20;
            tempCritDmgBonus = 0.2f;
        }
        else if (skillLower == "dirtystab")
        {
            int stacks = player.GetDirtyStabStacks();
            if (stacks > 0)
            {
                dirtyStabBonus = 1f + (Mathf.Min(stacks, 2) * 0.2f);
                Debug.Log($"[CombatManager] DirtyStab bonus: {dirtyStabBonus:F1}x from {stacks} stacks");
            }
        }
        else if (skillEffect != null && skillEffect.ToLower().Contains("aoe"))
        {
            isAoE = true;
        }
        
        // Calculate base damage from CSV percent
        float skillMultiplier = skillDamagePercent / 100f;
        int baseDamage = Mathf.RoundToInt(player.GetCharacterDamage() * skillMultiplier * dirtyStabBonus);
        
        // Add elemental bonuses
        int elementalBonus = player.HasElementPair() 
            ? player.GetElementalBonus(player.GetOrbAElement()) + player.GetElementalBonus(player.GetOrbBElement())
            : player.GetAffinityBonus();
        baseDamage += Mathf.RoundToInt(elementalBonus * skillMultiplier);
        
        int totalDamageDealt = 0;
        
        // Execute hits
        for (int hit = 0; hit < hitCount; hit++)
        {
            if (!target.IsAlive()) break;
            
            // Apply variance per hit
            int damage = player.ApplyVariance(baseDamage);
            
            // Apply crit with temp bonuses
            int effectiveCritChance = player.GetCritChance() + tempCritBonus;
            float effectiveCritDmg = player.GetCritDamage() + tempCritDmgBonus;
            bool isCrit = Random.Range(0, 100) < effectiveCritChance;
            
            int afterCrit = isCrit ? Mathf.RoundToInt(damage * effectiveCritDmg) : damage;
            int afterReaction = Mathf.RoundToInt(afterCrit * reactionMultiplier);
            int finalDamage = target.ApplyResistance(afterReaction, attackElement);
            
            if (isAoE)
            {
                DamageAllEnemies(finalDamage, forcedElement, isCrit);
                // Skip showing damage to primary target since DamageAllEnemies shows it for all
            }
            else
            {
                target.TakeDamage(finalDamage);
                if (combatUI != null)
                {
                    combatUI.ShowDamageToEnemy(target, finalDamage, isCrit);
                }
            }
            
            totalDamageDealt += finalDamage;
            
            string hitLabel = hitCount > 1 ? $" (hit {hit + 1}/{hitCount})" : "";
            Debug.Log($"[CombatManager] {skillName} deals {finalDamage} damage{hitLabel}{(isCrit ? " CRIT!" : "")}");
        }
        
        // Apply skill-specific effects AFTER damage
        ApplySkillEffects(skillLower, skillEffect, target, totalDamageDealt, attackElement);
        
        if (combatUI != null)
        {
            combatUI.UpdateEnemyHealth(target);
        }
        
        bool targetKilled = !target.IsAlive();
        if (targetKilled)
        {
            Debug.Log($"[CombatManager] {target.Name} defeated!");
            
            // Handle kill effects
            HandleKillEffects(skillLower, target, totalDamageDealt);
        }

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }
        
        // Apply skill cooldown and energy effects
        player.UseSkillAndApplyEffects(skillNumber - 1);
        
        // Track DirtyStab consecutive uses (handles 4-turn CD after 3rd use internally)
        if (skillLower == "dirtystab")
        {
            player.IncrementDirtyStabUse();
        }
        else
        {
            // Reset DirtyStab stacks if using different skill
            player.ResetDirtyStabStacks();
        }
        
        if (combatUI != null)
        {
            combatUI.UpdatePlayerEnergy(player);
            combatUI.UpdateSkillButtons(player);
        }

        isPlayerTurn = false;
        
        // Add combat pacing delay after player action for floating text readability
        StartCoroutine(DelayedEnemyTurn());
    }
    
    private System.Collections.IEnumerator DelayedEnemyTurn()
    {
        // Wait for floating text to be readable
        var fctManager = FloatingTextManager.Instance;
        float delay = fctManager != null ? fctManager.GetPlayerActionPause() : 0.5f;
        yield return new WaitForSeconds(delay);
        
        EnemyTurn();
    }
    
    private void ApplySkillEffects(string skillLower, string skillEffect, CombatEnemy target, int damageDealt, Element attackElement)
    {
        if (string.IsNullOrEmpty(skillEffect) || skillEffect.ToLower() == "null") return;
        
        string effectLower = skillEffect.ToLower();
        
        // Riposte - Block 50% next hit
        if (skillLower == "riposte")
        {
            player.ApplyBlock(50f);
        }
        // Bolt - 30% DoT for 2 turns (stacks up to 3x with 50% carryover)
        else if (skillLower == "bolt" && effectLower.Contains("dot"))
        {
            int dotDamage = Mathf.RoundToInt(damageDealt * 0.30f);
            target.ApplyDoT(dotDamage, 2, "Bolt");
            
            // Show burn status floating text
            if (combatUI != null)
            {
                combatUI.ShowStatusToEnemy(target, "Burn", true);
            }
        }
        // Meteor - Stun all enemies 1 turn
        else if (skillLower == "meteor" && effectLower.Contains("stun"))
        {
            foreach (var enemy in enemies)
            {
                if (enemy.IsAlive())
                {
                    enemy.ApplyStun(1);
                    
                    // Show stun status floating text
                    if (combatUI != null)
                    {
                        combatUI.ShowStatusToEnemy(enemy, "Stunned", true);
                    }
                }
            }
        }
        // CheapShot - Stun 1 turn
        else if (skillLower == "cheapshot" && effectLower.Contains("stun"))
        {
            if (target.IsAlive())
            {
                target.ApplyStun(1);
                
                // Show stun status floating text
                if (combatUI != null)
                {
                    combatUI.ShowStatusToEnemy(target, "Stunned", true);
                }
            }
        }
        // Judgement - 20% lifesteal
        else if (skillLower == "judgement" && effectLower.Contains("heal"))
        {
            int healAmount = Mathf.RoundToInt(damageDealt * 0.20f);
            player.Heal(healAmount);
            Debug.Log($"[CombatManager] Judgement healed player for {healAmount} HP");
            
            // Show heal floating text
            if (combatUI != null)
            {
                combatUI.ShowHealToPlayer(healAmount);
                combatUI.UpdatePlayerHealth(player);
            }
        }
        // HolyNova - Shield = 70% of damage dealt (cap 40% MaxHP)
        else if (skillLower == "holynova" && effectLower.Contains("shield"))
        {
            int shieldAmount = Mathf.RoundToInt(damageDealt * 0.70f);
            player.AddShield(shieldAmount);
            Debug.Log($"[CombatManager] HolyNova granted {shieldAmount} shield");
            
            // Show shield gain floating text
            if (combatUI != null)
            {
                combatUI.ShowShieldToPlayer(shieldAmount, FloatingTextType.ShieldGain);
                combatUI.UpdatePlayerHealth(player);
            }
        }
    }
    
    private void HandleKillEffects(string skillLower, CombatEnemy killedTarget, int damageDealt)
    {
        var character = player.GetCharacter();
        if (character == null) return;
        
        // Ambush - If kill: CD becomes 2 turns and refund 40% energy
        if (skillLower == "ambush")
        {
            // Refund 40% energy
            int refund = Mathf.RoundToInt(character.Skill3EnergyCost * 0.40f);
            player.GainEnergy(refund);
            
            // Set cooldown to 2 turns instead of normal cooldown
            player.SetSkillCooldown(2, 2);
            
            Debug.Log($"[CombatManager] Ambush kill! Refunded {refund} energy, CD reduced to 2 turns");
        }
        // DoubleUp - If target dies, next target hit for 150% damage
        else if (skillLower == "doubleup")
        {
            var nextTarget = GetFirstAliveEnemy();
            if (nextTarget != null)
            {
                int chainDamage = Mathf.RoundToInt(damageDealt * 1.50f);
                nextTarget.TakeDamage(chainDamage);
                Debug.Log($"[CombatManager] DoubleUp chain! {nextTarget.Name} takes {chainDamage} damage");
                
                if (combatUI != null)
                {
                    combatUI.ShowDamageToEnemy(nextTarget, chainDamage, false);
                    combatUI.UpdateEnemyHealth(nextTarget);
                }
                
                if (!nextTarget.IsAlive())
                {
                    Debug.Log($"[CombatManager] {nextTarget.Name} defeated by DoubleUp chain!");
                }
            }
        }
    }
    
    private void ExecuteSkillWithReaction(int skillNumber, CombatEnemy target, float reactionMultiplier, float qteMultiplier, Element? forcedElement, QTEResult qteResult)
    {
        var character = player.GetCharacter();
        if (character == null) return;

        // Get skill info from character data
        string skillName;
        float skillDamagePercent;
        string skillEffect;
        
        switch (skillNumber)
        {
            case 1:
                skillName = character.Skill1;
                skillDamagePercent = character.Skill1DamagePercent;
                skillEffect = character.Skill1Effect;
                break;
            case 2:
                skillName = character.Skill2;
                skillDamagePercent = character.Skill2DamagePercent;
                skillEffect = character.Skill2Effect;
                break;
            case 3:
                skillName = character.Skill3;
                skillDamagePercent = character.Skill3DamagePercent;
                skillEffect = character.Skill3Effect;
                break;
            default:
                skillName = "Unknown";
                skillDamagePercent = 100f;
                skillEffect = "null";
                break;
        }

        float skillMultiplier = skillDamagePercent / 100f;
        string skillLower = skillName.ToLower();
        Element attackElement = forcedElement ?? player.GetAffinity();
        
        // Check for AoE
        bool isAoE = skillEffect != null && skillEffect.ToLower().Contains("aoe");
        
        // Player Skill Attack Order with Reaction:
        // 1. Base damage = (character damage * skill multiplier) + elemental bonus
        int charDamage = player.GetCharacterDamage();
        int elementalBonus = player.HasElementPair() 
            ? player.GetElementalBonus(player.GetOrbAElement()) + player.GetElementalBonus(player.GetOrbBElement())
            : player.GetAffinityBonus();
        float baseDamage = (charDamage * skillMultiplier) + (elementalBonus * skillMultiplier);
        
        // 2. Apply variance (0.90-1.10)
        int afterVariance = player.ApplyVariance(baseDamage);
        
        // 3. Apply Offensive QTE multiplier
        float afterOQTE = afterVariance * qteMultiplier;
        
        // 4. Apply crit (direct hit only)
        bool isCrit = Random.Range(0, 100) < player.GetCritChance();
        float critMultiplier = isCrit ? player.GetCritDamage() : 1f;
        float directAfterCrit = afterOQTE * critMultiplier;
        
        // 5. Calculate reaction damage using afterOQTE (pre-crit, so crit doesn't inflate reactions)
        float reactionDamage = afterOQTE * (reactionMultiplier - 1f);
        
        // 6. Total = DirectAfterCrit + ReactionDamage
        float totalDamage = directAfterCrit + reactionDamage;
        int damageBeforeResist = Mathf.RoundToInt(totalDamage);
        
        // 7. Apply enemy resistance
        int finalDamage = target.ApplyResistance(damageBeforeResist, attackElement);
        int resistPercent = target.CalculateResistance(attackElement);
        
        if (isAoE)
        {
            DamageAllEnemies(finalDamage, forcedElement, isCrit);
            // DamageAllEnemies shows floating text for all enemies
        }
        else
        {
            target.TakeDamage(finalDamage);
            if (combatUI != null)
            {
                combatUI.ShowDamageToEnemy(target, finalDamage, isCrit);
                combatUI.UpdateEnemyHealth(target);
            }
        }
        
        // Apply skill effects
        ApplySkillEffects(skillLower, skillEffect, target, finalDamage, attackElement);
        
        string infusedSrc = "";
        if (player.HasElementPair())
        {
            if (attackElement == player.GetOrbAElement()) infusedSrc = "A";
            else if (attackElement == player.GetOrbBElement()) infusedSrc = "B";
        }
        
        Debug.Log($"[CombatLog] PlayerSkillReaction | Skill={skillName} | Reaction={pendingReactionName} | Base={baseDamage:F0} | Variance={afterVariance} | OQTE={afterOQTE:F0} | Crit={isCrit} | CritMult={critMultiplier:F1}x | ReactionBonus={reactionDamage:F0} | FinalDamage={finalDamage} | Element={attackElement} | InfusedFrom={infusedSrc} | Resist={resistPercent}% | Target={target.Name}");
        Debug.Log($"[CombatManager] REACTION SKILL! {skillName} + {pendingReactionName} ({pendingReactionElementA}+{pendingReactionElementB}) -> {finalDamage} damage to {target.Name} [QTE: {qteResult}]");

        bool targetKilled = !target.IsAlive();
        if (targetKilled)
        {
            Debug.Log($"[CombatManager] {target.Name} defeated!");
            HandleKillEffects(skillLower, target, finalDamage);
        }

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }
        
        // Apply skill cooldown and energy effects (skillNumber is 1-indexed, array is 0-indexed)
        player.UseSkillAndApplyEffects(skillNumber - 1);
        
        // Track DirtyStab
        if (skillLower == "dirtystab")
        {
            player.IncrementDirtyStabUse();
        }
        else
        {
            player.ResetDirtyStabStacks();
        }
        
        if (combatUI != null)
        {
            combatUI.UpdatePlayerEnergy(player);
            combatUI.UpdateSkillButtons(player);
        }

        isPlayerTurn = false;
        StartCoroutine(DelayedEnemyTurn());
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

    private void DamageAllEnemies(int damage, Element? forcedElement = null, bool isCrit = false)
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
                    // Show floating text for each enemy hit by AOE
                    combatUI.ShowDamageToEnemy(enemy, finalDamage, isCrit);
                    combatUI.UpdateEnemyHealth(enemy);
                }
            }
        }
    }

    private DefensiveQTE defensiveQTE;
    private List<CombatEnemy> pendingAttackers = new List<CombatEnemy>();
    private int currentAttackerIndex = 0;
    private const float DELAY_BETWEEN_ENEMY_ATTACKS = 0.8f; // Delay in seconds between enemy attacks

    private CombatEnemy pendingDefensiveQTEAttacker;
    private int pendingDefensiveQTEDamage;
    private int pendingDefensiveQTEBaseDamage;
    private int pendingDefensiveQTEAfterVariance;
    private int pendingDefensiveQTEResistPercent;
    
    private void EnemyTurn()
    {
        StartCoroutine(EnemyTurnCoroutine());
    }
    
    private System.Collections.IEnumerator EnemyTurnCoroutine()
    {
        // Collect all alive enemies that will attack
        // Turn order: 1) Check stun, 2) Apply DoT (even if stunned), 3) Attack (if not stunned)
        pendingAttackers.Clear();
        
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive())
            {
                // STEP 1: Check and consume stun at START of turn
                bool isStunned = enemy.CheckAndConsumeStun();
                
                // Show stun skip floating text
                if (isStunned && combatUI != null)
                {
                    combatUI.ShowTurnSkippedToEnemy(enemy, "Stunned!");
                    yield return new WaitForSeconds(0.3f); // Brief pause for stun text
                }
                
                // STEP 2: Tick DoT effects (DoT still ticks even when stunned)
                int dotDamage = enemy.TickDoTEffects();
                
                // Show DoT tick floating text
                if (dotDamage > 0 && combatUI != null)
                {
                    combatUI.ShowDoTTickToEnemy(enemy, dotDamage, "Burn");
                    yield return new WaitForSeconds(0.25f); // Brief pause for DoT text
                }
                
                if (combatUI != null)
                {
                    // Always update UI to reflect status changes
                    combatUI.UpdateEnemyHealth(enemy);
                }
                
                // Check if enemy died from DoT
                if (!enemy.IsAlive())
                {
                    Debug.Log($"[CombatManager] {enemy.Name} died from DoT!");
                    continue;
                }
                
                // STEP 3: If stunned, skip attack phase entirely
                if (isStunned)
                {
                    Debug.Log($"[CombatManager] {enemy.Name} turn skipped due to STUN!");
                    continue;
                }
                
                // Not stunned, can attack
                pendingAttackers.Add(enemy);
            }
        }
        
        // Check if all enemies are dead after DoT
        if (AllEnemiesDead())
        {
            EndCombat(true);
            yield break;
        }
        
        if (pendingAttackers.Count == 0)
        {
            EndEnemyTurn();
            yield break;
        }
        
        // Get DefensiveQTE reference
        if (defensiveQTE == null)
        {
            defensiveQTE = FindFirstObjectByType<DefensiveQTE>();
        }
        
        currentAttackerIndex = 0;
        ProcessNextEnemyAttack();
    }
    
    private void ProcessNextEnemyAttack()
    {
        if (currentAttackerIndex >= pendingAttackers.Count)
        {
            EndEnemyTurn();
            return;
        }
        
        var enemy = pendingAttackers[currentAttackerIndex];
        
        pendingDefensiveQTEAttacker = enemy;
        pendingDefensiveQTEBaseDamage = enemy.Damage;
        pendingDefensiveQTEAfterVariance = enemy.RollDamageWithVariance();
        pendingDefensiveQTEResistPercent = player.CalculateResistance(enemy.Affinity);
        pendingDefensiveQTEDamage = player.ApplyResistance(pendingDefensiveQTEAfterVariance, enemy.Affinity);

        string elementText = enemy.Affinity != Element.None ? $" ({enemy.Affinity})" : "";
        Debug.Log($"[CombatManager] {enemy.Name}{elementText} attacks for {pendingDefensiveQTEDamage} damage (base: {pendingDefensiveQTEBaseDamage}, variance: {pendingDefensiveQTEAfterVariance})");

        // Detailed log
        string resMarker = "";
        if (player.HasElementPair())
        {
            bool isA = enemy.Affinity == player.GetOrbAElement();
            bool isB = enemy.Affinity == player.GetOrbBElement();
            if (isA && isB) resMarker = "A,B";
            else if (isA) resMarker = "A";
            else if (isB) resMarker = "B";
        }
        else if (player.HasAffinity() && enemy.Affinity == player.GetAffinity())
        {
            resMarker = "Affinity";
        }

        Debug.Log($"[CombatLog] EnemyAttack | Attacker={enemy.Name} | Element={enemy.Affinity} | Base={pendingDefensiveQTEBaseDamage} | Variance={pendingDefensiveQTEAfterVariance} | ResistTotal={pendingDefensiveQTEResistPercent}% | ResistBonusFrom={resMarker} | Final={pendingDefensiveQTEDamage}");

        if (pendingDefensiveQTEDamage <= 0)
        {
            OnDefensiveQTEComplete(enemy, DefensiveQTEResult.Bad);
            return;
        }

        if (defensiveQTE != null)
        {
            defensiveQTE.StartQTE((result) => OnDefensiveQTEComplete(enemy, result), enemy.Name);
        }
        else
        {
            OnDefensiveQTEComplete(enemy, DefensiveQTEResult.Bad);
        }
    }
    
    private void OnDefensiveQTEComplete(CombatEnemy enemy, DefensiveQTEResult qteResult)
    {
        if (pendingDefensiveQTEAttacker != enemy)
        {
            pendingDefensiveQTEAttacker = enemy;
        }

        float healPercent = DefensiveQTE.GetHealPercent(qteResult);
        int healAmount = Mathf.RoundToInt(player.GetMaxHealth() * healPercent);
        if (healAmount > 0)
        {
            player.Heal(healAmount);
            Debug.Log($"[CombatManager] Defensive QTE {qteResult}: healed {healAmount} HP ({healPercent * 100}% of max)");
            
            if (combatUI != null)
            {
                combatUI.UpdatePlayerHealth(player);
                // Show heal floating text from QTE
                combatUI.ShowHealToPlayer(healAmount);
            }
        }

        Debug.Log($"[CombatLog] DefensiveQTE | Attacker={enemy.Name} | QTE={qteResult} | Healed={healAmount}");

        if (pendingDefensiveQTEDamage > 0)
        {
            player.TakeDamage(pendingDefensiveQTEDamage);

            if (combatUI != null)
            {
                var dmgInfo = player.GetLastDamageInfo();
                if (dmgInfo.shieldAbsorbed > 0)
                {
                    combatUI.ShowShieldToPlayer(dmgInfo.shieldAbsorbed, FloatingTextType.ShieldAbsorb);
                }
                if (dmgInfo.shieldBroken)
                {
                    combatUI.ShowShieldToPlayer(0, FloatingTextType.ShieldBroken);
                }
                if (dmgInfo.finalDamage > 0)
                {
                    combatUI.ShowDamageToPlayer(dmgInfo.finalDamage);
                }
                combatUI.UpdatePlayerHealth(player);
            }
        }

        if (!player.IsAlive())
        {
            EndCombat(false);
            return;
        }

        pendingDefensiveQTEAttacker = null;
        pendingDefensiveQTEDamage = 0;
        pendingDefensiveQTEBaseDamage = 0;
        pendingDefensiveQTEAfterVariance = 0;
        pendingDefensiveQTEResistPercent = 0;
        
        // Process next attacker with delay so player has time to prepare
        currentAttackerIndex++;
        if (currentAttackerIndex < pendingAttackers.Count)
        {
            Invoke(nameof(ProcessNextEnemyAttack), DELAY_BETWEEN_ENEMY_ATTACKS);
        }
        else
        {
            EndEnemyTurn();
        }
    }
    
    private void EndEnemyTurn()
    {
        pendingAttackers.Clear();
        
        if (!player.IsAlive())
        {
            EndCombat(false);
            return;
        }

        isPlayerTurn = true;
        
        // Tick down cooldowns at start of player's turn
        player.TickCooldowns();

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
            // XP System: grant XP based on combat type (Phase 1)
            int xpReward = currentCombatType switch
            {
                CombatType.Elite => 3,
                CombatType.Boss => 5,
                _ => 1
            };
            GameManager.AddRunXP(xpReward);
            
            // Gold reward still granted
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

            if (combatUI != null)
            {
                string lootTitle = currentCombatType == CombatType.Elite ? "ELITE VICTORY!" : "VICTORY!";
                combatUI.ShowLootPanel(goldReward, xpReward, player, currentNode, lootTitle);
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
