# Project R - Game Design Document

A turn-based roguelike RPG with elemental reaction combat, skill-based characters, and meta-progression.

---

## 1. Game Overview

### Genre
Turn-based roguelike RPG with elemental combat mechanics

### Core Concept
Players choose a character and navigate through 5 worlds, each containing encounters (combat, rest, shop, mystery nodes). Combat uses an Action Point (AP) system where skills apply elemental marks to enemies. When marks reach thresholds, elemental reactions trigger for bonus effects. Players gain gold, XP, and items throughout the run.

### Design Pillars
- Tactical depth through AP planning and elemental reaction setup
- Build expression through sigils, relics, and talent choices
- Fast run cadence with meaningful node decisions
- Strong replayability from route variance and meta-progression

### Target Experience
- **Primary Platform**: PC (keyboard/mouse) for v1.0
- **Controller Support**: Post-launch target once combat UX is stable
- **Full Run Duration Target**: 45-75 minutes
- **Combat Duration Target**: 2-4 min regular, 4-7 min elite, 8-12 min boss

### Win/Loss Conditions
- **Win**: Defeat all 5 world bosses
- **Loss**: Player health reaches 0

---

## 2. Game Flow

### Run Structure
```
Main Menu -> Character Select -> Character Loadout -> World 1 -> ... -> World 5 -> Victory
```

### World Structure
Each world contains:
1. **Fixed node count** to complete before boss
2. **Boss fight** after completing all nodes
3. World transition on boss defeat

| World | Nodes Before Boss |
|-------|-------------------|
| 1 | 10 |
| 2 | 12 |
| 3 | 14 |
| 4 | 16 |
| 5 | 18 |

### Node Distribution Targets (Per World)
- **Combat**: 50-60%
- **Elite**: 10-15%
- **Rest**: 10-15%
- **Shop**: 10-15%
- **Mystery**: 10-15%

### Node Types
| Node Type | Function |
|-----------|----------|
| **Combat** | Fight 1-3 regular enemies |
| **Elite** | Fight powerful elite enemy (guaranteed sigil + 50% relic) |
| **Rest** | Heal 20 HP OR +5 damage upgrade |
| **Shop** | Buy potions/relics with gold |
| **Mystery** | Random reward (gold, XP, relic) |

---

## 3. Characters

### Stats
| Stat | Description |
|------|-------------|
| **Max Health** | Total HP pool |
| **Damage** | Base damage for skills (min-max range) |
| **Max Energy** | Energy cap for ultimate skill |
| **Crit Chance** | % chance to critically hit |
| **Crit Damage** | Multiplier on critical hits (default 1.5x) |
| **Base Resistance** | Flat damage reduction (%) |
| **Bonus Resistance** | Extra resistance vs matching element |
| **Max Action Points** | AP per turn (default 10) |

### Characters (5 Total)
| Character | HP | Damage | Energy | Playstyle |
|-----------|-----|--------|--------|-----------|
| **Fighter** | 160 | 14-16 | 100 | Tank/sustain with shield bash and riposte |
| **Mage** | 145 | 12-14 | 120 | AoE damage with DoTs and stuns |
| **Archer** | 130 | 15-17 | 100 | High crit damage, multi-hit attacks |
| **Rogue** | 135 | 14-16 | 90 | Burst damage with stuns, chain attacks |
| **Cleric** | 145 | 12-14 | 100 | Healing, shields, and holy damage |

---

## 4. Combat System

### Turn Order
1. Player turn (spend AP on skills)
2. Player ends turn OR runs out of AP
3. Enemy turn (each enemy attacks)
4. Repeat until one side is defeated

### Action Points (AP)
- Start each turn with **10 AP**
- Skills cost 2-5 AP
- Can use multiple skills per turn
- End turn button available anytime

### Damage Calculation
```
FinalDamage = (BaseDamage * SkillMultiplier * Variance * CritMultiplier) * (1 - ResistancePercent / 100)
```
- **Variance**: 0.90-1.10 random roll per attack
- **Resistance**: Reduces damage by percentage
- **Resistance Clamp**: Total resistance is clamped to 0-80%

### Skill System
Each character has **5 skills**:
- **Skills 1-4**: Gain energy, have cooldowns
- **Skill 5 (Ultimate)**: Costs energy (90-120), powerful effect

#### Skill Properties
| Property | Description |
|----------|-------------|
| **AP Cost** | Action points required (2-5) |
| **Cooldown** | Turns before reuse (0-6) |
| **Damage %** | Multiplier on base damage |
| **Energy Gain/Cost** | Skills 1-4 gain, Skill 5 costs |
| **Element** | Elemental type for marks |
| **Mark Chance** | % to apply marks |
| **Mark Count** | Marks applied on hit |

#### Skill Effects
- **eff_deal_damage**: Deal damage based on scaling
- **eff_dot**: Apply damage over time
- **eff_apply_status**: Apply stun, slow, blind, etc.
- **eff_shield_gain**: Gain shield (absorbs damage)
- **eff_heal**: Restore health
- **eff_lifesteal**: Heal % of damage dealt
- **eff_temp_crit_bonus**: Temporary crit boost
- **eff_energy_delta**: Gain/spend energy
- **eff_on_kill_bonus**: Bonus effect if target dies
- **eff_stat_bonus**: Permanent or run-duration stat modification

### Shield System
- **Shield Cap**: 40% of max HP
- Absorbs damage before HP
- Persists between combats
- Clears on world transition

### Block System
- Reduces incoming damage by X%
- Lasts 1 turn
- Applied by defensive skills

---

## 5. Elemental System

### Elements (6)
Fire, Ice, Water, Wind, Rock, Lightning

### Mark System
- Skills apply elemental marks to enemies
- Marks accumulate on targets
- Reactions trigger at thresholds:
  - **6 marks** of same element = Pure reaction
  - **3+3 marks** of two elements = Combo reaction

### Reaction Effect Types
- **DoT**: Damage over time (% of hit damage)
- **Shield**: Flat or % of damage as shield
- **Status**: Apply stun/freeze
- **Crit Bonus**: Temporary crit chance/damage
- **Resist Buff**: Reduce incoming damage

### Skill Enchantment
- Sigils drop from elite enemies (100% chance)
- Sigils enchant skills with elements
- Enchanted skills apply their element's marks

### Reaction Resolution Rules
- Only one elemental reaction can trigger per hit
- Priority order: **Pure (6 marks)** over **Combo (3+3 marks)**
- Triggered reactions consume the marks they use
- Reaction damage does not apply new marks unless explicitly configured

## 6. Enemy System

### Enemy Types
| Type | Stats | Rewards |
|------|-------|---------|
| **Regular** | Low HP/damage | 1 XP, 1-3 gold |
| **Elite** | 2-3x regular stats | 3 XP, 6-9 gold, sigil + 50% relic |
| **Boss** | 5-6x regular stats | 9 XP, 12-16 gold, sigil + relic |

### World Scaling
Enemies scale per world using multipliers:

| World | HP Mult | DMG Mult | Resist Add |
|-------|---------|----------|------------|
| 1 | 1.0x | 1.0x | +0 |
| 2 | 1.7x | 1.5x | +10 |
| 3 | 2.3x | 1.9x | +15 |
| 4 | 3.0x | 2.4x | +20 |
| 5 | 3.8x | 3.0x | +25 |

Elites/Bosses have higher multipliers.

### Enemy List (17 Total)
**Regular (8)**: Grunt, Wizard, Archer, Rogue, Shieldbearer, Pyromaniac, Frostcaller, TideShaman

