# Health & Sustain System - Implementation Plan

## Overview
Reworking the game's health, XP, and QTE systems to create skill-based sustain mechanics that reward player mastery.

### Core Systems
1. **Wound/Threshold System** - Every 50 HP is a threshold. Crossing below locks that threshold until rest.
2. **QTE Healing** - Both offensive and defensive QTEs restore HP (Bad 1%, Good 3%, Perfect 5%)
3. **XP System** - Enemies grant XP (Normal 1, Elite 3, Boss 5) spent at Rest nodes
4. **Rest Rework** - Choose between Heal (restore HP + remove wounds) OR Ascension (spend XP)

---

## Phase 0 — Prep and Safety ✅
- [x] Create feature branch `feature/health-xp-qte`
- [x] Add debug overlay showing:
  - CurrentHP / MaxHP
  - WoundThresholdIndex (Wounds count)
  - RunXP
  - Last QTE result
  
**Files created/modified:**
- `DebugOverlay.cs` - Debug UI panel (toggle with F1)
- `GameManager.cs` - Added RunXP, LastQTEResult static fields
- `Player.cs` - Added WoundCount, ThresholdSize fields
- `Referencer.cs` - Initialize DebugOverlay

---

## Phase 1 — Replace Rewards with XP ✅
- [x] Add `RunXP` to GameManager
- [x] XP per encounter: Normal=1, Elite=3, Boss=5
- [x] On combat end: detect type, add XP
- [x] Remove direct stat rewards from combat (relics no longer drop)
- [x] Update reward UI to show "Gained +X XP"

**Files modified:**
- `CombatManager.cs` - Grant XP on victory based on combat type
- `CombatUI.cs` - Changed loot panel from relic to XP display

**Done check**: After combat, only XP is gained; stats don't change until rest. ✅

---

## Phase 2 — Implement Wounds/Thresholds ✅
- [x] Add fields: MaxHP, CurrentHP, ThresholdSize (50), WoundCount
- [x] Compute MaxRecoverableHP = MaxHP - (WoundCount * ThresholdSize)
- [x] On damage: if HP crosses threshold boundary, increment wounds
- [x] On heal: clamp to MaxRecoverableHP
- [x] Add ClearWounds() and FullRest() methods

**Files modified:**
- `Player.cs` - TakeDamage tracks wound thresholds, Heal clamps to max recoverable
- `DebugOverlay.cs` - Shows HP cap based on wounds

**Done check**: Heavy damage past threshold → can heal, but not past wound limit until rest. ✅

---

## Phase 3 — Add Defensive QTE ✅
- [x] Create Defensive QTE UI (timing bar)
- [x] On enemy attack: pause, show QTE
- [x] Resolve: Bad/Good/Perfect
- [x] Apply healing BETWEEN attacks (per hit):
  - Bad: 1% max HP
  - Good: 3%
  - Perfect: 5%
- [x] Then apply damage (skill prevents auto-death)

**Files created/modified:**
- `DefensiveQTE.cs` - New defensive QTE UI with timing bar
- `CombatManager.cs` - EnemyTurn now processes attacks individually with QTE
- `Referencer.cs` - Initialize DefensiveQTE

**Done check**: During enemy attacks, player regains HP but cannot heal past wound limits. ✅

---

## Phase 4 — Add Offensive QTE Healing ✅
- [x] After offensive QTE result determined, apply healing (1/3/5%)
- [x] Apply before end-of-turn checks
- [x] Clamp by wound threshold (handled by Player.Heal)

**Files modified:**
- `CombatManager.cs` - OnQTEComplete now heals player based on result

**Done check**: High-skill player stays healthier but cannot infinite-heal. ✅

---

## Phase 5 — Update Damage Order of Operations ✅
Player attack pipeline:
1. Calculate DirectBase
2. Apply offensive QTE multiplier → DirectAfterQTE
3. Roll crit on direct hit only → DirectAfterCrit
4. Use DirectAfterQTE (pre-crit) for reaction damage (crit doesn't affect reactions)
5. Final = DirectAfterCrit + ReactionBonus

**Files modified:**
- `CombatManager.cs` - ExecuteAttackWithReaction and ExecuteSkillWithReaction updated

**Done check**: Crit doesn't blow up reactions; QTE affects direct hit + reaction scaling. ✅

---

## Phase 6 — Rest Node Rework ✅
- [x] Rest UI: "Choose one" - Recover OR Ascension
- [x] **Recover**: Heal 40% of MaxHP + clear all wounds
- [x] **Ascension**: Spend XP on upgrades
  - +Max HP: 2 XP (+8 HP)
  - +Damage: 3 XP (+2 Damage) - Max 4 upgrades
  - +Crit Chance: 3 XP (+4%) - Max 3 upgrades
  - +Crit Damage: 3 XP (+0.1) - Capped at 2.0x
  - Heal: 1 XP (12% Max HP)
- [x] Add guardrails: per-run caps on damage/crit upgrades

**Files modified:**
- `RestUI.cs` - Complete rework with XP-based upgrade shop
- `Player.cs` - Added AddCritChance(), AddCritDamage() methods

**Done check**: Player can't heal AND upgrade; choice matters. ✅

---

## Phase 7 — Counter Relic + Elemental Synergies 🔲
- [ ] Perfect dQTE counter relic: deals base damage (no crit, no reaction)
- [ ] Rock: on damage dealt → gain shield (scales with Rock level)
- [ ] Water: on successful dQTE → bonus heal % (scales with Water level)

**Done check**: Defensive builds feel real.

---

## Phase 8 — Balance Pass 🔲
Log per 10 fights:
- Damage taken per fight
- HP regained from QTE
- Wounds triggered per fight
- XP earned per world

Adjust knobs:
- ThresholdSize (50)
- QTE heal % (1/3/5)
- Rest heal % (40%)

---

## Constants Reference
| Constant | Value |
|----------|-------|
| Threshold Size | 50 HP |
| QTE Heal (Bad) | 1% Max HP |
| QTE Heal (Good) | 3% Max HP |
| QTE Heal (Perfect) | 5% Max HP |
| XP (Normal) | 1 |
| XP (Elite) | 3 |
| XP (Boss) | 5 |
| Damage Cap | 4 upgrades |
| Crit Chance Cap | 3 upgrades |
| Crit Damage Cap | 2.0x |

---

## Suggested Priority Order
1. RunXP + rest spending
2. Wounds/threshold clamp
3. Defensive QTE healing
4. Offensive QTE healing
5. Rest heal/removes wounds
6. Counter + elemental defensive synergies
