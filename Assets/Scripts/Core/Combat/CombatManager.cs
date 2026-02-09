using System.Collections.Generic;
using UnityEngine;

public enum CombatType { Normal, Elite, Boss }

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
    private NodeBase currentNode;
    private CombatType currentCombatType = CombatType.Normal;
    
    private CombatEnemy pendingTarget;
    private int pendingSkillNumber;
    private bool pendingIsAttack;
    private Element pendingInfusedElement = Element.None;
    private string pendingReactionEffectId = "";
    private int pendingReactionEffectValue = 0;
    private string pendingReactionName = "";
    private string pendingReactionId = null;
    private Element pendingReactionFirstElement = Element.None;
    private Element pendingReactionDetonator = Element.None;

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
        
        for (int i = 0; i < bossCount; i++)
        {
            var bossData = DataCache.BossEnemies[Random.Range(0, DataCache.BossEnemies.Count)];
            enemies.Add(new CombatEnemy(bossData, world));
        }

        combatActive = true;
        isPlayerTurn = true;
        isEndingCombat = false;
        
        // Ensure player combat state is reset (sets AP to max, clears per-combat effects)
        if (player != null)
        {
            player.ResetCombatState();
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
        
        // Reset player combat state (energy to 0, cooldowns cleared)
        player.ResetCombatState();

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
        
        GameLog.Combat(GameLog.Join("EndTurnClicked"));
        
        isPlayerTurn = false;
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
        
        if (pendingIsAttack)
        {
            ExecuteAttackWithReaction(pendingTarget, pendingReactionEffectId, pendingReactionEffectValue, qteMultiplier, pendingInfusedElement, result);
        }
        else
        {
            ExecuteSkillWithReaction(pendingSkillNumber, pendingTarget, pendingReactionEffectId, pendingReactionEffectValue, qteMultiplier, pendingInfusedElement, result);
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
        pendingReactionEffectId = "";
        pendingReactionEffectValue = 0;
        pendingReactionName = "";
        pendingReactionId = null;
        pendingReactionFirstElement = Element.None;
        pendingReactionDetonator = Element.None;
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
        
        // Get reaction data from JSON
        var reactionDef = DataCache.GetReactionDef(pendingReactionId);
        if (reactionDef == null)
        {
            // Fallback for unknown reactions
            pendingReactionName = reactionInfo.IsSingleElement ? $"{reactionInfo.PrimaryElement} Burst" : $"{reactionInfo.PrimaryElement}-{reactionInfo.SecondaryElement} Fusion";
            pendingReactionEffectId = "eff_reaction_damage";
            pendingReactionEffectValue = reactionInfo.IsSingleElement ? 50 : 75;
        }
        else
        {
            pendingReactionName = reactionDef.Name;
            pendingReactionEffectId = reactionDef.EffectId;
            pendingReactionEffectValue = reactionDef.EffectValue;
        }
        
        // Consume the marks
        target.ConsumeMarksForReaction(reactionInfo);
        
        string attackerName = player != null && player.GetCharacter() != null ? player.GetCharacter().DisplayName : "Player";
        GameLog.Reaction(GameLog.Join(
            "MarkReactionTrigger",
            GameLog.KV("id", pendingReactionId),
            GameLog.KV("name", pendingReactionName),
            GameLog.KV("effectId", pendingReactionEffectId),
            GameLog.KV("effectValue", pendingReactionEffectValue),
            GameLog.KV("type", reactionInfo.IsSingleElement ? "single" : "dual"),
            GameLog.KV("primary", reactionInfo.PrimaryElement),
            GameLog.KV("secondary", reactionInfo.SecondaryElement),
            GameLog.KV("attacker", attackerName),
            GameLog.KV("target", target.Name)
        ));
        
        // Show reaction floating text
        ShowReactionToEnemy(target, pendingReactionName, pendingReactionEffectValue);
        
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
        Element attackElement = forcedElement ?? player.GetAffinity();

        string attackerName = player != null && player.GetCharacter() != null ? player.GetCharacter().DisplayName : "Player";
        string reactionId = reactionMultiplier > 1f ? "nonDirectional" : "none";

        // Player Attack Order:
        // 1. Base damage (character damage + elemental bonuses)
        int baseDamage = player.GetTotalDamage();
        
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

        if (!target.IsAlive())
        {
            GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", target.Name)));
            NotifyEnemyDeath(target);
        }

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }

    }
    
    private void ExecuteAttackWithReaction(CombatEnemy target, string reactionEffectId, int reactionEffectValue, float qteMultiplier, Element? forcedElement, QTEResult qteResult)
    {
        Element attackElement = forcedElement ?? player.GetAffinity();

        string attackerName = player != null && player.GetCharacter() != null ? player.GetCharacter().DisplayName : "Player";
        GameLog.Combat(GameLog.Join(
            "AttackStart",
            GameLog.KV("attacker", attackerName),
            GameLog.KV("target", target != null ? target.Name : "null"),
            GameLog.KV("element", attackElement),
            GameLog.KV("bonuses", GameLog.Join(GameLog.KV("oqte", qteMultiplier.ToString("F2")))),
            GameLog.KV("reactionId", pendingReactionId ?? "none")
        ));
        
        // Player Attack Order with Reaction:
        // 1. Base damage (character damage + elemental bonuses)
        int baseDamage = player.GetTotalDamage();
        
        // 2. Apply variance (0.90-1.10)
        int afterVariance = player.ApplyVariance(baseDamage);
        
        // 3. Apply Offensive QTE multiplier
        float afterOQTE = afterVariance * qteMultiplier;
        
        // 4. Apply crit (direct hit only)
        int critChance = player.GetCritChance();
        int critRoll = Random.Range(0, 100);
        bool isCrit = critRoll < critChance;
        float critMultiplier = isCrit ? player.GetCritDamage() : 1f;
        float directAfterCrit = afterOQTE * critMultiplier;
        
        // 5. Reaction effect value is added as flat damage (scaled by QTE)
        float reactionDamage = reactionEffectId == "eff_reaction_damage" ? reactionEffectValue * qteMultiplier : 0;
        
        // 6. Total = DirectAfterCrit + ReactionDamage
        float totalDamage = directAfterCrit + reactionDamage;
        int damageBeforeResist = Mathf.RoundToInt(totalDamage);
        
        GameLog.Combat(GameLog.Join(
            "AttackRoll",
            GameLog.KV("roll", afterVariance),
            GameLog.KV("reactionEffectId", reactionEffectId),
            GameLog.KV("reactionEffectValue", reactionEffectValue),
            GameLog.KV("critRoll", critRoll),
            GameLog.KV("critChance", critChance),
            GameLog.KV("crit", isCrit),
            GameLog.KV("critMult", critMultiplier.ToString("F2")),
            GameLog.KV("preResist", damageBeforeResist)
        ));

        // 7. Apply enemy resistance
        int damage = target.ApplyResistance(damageBeforeResist, attackElement);
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

        // Apply reaction effects from JSON data (track shield gain for floating text)
        int shieldBeforeReact = player.GetShield();
        ReactionEffectEngine.ApplyPostHitEffects(pendingReactionId, player, target, damage, attackElement);

        // Show floating text using CombatArena transforms
        ShowDamageToEnemy(target, damage, isCrit);
        int shieldAfterReact = player.GetShield();
        if (shieldAfterReact > shieldBeforeReact)
        {
            ShowShieldToPlayer(shieldAfterReact - shieldBeforeReact, FloatingTextType.ShieldGain);
        }
        
        // Notify in-world combat arena of damage
        NotifyEnemyHit(target);

        if (!target.IsAlive())
        {
            GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", target.Name)));
            AwardDetonatorXPForKill(target);
            NotifyEnemyDeath(target);
        }

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }

    }

    public void OnPlayerSkill(int skillNumber)
    {
        var target = GetFirstAliveEnemy();
        if (target != null)
        {
            OnPlayerSkillTarget(skillNumber, target);
        }
    }

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
        var character = player.GetCharacter();
        if (character == null) return;

        // Spend AP for this skill
        int apCost = GetSkillAPCost(character, skillNumber);
        player.SpendAP(apCost);

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
        int baseDamageBeforeElement = Mathf.RoundToInt(charDamage * skillMultiplier * chainBonus);
        
        // Add elemental bonus based on skill's element (from enchantment or base)
        int elementalBonus = attackElement != Element.None ? player.GetElementalBonus(attackElement) : 0;
        int elementalBonusScaled = Mathf.RoundToInt(elementalBonus * skillMultiplier);
        int baseDamage = baseDamageBeforeElement + elementalBonusScaled;
        
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
            
            // Apply crit with temp bonuses
            int effectiveCritChance = player.GetCritChance() + tempCritBonus;
            float effectiveCritDmg = player.GetCritDamage() + tempCritDmgBonus;
            int critRoll = Random.Range(0, 100);
            bool isCrit = critRoll < effectiveCritChance;
            
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
        bool reactionQTETriggered = false;
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
                            pendingSkillNumber = skillNumber;
                            TriggerMarkReaction(target, reactionInfo);
                            reactionQTETriggered = true;
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
            ShowShieldToPlayer(effectResult.ShieldGained, FloatingTextType.ShieldGain);
        
        // Notify in-world combat arena of damage
        NotifyEnemyHit(target);
        
        bool targetKilled = !target.IsAlive();
        if (targetKilled)
        {
            GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", target.Name)));
            NotifyEnemyDeath(target);
        }

        // When a reaction QTE was triggered, ExecuteSkillWithReaction will handle
        // cooldown, chain tracking, on-kill effects, and combat-end checks after the QTE resolves.
        // Do NOT double-apply them here.
        if (reactionQTETriggered)
        {
            // Still update UI and notify arena of the hit
            if (combatArena != null) combatArena.OnPlayerEnergyChanged();
            return;
        }

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
    
    private void ExecuteSkillWithReaction(int skillNumber, CombatEnemy target, string reactionEffectId, int reactionEffectValue, float qteMultiplier, Element? forcedElement, QTEResult qteResult)
    {
        var character = player.GetCharacter();
        if (character == null) return;

        // Look up SkillDefinition from JSON data
        var skillDef = GetSkillDefinition(skillNumber);
        string skillName = skillDef != null ? skillDef.displayName : GetSkillName(skillNumber);
        var effects = skillDef?.effects;
        
        float skillMultiplier = SkillEffectEngine.GetDamageMultiplier(effects);
        bool isAoE = SkillEffectEngine.IsAoE(effects);
        
        Element attackElement = forcedElement ?? player.GetAffinity();
        
        // Player Skill Attack Order with Reaction:
        // 1. Base damage = (character damage * skill multiplier) + elemental bonus
        int charDamage = player.GetCharacterDamage();
        int elementalBonus = player.GetAffinityBonus();
        float baseDamage = (charDamage * skillMultiplier) + (elementalBonus * skillMultiplier);
        
        // 2. Apply variance (0.90-1.10)
        int afterVariance = player.ApplyVariance(baseDamage);
        
        // 3. Apply Offensive QTE multiplier
        float afterOQTE = afterVariance * qteMultiplier;
        
        // 4. Apply crit (direct hit only)
        bool isCrit = Random.Range(0, 100) < player.GetCritChance();
        float critMultiplier = isCrit ? player.GetCritDamage() : 1f;
        float directAfterCrit = afterOQTE * critMultiplier;
        
        // 5. Reaction effect value is added as flat damage (scaled by QTE)
        float reactionDamage = reactionEffectId == "eff_reaction_damage" ? reactionEffectValue * qteMultiplier : 0;
        
        // 6. Total = DirectAfterCrit + ReactionDamage
        float totalDamage = directAfterCrit + reactionDamage;
        int damageBeforeResist = Mathf.RoundToInt(totalDamage);
        
        // 7. Apply enemy resistance
        int finalDamage = target.ApplyResistance(damageBeforeResist, attackElement);
        int resistPercent = target.CalculateResistance(attackElement);
        
        if (isAoE)
        {
            DamageAllEnemies(finalDamage, forcedElement, isCrit);
        }
        else
        {
            target.TakeDamage(finalDamage);
            ShowDamageToEnemy(target, finalDamage, isCrit);
        }
        
        // Apply skill post-hit effects via data-driven engine
        var effectCtx = new SkillEffectEngine.EffectContext
        {
            Player = player,
            Target = target,
            AllEnemies = enemies,
            DamageDealt = finalDamage,
            AttackElement = attackElement,
            SkillName = skillName,
            SkillNumber = skillNumber
        };
        var effectResult = SkillEffectEngine.Execute(effects, effectCtx);
        
        if (effectResult.HealAmount > 0)
            ShowHealToPlayer(effectResult.HealAmount);
        if (effectResult.ShieldGained > 0)
            ShowShieldToPlayer(effectResult.ShieldGained, FloatingTextType.ShieldGain);
        
        GameLog.Combat(GameLog.Join(
            "AttackRoll",
            GameLog.KV("skill", skillName),
            GameLog.KV("roll", afterVariance),
            GameLog.KV("reactionEffectId", reactionEffectId),
            GameLog.KV("reactionEffectValue", reactionEffectValue),
            GameLog.KV("crit", isCrit),
            GameLog.KV("critMult", critMultiplier.ToString("F2")),
            GameLog.KV("preResist", damageBeforeResist)
        ));

        GameLog.Reaction(GameLog.Join(
            "Trigger",
            GameLog.KV("id", pendingReactionId ?? "none"),
            GameLog.KV("name", pendingReactionName),
            GameLog.KV("effectId", pendingReactionEffectId),
            GameLog.KV("effectValue", pendingReactionEffectValue),
            GameLog.KV("first", pendingReactionFirstElement),
            GameLog.KV("det", pendingReactionDetonator),
            GameLog.KV("attacker", player != null && player.GetCharacter() != null ? player.GetCharacter().DisplayName : "Player"),
            GameLog.KV("target", target != null ? target.Name : "null")
        ), GameLogVerbosity.Minimal);

        // Apply reaction effects from JSON data (track shield gain for floating text)
        int shieldBeforeReact2 = player.GetShield();
        ReactionEffectEngine.ApplyPostHitEffects(pendingReactionId, player, target, finalDamage, attackElement);
        
        int shieldAfterReact2 = player.GetShield();
        if (shieldAfterReact2 > shieldBeforeReact2)
        {
            ShowShieldToPlayer(shieldAfterReact2 - shieldBeforeReact2, FloatingTextType.ShieldGain);
        }
        
        // Notify in-world combat arena of damage
        NotifyEnemyHit(target);

        bool targetKilled = !target.IsAlive();
        if (targetKilled)
        {
            GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", target.Name)));
            AwardDetonatorXPForKill(target);
            NotifyEnemyDeath(target);
        }

        // Apply skill cooldown and energy effects (skillNumber is 1-indexed, array is 0-indexed)
        player.UseSkillAndApplyEffects(skillNumber - 1);

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }

        // Handle on-kill effects from data
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
                    int chainDamage = Mathf.RoundToInt(finalDamage * effectResult.OnKillBonusDamageMultiplier);
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

        // Player can continue using skills - turn does NOT end after reaction skill
    }
    
    private void DamageAllEnemies(int damage, Element? forcedElement = null, bool isCrit = false)
    {
        Element attackElement = forcedElement ?? player.GetAffinity();
        foreach (var enemy in enemies)
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
                // Show floating text for each enemy hit by AOE
                ShowDamageToEnemy(enemy, finalDamage, isCrit);
                
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
                if (isStunned)
                {
                    ShowTurnSkippedToEnemy(enemy, "Stunned!");
                    yield return new WaitForSeconds(0.3f); // Brief pause for stun text
                }
                
                // STEP 2: Tick DoT effects (DoT still ticks even when stunned)
                int dotDamage = enemy.TickDoTEffects();

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
                
                // STEP 3: If stunned, skip attack phase entirely
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

        GameLog.Combat(GameLog.Join(
            "AttackStart",
            GameLog.KV("attacker", enemy.Name),
            GameLog.KV("target", "Player"),
            GameLog.KV("element", enemy.Affinity),
            GameLog.KV("dmgRange", "unknown"),
            GameLog.KV("bonuses", GameLog.Join(
                GameLog.KV("base", pendingDefensiveQTEBaseDamage),
                GameLog.KV("variance", pendingDefensiveQTEAfterVariance)
            )),
            GameLog.KV("reactionId", "none")
        ));

        GameLog.Combat(GameLog.Join(
            "AttackRoll",
            GameLog.KV("roll", pendingDefensiveQTEAfterVariance),
            GameLog.KV("reactionMult", "1.00"),
            GameLog.KV("crit", false),
            GameLog.KV("critMult", "1.00"),
            GameLog.KV("preResist", pendingDefensiveQTEDamage)
        ));

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

        // Tick temp resist durations for all combatants
        player.TickTempResists();
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive())
            {
                enemy.TickTempResists();
            }
        }

        isPlayerTurn = true;
        
        // Tick down cooldowns at start of player's turn
        player.TickCooldowns();
        
        // Refresh AP at start of player's turn
        player.RefreshAP();

        
        
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

    private void EndCombat(bool victory)
    {
        // Idempotent: if already ending, skip
        if (isEndingCombat) return;
        isEndingCombat = true;
        combatActive = false;
        
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
            // Calculate total rewards from all defeated enemies
            int totalXP = 0;
            int totalGold = 0;
            bool dropsSigil = false;
            bool dropsRelic = false;
            
            foreach (var enemy in enemies)
            {
                totalXP += enemy.RewardXP;
                // Roll gold within the enemy's gold range
                totalGold += Random.Range(enemy.RewardGoldMin, enemy.RewardGoldMax + 1);
                
                // Roll for sigil drop
                if (enemy.SigilChance > 0 && Random.Range(0, 100) < enemy.SigilChance)
                {
                    dropsSigil = true;
                }
                
                // Roll for relic drop
                if (enemy.RelicChance > 0 && Random.Range(0, 100) < enemy.RelicChance)
                {
                    dropsRelic = true;
                }
            }
            
            GameManager.AddRunXP(totalXP);
            
            GameLog.Combat(GameLog.Join(
                "CombatRewards",
                GameLog.KV("type", currentCombatType),
                GameLog.KV("xp", totalXP),
                GameLog.KV("gold", totalGold),
                GameLog.KV("sigil", dropsSigil),
                GameLog.KV("relic", dropsRelic)
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
        if (currentCombatType == CombatType.Boss && onBossCombatComplete != null)
        {
            var callback = onBossCombatComplete;
            onBossCombatComplete = null;
            callback.Invoke(true);
        }
    }

    public bool IsCombatActive() => combatActive;
    public bool IsInCombat() => combatActive;
    public bool IsEndingCombat() => isEndingCombat;
    public bool IsPlayerTurn() => isPlayerTurn;
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
        if (combatArena == null) return;
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
    /// Awards Elemental Ascension XP to the detonator element when an enemy is killed via reaction.
    /// XP amount is based on enemy type: Regular=1, Elite=3, Boss=5
    /// </summary>
    private void AwardDetonatorXPForKill(CombatEnemy killedEnemy)
    {
        if (pendingReactionDetonator == Element.None) return;
        if (killedEnemy == null) return;
        
        int xpAmount = 1; // Regular enemy
        if (killedEnemy.IsBoss)
        {
            xpAmount = 5;
        }
        else if (killedEnemy.IsElite)
        {
            xpAmount = 3;
        }
        
        GameManager.AddRunXPForDetonator(xpAmount, pendingReactionDetonator);
        GameLog.Combat(GameLog.Join(
            "DetonatorXP",
            GameLog.KV("element", pendingReactionDetonator),
            GameLog.KV("xp", xpAmount),
            GameLog.KV("enemy", killedEnemy.Name),
            GameLog.KV("type", killedEnemy.IsBoss ? "Boss" : killedEnemy.IsElite ? "Elite" : "Regular")
        ), GameLogVerbosity.Verbose);
    }
}