**Elite (4)**: Juggernaut, Spellbreaker, StormCaptain, StoneColossus

**Boss (5)**: SlimeBoss, RainbowBoss, ElementalHydra, FallenChampion, VoidSovereign

---

## 7. Progression Systems

### In-Run Progression
| Source | Rewards |
|--------|---------|
| Combat | Gold, XP, chance for sigils |
| Elite | Guaranteed sigil, 50% relic |
| Boss | Guaranteed sigil + relic |
| Shop | Buy potions/relics |
| Rest | Heal OR permanent +5 damage |
| Mystery | Random gold/XP/relic |

### Wound System
- HP thresholds every **50 HP**
- When HP drops below threshold, player gains a "wound"
- Wounds limit max recoverable HP until cleared
- Full rest at Rest nodes clears wounds

### Player Inventory
- **Potions**: Max 4, consumable in combat
- **Relics**: Passive permanent effects
- **Gold**: Spend at shops

### Economy Targets (Baseline)
| Source/Sink | Value Target |
|-------------|--------------|
| Regular combat reward | 1-3 gold |
| Elite combat reward | 6-9 gold |
| Boss combat reward | 12-16 gold |
| Potion price (S/M/L) | 12 / 24 / 36 gold |
| Basic relic price range | 50-90 gold |

---

## 8. Items

### Potions (3 Types)
| Category | Sizes | Effect |
|----------|-------|--------|
| **Health** | S/M/L | Restore 25/50/75 HP |
| **Damage** | S/M/L | Increase damage by 10/20/30% |
| **Energy** | S/M/L | Restore 25/50/75 Energy |

### Relics (4 Types)
| Relic | Effect |
|-------|--------|
| All Elemental | +10 all elemental damage |
| Crit Rate | +10% crit chance |
| Crit Damage | +20% crit damage |
| Bonus Health | +50 max HP |

---

## 9. Meta-Progression

### Character Ascension
- Completing a run awards 1 ascension level to that character
- Ascension provides permanent bonuses

### Elemental Ascension  
- Killing enemies with reactions awards XP to the elements
- XP thresholds unlock permanent elemental bonuses

### Talent Perks
- Each character has 4 talent tiers
- Each tier has 2 options (A/B)
- Unlock and choose perks between runs

---

## 10. QTE System

### Offensive QTE
When attacking, player may get a timing minigame:
| Result | Damage Multiplier |
|--------|-------------------|
| Bad | 0.9x |
| Good | 1.0x |
| Perfect | 1.1x |

### Defensive QTE
When being attacked, defensive timing affects damage taken/blocked.

### QTE Accessibility and Options
- Offensive and defensive QTE can be toggled independently
- If QTE is disabled, result defaults to **Good (1.0x)**
- Timing window presets: **Lenient**, **Normal**, **Strict**

---

## 11. Data Architecture

### JSON Files (Resources/Data/)
| File | Purpose |
|------|---------|
| `characters.json` | Character stats, skills, perks |
| `skills.json` | Skill definitions with effects |
| `effects.json` | Reusable effect and status definitions |
| `enemies.json` | Enemy types with world scaling |
| `worldEncounter.json` | Per-world node counts |
| `elementalReactions.json` | Reaction definitions |
| `reactionEffects.json` | Reaction effect details |
| `potions.json` | Potion items |
| `relics.json` | Relic items |
| `rest.json` | Rest node options |
| `qteOffensive.json` | QTE timing multipliers |
| `qteDefensive.json` | Defensive QTE data |
| `elementalTier.json` | Elemental ascension tiers |
| `profile.json` | Persistent meta unlocks, ascension, and currencies |
| `runSave.json` | Mid-run save state for resume |
| `settings.json` | User settings (audio, input, accessibility) |

---

## 12. JSON Data Structures

### Data Architecture Overview

The game uses a **data-driven effects system** where:
1. **Effects** are defined once in `effects.json` as reusable definitions
2. **Skills** reference effects by ID and provide per-skill overrides
3. **Characters** reference skills by ID
4. **Relics** reference effects by ID (same pattern as skills)

```
characters.json → skillIds[] → skills.json → effects[] → effects.json
                                              ↑
relics.json → effects[] ─────────────────────┘
```

---

### Damage Types
All damage in the game has a **damage type** that determines interactions:

| Damage Type | Description |
|-------------|-------------|
| **Physical** | Default melee/ranged damage, no elemental mark |
| **Elemental** | Carries an element (Fire, Ice, Water, Wind, Rock, Lightning) |

When a skill deals damage:
1. Apply damage of the specified type
2. If Elemental, apply elemental marks based on skill properties
3. Execute the skill's effect(s)

---

### Effects JSON (`effects.json`)

Effects are reusable definitions. Each effect defines its type and default values.

```json
{
  "effects": [
    { "id": "eff_deal_damage", "name": "Deal Damage", "type": "dealDamage" },
    { "id": "eff_energy_delta", "name": "Energy Change", "type": "energyDelta" },
    { "id": "eff_apply_status", "name": "Apply Status", "type": "applyStatus", "status": "" },
    { "id": "eff_dot", "name": "Damage Over Time", "type": "dot", "damagePercent": 30, "duration": 2 },
    { "id": "eff_shield_gain", "name": "Gain Shield", "type": "shieldGain", "capPercent": 40 },
    { "id": "eff_heal", "name": "Heal", "type": "heal" },
    { "id": "eff_lifesteal", "name": "Lifesteal", "type": "lifesteal", "percent": 20 },
    { "id": "eff_temp_crit_bonus", "name": "Crit Bonus", "type": "tempCritBonus" },
    { "id": "eff_multi_hit", "name": "Multi Hit", "type": "multiHit", "hitCount": 1 },
    { "id": "eff_on_kill_bonus", "name": "On Kill Bonus", "type": "onKillBonus" },
    { "id": "eff_elemental_reaction", "name": "Elemental Reaction", "type": "elementalReaction" },
    { "id": "eff_stat_bonus", "name": "Stat Bonus", "type": "statBonus" }
  ],
  "statuses": [
    { "id": "status_stun", "name": "Stunned", "type": "stun" },
    { "id": "status_slow", "name": "Slowed", "type": "slow" },
    { "id": "status_blind", "name": "Blinded", "type": "blind" },
    { "id": "status_weak", "name": "Weakened", "type": "weak" },
    { "id": "status_dot_burn", "name": "Burning", "type": "dot" },
    { "id": "status_evasion", "name": "Evasion", "type": "evasion" },
    { "id": "status_damage_up", "name": "Damage Up", "type": "damageBonus" },
    { "id": "status_riposte_stance", "name": "Riposte Stance", "type": "block" }
  ]
}
```

#### Effect Types
| Effect Type | Description | Key Parameters |
|-------------|-------------|----------------|
| `dealDamage` | Deal damage to target | `multiplier`, `ignoreArmor` |
| `energyDelta` | Gain/spend energy | `amount` (positive=gain, negative=cost) |
| `applyStatus` | Apply status effect | `status`, `duration`, `chance`, `magnitude` |
| `dot` | Damage over time | `damagePercent`, `duration` |
| `shieldGain` | Gain shield | `value` (flat) or `percentOfDamage`, `capPercent` |
| `heal` | Restore health | `value` |
| `lifesteal` | Heal % of damage | `percent` |
| `tempCritBonus` | Temp crit boost | `critChance`, `critDamage` |
| `multiHit` | Hit multiple times | `hitCount` |
| `onKillBonus` | Bonus on kill | `cooldownOverride`, `energyRefund`, `bonusDamageMultiplier` |
| `elementalReaction` | Trigger reaction | `triggerOnMarks` |
| `statBonus` | Add or multiply stat value | `stat`, `value`, `mode` |

