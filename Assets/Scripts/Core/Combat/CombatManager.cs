using System.Collections.Generic;
using UnityEngine;

public enum CombatType { Normal, Elite, Boss }

/// <summary>
/// Tracks the current phase within a combat turn.
/// A full turn = PlayerPhase → EnemyPhase.
/// </summary>
public enum CombatPhase { None, PlayerPhase, EnemyPhase }

public class CombatManager : MonoBehaviour
{
    private List<CombatEnemy> enemies = new List<CombatEnemy>();
    private Player player;
    private CombatUI combatUI;
    private CombatArena combatArena;
    private ReactionQTEPanel qtePanel;
    private bool isPlayerTurn = true;
    private bool combatActive = false;
    private bool isEndingCombat = false;
    
    // Centralized turn tracking
    private int currentTurnNumber = 0;
    private CombatPhase currentPhase = CombatPhase.None;
    
    // MirrorBoss reactive pattern: tracks the player's last action type
    private PlayerLastAction lastPlayerAction = PlayerLastAction.Attack;
    
    // FallenChampion spawn requirement: tracks bosses defeated across the run
    private static int bossesDefeatedThisRun = 0;
    
    /// <summary>
    /// Reset boss defeat counter. Call at the start of a new run.
    /// </summary>
    public static void ResetBossDefeatedCount()
    {
        bossesDefeatedThisRun = 0;
    }
    private NodeBase currentNode;
    private CombatType currentCombatType = CombatType.Normal;
    
    private CombatEnemy pendingTarget;
    private int pendingSkillNumber;
    private bool pendingIsAttack;
    private Element pendingInfusedElement = Element.None;
    private string pendingReactionName = "";
    private string pendingReactionId = null;
    private Element pendingReactionFirstElement = Element.None;
    private Element pendingReactionDetonator = Element.None;
    private ReactionTriggerInfo pendingReactionInfo = null;

    public void StartCombat(CombatNode node, Player playerRef)
    {
        currentCombatType = CombatType.Normal;
        var encounter = DataCache.GetWorldEncounter(GameManager.CurrentWorld);
        StartCombatInternal(node, playerRef, DataCache.RegularEnemies, encounter.RegularEnemy);
    }

    public void StartEliteCombat(NodeBase node, Player playerRef)
    {
        currentCombatType = CombatType.Elite;
        var encounter = DataCache.GetWorldEncounter(GameManager.CurrentWorld);
        StartCombatInternal(node, playerRef, DataCache.EliteEnemies, encounter.EliteEnemy);
    }

