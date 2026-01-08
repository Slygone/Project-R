using System.Collections.Generic;
using UnityEngine;

public enum CombatType { Normal, Elite, Boss }

public class CombatManager : MonoBehaviour
{
    private List<CombatEnemy> enemies = new List<CombatEnemy>();
    private Player player;
    private CombatUI combatUI;
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
    private float pendingReactionMultiplier = 1f;
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
        
        if (combatUI == null)
        {
            combatUI = FindFirstObjectByType<CombatUI>();
        }

        player = playerRef;
        enemies.Clear();

        int world = GameManager.CurrentWorld;
        var encounter = DataCache.GetWorldEncounter(world);
        int bossCount = encounter.BossEnemy;
        
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

        string title = world >= 5 ? "FINAL BOSS FIGHT!" : "BOSS FIGHT!";
        if (combatUI != null)
        {
            combatUI.ShowCombat(enemies, player, title);
            // Refresh skill/AP displays to reflect reset state
            combatUI.UpdateSkillButtons(player);
            combatUI.UpdateAPDisplay(player);
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

        if (combatUI != null)
        {
            string title = currentCombatType == CombatType.Elite ? "ELITE ENCOUNTER!" : null;
            combatUI.ShowCombat(enemies, player, title);
        }
        else
        {
            GameLog.Error(GameLogCategory.Combat, "[Combat]", "CombatStartError | reason=combat_ui_null");
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
        pendingReactionId = null;
        pendingReactionFirstElement = Element.None;
        pendingReactionDetonator = Element.None;
    }
    
    // DEPRECATED: Old orb-based reaction system - now using new mark system
    private void TriggerReactionAction(CombatEnemy target, int skillNumber, bool isAttack)
    {
        // This method is no longer used - reactions are now triggered by the mark system
        // When 6 marks of same element OR 3+3 of different elements are applied to an enemy
        GameLog.Combat(GameLog.Join("DeprecatedMethod", GameLog.KV("method", "TriggerReactionAction")));
    }
    
    /// <summary>
    /// New mark-based reaction trigger. Called when marks reach threshold on an enemy.
    /// </summary>
    private void TriggerMarkReaction(CombatEnemy target, ReactionTriggerInfo reactionInfo)
    {
        if (target == null || reactionInfo == null) return;
        
        pendingTarget = target;
        pendingReactionFirstElement = reactionInfo.PrimaryElement;
        pendingReactionDetonator = reactionInfo.SecondaryElement != Element.None ? reactionInfo.SecondaryElement : reactionInfo.PrimaryElement;
        pendingInfusedElement = reactionInfo.PrimaryElement;
        pendingReactionId = reactionInfo.GetReactionId();
        
        // Get reaction data from CSV
        var reactionDef = DataCache.GetReactionDef(pendingReactionId);
        if (reactionDef == null)
        {
            // Fallback for unknown reactions
            pendingReactionName = reactionInfo.IsSingleElement ? $"{reactionInfo.PrimaryElement} Burst" : $"{reactionInfo.PrimaryElement}-{reactionInfo.SecondaryElement} Fusion";
            pendingReactionMultiplier = reactionInfo.IsSingleElement ? 1.5f : 2.0f;
        }
        else
        {
            pendingReactionName = reactionDef.Name;
            pendingReactionMultiplier = reactionDef.DamageMultiplier;
        }
        
        // Consume the marks
        target.ConsumeMarksForReaction(reactionInfo);
        
        string attackerName = player != null && player.GetCharacter() != null ? player.GetCharacter().DisplayName : "Player";
        GameLog.Reaction(GameLog.Join(
            "MarkReactionTrigger",
            GameLog.KV("id", pendingReactionId),
            GameLog.KV("name", pendingReactionName),
            GameLog.KV("mult", pendingReactionMultiplier.ToString("F2")),
            GameLog.KV("type", reactionInfo.IsSingleElement ? "single" : "dual"),
            GameLog.KV("primary", reactionInfo.PrimaryElement),
            GameLog.KV("secondary", reactionInfo.SecondaryElement),
            GameLog.KV("attacker", attackerName),
            GameLog.KV("target", target.Name)
        ));
        
        // Show reaction floating text
        if (combatUI != null)
        {
            combatUI.ShowReactionToEnemy(target, pendingReactionName, pendingReactionMultiplier);
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

        if (combatUI != null)
        {
            combatUI.ShowDamageToEnemy(target, damage, isCrit);
            combatUI.UpdateEnemyHealth(target);
        }

        if (!target.IsAlive())
        {
            GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", target.Name)));
        }

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }

        // Player can continue using skills - turn does NOT end after attack
        if (combatUI != null)
        {
            combatUI.UpdateSkillButtons(player);
            combatUI.UpdateAPDisplay(player);
        }
    }
    
    private void ExecuteAttackWithReaction(CombatEnemy target, float reactionMultiplier, float qteMultiplier, Element? forcedElement, QTEResult qteResult)
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
        
        // 5. Calculate reaction damage using afterOQTE (pre-crit, so crit doesn't inflate reactions)
        float reactionDamage = afterOQTE * (reactionMultiplier - 1f); // Only the bonus from reaction
        
        // 6. Total = DirectAfterCrit + ReactionDamage
        float totalDamage = directAfterCrit + reactionDamage;
        int damageBeforeResist = Mathf.RoundToInt(totalDamage);
        
        GameLog.Combat(GameLog.Join(
            "AttackRoll",
            GameLog.KV("roll", afterVariance),
            GameLog.KV("reactionMult", reactionMultiplier.ToString("F2")),
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

        // Apply reaction effects from CSV data (track shield gain for floating text)
        int shieldBeforeReact = player.GetShield();
        ReactionEffectEngine.ApplyPostHitEffects(pendingReactionId, player, target, damage, attackElement);

        if (combatUI != null)
        {
            combatUI.ShowDamageToEnemy(target, damage, isCrit);
            combatUI.UpdateEnemyHealth(target);
            int shieldAfterReact = player.GetShield();
            if (shieldAfterReact > shieldBeforeReact)
            {
                combatUI.ShowShieldToPlayer(shieldAfterReact - shieldBeforeReact, FloatingTextType.ShieldGain);
            }
            combatUI.UpdatePlayerHealth(player); // Update in case of shield effects
        }

        if (!target.IsAlive())
        {
            GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", target.Name)));
            AwardDetonatorXPForKill(target);
        }

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }

        // Player can continue using skills - turn does NOT end after reaction
        if (combatUI != null)
        {
            combatUI.UpdateSkillButtons(player);
            combatUI.UpdateAPDisplay(player);
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

    public void OnPlayerSkillTarget(int skillNumber, CombatEnemy target)
    {
        if (!combatActive || !isPlayerTurn) return;
        if (target == null || !target.IsAlive()) return;

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
            case 4:
                skillName = character.Skill4;
                skillDamagePercent = character.Skill4DamagePercent;
                skillEffect = character.Skill4Effect;
                break;
            case 5:
                skillName = character.Skill5;
                skillDamagePercent = character.Skill5DamagePercent;
                skillEffect = character.Skill5Effect;
                break;
            default:
                skillName = "Unknown";
                skillDamagePercent = 100f;
                skillEffect = "null";
                break;
        }
        
        string skillLower = skillName.ToLower();
        // Get skill's enchanted element (from sigil or base character data)
        Element skillElement = player.GetSkillElement(skillNumber);
        Element attackElement = forcedElement ?? (skillElement != Element.None ? skillElement : player.GetAffinity());
        
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
            }
        }
        else if (skillEffect != null && skillEffect.ToLower().Contains("aoe"))
        {
            isAoE = true;
        }
        
        // Calculate base damage from CSV percent
        float skillMultiplier = skillDamagePercent / 100f;
        int charDamage = player.GetCharacterDamage();
        int baseDamageBeforeElement = Mathf.RoundToInt(charDamage * skillMultiplier * dirtyStabBonus);
        
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
                GameLog.KV("dirtyStab", dirtyStabBonus.ToString("F2"))
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
                if (combatUI != null)
                {
                    combatUI.ShowDamageToEnemy(target, finalDamage, isCrit);
                }
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
                            TriggerMarkReaction(target, reactionInfo);
                        }
                    }
                    
                    // Update enemy UI to show marks
                    if (combatUI != null)
                    {
                        combatUI.UpdateEnemyHealth(target);
                    }
                }
            }
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
            GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", target.Name)));
        }

        // Apply skill cooldown and energy effects BEFORE possible early return
        player.UseSkillAndApplyEffects(skillNumber - 1);

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }

        // Handle kill effects AFTER cooldown/energy application so overrides (e.g. Ambush) persist
        if (targetKilled)
        {
            HandleKillEffects(skillLower, target, totalDamageDealt);
        }
        
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
            combatUI.UpdateAPDisplay(player);
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
            GameLog.Combat(GameLog.Join(
                "Heal",
                GameLog.KV("who", "Player"),
                GameLog.KV("amount", healAmount),
                GameLog.KV("source", "Skill:Judgement")
            ));
            
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
            GameLog.Combat(GameLog.Join(
                "ShieldGain",
                GameLog.KV("who", "Player"),
                GameLog.KV("amount", shieldAmount),
                GameLog.KV("source", "Skill:HolyNova")
            ));
            
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
            // Refund 40% of max energy
            int refund = Mathf.RoundToInt(player.GetMaxEnergy() * 0.40f);
            player.GainEnergy(refund);
            
            // Set cooldown to 2 turns instead of normal cooldown
            player.SetSkillCooldown(2, 2);

            GameLog.Combat(GameLog.Join(
                "KillEffect",
                GameLog.KV("skill", "Ambush"),
                GameLog.KV("energyRefund", refund),
                GameLog.KV("cooldownSet", 2)
            ));

            if (combatUI != null)
            {
                combatUI.UpdatePlayerEnergy(player);
                combatUI.UpdateSkillButtons(player);
            }
        }
        // DoubleUp - If target dies, next target hit for 150% damage
        else if (skillLower == "doubleup")
        {
            var nextTarget = GetFirstAliveEnemy();
            if (nextTarget != null)
            {
                int chainDamage = Mathf.RoundToInt(damageDealt * 1.50f);
                int hpBefore = nextTarget.Health;
                nextTarget.TakeDamage(chainDamage);

                GameLog.Combat(GameLog.Join(
                    "DamageApply",
                    GameLog.KV("target", nextTarget.Name),
                    GameLog.KV("resistTotal", "0%"),
                    GameLog.KV("shieldBefore", 0),
                    GameLog.KV("shieldAbsorbed", 0),
                    GameLog.KV("shieldAfter", 0),
                    GameLog.KV("hpBefore", hpBefore),
                    GameLog.KV("dmgFinal", chainDamage),
                    GameLog.KV("hpAfter", nextTarget.Health),
                    GameLog.KV("source", "Skill:DoubleUpChain")
                ));
                
                if (combatUI != null)
                {
                    combatUI.ShowDamageToEnemy(nextTarget, chainDamage, false);
                    combatUI.UpdateEnemyHealth(nextTarget);
                }
                
                if (!nextTarget.IsAlive())
                {
                    GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", nextTarget.Name), GameLog.KV("source", "DoubleUpChain")));
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
            case 4:
                skillName = character.Skill4;
                skillDamagePercent = character.Skill4DamagePercent;
                skillEffect = character.Skill4Effect;
                break;
            case 5:
                skillName = character.Skill5;
                skillDamagePercent = character.Skill5DamagePercent;
                skillEffect = character.Skill5Effect;
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
        
        GameLog.Combat(GameLog.Join(
            "AttackRoll",
            GameLog.KV("skill", skillName),
            GameLog.KV("roll", afterVariance),
            GameLog.KV("reactionMult", reactionMultiplier.ToString("F2")),
            GameLog.KV("crit", isCrit),
            GameLog.KV("critMult", critMultiplier.ToString("F2")),
            GameLog.KV("preResist", damageBeforeResist)
        ));

        GameLog.Reaction(GameLog.Join(
            "Trigger",
            GameLog.KV("id", pendingReactionId ?? "none"),
            GameLog.KV("name", pendingReactionName),
            GameLog.KV("mult", reactionMultiplier.ToString("F2")),
            GameLog.KV("first", pendingReactionFirstElement),
            GameLog.KV("det", pendingReactionDetonator),
            GameLog.KV("attacker", player != null && player.GetCharacter() != null ? player.GetCharacter().DisplayName : "Player"),
            GameLog.KV("target", target != null ? target.Name : "null")
        ), GameLogVerbosity.Minimal);

        // Apply reaction effects from CSV data (track shield gain for floating text)
        int shieldBeforeReact2 = player.GetShield();
        ReactionEffectEngine.ApplyPostHitEffects(pendingReactionId, player, target, finalDamage, attackElement);
        
        if (combatUI != null)
        {
            int shieldAfterReact2 = player.GetShield();
            if (shieldAfterReact2 > shieldBeforeReact2)
            {
                combatUI.ShowShieldToPlayer(shieldAfterReact2 - shieldBeforeReact2, FloatingTextType.ShieldGain);
            }
            combatUI.UpdatePlayerHealth(player); // Update in case of shield effects
        }

        bool targetKilled = !target.IsAlive();
        if (targetKilled)
        {
            GameLog.Combat(GameLog.Join("EnemyDefeated", GameLog.KV("target", target.Name)));
            AwardDetonatorXPForKill(target);
        }

        // Apply skill cooldown and energy effects (skillNumber is 1-indexed, array is 0-indexed)
        player.UseSkillAndApplyEffects(skillNumber - 1);

        if (AllEnemiesDead())
        {
            EndCombat(true);
            return;
        }

        // Handle kill effects AFTER cooldown/energy application so overrides (e.g. Ambush) persist
        if (targetKilled)
        {
            HandleKillEffects(skillLower, target, finalDamage);
        }
        
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
            combatUI.UpdateAPDisplay(player);
        }

        // Player can continue using skills - turn does NOT end after reaction skill
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

        float healPercent = DefensiveQTE.GetHealPercent(qteResult);
        int healAmount = Mathf.RoundToInt(player.GetMaxHealth() * healPercent);
        if (healAmount > 0)
        {
            player.Heal(healAmount);
            GameLog.Combat(GameLog.Join(
                "Heal",
                GameLog.KV("who", "Player"),
                GameLog.KV("amount", healAmount),
                GameLog.KV("source", $"DefensiveQTE:{qteResult}")
            ));
            
            if (combatUI != null)
            {
                combatUI.UpdatePlayerHealth(player);
                // Show heal floating text from QTE
                combatUI.ShowHealToPlayer(healAmount);
            }
        }

        GameLog.Combat(GameLog.Join(
            "DefensiveQTE",
            GameLog.KV("attacker", enemy.Name),
            GameLog.KV("result", qteResult),
            GameLog.KV("healed", healAmount)
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

            if (combatUI != null)
            {
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

        if (combatUI != null)
        {
            combatUI.SetPlayerTurn(true);
            combatUI.UpdateAPDisplay(player);
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

        // Re-enable player free roam at combat end
        var pcEnd = FindFirstObjectByType<PlayerController>();
        if (pcEnd != null) pcEnd.SetCanMove(true);

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
            
            // Show reward UI for all combat victories
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
            if (combatUI != null)
            {
                combatUI.HideCombat();
            }
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
        if (combatUI != null)
        {
            combatUI.HideCombat();
        }

        if (currentNode != null)
        {
            currentNode.OnNodeCompleted();
        }

        currentNode = null;

        // Ensure movement is enabled after loot is collected
        var pcLoot = FindFirstObjectByType<PlayerController>();
        if (pcLoot != null) pcLoot.SetCanMove(true);
        
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