---

### Skills JSON (`skills.json`)

Skills reference effects by ID with optional per-skill overrides.

```json
{
  "skills": [
    {
      "id": "skill_shield_bash",
      "name": "Shield Bash",
      "description": "Bash enemy with shield, dealing damage and weakening them",
      "apCost": 2,
      "cooldown": 2,
      "element": "none",
      "markChance": 100,
      "markCount": 1,
      "effects": [
        { "effectId": "eff_deal_damage", "target": "SelectedEnemy", "multiplier": 0.80 },
        { "effectId": "eff_apply_status", "target": "SelectedEnemy", "status": "status_weak", "duration": 2, "magnitude": 20 },
        { "effectId": "eff_energy_delta", "target": "Self", "amount": 15 }
      ]
    },
    {
      "id": "skill_fireball",
      "name": "Fireball",
      "description": "Hurl a ball of fire that burns the target",
      "apCost": 4,
      "cooldown": 3,
      "element": "Fire",
      "markChance": 80,
      "markCount": 2,
      "effects": [
        { "effectId": "eff_deal_damage", "target": "SelectedEnemy", "multiplier": 1.50 },
        { "effectId": "eff_dot", "target": "SelectedEnemy", "damagePercent": 20, "duration": 3 },
        { "effectId": "eff_elemental_reaction", "target": "SelectedEnemy", "triggerOnMarks": 6 },
        { "effectId": "eff_energy_delta", "target": "Self", "amount": 20 }
      ]
    },
    {
      "id": "skill_bladestorm",
      "name": "Bladestorm",
      "description": "Whirlwind attack hitting all enemies (Ultimate)",
      "apCost": 4,
      "cooldown": 4,
      "element": "none",
      "markChance": 100,
      "markCount": 1,
      "effects": [
        { "effectId": "eff_energy_delta", "target": "Self", "amount": -100 },
        { "effectId": "eff_deal_damage", "target": "AllEnemies", "multiplier": 1.50 }
      ]
    }
  ]
}
```

#### Effect Reference Fields
| Field | Description |
|-------|-------------|
| `effectId` | References effect ID in `effects.json` |
| `target` | `Self`, `SelectedEnemy`, `AllEnemies` |
| *override fields* | Any type-specific values (e.g., `multiplier`, `duration`, `amount`) |

---

### Characters JSON (`characters.json`)

Characters reference skills by ID via `skillIds` array.

```json
{
  "characters": [
    {
      "id": "char_fighter_01",
      "displayName": "Fighter",
      "characterId": 1,
      "stats": {
        "maxHealth": 160,
        "damageMin": 14,
        "damageMax": 16,
        "maxEnergy": 100,
        "critChance": 5,
        "critDamage": 1.5,
        "baseResistance": 15,
        "bonusResistance": 5
      },
      "gold": 10,
      "maxActionPoints": 10,
      "skillIds": ["skill_slash", "skill_shield_bash", "skill_war_cry", "skill_riposte", "skill_bladestorm"],
      "perkIds": {
        "tier1": ["fighter_option_a", "fighter_option_b"],
        "tier2": ["fighter_option_a", "fighter_option_b"],
        "tier3": ["fighter_option_a", "fighter_option_b"],
        "tier4": ["fighter_option_a", "fighter_option_b"]
      }
    }
  ]
}
```

---

### Relics JSON (`relics.json`)

Relics use the same effect reference pattern as skills.

```json
{
  "relics": [
    {
      "id": "relic_burning_aura",
      "displayName": "Burning Aura",
      "relicId": 1,
      "rarity": "Rare",
      "description": "Applies a burn effect at the start of combat",
      "effects": [
        { "effectId": "eff_dot", "target": "AllEnemies", "damagePercent": 5, "duration": 2 }
      ]
    },
    {
      "id": "relic_crit_rate",
      "displayName": "Crit Rate Relic",
      "relicId": 2,
      "rarity": "Uncommon",
      "description": "Increases critical hit chance by 10%",
      "effects": [
        { "effectId": "eff_stat_bonus", "target": "Self", "stat": "critChance", "value": 10 }
      ]
    },
    {
      "id": "relic_bonus_health",
      "displayName": "Bonus Health Relic",
      "relicId": 3,
      "rarity": "Rare",
      "description": "Increases maximum health by 50",
      "effects": [
        { "effectId": "eff_stat_bonus", "target": "Self", "stat": "maxHealth", "value": 50 }
      ]
    }
  ]
}
```

---

### Enemy JSON Structure

Enemies are categorized into three tiers: **Regular**, **Elite**, and **Boss**.
Example entries are shown below; production data includes the full enemy roster.

```json
{
  "enemies": {
    "regular": [
      {
        "id": "grunt",
        "name": "Grunt",
        "tier": "regular",
        "baseStats": {
          "health": 45,
          "damageMin": 8,
          "damageMax": 12,
          "resistance": 0
        },
        "damageType": "Physical",
        "attackPattern": ["basic_attack"],
        "rewards": {
          "xp": 1,
          "goldMin": 1,
          "goldMax": 3
        }
      },
      {
        "id": "pyromaniac",
        "name": "Pyromaniac",
        "tier": "regular",
        "baseStats": {
          "health": 40,
          "damageMin": 10,
          "damageMax": 14,
          "resistance": 5
        },
        "damageType": "Elemental",
        "element": "Fire",
        "attackPattern": ["fire_bolt", "flame_burst"],
        "rewards": {
          "xp": 1,
          "goldMin": 2,
          "goldMax": 4
        }
      }
    ],
    "elite": [
      {
        "id": "juggernaut",
        "name": "Juggernaut",
        "tier": "elite",
        "baseStats": {
          "health": 120,
          "damageMin": 18,
          "damageMax": 24,
          "resistance": 15
        },
        "damageType": "Physical",
        "attackPattern": ["heavy_slam", "intimidate", "shield_wall"],
        "specialAbility": {
          "type": "enrage",
          "trigger": "healthBelow",
          "threshold": 30,
          "effect": "damageBoost",
          "value": 50
        },
        "rewards": {
          "xp": 3,
          "goldMin": 6,
          "goldMax": 9,
          "sigilChance": 100,
          "relicChance": 50
        }
      },
      {
        "id": "storm_captain",
        "name": "Storm Captain",
        "tier": "elite",
        "baseStats": {
          "health": 100,
          "damageMin": 20,
          "damageMax": 28,
          "resistance": 10
        },
        "damageType": "Elemental",
        "element": "Lightning",
        "attackPattern": ["chain_lightning", "thunder_strike", "storm_call"],
        "specialAbility": {
          "type": "summon",
          "trigger": "turnCount",
          "threshold": 3,
          "summonId": "lightning_wisp"
        },
        "rewards": {
          "xp": 3,
          "goldMin": 7,
          "goldMax": 10,
          "sigilChance": 100,
          "relicChance": 50
        }
      }
    ],
    "boss": [
      {
        "id": "slime_boss",
        "name": "Slime King",
        "tier": "boss",
        "world": 1,
        "baseStats": {
          "health": 300,
          "damageMin": 15,
          "damageMax": 20,
          "resistance": 10
        },
        "damageType": "Physical",
        "attackPattern": ["slam", "split", "absorb", "toxic_spray"],
        "phases": [
          {
            "healthThreshold": 100,
            "behavior": "aggressive"
          },
          {
            "healthThreshold": 50,
            "behavior": "defensive",
            "abilityUnlock": "split"
          },
          {
            "healthThreshold": 25,
            "behavior": "enraged",
            "damageMultiplier": 1.5
          }
        ],
        "rewards": {
          "xp": 9,
          "goldMin": 12,
          "goldMax": 16,
          "sigilChance": 100,
          "relicChance": 100
        }
      },
      {
        "id": "elemental_hydra",
        "name": "Elemental Hydra",
        "tier": "boss",
        "world": 4,
        "baseStats": {
          "health": 600,
          "damageMin": 30,
          "damageMax": 45,
          "resistance": 25
        },
        "damageType": "Elemental",
        "elements": ["Fire", "Ice", "Lightning"],
        "heads": 3,
        "attackPattern": ["fire_breath", "ice_storm", "lightning_strike", "elemental_chaos"],
        "phases": [
          {
            "healthThreshold": 100,
            "activeHeads": 3,
            "behavior": "rotate_elements"
          },
          {
            "healthThreshold": 60,
            "activeHeads": 2,
            "behavior": "dual_attack"
          },
          {
            "healthThreshold": 30,
            "activeHeads": 1,
            "behavior": "all_elements",
            "damageMultiplier": 2.0
          }
        ],
        "rewards": {
          "xp": 9,
          "goldMin": 14,
          "goldMax": 18,
          "sigilChance": 100,
          "relicChance": 100
        }
      }
    ]
  },
  "worldScaling": {
    "1": { "hpMult": 1.0, "dmgMult": 1.0, "resistAdd": 0 },
    "2": { "hpMult": 1.7, "dmgMult": 1.5, "resistAdd": 10 },
    "3": { "hpMult": 2.3, "dmgMult": 1.9, "resistAdd": 15 },
    "4": { "hpMult": 3.0, "dmgMult": 2.4, "resistAdd": 20 },
    "5": { "hpMult": 3.8, "dmgMult": 3.0, "resistAdd": 25 }
  },
  "tierMultipliers": {
    "regular": { "hp": 1.0, "dmg": 1.0 },
    "elite": { "hp": 2.5, "dmg": 2.0 },
    "boss": { "hp": 5.0, "dmg": 3.0 }
  }
}
```