    public void StartBossCombat(Player playerRef, System.Action<bool> onBossComplete)
    {
        currentCombatType = CombatType.Boss;
        onBossCombatComplete = onBossComplete;
        currentNode = null;
        
        player = playerRef;
        enemies.Clear();

        int world = GameManager.CurrentWorld;
        var encounter = DataCache.GetWorldEncounter(world);
        int bossCount = encounter.BossEnemy;
        
        // Ensure DataCache is loaded
        if (!DataCache.IsLoaded)
        {
            DataCache.LoadAll();
        }
        
        if (DataCache.BossEnemies == null || DataCache.BossEnemies.Count == 0)
        {
            Debug.LogError($"[CombatManager] No boss enemies loaded! Cannot start boss fight for world {world}");
            // Don't auto-win - show error state
            onBossCombatComplete?.Invoke(false);
            return;
        }
        
        // Build eligible boss pool: filter by world, then by spawn requirements
        var eligibleBosses = new List<EnemyData>();
        foreach (var boss in DataCache.BossEnemies)
        {
            // Filter by world pool (if defined)
            if (boss.Worlds != null && boss.Worlds.Length > 0)
            {
                if (System.Array.IndexOf(boss.Worlds, world) < 0)
                {
                    GameLog.Combat(GameLog.Join("BossFiltered",
                        GameLog.KV("boss", boss.DisplayName),
                        GameLog.KV("requirement", "worldPool"),
                        GameLog.KV("allowedWorlds", string.Join(",", boss.Worlds)),
                        GameLog.KV("currentWorld", world)));
                    continue;
                }
            }
            
            // Filter by spawn requirements
            if (!string.IsNullOrEmpty(boss.SpawnRequirement) && boss.SpawnRequirement == "defeatBosses")
            {
                if (bossesDefeatedThisRun < boss.SpawnRequirementCount)
                {
                    GameLog.Combat(GameLog.Join("BossFiltered",
                        GameLog.KV("boss", boss.DisplayName),
                        GameLog.KV("requirement", boss.SpawnRequirement),
                        GameLog.KV("needed", boss.SpawnRequirementCount),
                        GameLog.KV("current", bossesDefeatedThisRun)));
                    continue;
                }
            }
            eligibleBosses.Add(boss);
        }
        
        // Fallback: if no bosses match world + requirements, try world-only (ignore requirements)
        if (eligibleBosses.Count == 0)
        {
            eligibleBosses.AddRange(DataCache.BossEnemies.FindAll(b =>
                b.Worlds != null && b.Worlds.Length > 0 && System.Array.IndexOf(b.Worlds, world) >= 0
                && string.IsNullOrEmpty(b.SpawnRequirement)));
        }
        
        // Last resort: any boss with no spawn requirement
        if (eligibleBosses.Count == 0)
        {
            eligibleBosses.AddRange(DataCache.BossEnemies.FindAll(b => string.IsNullOrEmpty(b.SpawnRequirement)));
        }
        
        // Shuffle eligible bosses to get random order, then pick without duplicates
        var shuffled = new List<EnemyData>(eligibleBosses);
        for (int j = shuffled.Count - 1; j > 0; j--)
        {
            int r = Random.Range(0, j + 1);
            var temp = shuffled[j];
            shuffled[j] = shuffled[r];
            shuffled[r] = temp;
        }
        
        for (int i = 0; i < bossCount; i++)
        {
            // Pick from shuffled list without duplicates; wrap around if more bosses needed than available
            var bossData = shuffled[i % shuffled.Count];
            enemies.Add(new CombatEnemy(bossData, world));
        }

        combatActive = true;
        isPlayerTurn = true;
        isEndingCombat = false;
        currentTurnNumber = 1;
        currentPhase = CombatPhase.PlayerPhase;
        
        // Ensure player combat state is reset (sets AP to max, clears per-combat effects)
        if (player != null)
        {
            player.ResetCombatState();
            player.OnCombatStart();
            ApplyCombatStartRelicEffects();
        }

        string bossNames = string.Join(" & ", enemies.ConvertAll(e => e.Name));
        GameLog.Combat(GameLog.Join(
            "CombatStart",
            GameLog.KV("type", "Boss"),
            GameLog.KV("world", world),
            GameLog.KV("enemies", bossNames)
        ));

        // Disable player free roam during boss combat
        var pcBoss = FindFirstObjectByType<PlayerController>();
        if (pcBoss != null) pcBoss.SetCanMove(false);

        // Use in-world combat arena if available
        if (combatArena == null)
        {
            combatArena = FindFirstObjectByType<CombatArena>();
        }
        
        if (combatArena != null && pcBoss != null)
        {
            Vector3 combatCenter = pcBoss.transform.position;
            combatArena.EnterCombat(pcBoss.transform, enemies, combatCenter);
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
        
        currentNode = node;
        player = playerRef;
        enemies.Clear();

        if (enemyPool == null || enemyPool.Count == 0)
        {
            enemyPool = DataCache.RegularEnemies;
        }
        
        if (enemyPool == null || enemyPool.Count == 0)
        {
            GameLog.Error(GameLogCategory.Combat, "[Combat]", "CombatStartError | reason=no_enemies_loaded");
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
        isEndingCombat = false;
        currentTurnNumber = 1;
        currentPhase = CombatPhase.PlayerPhase;
        
        // Reset player combat state (energy to 0, cooldowns cleared)
        player.ResetCombatState();
        
        // Trigger relic combat-start effects
        player.OnCombatStart();
        ApplyCombatStartRelicEffects();

        // Disable player free roam during combat
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.SetCanMove(false);

        string combatTypeLabel = currentCombatType == CombatType.Elite ? "Elite" : "Normal";
        GameLog.Combat(GameLog.Join(
            "CombatStart",
            GameLog.KV("type", combatTypeLabel),
            GameLog.KV("enemyCount", enemies.Count)
        ));

        // Use in-world combat arena if available
        if (combatArena == null)
        {
            combatArena = FindFirstObjectByType<CombatArena>();
        }
        
        if (combatArena != null && pc != null)
        {
            Vector3 combatCenter = node != null ? node.transform.position : pc.transform.position;
            combatArena.EnterCombat(pc.transform, enemies, combatCenter);
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
    
    /// <summary>
    /// End the player's turn manually (called by End Turn button).
    /// </summary>
    public void EndPlayerTurn()
    {
        if (!combatActive || !isPlayerTurn) return;
        
        // Process relic turn-end effects (AP banking, Elemental Drip, Blood Tithe self-damage)
        if (player != null)
        {
            player.OnRelicTurnEnd();
            ApplyTurnEndRelicEffects();
        }
        
        GameLog.Combat(GameLog.Join("PlayerPhaseEnd", GameLog.KV("turn", currentTurnNumber)));
        
        // Execute any deferred SlimeBoss splits before ending the turn.
        // The split uses the boss's current HP at this moment (not when threshold was crossed).
        var snapshot = new List<CombatEnemy>(enemies);
        foreach (var enemy in snapshot)
        {
            if (enemy.IsAlive() && enemy.IsPendingSplit)
            {
                ExecuteSlimeBossSplit(enemy);
            }
        }
        
        isPlayerTurn = false;
        currentPhase = CombatPhase.EnemyPhase;
        StartCoroutine(DelayedEnemyTurn());
    }

    public void OnPlayerAttackTarget(CombatEnemy target)
    {
        if (!combatActive || !isPlayerTurn) return;
        if (target == null || !target.IsAlive()) return;

        // New mark system: Just execute the attack directly
        // Marks will be applied based on skill element enchantments
        ExecuteAttack(target);
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
            4 => character.Skill4,
            5 => character.Skill5,
            _ => $"Skill {skillNumber}"
        };
    }
    
    private void OnQTEComplete(QTEResult result)
    {
        float qteMultiplier = ReactionQTE.GetQteMultiplier(result);
        
        // Update debug overlay
        GameManager.SetLastQTEResult($"Reaction: {result}");
        
        GameLog.Combat(GameLog.Join(
            "ReactionQTEComplete",
            GameLog.KV("result", result),
            GameLog.KV("qteMult", qteMultiplier.ToString("F2"))
        ), GameLogVerbosity.Verbose);
        
        // Consume marks now (deferred from TriggerMarkReaction)
        if (pendingReactionInfo != null && pendingTarget != null)
        {
            pendingTarget.ConsumeMarksForReaction(pendingReactionInfo);
            
            // Perfect Reaction Save Mark relic: restore N marks on Perfect QTE
            if (result == QTEResult.Perfect && player != null && player.HasPerfectReactionSaveMark())
            {
                int saveCount = player.GetPerfectReactionSaveMarkCount();
                if (pendingReactionInfo.IsSingleElement)
                {
                    pendingTarget.AddMarks(pendingReactionInfo.PrimaryElement, saveCount);
                }
                else
                {
                    pendingTarget.AddMarks(pendingReactionInfo.PrimaryElement, saveCount);
                    pendingTarget.AddMarks(pendingReactionInfo.SecondaryElement, saveCount);
                }
                Debug.Log($"[CombatManager] Perfect Reaction Save Mark: restored {saveCount} mark(s)");
            }
        }
        
        // Process reaction effects via data-driven engine (separate from skill/attack damage)
        ProcessReactionAfterQTE(qteMultiplier);
        
        ClearPendingAction();
    }
    
    /// <summary>
    /// Process all reaction effects after QTE completes. Reaction damage is its own source,
    /// separate from skill damage. Called from OnQTEComplete.
    /// </summary>
    private void ProcessReactionAfterQTE(float qteMultiplier)
    {
        if (string.IsNullOrEmpty(pendingReactionId) || pendingTarget == null) return;
        
        Element attackElement = pendingInfusedElement != Element.None ? pendingInfusedElement : player.GetAffinity();
        
        // Process all reaction effects via the data-driven engine
        var reactionResult = ReactionEffectEngine.ProcessReaction(
            pendingReactionId,
            player,
            pendingTarget,
            enemies,
            qteMultiplier,
            attackElement
        );
        
        // Show floating text for reaction damage instances
        foreach (var dmgInstance in reactionResult.DamageInstances)
        {
            if (dmgInstance.Damage > 0)
            {
                ShowDamageToEnemy(dmgInstance.Target, dmgInstance.Damage, dmgInstance.IsCrit);
                NotifyEnemyHit(dmgInstance.Target);
                
                // Shatter accumulation on damage dealt
                if (dmgInstance.Target.HasShatter)
                {
                    int shatterPop = dmgInstance.Target.AccumulateShatterDamage(dmgInstance.Damage);
                    if (shatterPop > 0)
                    {
                        ShowDamageToEnemy(dmgInstance.Target, shatterPop, false);
                    }
                }
            }
        }
        
        // Show shield gain floating text
        if (reactionResult.ShieldGained > 0)
        {
            ShowShieldToPlayer(reactionResult.ShieldGained, FloatingTextType.ShieldGain);
        }
        
        GameLog.Reaction(GameLog.Join(
            "ReactionProcessed",
            GameLog.KV("id", pendingReactionId),
            GameLog.KV("name", pendingReactionName),
            GameLog.KV("dmgToTarget", reactionResult.TotalDamageToTarget),
            GameLog.KV("dmgToOthers", reactionResult.TotalDamageToOthers),
            GameLog.KV("shield", reactionResult.ShieldGained),
            GameLog.KV("crit", reactionResult.DidCrit)
        ));
        
        // Show reaction name floating text after QTE (ensures it's visible)
        ShowReactionToEnemy(pendingTarget, pendingReactionName, 0);
        
        // Refresh AP display immediately (reaction buffs like BonusAP change AP mid-turn)
        if (combatArena != null)
        {
            combatArena.RefreshAPDisplay();
        }
        
        // Create UI chips for reaction effects
        CreateReactionChips(pendingReactionId, pendingReactionName, pendingTarget);
        
        // Check for enemy deaths from reaction damage
        bool rebornTriggered = false;
        CombatEnemy rebornTarget = null;
        foreach (var dmgInstance in reactionResult.DamageInstances)
        {
            var t = dmgInstance.Target;
            if (t != null && !t.IsAlive())
            {
                if (t.ShouldReborn())
                {
                    t.ActivateReborn();
                    rebornTriggered = true;
                    rebornTarget = t;
                }
                else
                {
                    GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", t.Name), GameLog.KV("source", "Reaction")));
                    NotifyEnemyDeath(t);
                }
            }
            if (t != null && t.IsAlive())
            {
                t.CheckSplitThreshold();
            }
        }
        
        if (rebornTriggered && rebornTarget != null)
        {
            ForceEndPlayerTurnForReborn(rebornTarget);
            return;
        }
        
        if (AllEnemiesDead())
        {
            EndCombat(true);
        }
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
        pendingReactionName = "";
        pendingReactionId = null;
        pendingReactionFirstElement = Element.None;
        pendingReactionDetonator = Element.None;
        pendingReactionInfo = null;
    }
    
    /// <summary>
    /// New mark-based reaction trigger. Called when marks reach threshold on an enemy.
    /// </summary>
    private void TriggerMarkReaction(CombatEnemy target, ReactionTriggerInfo reactionInfo)
    {
        if (target == null || reactionInfo == null) return;
        
        pendingTarget = target;
        pendingIsAttack = false;
        pendingReactionFirstElement = reactionInfo.PrimaryElement;
        pendingReactionDetonator = reactionInfo.SecondaryElement != Element.None ? reactionInfo.SecondaryElement : reactionInfo.PrimaryElement;
        pendingInfusedElement = reactionInfo.PrimaryElement;
        pendingReactionId = reactionInfo.GetReactionId();
        
        // Get reaction data from JSON — no fallbacks
        var reactionDef = DataCache.GetReactionDef(pendingReactionId);
        if (reactionDef == null)
        {
            GameLog.Warn(GameLogCategory.Reaction, "[CombatManager]", $"No reaction data for {pendingReactionId}");
            return;
        }
        
        pendingReactionName = reactionDef.Name;
        
        // Defer mark consumption until after QTE (Perfect Reaction Save Mark relic)
        pendingReactionInfo = reactionInfo;
        
        string attackerName = player != null && player.GetCharacter() != null ? player.GetCharacter().DisplayName : "Player";
        GameLog.Reaction(GameLog.Join(
            "MarkReactionTrigger",
            GameLog.KV("id", pendingReactionId),
            GameLog.KV("name", pendingReactionName),
            GameLog.KV("type", reactionInfo.IsSingleElement ? "single" : "dual"),
            GameLog.KV("primary", reactionInfo.PrimaryElement),
            GameLog.KV("secondary", reactionInfo.SecondaryElement),
            GameLog.KV("attacker", attackerName),
            GameLog.KV("target", target.Name)
        ));
        
        // Reaction floating text is shown after QTE completes in ProcessReactionAfterQTE
        
        // Sigil Renounce: skip reaction QTE entirely, auto-resolve as Good
        if (player != null && player.IsReactionQTEDisabled())
        {
            GameLog.Combat(GameLog.Join("ReactionQTESkipped", GameLog.KV("reason", "ReactionQTEDisabled")));
            OnQTEComplete(QTEResult.Good);
            return;
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
            GameLog.Warn(GameLogCategory.Combat, "[Combat]", "ReactionQTEPanelMissing | action=auto_good");
            OnQTEComplete(QTEResult.Good);
        }
    }
    
    private void ExecuteAttack(CombatEnemy target, float reactionMultiplier = 1f, Element? forcedElement = null)
    {
        lastPlayerAction = PlayerLastAction.Attack;
        Element attackElement = forcedElement ?? player.GetAffinity();

        string attackerName = player != null && player.GetCharacter() != null ? player.GetCharacter().DisplayName : "Player";
        string reactionId = reactionMultiplier > 1f ? "nonDirectional" : "none";

        // Player Attack Order:
        // 1. Base damage (character damage + elemental bonuses), reduced by Weaken debuff
        int baseDamage = Mathf.RoundToInt(player.GetTotalDamage() * player.GetWeakenMultiplier());
        
        // 1b. Apply Rock damage bonus while shielded (Stoneguard)
        if (attackElement == Element.Rock)
        {
            float rockBonus = player.GetRockDamageWhileShieldedBonus();
            if (rockBonus > 0f)
            {
                baseDamage = Mathf.RoundToInt(baseDamage * (1f + rockBonus));
                GameLog.Combat(GameLog.Join("RockShieldBonus", GameLog.KV("bonus", $"{rockBonus * 100f:F0}%")));
            }
        }
        
        GameLog.Combat(GameLog.Join(
            "AttackStart",
            GameLog.KV("attacker", attackerName),
            GameLog.KV("target", target != null ? target.Name : "null"),
            GameLog.KV("element", attackElement),
            GameLog.KV("dmgRange", $"{Mathf.RoundToInt(baseDamage * 0.9f)}-{Mathf.RoundToInt(baseDamage * 1.1f)}"),
            GameLog.KV("bonuses", "base"),
            GameLog.KV("reactionId", reactionId)
        ));

        // 2. Apply variance (0.90-1.10)
        int afterVariance = player.ApplyVariance(baseDamage);
        
        // 3. No OQTE for basic attack without reaction
        
        // 4. Apply crit
        int critChance = player.GetCritChance();
        int critRoll = Random.Range(0, 100);
        bool isCrit = critRoll < critChance;
        int afterCrit = isCrit ? Mathf.RoundToInt(afterVariance * player.GetCritDamage()) : afterVariance;
        
        // 5. Apply reaction multiplier
        int totalDamage = Mathf.RoundToInt(afterCrit * reactionMultiplier);

        GameLog.Combat(GameLog.Join(
            "AttackRoll",
            GameLog.KV("roll", afterVariance),
            GameLog.KV("reactionMult", reactionMultiplier.ToString("F2")),
            GameLog.KV("critRoll", critRoll),
            GameLog.KV("critChance", critChance),
            GameLog.KV("crit", isCrit),
            GameLog.KV("critMult", (isCrit ? player.GetCritDamage() : 1f).ToString("F2")),
            GameLog.KV("preResist", totalDamage)
        ));

        // 6. Apply enemy resistance
        int damage = target.ApplyResistance(totalDamage, attackElement);
        int resistPercent = target.CalculateResistance(attackElement);

        int hpBefore = target.Health;
        target.TakeDamage(damage);
        int hpAfter = target.Health;
        GameLog.Combat(GameLog.Join(
            "DamageApply",
            GameLog.KV("target", target.Name),
            GameLog.KV("resistTotal", $"{resistPercent}%"),
            GameLog.KV("shieldBefore", 0),
            GameLog.KV("shieldAbsorbed", 0),
            GameLog.KV("shieldAfter", 0),
            GameLog.KV("hpBefore", hpBefore),
            GameLog.KV("dmgFinal", damage),
            GameLog.KV("hpAfter", hpAfter)
        ));

        ShowDamageToEnemy(target, damage, isCrit);
        
        // Notify in-world combat arena of damage
        NotifyEnemyHit(target);
        
        // Check Frost Shield break (Frostcaller)
        CheckFrostShieldBreak(target);
        
        // Check Retaliation counter-attack (Shieldbearer)
        CheckRetaliationCounter(target);
        
        // Player could die from Frost Shield break or Retaliation
        if (!player.IsAlive())
        {
            EndCombat(false);
            return;
        }

        if (!target.IsAlive())
        {
            // Check Reborn mechanic (FallenChampion)
            if (target.ShouldReborn())
            {
                target.ActivateReborn();
                ForceEndPlayerTurnForReborn(target);
                return;
            }
            else
            {
                GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", target.Name)));
                NotifyEnemyDeath(target);
            }
        }
        
        // Check SlimeBoss split threshold (deferred until End Turn)
        if (target.IsAlive())
        {
            target.CheckSplitThreshold();
        }

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }

    }
    
    // ExecuteAttackWithReaction removed — reaction processing is now handled by ProcessReactionAfterQTE

    public bool IsQTEActive()
    {
        if (qtePanel != null && qtePanel.IsActive()) return true;
        if (defensiveQTE != null && defensiveQTE.IsActive()) return true;
        return false;
    }

    public void OnPlayerSkillTarget(int skillNumber, CombatEnemy target)
    {
        if (!combatActive || !isPlayerTurn) return;
        if (target == null || !target.IsAlive()) return;
        
        // Block skill usage during QTE
        if (IsQTEActive()) return;

        var character = player.GetCharacter();
        if (character == null)
        {
            GameLog.Combat(
                GameLog.Join(
                    "SkillBlocked",
                    GameLog.KV("reason", "no_character")
                ),
                GameLogVerbosity.Verbose
            );
            return;
        }
        
        // Check cooldown (skillNumber is 1-indexed, array is 0-indexed)
        int skillIndex = skillNumber - 1;
        if (player.IsSkillOnCooldown(skillIndex))
        {
            GameLog.Combat(GameLog.Join(
                "SkillBlocked",
                GameLog.KV("skill", skillNumber),
                GameLog.KV("reason", "cooldown"),
                GameLog.KV("cd", player.GetSkillCooldown(skillIndex))
            ), GameLogVerbosity.Verbose);
            return;
        }
        
        // Check AP cost
        int apCost = GetSkillAPCost(character, skillNumber);
        if (!player.HasEnoughAP(apCost))
        {
            GameLog.Combat(GameLog.Join(
                "SkillBlocked",
                GameLog.KV("skill", skillNumber),
                GameLog.KV("reason", "ap"),
                GameLog.KV("need", apCost),
                GameLog.KV("have", player.GetCurrentAP())
            ), GameLogVerbosity.Verbose);
            return;
        }
        
        // Check energy for Skill 5 (Ultimate)
        if (skillNumber == 5 && !player.CanUseUltimate())
        {
            GameLog.Combat(GameLog.Join(
                "SkillBlocked",
                GameLog.KV("skill", skillNumber),
                GameLog.KV("reason", "energy"),
                GameLog.KV("need", player.GetUltimateEnergyCost()),
                GameLog.KV("have", player.GetEnergy())
            ), GameLogVerbosity.Verbose);
            return;
        }

        // New mark system: Just execute the skill directly
        // Marks will be applied based on skill element enchantments
        ExecuteSkill(skillNumber, target);
    }
    
    private void ExecuteSkill(int skillNumber, CombatEnemy target, float reactionMultiplier = 1f, Element? forcedElement = null)
    {
        lastPlayerAction = PlayerLastAction.Attack;
        var character = player.GetCharacter();
        if (character == null) return;

        // Spend AP for this skill
        int apCost = GetSkillAPCost(character, skillNumber);
        player.SpendAP(apCost);
        
        // Deadeye Counter: consume guaranteed crit BEFORE incrementing counter
        // so it applies to the skill AFTER the counter reached its threshold
        bool deadeyeGuaranteedCrit = player.ConsumeDeadeyeCritIfReady();
        
        // Relic triggers: skill used + AP spent tracking
        player.OnRelicSkillUsed();
        if (apCost > 0) player.OnRelicAPSpent(apCost);

        // Look up SkillDefinition from JSON data
        var skillDef = GetSkillDefinition(skillNumber);
        string skillName = skillDef != null ? skillDef.displayName : GetSkillName(skillNumber);
        var effects = skillDef?.effects;
        
        // Read skill properties from data
        float skillMultiplier = SkillEffectEngine.GetDamageMultiplier(effects);
        int hitCount = SkillEffectEngine.GetMultiHitCount(effects);
        bool isAoE = SkillEffectEngine.IsAoE(effects);
        var (tempCritBonus, tempCritDmgBonusPct) = SkillEffectEngine.GetTempCritBonus(effects);
        float tempCritDmgBonus = tempCritDmgBonusPct / 100f;
        
        // Chain mechanic from chainSettings (data-driven, per-skill)
        int skillIndex = skillNumber - 1;
        float chainBonus = 1f;
        if (skillDef?.chainSettings != null && skillDef.chainSettings.maxChainUses > 0)
        {
            int stacks = player.GetChainStacks(skillIndex);
            if (stacks > 0)
            {
                int cappedStacks = Mathf.Min(stacks, skillDef.chainSettings.maxStacks);
                chainBonus = 1f + (cappedStacks * skillDef.chainSettings.stackBonusPerUse);
            }
        }
        
        string skillLower = skillName.ToLower();
        // Get skill's enchanted element (from sigil or base character data)
        Element skillElement = player.GetSkillElement(skillNumber);
        Element attackElement = forcedElement ?? (skillElement != Element.None ? skillElement : player.GetAffinity());
        int charDamage = player.GetCharacterDamage();
        float weakenMult = player.GetWeakenMultiplier();
        int baseDamageBeforeElement = Mathf.RoundToInt(charDamage * skillMultiplier * chainBonus * weakenMult);
        
        // Add elemental bonus based on skill's element (from enchantment or base)
        int elementalBonus = attackElement != Element.None ? player.GetElementalBonus(attackElement) : 0;
        int elementalBonusScaled = Mathf.RoundToInt(elementalBonus * skillMultiplier * weakenMult);
        int baseDamage = baseDamageBeforeElement + elementalBonusScaled;
        
        // Apply Rock damage bonus while shielded (Stoneguard)
        if (attackElement == Element.Rock)
        {
            float rockBonus = player.GetRockDamageWhileShieldedBonus();
            if (rockBonus > 0f)
            {
                baseDamage = Mathf.RoundToInt(baseDamage * (1f + rockBonus));
                GameLog.Combat(GameLog.Join("RockShieldBonus", GameLog.KV("bonus", $"{rockBonus * 100f:F0}%")));
            }
        }
        
        GameLog.Combat(GameLog.Join(
            "DamageCalc",
            GameLog.KV("charDmg", charDamage),
            GameLog.KV("skillMult", skillMultiplier.ToString("F2")),
            GameLog.KV("baseBefore", baseDamageBeforeElement),
            GameLog.KV("element", attackElement),
            GameLog.KV("elemBonus", elementalBonus),
            GameLog.KV("elemScaled", elementalBonusScaled),
            GameLog.KV("baseFinal", baseDamage)
        ), GameLogVerbosity.Verbose);
        
        int totalDamageDealt = 0;
        
        string attackerName = player != null && player.GetCharacter() != null ? player.GetCharacter().DisplayName : "Player";
        GameLog.Combat(GameLog.Join(
            "AttackStart",
            GameLog.KV("attacker", attackerName),
            GameLog.KV("target", target != null ? target.Name : "null"),
            GameLog.KV("element", attackElement),
            GameLog.KV("dmgRange", $"{Mathf.RoundToInt(baseDamage * 0.9f)}-{Mathf.RoundToInt(baseDamage * 1.1f)}"),
            GameLog.KV("bonuses", GameLog.Join(
                GameLog.KV("skill", skillName),
                GameLog.KV("hitCount", hitCount),
                GameLog.KV("critChanceBonus", tempCritBonus),
                GameLog.KV("critDmgBonus", tempCritDmgBonus.ToString("F2")),
                GameLog.KV("chainBonus", chainBonus.ToString("F2"))
            )),
            GameLog.KV("reactionId", reactionMultiplier > 1f ? (pendingReactionId ?? "unknown") : "none")
        ));

        // Execute hits
        for (int hit = 0; hit < hitCount; hit++)
        {
            if (!target.IsAlive()) break;
            
            // Apply variance per hit
            int damage = player.ApplyVariance(baseDamage);
            
            // Apply crit with temp bonuses + Deadeye guaranteed crit
            int effectiveCritChance = player.GetCritChance() + tempCritBonus;
            float effectiveCritDmg = player.GetCritDamage() + tempCritDmgBonus;
            int critRoll = Random.Range(0, 100);
            bool isCrit = deadeyeGuaranteedCrit || critRoll < effectiveCritChance;
            
            int afterCrit = isCrit ? Mathf.RoundToInt(damage * effectiveCritDmg) : damage;
            int afterReaction = Mathf.RoundToInt(afterCrit * reactionMultiplier);

            int resistPercent = target.CalculateResistance(attackElement);
            GameLog.Combat(GameLog.Join(
                "AttackRoll",
                GameLog.KV("hit", hit + 1),
                GameLog.KV("hitCount", hitCount),
                GameLog.KV("roll", damage),
                GameLog.KV("reactionMult", reactionMultiplier.ToString("F2")),
                GameLog.KV("critRoll", critRoll),
                GameLog.KV("critChance", effectiveCritChance),
                GameLog.KV("crit", isCrit),
                GameLog.KV("critMult", (isCrit ? effectiveCritDmg : 1f).ToString("F2")),
                GameLog.KV("preResist", afterReaction)
            ), hitCount > 1 ? GameLogVerbosity.Verbose : GameLogVerbosity.Minimal);

            int finalDamage = target.ApplyResistance(afterReaction, attackElement);
            
            if (isAoE)
            {
                DamageAllEnemies(finalDamage, forcedElement, isCrit);
                // Skip showing damage to primary target since DamageAllEnemies shows it for all
            }
            else
            {
                int hpBefore = target.Health;
                target.TakeDamage(finalDamage);
                int hpAfter = target.Health;
                GameLog.Combat(GameLog.Join(
                    "DamageApply",
                    GameLog.KV("hit", hit + 1),
                    GameLog.KV("target", target.Name),
                    GameLog.KV("resistTotal", $"{resistPercent}%"),
                    GameLog.KV("shieldBefore", 0),
                    GameLog.KV("shieldAbsorbed", 0),
                    GameLog.KV("shieldAfter", 0),
                    GameLog.KV("hpBefore", hpBefore),
                    GameLog.KV("dmgFinal", finalDamage),
                    GameLog.KV("hpAfter", hpAfter)
                ), hitCount > 1 ? GameLogVerbosity.Verbose : GameLogVerbosity.Minimal);
                ShowDamageToEnemy(target, finalDamage, isCrit);
            }
            
            totalDamageDealt += finalDamage;
            
            // Per-hit detailed logs handled above (verbose)
        }

        GameLog.Combat(GameLog.Join(
            "SkillSummary",
            GameLog.KV("skill", skillName),
            GameLog.KV("target", target != null ? target.Name : "null"),
            GameLog.KV("totalDamage", totalDamageDealt)
        ));
        
        // ========== ELEMENTAL MARK SYSTEM ==========
        // Apply elemental marks based on skill data (element, markChance, markCount)
        if (target != null && target.IsAlive())
        {
            Element markElement = player.GetSkillElement(skillNumber);
            int markChance = GetSkillMarkChance(character, skillNumber);
            int markCount = GetSkillMarkCount(character, skillNumber);
            
            if (markElement != Element.None && markChance > 0 && markCount > 0)
            {
                int roll = Random.Range(0, 100);
                if (roll < markChance)
                {
                    bool reactionTriggered = target.AddMarks(markElement, markCount);
                    
                    GameLog.Combat(GameLog.Join(
                        "MarkApply",
                        GameLog.KV("skill", skillName),
                        GameLog.KV("element", markElement),
                        GameLog.KV("chance", $"{markChance}%"),
                        GameLog.KV("roll", roll),
                        GameLog.KV("marks", markCount),
                        GameLog.KV("target", target.Name)
                    ));
                    
                    // Check if reaction was triggered
                    if (reactionTriggered)
                    {
                        var reactionInfo = target.GetReactionTriggerInfo();
                        if (reactionInfo != null)
                        {
                            string reactionId = reactionInfo.GetReactionId();
                            GameLog.Combat(GameLog.Join(
                                "ElementalReaction",
                                GameLog.KV("type", reactionInfo.IsSingleElement ? "single" : "dual"),
                                GameLog.KV("reaction", reactionId),
                                GameLog.KV("primary", reactionInfo.PrimaryElement),
                                GameLog.KV("secondary", reactionInfo.SecondaryElement),
                                GameLog.KV("target", target.Name)
                            ));
                            
                            // Trigger reaction effect (QTE panel or direct execution)
                            // Note: marks are consumed inside TriggerMarkReaction
                            lastPlayerAction = PlayerLastAction.Reaction;
                            pendingSkillNumber = skillNumber;
                            TriggerMarkReaction(target, reactionInfo);
                        }
                    }
                    
                    // Update enemy UI to show marks
                    
                }
            }
        }
        
        // Apply skill post-hit effects via data-driven engine
        var effectCtx = new SkillEffectEngine.EffectContext
        {
            Player = player,
            Target = target,
            AllEnemies = enemies,
            DamageDealt = totalDamageDealt,
            AttackElement = attackElement,
            SkillName = skillName,
            SkillNumber = skillNumber
        };
        var effectResult = SkillEffectEngine.Execute(effects, effectCtx);
        
        // Show floating text for effects
        if (effectResult.HealAmount > 0)
            ShowHealToPlayer(effectResult.HealAmount);
        if (effectResult.ShieldGained > 0)
        {
            ShowShieldToPlayer(effectResult.ShieldGained, FloatingTextType.ShieldGain);
            lastPlayerAction = PlayerLastAction.Shield;
        }
        
        // Notify in-world combat arena of damage
        NotifyEnemyHit(target);
        
        // Check Frost Shield break (Frostcaller)
        CheckFrostShieldBreak(target);
        
        // Check Retaliation counter-attack (Shieldbearer)
        CheckRetaliationCounter(target);
        
        // Player could die from Frost Shield break or Retaliation
        if (!player.IsAlive())
        {
            EndCombat(false);
            return;
        }
        
        bool targetKilled = !target.IsAlive();
        if (targetKilled)
        {
            // Check Reborn mechanic (FallenChampion)
            if (target.ShouldReborn())
            {
                target.ActivateReborn();
                targetKilled = false;
                // Apply skill cooldown before ending turn
                player.UseSkillAndApplyEffects(skillNumber - 1);
                ForceEndPlayerTurnForReborn(target);
                return;
            }
            else
            {
                GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", target.Name)));
                NotifyEnemyDeath(target);
            }
        }
        
        // Check SlimeBoss split threshold (deferred until End Turn)
        if (target.IsAlive())
        {
            target.CheckSplitThreshold();
        }

        // Reaction effects are now processed separately via ProcessReactionAfterQTE,
        // so skill cooldowns/chains/on-kill must always apply here regardless of reaction state.

        // Apply skill cooldown and energy effects BEFORE possible early return
        player.UseSkillAndApplyEffects(skillNumber - 1);

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }

        // Handle on-kill effects from data (e.g., Ambush energy refund, DoubleUp chain)
        if (targetKilled && effectResult.HasOnKillBonus)
        {
            if (effectResult.OnKillEnergyRefundPercent > 0)
            {
                int refund = Mathf.RoundToInt(player.GetMaxEnergy() * (effectResult.OnKillEnergyRefundPercent / 100f));
                player.GainEnergy(refund);
            }
            if (effectResult.OnKillCooldownOverride > 0)
            {
                player.SetSkillCooldown(skillNumber - 1, effectResult.OnKillCooldownOverride);
            }
            if (effectResult.OnKillBonusDamageMultiplier > 0)
            {
                var nextTarget = GetFirstAliveEnemy();
                if (nextTarget != null)
                {
                    int chainDamage = Mathf.RoundToInt(totalDamageDealt * effectResult.OnKillBonusDamageMultiplier);
                    nextTarget.TakeDamage(chainDamage);
                    ShowDamageToEnemy(nextTarget, chainDamage, false);
                }
            }
            if (combatArena != null) combatArena.OnPlayerEnergyChanged();
        }
        
        // Track chain skills (data-driven, per-skill)
        // Using a chain skill: reset all other chains, then increment this one
        // Using a non-chain skill: reset ALL chains (breaks any active chain)
        if (skillDef?.chainSettings != null && skillDef.chainSettings.maxChainUses > 0)
        {
            player.ResetAllChainStacksExcept(skillNumber - 1);
            player.IncrementChainUse(skillNumber - 1, skillDef.chainSettings);
        }
        else
        {
            player.ResetAllChainStacksExcept(-1);
        }
        
        // Update CombatArena skill states (for ultimate availability)
        if (combatArena != null)
        {
            combatArena.OnPlayerEnergyChanged();
        }
        
        // Player can continue using skills if they have AP - turn does NOT end automatically
    }
    
    private int GetSkillAPCost(CharacterData character, int skillNumber)
    {
        switch (skillNumber)
        {
            case 1: return character.Skill1APCost;
            case 2: return character.Skill2APCost;
            case 3: return character.Skill3APCost;
            case 4: return character.Skill4APCost;
            case 5: return character.Skill5APCost;
            default: return 2;
        }
    }
    
    private int GetSkillMarkChance(CharacterData character, int skillNumber)
    {
        switch (skillNumber)
        {
            case 1: return character.Skill1MarkChance;
            case 2: return character.Skill2MarkChance;
            case 3: return character.Skill3MarkChance;
            case 4: return character.Skill4MarkChance;
            case 5: return character.Skill5MarkChance;
            default: return 0;
        }
    }
    
    private int GetSkillMarkCount(CharacterData character, int skillNumber)
    {
        switch (skillNumber)
        {
            case 1: return character.Skill1MarkCount;
            case 2: return character.Skill2MarkCount;
            case 3: return character.Skill3MarkCount;
            case 4: return character.Skill4MarkCount;
            case 5: return character.Skill5MarkCount;
            default: return 0;
        }
    }
    
    private System.Collections.IEnumerator DelayedEnemyTurn()
    {
        // Wait for floating text to be readable
        var fctManager = FloatingTextManager.Instance;
        float delay = fctManager != null ? fctManager.GetPlayerActionPause() : 0.5f;
        yield return new WaitForSeconds(delay);
        
        EnemyTurn();
    }
    
    private SkillDefinition GetSkillDefinition(int skillNumber)
    {
        var charDef = player.GetCharacter();
        if (charDef == null) return null;
        
        // Map skill number to character's skillIds via GameDataLoader
        string skillId = null;
        var characterDef = GameDataLoader.GetCharacterByNumericId(charDef.CharacterID);
        if (characterDef != null && characterDef.skillIds != null && skillNumber >= 1 && skillNumber - 1 < characterDef.skillIds.Count)
        {
            skillId = characterDef.skillIds[skillNumber - 1];
        }
        
        if (string.IsNullOrEmpty(skillId)) return null;
        return GameDataLoader.GetSkill(skillId);
    }
    
    // ExecuteSkillWithReaction removed — reaction processing is now handled by ProcessReactionAfterQTE
    
    private void DamageAllEnemies(int damage, Element? forcedElement = null, bool isCrit = false)
    {
        Element attackElement = forcedElement ?? player.GetAffinity();
        // Copy list to avoid modification during iteration (split can add enemies)
        var snapshot = new List<CombatEnemy>(enemies);
        foreach (var enemy in snapshot)
        {
            if (enemy.IsAlive())
            {
                int finalDamage = enemy.ApplyResistance(damage, attackElement);
                int resistPercent = enemy.CalculateResistance(attackElement);
                int hpBefore = enemy.Health;
                enemy.TakeDamage(finalDamage);
                GameLog.Combat(GameLog.Join(
                    "DamageApply",
                    GameLog.KV("target", enemy.Name),
                    GameLog.KV("resistTotal", $"{resistPercent}%"),
                    GameLog.KV("shieldBefore", 0),
                    GameLog.KV("shieldAbsorbed", 0),
                    GameLog.KV("shieldAfter", 0),
                    GameLog.KV("hpBefore", hpBefore),
                    GameLog.KV("dmgFinal", finalDamage),
                    GameLog.KV("hpAfter", enemy.Health),
                    GameLog.KV("source", "AoE")
                ));
                ShowDamageToEnemy(enemy, finalDamage, isCrit);
                
                // Check Frost Shield break
                CheckFrostShieldBreak(enemy);
                
                // Check Retaliation counter-attack
                CheckRetaliationCounter(enemy);
                
                // Player could die from Frost Shield break or Retaliation
                if (!player.IsAlive())
                {
                    EndCombat(false);
                    return;
                }
                
                if (!enemy.IsAlive())
                {
                    if (enemy.ShouldReborn())
                    {
                        enemy.ActivateReborn();
                        ForceEndPlayerTurnForReborn(enemy);
                        return;
                    }
                    else
                    {
                        GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", enemy.Name)));
                        NotifyEnemyDeath(enemy);
                    }
                }
                
                if (enemy.IsAlive())
                {
                    enemy.CheckSplitThreshold();
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
    
    /// <summary>
    /// Called after FallenChampion reborn: ends the player's turn and gives the reborn enemy an immediate attack.
    /// </summary>
    private void ForceEndPlayerTurnForReborn(CombatEnemy rebornEnemy)
    {
        isPlayerTurn = false;
        currentPhase = CombatPhase.EnemyPhase;
        GameLog.Combat(GameLog.Join("RebornTurnSteal",
            GameLog.KV("enemy", rebornEnemy.Name),
            GameLog.KV("turn", currentTurnNumber)));
        
        // Give the reborn enemy an immediate turn
        pendingAttackers.Clear();
        pendingAttackers.Add(rebornEnemy);
        currentAttackerIndex = 0;
        
        StartCoroutine(DelayedRebornAttack());
    }
    
    private System.Collections.IEnumerator DelayedRebornAttack()
    {
        yield return new WaitForSeconds(0.6f);
        ProcessNextEnemyAttack();
    }
    
    private void EnemyTurn()
    {
        StartCoroutine(EnemyTurnCoroutine());
    }
    
    private System.Collections.IEnumerator EnemyTurnCoroutine()
    {
        GameLog.Combat(GameLog.Join("EnemyPhaseStart", GameLog.KV("turn", currentTurnNumber)));
        
        // Collect all alive enemies that will attack
        // Turn order: 1) Check stun/freeze, 2) Apply DoT (even if stunned/frozen), 3) Attack (if not stunned/frozen)
        pendingAttackers.Clear();
        
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive())
            {
                // STEP 0: Tick skill cooldowns and buff/debuff durations
                enemy.TickSkillCooldowns();
                enemy.TickDamageBuff();
                
                // Tick Mark Block and apply Null Sigil damage when it expires
                int markBlockBefore = enemy.MarkBlockTurns;
                enemy.TickMarkBlock();
                if (markBlockBefore > 0 && enemy.MarkBlockTurns <= 0)
                {
                    ApplyNullSigilDamage(enemy);
                    if (!player.IsAlive())
                    {
                        EndCombat(false);
                        yield break;
                    }
                }
                
                // Tick HoT (heal over time) from Monsoon
                int hotHeal = enemy.TickHoT();
                if (hotHeal > 0)
                {
                    GameLog.Status(GameLog.Join(
                        "Tick",
                        GameLog.KV("target", enemy.Name),
                        GameLog.KV("type", "HoT"),
                        GameLog.KV("heal", hotHeal),
                        GameLog.KV("hpAfter", enemy.Health)
                    ));
                }
                
                // STEP 1: Check and consume stun or freeze at START of turn
                bool isStunned = enemy.CheckAndConsumeStun();
                bool isFrozen = !isStunned && enemy.CheckAndConsumeFreeze();
                
                // Show stun/freeze skip floating text
                if (isStunned)
                {
                    ShowTurnSkippedToEnemy(enemy, "Stunned!");
                    yield return new WaitForSeconds(0.3f);
                }
                else if (isFrozen)
                {
                    ShowTurnSkippedToEnemy(enemy, "Frozen!");
                    yield return new WaitForSeconds(0.3f);
                }
                
                // STEP 2: Tick DoT effects (DoT still ticks even when stunned/frozen)
                int dotDamage = enemy.TickDoTEffects();
                
                // Tick named DoTs from reactions (Ignite, Magma Scorch, etc.)
                int namedDotDamage = enemy.TickNamedDoTs();
                dotDamage += namedDotDamage;

                if (dotDamage > 0)
                {
                    GameLog.Status(GameLog.Join(
                        "Tick",
                        GameLog.KV("target", enemy.Name),
                        GameLog.KV("type", "DoT"),
                        GameLog.KV("tick", dotDamage),
                        GameLog.KV("hpAfter", enemy.Health)
                    ));
                }
                
                // Show DoT tick floating text
                if (dotDamage > 0)
                {
                    ShowDoTTickToEnemy(enemy, dotDamage, "Burn");
                    yield return new WaitForSeconds(0.25f); // Brief pause for DoT text
                }
                
                // Check if enemy died from DoT
                if (!enemy.IsAlive())
                {
                    GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", enemy.Name), GameLog.KV("source", "DoT")));
                    continue;
                }
                
                // STEP 3: If stunned or frozen, skip attack phase entirely
                if (isStunned)
                {
                    GameLog.Status(GameLog.Join(
                        "Tick",
                        GameLog.KV("target", enemy.Name),
                        GameLog.KV("type", "Stun"),
                        GameLog.KV("tick", "skip")
                    ));
                    continue;
                }
                if (isFrozen)
                {
                    GameLog.Status(GameLog.Join(
                        "Tick",
                        GameLog.KV("target", enemy.Name),
                        GameLog.KV("type", "Freeze"),
                        GameLog.KV("tick", "skip")
                    ));
                    continue;
                }
                
                // Not stunned/frozen, can attack
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
    
    // Tracks the current enemy skill being used for effect processing after QTE
    private string pendingEnemySkillId;
    private SkillDefinition pendingEnemySkillDef;
    
    private void ProcessNextEnemyAttack()
    {
        if (currentAttackerIndex >= pendingAttackers.Count)
        {
            EndEnemyTurn();
            return;
        }
        
        var enemy = pendingAttackers[currentAttackerIndex];
        
        // Select skill based on attack pattern / cooldowns (reactive for MirrorBoss)
        string skillId = enemy.ReactivePattern 
            ? enemy.GetReactiveSkillId(lastPlayerAction) 
            : enemy.GetNextSkillId();
        var skillDef = !string.IsNullOrEmpty(skillId) ? GameDataLoader.GetSkill(skillId) : null;
        string skillName = skillDef != null ? skillDef.displayName : "Attack";
        
        // Store for use after QTE resolution
        pendingEnemySkillId = skillId;
        pendingEnemySkillDef = skillDef;
        pendingDefensiveQTEAttacker = enemy;
        
        // Check if skill is non-damage (pure effect skill like Weaken, Arcane Shield, etc.)
        float damageMultiplier = 0f;
        bool hasDamageEffect = false;
        if (skillDef != null && skillDef.effects != null)
        {
            foreach (var eff in skillDef.effects)
            {
                if (eff.effectId == "eff_deal_damage" && eff.multiplier > 0f)
                {
                    damageMultiplier = eff.multiplier;
                    hasDamageEffect = true;
                    break;
                }
                if (eff.effectId == "eff_combo_attack" && eff.multiplier > 0f)
                {
                    damageMultiplier = eff.multiplier;
                    hasDamageEffect = true;
                    break;
                }
            }
        }
        
        // If skill has no damage component, apply effects immediately and skip QTE
        if (!hasDamageEffect)
        {
            ApplyEnemySkillEffects(enemy, skillDef);
            enemy.OnSkillUsed(skillId);
            
            GameLog.Combat(GameLog.Join(
                "EnemySkill",
                GameLog.KV("attacker", enemy.Name),
                GameLog.KV("skill", skillName),
                GameLog.KV("type", "effect_only")
            ));
            
            // Move to next attacker
            currentAttackerIndex++;
            if (currentAttackerIndex < pendingAttackers.Count)
            {
                Invoke(nameof(ProcessNextEnemyAttack), DELAY_BETWEEN_ENEMY_ATTACKS);
            }
            else
            {
                EndEnemyTurn();
            }
            return;
        }
        
        // Apply Granite Bastion crush bonus (StoneColossus: each stack adds 10% to Crush)
        float graniteCrushBonus = GetGraniteBastionCrushBonus(enemy);
        float effectiveDamageMultiplier = damageMultiplier + graniteCrushBonus;
        
        // Calculate damage using skill multiplier
        float baseDamageRaw = enemy.Damage * enemy.GetDamageMultiplier() * effectiveDamageMultiplier;
        float variance = UnityEngine.Random.Range(0.90f, 1.10f);
        int afterVariance = Mathf.RoundToInt(baseDamageRaw * variance);
        
        pendingDefensiveQTEBaseDamage = Mathf.RoundToInt(baseDamageRaw);
        pendingDefensiveQTEAfterVariance = afterVariance;
        pendingDefensiveQTEResistPercent = player.CalculateResistance(enemy.DamageElement);
        
        // Check ignoreArmor flag from skill effects
        bool ignoreArmor = false;
        bool ignoreShield = false;
        if (skillDef != null && skillDef.effects != null)
        {
            foreach (var eff in skillDef.effects)
            {
                if (eff.effectId == "eff_deal_damage")
                {
                    ignoreArmor = eff.ignoreArmor;
                    ignoreShield = eff.ignoreShield;
                    break;
                }
            }
        }
        
        if (ignoreArmor)
        {
            pendingDefensiveQTEDamage = afterVariance; // bypass resistance
        }
        else
        {
            pendingDefensiveQTEDamage = player.ApplyResistance(afterVariance, enemy.DamageElement);
        }
        
        // Apply Vulnerable debuff: increases damage taken
        float vulnMult = player.GetVulnerableMultiplier();
        if (vulnMult > 1f)
        {
            pendingDefensiveQTEDamage = Mathf.RoundToInt(pendingDefensiveQTEDamage * vulnMult);
        }

        GameLog.Combat(GameLog.Join(
            "EnemySkill",
            GameLog.KV("attacker", enemy.Name),
            GameLog.KV("skill", skillName),
            GameLog.KV("target", "Player"),
            GameLog.KV("element", enemy.DamageElement),
            GameLog.KV("multiplier", damageMultiplier.ToString("F2")),
            GameLog.KV("base", pendingDefensiveQTEBaseDamage),
            GameLog.KV("variance", pendingDefensiveQTEAfterVariance),
            GameLog.KV("preResist", pendingDefensiveQTEDamage),
            GameLog.KV("ignoreArmor", ignoreArmor),
            GameLog.KV("ignoreShield", ignoreShield)
        ));

        if (pendingDefensiveQTEDamage <= 0)
        {
            OnDefensiveQTEComplete(enemy, DefensiveQTEResult.Bad);
            return;
        }

        // Skip defensive QTE if player is stunned — they can't defend
        if (player.IsPlayerStunned)
        {
            GameLog.Combat(GameLog.Join("DefensiveQTESkipped", GameLog.KV("reason", "PlayerStunned")));
            OnDefensiveQTEComplete(enemy, DefensiveQTEResult.Bad);
            return;
        }
        
        // Early Guard Override: skip defensive QTE for first N turns
        if (player.IsDefensiveQTEDisabled(currentTurnNumber))
        {
            GameLog.Combat(GameLog.Join("DefensiveQTESkipped", GameLog.KV("reason", "EarlyGuardOverride")));
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

        // Convert DefensiveQTEResult to QTEResult for data lookup
        QTEResult dataResult = qteResult switch
        {
            DefensiveQTEResult.Perfect => QTEResult.Perfect,
            DefensiveQTEResult.Good => QTEResult.Good,
            _ => QTEResult.Bad
        };
        
        // Apply shield based on QTE result (percentage of max HP)
        int shieldPercent = ReactionQTE.GetQteDefensiveShieldPercent(dataResult);
        int shieldAmount = Mathf.RoundToInt(player.GetMaxHealth() * (shieldPercent / 100f));
        if (shieldAmount > 0)
        {
            player.AddShield(shieldAmount);
            GameLog.Combat(GameLog.Join(
                "ShieldGain",
                GameLog.KV("who", "Player"),
                GameLog.KV("amount", shieldAmount),
                GameLog.KV("source", $"DefensiveQTE:{qteResult}")
            ));
            
            ShowShieldToPlayer(shieldAmount, FloatingTextType.ShieldGain);
            
        }

        GameLog.Combat(GameLog.Join(
            "DefensiveQTE",
            GameLog.KV("attacker", enemy.Name),
            GameLog.KV("result", qteResult),
            GameLog.KV("shieldGained", shieldAmount)
        ), GameLogVerbosity.Verbose);

        if (pendingDefensiveQTEDamage > 0)
        {
            int shieldBefore = player.GetShield();
            int hpBefore = player.GetHealth();
            player.TakeDamage(pendingDefensiveQTEDamage);
            var dmgInfo = player.GetLastDamageInfo();

            GameLog.Combat(GameLog.Join(
                "DamageApply",
                GameLog.KV("target", "Player"),
                GameLog.KV("resistTotal", $"{pendingDefensiveQTEResistPercent}%"),
                GameLog.KV("shieldBefore", shieldBefore),
                GameLog.KV("shieldAbsorbed", dmgInfo.shieldAbsorbed),
                GameLog.KV("shieldAfter", player.GetShield()),
                GameLog.KV("hpBefore", hpBefore),
                GameLog.KV("dmgFinal", dmgInfo.finalDamage),
                GameLog.KV("hpAfter", player.GetHealth())
            ));

            if (dmgInfo.shieldAbsorbed > 0)
            {
                ShowShieldToPlayer(dmgInfo.shieldAbsorbed, FloatingTextType.ShieldAbsorb);
            }
            if (dmgInfo.shieldBroken)
            {
                ShowShieldToPlayer(0, FloatingTextType.ShieldBroken);
            }
            if (dmgInfo.finalDamage > 0)
            {
                ShowDamageToPlayer(dmgInfo.finalDamage);
            }
        }
        
        // HealOnHit: if this enemy has the debuff, heal player (once per attack)
        CheckHealOnHit(enemy);

        if (!player.IsAlive())
        {
            EndCombat(false);
            return;
        }

        // Apply post-damage skill effects (statuses, lifesteal, etc.)
        if (pendingEnemySkillDef != null)
        {
            ApplyEnemySkillEffects(enemy, pendingEnemySkillDef);
        }
        
        // Track skill usage (cooldown, pattern advance)
        if (!string.IsNullOrEmpty(pendingEnemySkillId))
        {
            enemy.OnSkillUsed(pendingEnemySkillId);
        }
        
        // Check for SlimeBoss split after player was hit (enemy turn context — no split here)
        // Split and Reborn checks only apply when the enemy takes damage, not the player

        pendingDefensiveQTEAttacker = null;
        pendingDefensiveQTEDamage = 0;
        pendingDefensiveQTEBaseDamage = 0;
        pendingDefensiveQTEAfterVariance = 0;
        pendingDefensiveQTEResistPercent = 0;
        pendingEnemySkillId = null;
        pendingEnemySkillDef = null;
        
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
    
    /// <summary>
    /// Apply non-damage effects from an enemy skill definition.
    /// Handles: status effects on player, self-buffs on enemy, enemy shield, HoT, etc.
    /// </summary>
    private void ApplyEnemySkillEffects(CombatEnemy enemy, SkillDefinition skillDef)
    {
        if (skillDef == null || skillDef.effects == null) return;
        
        foreach (var eff in skillDef.effects)
        {
            if (eff == null || string.IsNullOrEmpty(eff.effectId)) continue;
            
            switch (eff.effectId)
            {
                case "eff_apply_status":
                    ApplyEnemyStatusEffect(enemy, eff);
                    break;
                    
                case "eff_enemy_shield":
                    ApplyEnemyShieldEffect(enemy, eff);
                    break;
                    
                case "eff_lifesteal":
                    // Enemy lifesteal: heal based on damage dealt
                    if (eff.target == "Self" && eff.percent > 0 && pendingDefensiveQTEDamage > 0)
                    {
                        int healAmount = Mathf.RoundToInt(pendingDefensiveQTEDamage * (eff.percent / 100f));
                        enemy.Health = Mathf.Min(enemy.Health + healAmount, enemy.MaxHealth);
                        GameLog.Combat(GameLog.Join(
                            "EnemyLifesteal",
                            GameLog.KV("enemy", enemy.Name),
                            GameLog.KV("heal", healAmount),
                            GameLog.KV("hpAfter", enemy.Health)
                        ));
                    }
                    break;
                    
                case "eff_dot":
                    // DoT applied to the player
                    if (eff.target == "Player" && eff.multiplier > 0f)
                    {
                        int dotDamagePerTick = Mathf.RoundToInt(enemy.Damage * enemy.GetDamageMultiplier() * eff.multiplier);
                        int dotDuration = eff.duration > 0 ? eff.duration : 2;
                        player.ApplyDoT(dotDamagePerTick, dotDuration, skillDef.displayName);
                        GameLog.Status(GameLog.Join(
                            "Apply",
                            GameLog.KV("target", "Player"),
                            GameLog.KV("type", "DoT"),
                            GameLog.KV("damage", dotDamagePerTick),
                            GameLog.KV("duration", dotDuration),
                            GameLog.KV("source", skillDef.displayName)
                        ));
                    }
                    break;
                    
                case "eff_syphon_shield":
                    // Steal all player shield and deal damage based on it
                    if (eff.target == "Player")
                    {
                        int stolenShield = player.GetShield();
                        if (stolenShield > 0)
                        {
                            player.RemoveAllShield();
                            int syphonDamage = Mathf.RoundToInt(stolenShield * eff.damagePerPoint);
                            player.TakeDamage(syphonDamage);
                            GameLog.Combat(GameLog.Join(
                                "SyphonMagic",
                                GameLog.KV("attacker", enemy.Name),
                                GameLog.KV("shieldStolen", stolenShield),
                                GameLog.KV("damage", syphonDamage)
                            ));
                            ShowDamageToPlayer(syphonDamage);
                        }
                    }
                    break;
                    
                case "eff_split":
                    // Handled separately in CheckBossSplitAfterDamage
                    break;
                    
                case "eff_reborn":
                    // Handled separately in death check
                    break;
                    
                case "eff_mirror_copy":
                    // Mirror Reaction: handled in MirrorBoss reactive turn processing
                    break;
                    
                case "eff_combo_attack":
                    // Combo damage is handled in ProcessNextEnemyAttack damage calculation
                    break;
                    
                case "eff_deal_damage":
                    // Damage is handled in ProcessNextEnemyAttack damage calculation
                    break;
                    
                case "eff_multi_hit":
                    // Multi-hit is handled in ProcessNextEnemyAttack (multiple QTE rounds)
                    break;
            }
        }
    }
    
    /// <summary>
    /// Apply a status effect from an enemy skill to the appropriate target.
    /// </summary>
    private void ApplyEnemyStatusEffect(CombatEnemy enemy, EffectEntry eff)
    {
        string target = eff.target ?? "Player";
        string status = eff.status ?? "";
        int duration = eff.duration;
        float magnitude = eff.magnitude;
        
        switch (status)
        {
            case "status_player_weaken":
                if (target == "Player")
                {
                    player.ApplyWeaken(magnitude, duration);
                    GameLog.Status(GameLog.Join("Apply",
                        GameLog.KV("target", "Player"), GameLog.KV("type", "Weaken"),
                        GameLog.KV("magnitude", magnitude), GameLog.KV("duration", duration)));
                }
                break;
                
            case "status_player_sunder":
                if (target == "Player")
                {
                    player.ApplySunder(magnitude, duration);
                    GameLog.Status(GameLog.Join("Apply",
                        GameLog.KV("target", "Player"), GameLog.KV("type", "Sunder"),
                        GameLog.KV("magnitude", magnitude), GameLog.KV("duration", duration)));
                }
                break;
                
            case "status_player_vulnerable":
                if (target == "Player")
                {
                    player.ApplyVulnerable(magnitude, duration, eff.maxStacks);
                    GameLog.Status(GameLog.Join("Apply",
                        GameLog.KV("target", "Player"), GameLog.KV("type", "Vulnerable"),
                        GameLog.KV("magnitude", magnitude), GameLog.KV("duration", duration),
                        GameLog.KV("maxStacks", eff.maxStacks)));
                }
                break;
                
            case "status_player_stun":
                if (target == "Player")
                {
                    player.ApplyStun(duration);
                    GameLog.Status(GameLog.Join("Apply",
                        GameLog.KV("target", "Player"), GameLog.KV("type", "Stun"),
                        GameLog.KV("duration", duration)));
                }
                break;
                
            case "status_enemy_damage_up":
                // Apply to all allies (all enemies)
                if (target == "AllAllies")
                {
                    foreach (var ally in enemies)
                    {
                        if (ally.IsAlive())
                        {
                            ally.DamageBuffPercent += magnitude;
                            ally.DamageBuffTurns = Mathf.Max(ally.DamageBuffTurns, duration);
                        }
                    }
                    GameLog.Status(GameLog.Join("Apply",
                        GameLog.KV("target", "AllEnemies"), GameLog.KV("type", "DamageBuff"),
                        GameLog.KV("magnitude", magnitude), GameLog.KV("duration", duration)));
                }
                break;
                
            case "status_enemy_hot":
                // Apply HoT to all allies (all enemies)
                if (target == "AllAllies")
                {
                    foreach (var ally in enemies)
                    {
                        if (ally.IsAlive())
                        {
                            ally.HoTActive = true;
                            ally.HoTAmount = Mathf.RoundToInt(magnitude);
                        }
                    }
                    GameLog.Status(GameLog.Join("Apply",
                        GameLog.KV("target", "AllEnemies"), GameLog.KV("type", "HoT"),
                        GameLog.KV("magnitude", magnitude)));
                }
                break;
                
            case "status_retaliation":
                if (target == "Self")
                {
                    enemy.RetaliationActive = true;
                    enemy.RetaliationDamageMult = magnitude;
                    GameLog.Status(GameLog.Join("Apply",
                        GameLog.KV("target", enemy.Name), GameLog.KV("type", "Retaliation"),
                        GameLog.KV("magnitude", magnitude)));
                }
                break;
                
            case "status_frost_shield":
                if (target == "Self")
                {
                    enemy.ActivateFrostShield(
                        Mathf.RoundToInt(magnitude),
                        eff.breakThreshold,
                        eff.breakDamageMultiplier
                    );
                    GameLog.Status(GameLog.Join("Apply",
                        GameLog.KV("target", enemy.Name), GameLog.KV("type", "FrostShield"),
                        GameLog.KV("resistBonus", magnitude),
                        GameLog.KV("breakThreshold", eff.breakThreshold),
                        GameLog.KV("breakDamageMult", eff.breakDamageMultiplier)));
                }
                break;
                
            case "status_mark_block":
                if (target == "Self")
                {
                    enemy.MarkBlockTurns = duration;
                    enemy.MarkBlockDamageMult = magnitude;
                    enemy.MarksBlocked = 0;
                    GameLog.Status(GameLog.Join("Apply",
                        GameLog.KV("target", enemy.Name), GameLog.KV("type", "MarkBlock"),
                        GameLog.KV("duration", duration), GameLog.KV("damageMult", magnitude)));
                }
                break;
                
            case "status_granite_bastion":
                if (target == "Self")
                {
                    enemy.GraniteStacks++;
                    GameLog.Status(GameLog.Join("Apply",
                        GameLog.KV("target", enemy.Name), GameLog.KV("type", "GraniteBastion"),
                        GameLog.KV("stacks", enemy.GraniteStacks),
                        GameLog.KV("crushBonus", $"{enemy.GraniteStacks * magnitude}%")));
                }
                break;
                
            default:
                GameLog.Warn(GameLogCategory.System, "[CombatManager]",
                    GameLog.Join("UnknownEnemyStatus", GameLog.KV("status", status)));
                break;
        }
    }
    
    /// <summary>
    /// Apply enemy shield effect (Arcane Shield, Mirror Shield, Granite Bastion).
    /// </summary>
    private void ApplyEnemyShieldEffect(CombatEnemy enemy, EffectEntry eff)
    {
        if (eff.percentOfMaxHealth > 0)
        {
            int shieldAmount = Mathf.RoundToInt(enemy.MaxHealth * (eff.percentOfMaxHealth / 100f));
            enemy.AddShield(shieldAmount);
            GameLog.Combat(GameLog.Join(
                "EnemyShieldGain",
                GameLog.KV("enemy", enemy.Name),
                GameLog.KV("amount", shieldAmount),
                GameLog.KV("totalShield", enemy.Shield)
            ));
        }
        else if (eff.value > 0)
        {
            enemy.AddShield(eff.value);
            GameLog.Combat(GameLog.Join(
                "EnemyShieldGain",
                GameLog.KV("enemy", enemy.Name),
                GameLog.KV("amount", eff.value),
                GameLog.KV("totalShield", enemy.Shield)
            ));
        }
    }
    
    /// <summary>
    /// Check if Frost Shield broke from accumulated damage. If so, deal break damage to player.
    /// </summary>
    private void CheckFrostShieldBreak(CombatEnemy enemy)
    {
        int breakDamage = enemy.CheckFrostShieldBreak();
        if (breakDamage > 0)
        {
            player.TakeDamage(breakDamage);
            ShowDamageToPlayer(breakDamage);
            GameLog.Combat(GameLog.Join(
                "FrostShieldBreak",
                GameLog.KV("enemy", enemy.Name),
                GameLog.KV("damage", breakDamage)
            ));
        }
    }
    
    /// <summary>
    /// Check if the attacked enemy has Retaliation active. If so, counter-attack the player.
    /// </summary>
    private void CheckRetaliationCounter(CombatEnemy enemy)
    {
        if (!enemy.RetaliationActive || !enemy.IsAlive()) return;
        
        int counterDamage = Mathf.RoundToInt(enemy.Damage * enemy.RetaliationDamageMult);
        if (counterDamage > 0)
        {
            player.TakeDamage(counterDamage);
            ShowDamageToPlayer(counterDamage);
            GameLog.Combat(GameLog.Join(
                "RetaliationCounter",
                GameLog.KV("enemy", enemy.Name),
                GameLog.KV("damage", counterDamage)
            ));
        }
    }
    
    /// <summary>
    /// Execute SlimeBoss split: remove SlimeBoss and spawn MadSlime + SadSlime.
    /// Each spawned slime gets a percentage of the SlimeBoss's current health.
    /// </summary>
    private void ExecuteSlimeBossSplit(CombatEnemy slimeBoss)
    {
        slimeBoss.MarkAsSplit();
        int currentHealth = slimeBoss.Health;
        
        // Kill the SlimeBoss
        slimeBoss.Health = 0;
        GameLog.Combat(GameLog.Join("SlimeBossSplit",
            GameLog.KV("boss", slimeBoss.Name),
            GameLog.KV("healthAtSplit", currentHealth)));
        NotifyEnemyDeath(slimeBoss);
        
        if (slimeBoss.SplitInto == null) return;
        
        // Spawn sub-enemies
        int world = GameManager.CurrentWorld;
        
        foreach (string spawnName in slimeBoss.SplitInto)
        {
            EnemyData spawnData = DataCache.GetEnemyByName(spawnName);
            if (spawnData == null)
            {
                GameLog.Warn(GameLogCategory.System, "[CombatManager]",
                    GameLog.Join("SplitSpawnFail", GameLog.KV("name", spawnName)));
                continue;
            }
            
            var spawned = new CombatEnemy(spawnData, world);
            // Set health to percentage of SlimeBoss's current health at split
            int splitHealth = Mathf.RoundToInt(currentHealth * (slimeBoss.SplitHealthPercent / 100f));
            spawned.Health = Mathf.Min(splitHealth, spawned.MaxHealth);
            spawned.MaxHealth = spawned.Health;
            
            enemies.Add(spawned);
            
            GameLog.Combat(GameLog.Join("SplitSpawn",
                GameLog.KV("name", spawned.Name),
                GameLog.KV("health", spawned.Health),
                GameLog.KV("damage", spawned.Damage)));
        }
        
        // Notify combat arena to update enemy visuals
        if (combatArena != null)
        {
            combatArena.OnEnemiesChanged(enemies);
        }
    }
    
    /// <summary>
    /// Get Granite Bastion crush damage bonus for StoneColossus.
    /// Each stack adds the magnitude% to the crush skill multiplier.
    /// </summary>
    private float GetGraniteBastionCrushBonus(CombatEnemy enemy)
    {
        if (enemy.GraniteStacks <= 0) return 0f;
        
        // Each stack adds 10% (magnitude from status_granite_bastion)
        // The magnitude is stored per-stack as 10 in the skill definition
        return enemy.GraniteStacks * 0.10f;
    }
    
    /// <summary>
    /// Calculate and deal Null Sigil mark block damage to the player.
    /// Called when mark block expires or when the StormCaptain attacks.
    /// </summary>
    private void ApplyNullSigilDamage(CombatEnemy enemy)
    {
        if (enemy.MarksBlocked <= 0 || enemy.MarkBlockDamageMult <= 0f) return;
        
        int sigilDamage = Mathf.RoundToInt(enemy.Damage * enemy.MarkBlockDamageMult * enemy.MarksBlocked);
        if (sigilDamage > 0)
        {
            player.TakeDamage(sigilDamage);
            ShowDamageToPlayer(sigilDamage);
            GameLog.Combat(GameLog.Join(
                "NullSigilDamage",
                GameLog.KV("enemy", enemy.Name),
                GameLog.KV("marksBlocked", enemy.MarksBlocked),
                GameLog.KV("damage", sigilDamage)
            ));
        }
        enemy.MarksBlocked = 0;
    }
    
    private void EndEnemyTurn()
    {
        pendingAttackers.Clear();
        
        GameLog.Combat(GameLog.Join("EnemyPhaseEnd", GameLog.KV("turn", currentTurnNumber)));
        
        // If combat is ending, do not process turn advancement
        if (isEndingCombat)
        {
            GameLog.Combat(GameLog.Join(
                "TurnAdvanceBlocked",
                GameLog.KV("state", "EndingCombat")
            ), GameLogVerbosity.Verbose);
            return;
        }
        
        if (!player.IsAlive())
        {
            EndCombat(false);
            return;
        }

        // ========== BETWEEN-TURN TICKS ==========
        // These always happen between Enemy Phase end and next Player Phase start,
        // regardless of whether the player is stunned.
        
        // Tick temp resist durations for all combatants
        player.TickTempResists();
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive())
            {
                enemy.TickTempResists();
                enemy.TickReactionDebuffs();
                enemy.TickReactionChips();
            }
        }

        // Tick player debuff durations
        player.TickDebuffs();
        
        // Tick reaction buff durations
        player.TickReactionBuffs();
        player.TickReactionChips();
        
        // Tick player DoT
        int playerDotDmg = player.TickPlayerDoT();
        if (playerDotDmg > 0)
        {
            ShowDamageToPlayer(playerDotDmg);
            if (!player.IsAlive())
            {
                EndCombat(false);
                return;
            }
        }
        
        // Tick down cooldowns ALWAYS (even when stunned — time still passes)
        player.TickCooldowns();
        
        // ========== ADVANCE TURN ==========
        currentTurnNumber++;
        
        // Check player stun (Hydra Tail)
        if (player.CheckAndConsumePlayerStun())
        {
            GameLog.Status(GameLog.Join(
                "PlayerPhaseSkipped",
                GameLog.KV("turn", currentTurnNumber),
                GameLog.KV("reason", "Stunned")
            ));
            // Skip player phase entirely — go straight to enemy phase
            isPlayerTurn = false;
            currentPhase = CombatPhase.EnemyPhase;
            EnemyTurn();
            return;
        }
        
        // ========== PLAYER PHASE START ==========
        isPlayerTurn = true;
        currentPhase = CombatPhase.PlayerPhase;
        
        // Refresh AP at start of player's turn
        player.RefreshAP();
        
        // Process relic turn-start effects (AP banking, Siphoning Aura, Mark Echo, Rhythm Discount)
        player.OnRelicTurnStart();
        
        GameLog.Combat(GameLog.Join("PlayerPhaseStart", GameLog.KV("turn", currentTurnNumber)));
        
        // Reset AP in CombatArena UI
        if (combatArena != null)
        {
            combatArena.OnPlayerTurnStart();
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
        // Guard: if no enemies were spawned, don't consider it a victory
        if (enemies.Count == 0) return false;
        
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive()) return false;
        }
        return true;
    }

    /// <summary>
    /// Apply relic effects at combat start that need enemy access (status application, marks).
    /// Called after player.OnCombatStart() which handles player-side effects.
    /// </summary>
    private void ApplyCombatStartRelicEffects()
    {
        if (player == null) return;
        
        var combatStartRelics = player.GetRelicsByTrigger("combatStart");
        foreach (var relic in combatStartRelics)
        {
            if (relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                if (eff.effectId == "eff_apply_status")
                {
                    if (eff.target == "AllEnemies")
                    {
                        // Ambush Seal: Weaken+Vulnerable on all enemies
                        foreach (var enemy in enemies)
                        {
                            if (!enemy.IsAlive()) continue;
                            string s = (eff.status ?? "").ToLower();
                            if (s.Contains("weak")) enemy.ApplyWeak(eff.duration, eff.magnitude);
                            else if (s.Contains("vulnerable")) enemy.ApplyTempResistAll(-Mathf.RoundToInt(eff.magnitude), eff.duration);
                        }
                        Debug.Log($"[CombatManager] Relic {relic.DisplayName}: applied {eff.status} to all enemies for {eff.duration} turns");
                    }
                    else if (eff.target == "Self")
                    {
                        // Crippling Weakness / Rustbound Sunder on player
                        string s = (eff.status ?? "").ToLower();
                        if (s.Contains("weaken")) player.ApplyWeaken(eff.magnitude, eff.duration);
                        else if (s.Contains("sunder")) player.ApplySunder(eff.magnitude, eff.duration);
                        Debug.Log($"[CombatManager] Relic {relic.DisplayName}: applied {eff.status} to player for {eff.duration} turns");
                    }
                }
                // Elemental Broadcast: apply 1 of equipped element mark to all enemies
                else if (eff.effectId == "eff_apply_equipped_mark")
                {
                    Element equipped = player.GetAffinity();
                    if (equipped != Element.None)
                    {
                        foreach (var enemy in enemies)
                        {
                            if (!enemy.IsAlive()) continue;
                            enemy.AddMarks(equipped, eff.extraMarks);
                        }
                        Debug.Log($"[CombatManager] Relic {relic.DisplayName}: applied {eff.extraMarks} {equipped} mark(s) to all enemies");
                    }
                }
            }
        }
        
        // Show floating heal text for combat start heal (Field Rations)
        int combatStartHeal = player.GetLastCombatStartHeal();
        if (combatStartHeal > 0)
        {
            ShowHealToPlayer(combatStartHeal);
        }
    }
    
    /// <summary>
    /// Apply relic effects at end of player turn that need enemy access (Elemental Drip).
    /// Called after player.OnRelicTurnEnd() which handles player-side effects.
    /// </summary>
    private void ApplyTurnEndRelicEffects()
    {
        if (player == null) return;
        
        var turnEndRelics = player.GetRelicsByTrigger("onTurnEnd");
        foreach (var relic in turnEndRelics)
        {
            if (relic.Effects == null) continue;
            foreach (var eff in relic.Effects)
            {
                // Elemental Drip: apply mark of equipped element to random alive enemy
                if (eff.effectId == "eff_end_turn_mark")
                {
                    Element equipped = player.GetAffinity();
                    if (equipped == Element.None) continue;
                    
                    var alive = new List<CombatEnemy>();
                    foreach (var e in enemies) { if (e.IsAlive()) alive.Add(e); }
                    
                    if (alive.Count > 0)
                    {
                        var target = alive[Random.Range(0, alive.Count)];
                        target.AddMarks(equipped, eff.extraMarks);
                        Debug.Log($"[CombatManager] Relic {relic.DisplayName}: applied {eff.extraMarks} {equipped} mark to {target.Name}");
                    }
                }
            }
        }
    }
    
    private void EndCombat(bool victory)
    {
        // Idempotent: if already ending, skip
        if (isEndingCombat) return;
        isEndingCombat = true;
        combatActive = false;
        currentPhase = CombatPhase.None;
        
        // If combat ends on player's turn (victory by killing last enemy),
        // the turn is considered completed - tick cooldowns so skills progress
        if (victory && isPlayerTurn && player != null)
        {
            player.TickCooldowns();
            GameLog.Combat("CooldownTickOnVictory", GameLogVerbosity.Verbose);
        }
        
        GameLog.Combat(GameLog.Join(
            "VictoryTriggered",
            GameLog.KV("reason", "AllEnemiesDead"),
            GameLog.KV("victory", victory)
        ));
        GameLog.Combat(GameLog.Join(
            "CombatEnd",
            GameLog.KV("victory", victory),
            GameLog.KV("type", currentCombatType)
        ));

        if (victory)
        {
            // Process relic combat-end effects (Field Rations heal, Blood Toll damage)
            if (player != null) player.OnRelicCombatEnd();
            
            // Calculate total rewards from all defeated enemies
            int totalXP = 0;
            int totalGold = 0;
            bool dropsSigil = false;
            bool dropsRelic = false;
            
            int totalRegularCores = 0;
            int totalAscendedCores = 0;
            
            foreach (var enemy in enemies)
            {
                totalXP += enemy.RewardXP;
                // Roll gold within the enemy's gold range
                totalGold += Random.Range(enemy.RewardGoldMin, enemy.RewardGoldMax + 1);
                
                // Roll for sigil drop (suppressed if Sigil Renounce active)
                if (enemy.SigilChance > 0 && Random.Range(0, 100) < enemy.SigilChance
                    && (player == null || !player.AreSigilsDisabled()))
                {
                    dropsSigil = true;
                }
                
                // Roll for relic drop
                if (enemy.RelicChance > 0 && Random.Range(0, 100) < enemy.RelicChance)
                {
                    dropsRelic = true;
                }
                
                // Roll for Regular Essence Core drop
                if (enemy.RegularCoreChance > 0 && Random.Range(0, 100) < enemy.RegularCoreChance)
                {
                    totalRegularCores++;
                }
                
                // Roll for Ascended Essence Core drop
                if (enemy.AscendedCoreChance > 0 && Random.Range(0, 100) < enemy.AscendedCoreChance)
                {
                    totalAscendedCores++;
                }
            }
            
            // Award Essence Cores to player inventory
            if (totalRegularCores > 0 || totalAscendedCores > 0)
            {
                GameManager.AddRunCores(totalRegularCores, totalAscendedCores);
            }
            
            GameLog.Combat(GameLog.Join(
                "CombatRewards",
                GameLog.KV("type", currentCombatType),
                GameLog.KV("xp", totalXP),
                GameLog.KV("gold", totalGold),
                GameLog.KV("sigil", dropsSigil),
                GameLog.KV("relic", dropsRelic),
                GameLog.KV("regularCores", totalRegularCores),
                GameLog.KV("ascendedCores", totalAscendedCores)
            ));
            
            // Hide CombatArena UI before showing loot panel (so it doesn't block input)
            if (combatArena != null)
            {
                combatArena.HideCombatUIForLoot();
            }
            
            // Show reward UI for all combat victories
            if (combatUI == null) combatUI = FindFirstObjectByType<CombatUI>();
            if (combatUI != null)
            {
                bool isElite = currentCombatType == CombatType.Elite;
                bool isBoss = currentCombatType == CombatType.Boss;
                combatUI.ShowLootPanel(totalGold, totalXP, player, currentNode, null, isElite, isBoss, dropsSigil, dropsRelic);
            }
            
            // For boss combat, invoke callback after loot is collected (handled in OnLootCollected)
            if (currentCombatType == CombatType.Boss)
            {
                // Store callback to be invoked after loot collection
                // The callback will be invoked in OnLootCollected
                return;
            }
        }
        else
        {
            // Exit in-world combat arena on defeat
            if (combatArena != null)
            {
                combatArena.ExitCombat();
            }
            
            if (combatUI != null)
            {
                combatUI.HideCombat();
            }
            
            // Re-enable movement on defeat (player can move after game over screen)
            var pcDefeat = FindFirstObjectByType<PlayerController>();
            if (pcDefeat != null) pcDefeat.SetCanMove(true);
            Debug.Log("[CombatManager] Combat ended (defeat) - movement re-enabled");
            
            GameLog.Combat("PlayerDefeated");
            
            // Notify GameManager of player defeat
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.OnPlayerDefeated();
            }
        }
    }

    public void OnLootCollected()
    {
        // Capture combat state BEFORE side-effects (OnNodeCompleted may trigger
        // StartBossCombat which overwrites currentCombatType / onBossCombatComplete)
        var completedType = currentCombatType;
        System.Action<bool> bossCallback = null;
        if (completedType == CombatType.Boss && onBossCombatComplete != null)
        {
            bossCallback = onBossCombatComplete;
            onBossCombatComplete = null;
        }
        
        // Exit in-world combat arena on victory
        if (combatArena != null)
        {
            combatArena.ExitCombat();
        }
        
        if (combatUI != null)
        {
            combatUI.HideCombat();
        }

        if (currentNode != null)
        {
            currentNode.OnNodeCompleted();
        }

        currentNode = null;

        // Movement is re-enabled by CombatArena.TransitionFromCombat AFTER transition completes
        // This prevents player from walking into new combat during exit transition
        
        // Handle boss combat callback after loot collection
        if (bossCallback != null)
        {
            bossCallback.Invoke(true);
        }
    }

    public bool IsCombatActive() => combatActive;
    public bool IsInCombat() => combatActive;
    public bool IsEndingCombat() => isEndingCombat;
    public bool IsPlayerTurn() => isPlayerTurn;
    public CombatPhase GetCurrentPhase() => currentPhase;
    public int GetCurrentTurnNumber() => currentTurnNumber;
    public List<CombatEnemy> GetEnemies() => enemies;
    
    /// <summary>
    /// Notify the in-world combat arena that an enemy was hit (for visual feedback)
    /// </summary>
    private void NotifyEnemyHit(CombatEnemy enemy)
    {
        if (combatArena == null) return;
        var unit = combatArena.GetEnemyUnit(enemy);
        if (unit != null)
        {
            unit.FlashDamage();
        }
    }
    
    /// <summary>
    /// Notify the in-world combat arena that an enemy died
    /// </summary>
    private void NotifyEnemyDeath(CombatEnemy enemy)
    {
        // Mark Transfer relic: transfer marks from dying enemy to random alive enemy
        if (player != null && player.HasMarkTransfer())
        {
            var marks = enemy.GetMarks();
            if (marks.Count > 0)
            {
                var alive = new List<CombatEnemy>();
                foreach (var e in enemies) { if (e.IsAlive() && e != enemy) alive.Add(e); }
                
                if (alive.Count > 0)
                {
                    var recipient = alive[Random.Range(0, alive.Count)];
                    int transferCount = player.GetMarkTransferCount();
                    foreach (var kvp in marks)
                    {
                        int toTransfer = Mathf.Min(kvp.Value, transferCount);
                        if (toTransfer > 0)
                        {
                            recipient.AddMarks(kvp.Key, toTransfer);
                        }
                    }
                    Debug.Log($"[CombatManager] Mark Transfer: moved marks from {enemy.Name} to {recipient.Name}");
                }
            }
        }
        
        // Track boss defeats for FallenChampion spawn requirement
        if (enemy.IsBoss && !enemy.SpawnOnly)
        {
            bossesDefeatedThisRun++;
            GameLog.Combat(GameLog.Join("BossDefeated",
                GameLog.KV("boss", enemy.Name),
                GameLog.KV("totalBossesDefeated", bossesDefeatedThisRun)));
        }
        
        if (combatArena == null) return;
        
        // Auto-advance target selection if the dead enemy was the selected target
        combatArena.OnEnemyDied(enemy);
        
        var unit = combatArena.GetEnemyUnit(enemy);
        if (unit != null)
        {
            unit.PlayDeathAnimation();
        }
    }
    
    #region Floating Text Helpers
    
    /// <summary>
    /// Show damage floating text on an enemy using CombatArena's world transform
    /// </summary>
    private void ShowDamageToEnemy(CombatEnemy enemy, int damage, bool isCrit)
    {
        var ftm = FloatingTextManager.Instance;
        if (ftm == null) return;
        
        // Try CombatArena first (in-world combat)
        if (combatArena != null)
        {
            var transform = combatArena.GetEnemyTransform(enemy);
            if (transform != null)
            {
                ftm.ShowDamage(transform, damage, isCrit);
                return;
            }
        }
        
    }
    
    /// <summary>
    /// Show damage floating text on player using CombatArena's world transform
    /// </summary>
    private void ShowDamageToPlayer(int damage)
    {
        var ftm = FloatingTextManager.Instance;
        if (ftm == null) return;
        
        // Try CombatArena first (in-world combat)
        if (combatArena != null)
        {
            var transform = combatArena.GetPlayerTransform();
            if (transform != null)
            {
                ftm.ShowDamageTaken(transform, damage);
                return;
            }
        }
        
    }
    
    /// <summary>
    /// Show heal floating text on player
    /// </summary>
    private void ShowHealToPlayer(int amount)
    {
        var ftm = FloatingTextManager.Instance;
        if (ftm == null) return;
        
        if (combatArena != null)
        {
            var transform = combatArena.GetPlayerTransform();
            if (transform != null)
            {
                ftm.ShowHeal(transform, amount);
            }
        }
    }
    
    /// <summary>
    /// Show shield floating text on player
    /// </summary>
    private void ShowShieldToPlayer(int amount, FloatingTextType type)
    {
        var ftm = FloatingTextManager.Instance;
        if (ftm == null) return;
        
        if (combatArena != null)
        {
            var transform = combatArena.GetPlayerTransform();
            if (transform != null)
            {
                ftm.ShowShield(transform, amount, type);
            }
        }
    }
    
    /// <summary>
    /// Show reaction floating text on an enemy
    /// </summary>
    private void ShowReactionToEnemy(CombatEnemy enemy, string reactionName, int effectValue)
    {
        var ftm = FloatingTextManager.Instance;
        if (ftm == null || combatArena == null) return;
        
        var transform = combatArena.GetEnemyTransform(enemy);
        if (transform != null)
        {
            ftm.ShowReaction(transform, reactionName, 0f, effectValue);
        }
    }
    
    /// <summary>
    /// Show status effect floating text on an enemy
    /// </summary>
    private void ShowStatusToEnemy(CombatEnemy enemy, string statusName, bool gained)
    {
        var ftm = FloatingTextManager.Instance;
        if (ftm == null || combatArena == null) return;
        
        var transform = combatArena.GetEnemyTransform(enemy);
        if (transform != null)
        {
            ftm.ShowStatus(transform, statusName, gained);
        }
    }
    
    /// <summary>
    /// Show turn skipped floating text on an enemy
    /// </summary>
    private void ShowTurnSkippedToEnemy(CombatEnemy enemy, string reason)
    {
        var ftm = FloatingTextManager.Instance;
        if (ftm == null || combatArena == null) return;
        
        var transform = combatArena.GetEnemyTransform(enemy);
        if (transform != null)
        {
            ftm.ShowTurnSkipped(transform, reason);
        }
    }
    
    /// <summary>
    /// Show DoT tick floating text on an enemy
    /// </summary>
    private void ShowDoTTickToEnemy(CombatEnemy enemy, int damage, string dotName)
    {
        var ftm = FloatingTextManager.Instance;
        if (ftm == null || combatArena == null) return;
        
        var transform = combatArena.GetEnemyTransform(enemy);
        if (transform != null)
        {
            ftm.ShowDoTTick(transform, damage, dotName);
        }
    }
    
    #endregion
    
    /// <summary>
    /// Create UI chips for all effects of a reaction. Called after ProcessReactionAfterQTE.
    /// Chips track display info (name + tooltip) for the EnemyWorldUnit and CombatArena UI.
    /// </summary>
    private void CreateReactionChips(string reactionId, string reactionName, CombatEnemy target)
    {
        var reactionDef = DataCache.GetReactionDef(reactionId);
        if (reactionDef == null || reactionDef.Effects == null) return;
        
        foreach (var effect in reactionDef.Effects)
        {
            if (effect == null || string.IsNullOrEmpty(effect.effectId)) continue;
            
            int dur = effect.duration > 0 ? effect.duration : 1;
            
            switch (effect.effectId)
            {
                case "rxn_apply_dot":
                {
                    string dotName = !string.IsNullOrEmpty(effect.dotName) ? effect.dotName : "DoT";
                    string tooltip = $"Deals damage per turn ({dur} turns)";
                    if (target != null) target.AddReactionChip(dotName, tooltip, dur);
                    break;
                }
                case "rxn_player_buff":
                {
                    string tooltip = effect.buffType switch
                    {
                        "CritDamage" => $"Bonus Crit Damage +{effect.value:F0}% ({dur} turns)",
                        "BonusAP" => $"+{effect.value:F0} max AP ({dur} turns)",
                        "ReflectiveArmor" => $"Reflects {effect.value:F0}% damage back ({dur} turns)",
                        "DamageReduction" => $"Takes {effect.value:F0}% less damage ({dur} turns)",
                        "RockDamageWhileShielded" => $"+{effect.value:F0}% Rock damage while shielded",
                        _ => reactionName
                    };
                    player.AddReactionChip(reactionName, tooltip, dur > 0 ? dur : 99);
                    break;
                }
                case "rxn_enemy_debuff":
                {
                    string chipName;
                    string tooltip;
                    switch (effect.debuffType)
                    {
                        case "Freeze":
                            chipName = "Frozen";
                            tooltip = "Enemy is frozen and skips the next turn";
                            break;
                        case "Weak":
                            chipName = reactionName;
                            tooltip = $"Enemy deals {effect.value:F0}% less damage ({dur} turns)";
                            break;
                        case "Shatter":
                            chipName = reactionName;
                            tooltip = $"Accumulates damage taken. Pops for bonus damage at threshold ({dur} turns)";
                            break;
                        case "HealOnHit":
                            chipName = reactionName;
                            tooltip = $"Player heals {effect.value:F0}% max HP when this enemy attacks ({dur} turns)";
                            break;
                        case "Electrocute":
                            chipName = reactionName;
                            tooltip = $"Takes bonus damage when hit. Stacks up to {effect.maxStacks} ({dur} turns)";
                            break;
                        case "Mudslide":
                            chipName = reactionName;
                            tooltip = $"Slowed. At max stacks, consumes for damage + stun ({dur} turns)";
                            break;
                        default:
                            chipName = reactionName;
                            tooltip = reactionName;
                            break;
                    }
                    if (target != null) target.AddReactionChip(chipName, tooltip, dur);
                    break;
                }
                case "rxn_apply_shield":
                {
                    string tooltip = $"+{effect.value:F0} Shield";
                    player.AddReactionChip(reactionName, tooltip, 1);
                    break;
                }
                case "rxn_reduce_resist":
                {
                    string elems = effect.elements ?? "All";
                    string tooltip = $"Resistances reduced by {effect.value:F0}% ({elems}) ({dur} turns)";
                    if (target != null) target.AddReactionChip(reactionName, tooltip, dur);
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Check HealOnHit: if the attacking enemy has the HealOnHit reaction debuff,
    /// heal the player for the specified % of max HP. Called after enemy deals damage.
    /// </summary>
    private void CheckHealOnHit(CombatEnemy attacker)
    {
        if (attacker == null || player == null || !player.IsAlive()) return;
        
        var healDebuff = attacker.GetReactionDebuff("HealOnHit");
        if (healDebuff == null || healDebuff.Value <= 0) return;
        
        int healAmount = Mathf.RoundToInt(player.GetMaxHealth() * (healDebuff.Value / 100f));
        if (healAmount <= 0) return;
        
        int hpBefore = player.GetHealth();
        player.Heal(healAmount);
        int actualHeal = player.GetHealth() - hpBefore;
        
        if (actualHeal > 0)
        {
            var ftm = FloatingTextManager.Instance;
            if (ftm != null && combatArena != null)
            {
                var pt = combatArena.GetPlayerTransform();
                if (pt != null) ftm.ShowHeal(pt, actualHeal);
            }
            
            GameLog.Reaction(GameLog.Join(
                "HealOnHit",
                GameLog.KV("source", attacker.Name),
                GameLog.KV("healPercent", healDebuff.Value),
                GameLog.KV("healed", actualHeal)
            ));
        }
    }
    
}