#### Enemy Tier Summary
| Tier | HP Multiplier | DMG Multiplier | Special Features |
|------|---------------|----------------|------------------|
| **Regular** | 1.0x | 1.0x | Basic attacks only |
| **Elite** | 2.5x | 2.0x | Special abilities, guaranteed sigil |
| **Boss** | 5.0x | 3.0x | Multiple phases, guaranteed sigil + relic |

---

## 13. Scene Architecture

The game uses a multi-scene architecture for clean separation of game states.

### Main Scene
**Purpose**: Entry point and menu hub

Contains:
- **Main Menu**: Start game, options, credits, quit
- **Character Select**: Choose from 5 characters
- **Pre-Run Loadout**: Configure character before run
  - View stats and skills
  - Equip sigils (if unlocked)
  - Select talent perks
  - View meta-progression bonuses

Flow:
```
Main Menu → Character Select → Loadout → Start Run (load World Scene)
```

---

### World Run Scenes
**Purpose**: One scene per world for exploration/navigation

| Scene | World | Description |
|-------|-------|-------------|
| `WorldScene_1` | World 1 | Starting world, tutorial hints |
| `WorldScene_2` | World 2 | Intermediate difficulty |
| `WorldScene_3` | World 3 | Advanced enemies |
| `WorldScene_4` | World 4 | Near-endgame challenge |
| `WorldScene_5` | World 5 | Final world, hardest enemies |

Each World Scene contains:
- **Node Map**: Visual representation of available nodes
- **Node Spawner**: Generates nodes based on `worldEncounter.json`
- **World UI**: Current world info, progress, player stats
- **Shop/Rest/Mystery UI**: Overlays for non-combat nodes

Flow:
```
World Scene → Select Node → [Combat? → Combat Arena] OR [Shop/Rest/Mystery → Stay in World]
```

On boss defeat:
```
World Scene N → (Boss Defeated) → World Scene N+1
```

---

### Combat Arena Scene
**Purpose**: Dedicated scene for all combat encounters

Contains:
- **Combat Layout**: Player and enemy positions
- **Combat UI**: AP bar, skill buttons, health displays
- **Floating Text Manager**: Damage/heal numbers
- **Turn Indicator**: Shows current turn owner
- **QTE System**: Offensive/defensive timing minigames

Scene Transitions:
```
World Scene → (Enter Combat Node) → Load Combat Arena
Combat Arena → (Combat Ends) → Return to World Scene
```

Data passed to Combat Arena:
- Current player state (HP, energy, cooldowns, inventory)
- Enemy composition (1-3 regulars, 1 elite, or 1 boss)
- Current world (for scaling)

Data returned from Combat Arena:
- Updated player state
- Combat rewards (XP, gold, items)
- Combat result (victory/defeat)

---

### Scene Manager
The `SceneManager` handles all scene transitions:

```csharp
// Scene transition examples
SceneManager.LoadScene("MainScene");           // Return to main menu
SceneManager.LoadScene("WorldScene_1");        // Enter World 1
SceneManager.LoadScene("WorldScene_2");        // Enter World 2
SceneManager.LoadScene("CombatArena");         // Enter combat
SceneManager.LoadScene("VictoryScene");        // Game won
SceneManager.LoadScene("DefeatScene");         // Game over
```

---

### Camera System (Cinemachine)

The game uses **Unity Cinemachine** for all camera control instead of manipulating the Main Camera directly. This provides smooth transitions, easy configuration, and cinematic effects.

#### Virtual Camera Setup
| Virtual Camera | Target Scene | Purpose |
|----------------|--------------|----------|
| `VCam_Menu` | MainScene | Static framing for menus |
| `VCam_WorldMap` | WorldScene_N | Overview of node map, follows player marker |
| `VCam_Combat` | CombatArena | Combat framing, centered on battlefield |
| `VCam_Victory` | VictoryScene | Celebration camera |
| `VCam_Defeat` | DefeatScene | Defeat screen camera |

#### Cinemachine Configuration
- **CinemachineBrain** attached to the Main Camera in each scene
- Only **one Virtual Camera active** at any time (Priority system handles blending)
- Use **Cinemachine Transitions** for smooth cuts between states
- Each Virtual Camera should be configured with appropriate:
  - **Body**: Transposer/Framing for follow behavior
  - **Aim**: Composer for look-at behavior
  - **Lens**: FOV and near/far clip settings

#### Combat Camera Behaviors
```
Player Turn Start     → VCam_Combat focuses on player
Skill Targeting       → Camera shifts toward targeted enemy
Enemy Turn            → Camera pulls back for overview
Reaction Trigger      → Brief zoom/shake for impact
```

#### Implementation Guidelines
- **Never manipulate Main Camera transform directly** — always use Virtual Cameras
- Create Virtual Cameras as scene objects, not instantiated at runtime
- Use Cinemachine **Impulse Sources** for screen shake effects
- For cutscenes/special moments, use **Cinemachine Timeline** integration

#### Package Requirement
Install via Package Manager: `com.unity.cinemachine` (version 2.9+ or 3.x)

---

## 14. Core Classes

### Infrastructure
- `Referencer`: Centralized reference hub that auto-discovers and caches all managers, UI components, and scene objects at startup

#### Referencer Pattern

The `Referencer` is a foundational MonoBehaviour that runs before all other scripts using `[DefaultExecutionOrder(-10000)]`. It serves as a **single access point** for cross-referencing any manager, UI component, or scene object — eliminating the need for scattered `Find` calls throughout the codebase.

**Design principles:**
- Runs first in execution order so all references are available before any other `Awake()` or `Start()` call
- Uses `FindFirstObjectByType<T>()` to locate components already in the scene
- If a required component is missing, it creates a fallback `GameObject` and attaches the component automatically
- Provides public fields for inspector assignment as an alternative to auto-discovery
- Scene-specific UI elements (panels, buttons, text) are located by hierarchy path and cached

**Example structure:**
```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(-10000)]
public class Referencer : MonoBehaviour
{
    // --- Managers ---
    public GameManager gameManager;
    public CombatManager combatManager;
    public DataCache dataCache;

    // --- UI ---
    public CombatUI combatUI;
    public ShopUI shopUI;
    public RestUI restUI;

    // --- Scene Objects ---
    public GameObject popupPanel;
    public Button confirmButton;
    public TextMeshProUGUI statusText;

    void Awake()
    {
        // Auto-discover managers, create if missing
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            var obj = new GameObject("GameManager");
            gameManager = obj.AddComponent<GameManager>();
        }

        combatManager = FindFirstObjectByType<CombatManager>();
        if (combatManager == null)
        {
            var obj = new GameObject("CombatManager");
            combatManager = obj.AddComponent<CombatManager>();
        }

        // Auto-discover UI components (same pattern)
        combatUI = FindFirstObjectByType<CombatUI>();
        if (combatUI == null)
        {
            var obj = new GameObject("CombatUI");
            combatUI = obj.AddComponent<CombatUI>();
        }

        // Locate scene-specific UI by hierarchy path
        var panel = GameObject.Find("Canvas/PopupPanel");
        if (panel == null) Debug.LogError("PopupPanel not found at Canvas/PopupPanel");
        popupPanel = panel;

        if (popupPanel != null)
        {
            var btn = popupPanel.transform.Find("ConfirmButton");
            if (btn == null) Debug.LogError("ConfirmButton not found under Canvas/PopupPanel/ConfirmButton");
            confirmButton = btn != null ? btn.GetComponent<Button>() : null;

            popupPanel.SetActive(false);
        }
    }
}
```

**Usage by other scripts:**
Any script that needs a reference simply accesses the `Referencer` instance rather than performing its own lookups. This keeps initialization centralized and deterministic. 
When setuping this script this is the only place where we should throw null checks. This is the main use of this script. 

**Placement:** `Assets/Scripts/Core/Referencer.cs`

---

### Managers
- `GameManager`: Run flow, world transitions, victory/defeat
- `CombatManager`: Turn-based combat loop
- `MetaProgressionManager`: Ascensions and unlocks
- `DataCache`: Load and cache all JSON data

### Entities
- `Player`: Stats, skills, inventory, cooldowns
- `CombatEnemy`: Enemy instance with marks and status

### UI
- `CombatUI`: Combat actions and health display
- `PlayerStatsUI`: Stats sheet (Tab key)
- `RestUI`, `ShopUI`, `MysteryUI`: Node UIs
- `FloatingTextManager`: Damage numbers

### Camera
- All camera control uses **Cinemachine Virtual Cameras**
- Main Camera has `CinemachineBrain` component
- Virtual Cameras per scene/state handle framing and transitions

### Nodes
- `NodeBase`: Base node class
- `CombatNode`, `RestNode`, `ShopNode`: Node types
- `NodeSpawner`: Spawn nodes for worlds

---

## 15. Key Formulas

### Damage
```csharp
baseDamage = character.Damage * skillMultiplier
variance = Random(0.90, 1.10)
critMultiplier = isCrit ? critDamage : 1.0
resistance = enemy.BaseResistance + tempResists
finalDamage = (baseDamage * variance * critMultiplier) * (1 - resistance/100)
```

### Resistance
```csharp
totalResist = baseResist + bonusResist
damageReduction = totalResist / 100  // capped at 1.0
```

### Healing Cap (Wound System)
```csharp
maxRecoverable = (lowestThresholdLevel + 1) * THRESHOLD_SIZE  // 50 HP per threshold
```

---

## 16. Save and Persistence

### Save Files
| File | Scope | Notes |
|------|-------|-------|
| `profile.json` | Account/meta | Ascension levels, elemental tiers, unlocked perks, currencies |
| `runSave.json` | Active run only | Current world, node graph state, player HP/energy/cooldowns, inventory |
| `settings.json` | User settings | Audio, controls, QTE/accessibility options |

### Autosave Rules
- Autosave after every node resolution (combat/rest/shop/mystery)
- Autosave immediately after boss defeat and before scene transition
- On run defeat/victory, `runSave.json` is cleared
- Only one active run is supported in v1.0

### Save Integrity
- Include `saveVersion` in each file for migration safety
- Validate required fields before load; if invalid, fail gracefully to main menu
- Use write-then-rename pattern to avoid corruption on interrupted writes

---

## 17. Scope and Milestones

### First Playable (Milestone A)
- 1 playable character
- 1 world + 1 boss
- Combat, rest, shop, and mystery nodes functioning
- Basic relic/potion drops and one persistent profile

### Vertical Slice (Milestone B)
- 3 playable characters
- 3 worlds + 3 bosses
- Full AP + elemental reaction loop
- Sigils, relics, and talent perks integrated

### v1.0 Release Scope
- 5 playable characters
- 5 worlds + 5 bosses
- Full meta-progression systems
- Save/resume and accessibility settings complete

---

## 18. AI-Assisted Implementation Prompts

This section contains step-by-step prompts for implementing the game using AI assistance (Claude Opus 4.6). Each prompt is intentionally small and focused to allow iterative development and debugging.

### Global Rules for All Prompts

> [!CAUTION]
> **Production Code Standards** - Apply these rules to EVERY implementation prompt:
> - **No fallbacks**: If data is missing, throw a clear exception with context
> - **No hardcoded data**: All game values must come from JSON files
> - **No duplicated data**: Single source of truth for each data type
> - **Clear errors**: Exception messages must explain what failed and why
> - **Clean architecture**: No god classes, single responsibility per class
> - **No shortcuts**: Complete implementations, no TODO comments or placeholder code

> [!NOTE]
> **Validation Markers** - Temporary validation code should be wrapped with:
> ```csharp
> // #VALIDATION_START
> Debug.Log($"[VALIDATE] Description of what we're checking: {value}");
> // #VALIDATION_END
> ```
> These markers allow easy removal before release using search/replace.

---

### Phase 1: Data Layer Foundation

#### Prompt 1.0: Referencer (Core Infrastructure)
```
Create a Referencer MonoBehaviour that acts as the centralized reference hub for the entire project.

Requirements:
- Use [DefaultExecutionOrder(-10000)] so it runs before all other scripts
- Declare public fields for every manager and UI component the project will use
- In Awake(), auto-discover each component using FindFirstObjectByType<T>()
- If a required manager is not found in the scene, create a new GameObject and attach the component as a fallback
- For scene-specific UI elements (panels, buttons, text), locate them by Canvas hierarchy path using GameObject.Find() and Transform.Find()
- Log clear Debug.LogError messages when expected scene objects are missing
- Cache UI references (Button, TextMeshProUGUI, GameObject) as public fields for external access
- Disable popup/overlay panels by default after caching (SetActive(false))
- Place in Assets/Scripts/Core/Referencer.cs

Design principles:
- Single source of truth for all cross-script references
- No other script should call FindFirstObjectByType or GameObject.Find at runtime
- Other scripts access what they need through the Referencer instance
- Add new fields as new managers/UI components are created in later phases

Production rules:
- No fallback values for scene UI — if a path is wrong, log an error with the exact expected path
- Do not silently swallow missing references
```

#### Prompt 1.1: JSON Data Classes (Effects)
```
Create C# data classes for deserializing effects.json using Unity's JsonUtility.

Requirements:
- Create EffectDefinition class with: id, name, type, and optional default fields
- Create StatusDefinition class with: id, name, type
- Create EffectsData wrapper class containing: effects (List), statuses (List)
- All classes must be [Serializable]
- Add XML documentation comments for each field
- Do NOT use dictionaries (JsonUtility incompatible)
- Place in Assets/Scripts/Core/Data/EffectsData.cs

Production rules:
- No fallback values - if JSON is malformed, deserialization should fail clearly
- Add #VALIDATION_START markers around any Debug.Log statements

Test by adding a simple MonoBehaviour that loads the JSON and logs the count of effects and statuses.
```

#### Prompt 1.2: JSON Data Classes (Skills)
```
Create C# data classes for deserializing skills.json using Unity's JsonUtility.

Requirements:
- Create SkillEffectReference class with: effectId, target, and ALL possible override fields (multiplier, amount, duration, status, magnitude, percent, value, critChance, critDamage, hitCount, ignoreArmor, percentOfDamage, capPercent, cooldownOverride, energyRefund, bonusDamageMultiplier, triggerOnMarks)
- Create ChainSettings class with: maxChainUses, stackBonusPerUse, maxStacks, forceCooldownAfterMaxChain
- Create SkillDefinition class with: id, name, description, apCost, cooldown, element, markChance, markCount, chainSettings (optional), effects (List<SkillEffectReference>)
- Create SkillsData wrapper class containing: skills (List)
- All optional fields should use nullable types or reasonable defaults
- Place in Assets/Scripts/Core/Data/SkillsData.cs

Production rules: Same as Prompt 1.1
```

#### Prompt 1.3: JSON Data Classes (Characters)
```
Create C# data classes for deserializing characters.json using Unity's JsonUtility.

Requirements:
- Create CharacterStats class with all stat fields from JSON
- Create PerkIds class with tier1-tier4 arrays
- Create CharacterDefinition class with: id, displayName, characterId, stats, gold, maxActionPoints, skillIds (List<string>), perkIds
- Create CharactersData wrapper class containing: characters (List)
- Place in Assets/Scripts/Core/Data/CharacterData.cs

Production rules: Same as Prompt 1.1
```

#### Prompt 1.4: JSON Data Classes (Relics)
```
Create C# data classes for deserializing relics.json using Unity's JsonUtility.

Requirements:
- Reuse SkillEffectReference from SkillsData.cs for the effects array
- Create RelicDefinition class with: id, displayName, relicId, rarity, description, effects (List<SkillEffectReference>)
- Create RelicsData wrapper class containing: relics (List)
- Place in Assets/Scripts/Core/Data/RelicData.cs

Production rules: Same as Prompt 1.1
```

#### Prompt 1.5: DataCache Manager
```
Create a DataCache singleton that loads and caches all JSON data files.

Requirements:
- Singleton pattern with static Instance property
- Private constructor, initialize in Awake()
- Public properties for each data type: Effects, Skills, Characters, Relics
- Load from Resources.Load<TextAsset>("Data/filename")
- Add GetEffectById(string id), GetSkillById(string id), etc. lookup methods
- Throw KeyNotFoundException with descriptive message if ID not found
- Place in Assets/Scripts/Core/Managers/DataCache.cs

Do NOT use dictionaries for the JSON structure, but you MAY use Dictionary<string, T> internally for fast lookups after loading.

Add #VALIDATION_START block that logs all loaded counts on initialization.
```

---

### Phase 2: Core Combat Classes

#### Prompt 2.1: Combat Entity Base
```
Create a base class for combat entities (player and enemies).

Requirements:
- Create abstract CombatEntity class
- Properties: CurrentHealth, MaxHealth, CurrentShield, CurrentEnergy, MaxEnergy
- Properties: CritChance, CritDamage, BaseResistance, BonusResistance
- Method: TakeDamage(int amount, bool ignoreArmor) - returns actual damage taken
- Method: Heal(int amount) - respects max health
- Method: AddShield(int amount, float capPercent) - respects shield cap
- Method: ModifyEnergy(int delta)
- Event: OnHealthChanged, OnShieldChanged, OnEnergyChanged
- Shield absorbs damage before health
- Resistance clamped to 0-80% as per GDD
- Place in Assets/Scripts/Core/Combat/CombatEntity.cs

Add #VALIDATION_START blocks that log damage/heal calculations for debugging.
```

#### Prompt 2.2: Status Effect System
```
Create a status effect system that can be applied to CombatEntity.

Requirements:
- Create StatusEffectInstance class with: statusId, remainingTurns, magnitude
- Add to CombatEntity: activeStatuses (List<StatusEffectInstance>)
- Method: ApplyStatus(string statusId, int duration, float magnitude, float chance)
- Method: RemoveStatus(string statusId)
- Method: HasStatus(string statusId) -> bool
- Method: GetStatusMagnitude(string statusId) -> float (0 if not present)
- Method: TickStatuses() - decrements duration, removes expired
- Status types to handle: stun, slow, blind, weak, dot, evasion, damageBonus, block
- Place status logic in Assets/Scripts/Core/Combat/StatusEffectSystem.cs

Do NOT implement status effects behavior here - just the data tracking.
```

#### Prompt 2.3: Player Combat Entity
```
Create PlayerCombat class extending CombatEntity.

Requirements:
- Additional properties: CurrentActionPoints, MaxActionPoints
- Property: EquippedSkillIds (List<string>) - loaded from character data
- Property: Cooldowns (Dictionary<string, int>) - tracks remaining cooldown per skill
- Method: CanUseSkill(string skillId) -> bool (check AP, cooldown, energy for ultimates)
- Method: UseSkill(string skillId) -> void (deduct AP, set cooldown)
- Method: StartTurn() - restore AP to max, tick cooldowns
- Method: EndTurn()
- Initialize from CharacterDefinition via Init(CharacterDefinition def)
- Place in Assets/Scripts/Core/Combat/PlayerCombat.cs

Do NOT implement skill execution here - just resource management.
```

#### Prompt 2.4: Enemy Combat Entity
```
Create EnemyCombat class extending CombatEntity.

Requirements:
- Load stats from EnemyDefinition (you may need to create this data class first)
- Property: EnemyId, DisplayName, Tier (regular/elite/boss)
- Property: AttackPattern (List<string>)
- Property: ElementalMarks (Dictionary<string, int>) - tracks marks per element
- Method: AddMarks(string element, int count)
- Method: GetMarkCount(string element) -> int
- Method: ClearMarks(string element)
- Method: GetTotalMarks() -> int
- Initialize from EnemyDefinition via Init(EnemyDefinition def, int worldNumber)
- Apply world scaling multipliers on init
- Place in Assets/Scripts/Core/Combat/EnemyCombat.cs
```

---

### Phase 3: Effect Execution System

#### Prompt 3.1: Effect Executor Interface
```
Create an interface and base structure for executing effects.

Requirements:
- Create IEffectExecutor interface with: Execute(EffectContext context)
- Create EffectContext class containing:
  - Caster (CombatEntity)
  - Target (CombatEntity or List<CombatEntity>)
  - SkillEffectReference (the effect reference with overrides)
  - EffectDefinition (the base effect definition)
  - LastDamageDealt (int, for effects that scale off damage)
  - DidKillTarget (bool, for on-kill effects)
- Create EffectExecutorFactory that returns the correct executor for a given effect type
- Throw NotImplementedException for unimplemented effect types with clear message
- Place in Assets/Scripts/Core/Combat/Effects/

Do NOT implement individual executors yet.
```

#### Prompt 3.2: Deal Damage Executor
```
Implement the DealDamageExecutor for the "dealDamage" effect type.

Requirements:
- Get multiplier from effect reference (default 1.0)
- Get ignoreArmor from effect reference (default false)
- Calculate: baseDamage * multiplier * variance(0.9-1.1)
- Apply crit if roll succeeds: damage * critDamage
- Call target.TakeDamage(finalDamage, ignoreArmor)
- Store result in context.LastDamageDealt
- Check if target died, set context.DidKillTarget
- Place in Assets/Scripts/Core/Combat/Effects/DealDamageExecutor.cs

Add #VALIDATION_START block logging the full damage calculation breakdown.
```

#### Prompt 3.3: Energy Delta Executor
```
Implement the EnergyDeltaExecutor for the "energyDelta" effect type.

Requirements:
- Get amount from effect reference (required, throw if missing)
- Positive = gain energy, negative = cost energy
- For ultimates (negative), verify entity has enough energy before skill use
- Call target.ModifyEnergy(amount)
- Place in Assets/Scripts/Core/Combat/Effects/EnergyDeltaExecutor.cs
```

#### Prompt 3.4: Apply Status Executor
```
Implement the ApplyStatusExecutor for the "applyStatus" effect type.

Requirements:
- Get status, duration, magnitude, chance from effect reference
- Roll against chance (default 100%)
- If success, call target.ApplyStatus(status, duration, magnitude)
- Place in Assets/Scripts/Core/Combat/Effects/ApplyStatusExecutor.cs
```

#### Prompt 3.5: Remaining Effect Executors
```
Implement the remaining effect executors:

1. DotExecutor ("dot") - Apply damage over time status
2. ShieldGainExecutor ("shieldGain") - Add shield to caster, respect cap
3. HealExecutor ("heal") - Restore health to target
4. LifestealExecutor ("lifesteal") - Heal caster based on LastDamageDealt
5. TempCritBonusExecutor ("tempCritBonus") - Temporary crit boost for this action
6. MultiHitExecutor ("multiHit") - Execute damage multiple times
7. OnKillBonusExecutor ("onKillBonus") - Bonus if DidKillTarget is true
8. StatBonusExecutor ("statBonus") - Modify a stat value
9. ElementalReactionExecutor ("elementalReaction") - Check marks and trigger

Each in its own file in Assets/Scripts/Core/Combat/Effects/

For ElementalReactionExecutor, just check if marks >= triggerOnMarks and log for now - actual reaction logic comes later.
```

---

### Phase 4: Skill Execution Pipeline

#### Prompt 4.1: Skill Executor
```
Create the SkillExecutor class that runs a complete skill.

Requirements:
- Method: ExecuteSkill(PlayerCombat caster, SkillDefinition skill, CombatEntity target, List<CombatEntity> allEnemies)
- For each effect in skill.effects:
  - Resolve target based on effect.target ("Self", "SelectedEnemy", "AllEnemies")
  - Create EffectContext
  - Get executor from EffectExecutorFactory
  - Execute the effect
  - Carry forward LastDamageDealt and DidKillTarget between effects
- Handle mark application (check markChance, add marks if elemental)
- Place in Assets/Scripts/Core/Combat/SkillExecutor.cs

Add #VALIDATION_START blocks logging each effect execution step.
```

#### Prompt 4.2: Target Resolution
```
Create TargetResolver utility class.

Requirements:
- Static method: Resolve(string targetType, CombatEntity caster, CombatEntity selectedTarget, List<CombatEntity> allEnemies) -> List<CombatEntity>
- "Self" -> returns [caster]
- "SelectedEnemy" -> returns [selectedTarget]
- "AllEnemies" -> returns allEnemies (filter out dead)
- Throw ArgumentException for unknown target types
- Place in Assets/Scripts/Core/Combat/TargetResolver.cs
```

---

### Phase 5: Combat Manager

#### Prompt 5.1: Combat State Machine
```
Create CombatManager with a state machine for combat flow.

Requirements:
- Enum: CombatState (Initializing, PlayerTurn, EnemyTurn, Victory, Defeat)
- Track current state, current player, list of enemies
- Method: StartCombat(PlayerCombat player, List<EnemyCombat> enemies)
- Method: EndPlayerTurn() - transition to EnemyTurn, process enemy actions
- Method: ProcessEnemyTurn() - each enemy takes action, then back to PlayerTurn
- Check victory condition: all enemies dead
- Check defeat condition: player dead
- Events: OnCombatStateChanged, OnCombatEnded
- Place in Assets/Scripts/Core/Combat/CombatManager.cs

Do NOT implement enemy AI yet - just structure.
```

#### Prompt 5.2: Combat Initialization
```
Extend CombatManager to handle initialization.

Requirements:
- Load player from current run state (for now, hardcode Fighter for testing)
- Receive enemy composition from scene/node data
- Instantiate EnemyCombat instances from enemy definitions
- Apply world scaling
- Set initial state to PlayerTurn
- Fire OnCombatStarted event

Add a test scene setup that starts combat with 1 player vs 2 enemies.
```

#### Manual Unity Step 5.2.1
```
In Unity Editor:
1. Create scene: Assets/Scenes/CombatTestScene.unity
2. Create empty GameObject named "CombatManager"
3. Attach CombatManager component
4. Create empty GameObject named "DataCache"
5. Attach DataCache component
6. Ensure DataCache is first in Script Execution Order (Edit > Project Settings > Script Execution Order)
```

---

### Phase 6: Combat UI

#### Prompt 6.1: Combat UI Manager
```
Create CombatUIManager that displays combat state.

Requirements:
- Reference UI elements: playerHealthBar, playerEnergyBar, playerAPText
- Reference UI elements: enemyHealthBars (List), enemyNameTexts
- Reference UI elements: skillButtons (List of Button)
- Method: UpdatePlayerUI(PlayerCombat player)
- Method: UpdateEnemyUI(List<EnemyCombat> enemies)
- Method: SetupSkillButtons(List<string> skillIds)
- Skill buttons should be interactable only if CanUseSkill returns true
- Subscribe to CombatManager.OnCombatStateChanged
- Place in Assets/Scripts/Core/Combat/CombatUI.cs

Do NOT create the actual UI prefab yet - just the script.
```

#### Manual Unity Step 6.1.1
```
In Unity Editor, create Combat UI:
1. Create Canvas (Screen Space - Overlay)
2. Create Panel "PlayerPanel" at bottom:
   - Slider "HealthBar" (red fill)
   - Slider "EnergyBar" (blue fill)
   - Text "APText"
   - 5 Buttons for skills (horizontal layout group)
3. Create Panel "EnemyPanel" at top:
   - Vertical layout for enemy health bars (3 max)
4. Create "EndTurnButton"
5. Assign all references to CombatUIManager component
```

#### Prompt 6.2: Skill Button Setup
```
Create SkillButton component for individual skill buttons.

Requirements:
- Properties: SkillId, SkillDefinition (cached)
- Display: skill name, AP cost, cooldown status
- Visual states: Available, OnCooldown, InsufficientAP, InsufficientEnergy
- Method: UpdateState(PlayerCombat player)
- OnClick: Fire event with skillId (handled by CombatUIManager)
- Place in Assets/Scripts/Core/Combat/UI/SkillButton.cs
```

---

### Phase 7: Damage Numbers and Feedback

#### Prompt 7.1: Floating Text System
```
Create a floating text system for damage/heal numbers.

Requirements:
- Create FloatingText prefab script with: text, color, animation
- Create FloatingTextManager singleton
- Method: SpawnText(Vector3 position, string text, Color color)
- Animate: float upward, fade out over 1 second
- Pool floating text objects (min 10)
- Place in Assets/Scripts/Core/Combat/UI/FloatingTextManager.cs
```

#### Manual Unity Step 7.1.1
```
In Unity Editor:
1. Create prefab: Assets/Prefabs/UI/FloatingText.prefab
   - Canvas (World Space, small scale)
   - TextMeshPro text component
2. Attach FloatingText script
3. Create empty GameObject "FloatingTextManager"
4. Attach FloatingTextManager, assign prefab reference
```

---

### Phase 8: Enemy AI

#### Prompt 8.1: Basic Enemy AI
```
Create EnemyAI class for enemy turn behavior.

Requirements:
- Method: DecideAction(EnemyCombat enemy, PlayerCombat player) -> EnemyAction
- Create EnemyAction class: actionType (Attack/Skill), targetIndex, damage
- For regular enemies: always attack player
- Calculate damage: enemy base damage * variance * (1 - player resistance)
- Place in Assets/Scripts/Core/Combat/EnemyAI.cs

Keep it simple - more complex AI patterns come later.
```

#### Prompt 8.2: Hook Enemy AI into Combat Manager
```
Integrate enemy AI into CombatManager.ProcessEnemyTurn().

Requirements:
- For each alive enemy:
  - Get action from EnemyAI.DecideAction()
  - Apply damage to player
  - Spawn floating damage text
  - Check if player died
- After all enemies act, transition to PlayerTurn (or Defeat)
- Add delay between enemy actions for readability (0.5s)

Use coroutines for timing.
```

---

### Phase 9: Node System Foundation

#### Prompt 9.1: Node Types and Data
```
Create node type definitions and spawn configuration.

Requirements:
- Enum: NodeType (Combat, Elite, Rest, Shop, Mystery, Boss)
- Create NodeSpawnConfig ScriptableObject with:
  - nodesPerWorld (int[5])
  - combatWeight, eliteWeight, restWeight, shopWeight, mysteryWeight
- Create NodeDefinition class: nodeType, position, isCompleted, connectedNodes
- Place in Assets/Scripts/Core/World/NodeData.cs

Do NOT create the visual node graph yet.
```

#### Prompt 9.2: Node Spawner
```
Create NodeSpawner that generates a node graph for a world.

Requirements:
- Method: GenerateWorld(int worldNumber) -> List<NodeDefinition>
- Use weights from NodeSpawnConfig to distribute node types
- Always end with Boss node
- Elite nodes: minimum 1, maximum 2 per world
- Create linear path for now (node N connects to node N+1)
- Place in Assets/Scripts/Core/World/NodeSpawner.cs

Branching paths come in a later prompt.
```

---

### Phase 10: Camera System

#### Prompt 10.1: Cinemachine Setup
```
Set up Cinemachine camera system for the game.

Requirements:
- Install Cinemachine package (com.unity.cinemachine)
- Add CinemachineBrain component to Main Camera in each scene
- Create Virtual Cameras for each scene:
  - VCam_Menu (MainScene) - static menu framing
  - VCam_WorldMap (WorldScene_N) - follows player on node map
  - VCam_Combat (CombatArena) - battlefield overview
- Configure appropriate Body/Aim settings for each
- NEVER manipulate Main Camera transform directly

Place Virtual Cameras as scene objects, not runtime instantiated.
```

#### Prompt 10.2: Combat Camera Transitions
```
Implement combat camera transitions using Cinemachine.

Requirements:
- Create CameraController MonoBehaviour
- Methods:
  - FocusOnPlayer() - shift camera toward player
  - FocusOnEnemy(CombatEnemy target) - shift toward target
  - CombatOverview() - pull back for full battlefield
  - TriggerImpact(float intensity) - screen shake via Impulse
- Use Cinemachine Priority system to blend between states
- Add Cinemachine Impulse Source for reaction effects
- Place in Assets/Scripts/Core/Camera/CameraController.cs

Integrate with CombatManager state transitions.
```

---

### Phase 11: Save System

#### Prompt 11.1: Profile Data Structure
```
Create profile save data structure.

Requirements:
- Create ProfileData class (Serializable):
  - saveVersion (int)
  - characterAscensions (Dictionary<string, int>)
  - elementalTiers (Dictionary<string, int>)
  - unlockedPerks (List<string>)
  - totalGoldEarned (int)
- Create ProfileManager singleton
- Method: LoadProfile() / SaveProfile()
- Save to Application.persistentDataPath + "/profile.json"
- Use write-then-rename pattern for safety
- Place in Assets/Scripts/Core/Save/ProfileManager.cs

Throw exception with details if save file is corrupted.
```

#### Prompt 10.2: Run Save Data Structure
```
Create run save data structure.

Requirements:
- Create RunSaveData class (Serializable):
  - saveVersion (int)
  - currentWorldIndex (int)
  - currentNodeIndex (int)
  - playerState (PlayerStateData - health, energy, gold, inventory)
  - nodeGraph (List<NodeDefinition>)
  - completedNodes (List<int>)
- Create RunSaveManager singleton
- Method: SaveRun() / LoadRun() / ClearRun()
- Save to Application.persistentDataPath + "/runSave.json"
- Place in Assets/Scripts/Core/Save/RunSaveManager.cs
```

---

### Phase 11: Integration Testing

#### Prompt 11.1: Full Combat Integration Test
```
Create an integration test scene that validates the full combat loop.

Requirements:
- Create TestCombatIntegration MonoBehaviour
- Initialize DataCache, load all JSON
- Create PlayerCombat from Fighter definition
- Create 2 EnemyCombat from Grunt definition
- Start combat via CombatManager
- Log each state transition
- Simulate: player uses Slash on enemy 1, end turn, enemies attack, repeat
- Verify damage calculations match expected values
- Place in Assets/Scripts/Tests/TestCombatIntegration.cs

Mark all validation logs with #VALIDATION_START/#VALIDATION_END.
Run this test manually in CombatTestScene.
```

#### Prompt 11.2: Remove Validation Markers
```
Search the entire codebase for #VALIDATION_START and #VALIDATION_END blocks.

For each block found:
1. Log the file and line numbers
2. Comment out (do not delete) the validation code
3. Add // VALIDATION DISABLED prefix

This allows re-enabling for future debugging while keeping production code clean.
```

---

### Build Order Summary

For each phase, complete ALL prompts in order before moving to the next phase:

| Phase | Description | Dependencies |
|-------|-------------|--------------|
| 1 | Data Layer | None |
| 2 | Core Combat Classes | Phase 1 |
| 3 | Effect Execution | Phase 2 |
| 4 | Skill Execution | Phase 3 |
| 5 | Combat Manager | Phase 4 |
| 6 | Combat UI | Phase 5 |
| 7 | Floating Text | Phase 5 |
| 8 | Enemy AI | Phase 5 |
| 9 | Node System | Phase 1 |
| 10 | Save System | Phase 1 |
| 11 | Integration | All above |