# Enemy Balance Pass

## Simulation Methodology

**Combat Math:**
- `damage_after_resist = raw_damage * (1 - resistance/100)`
- Player gets 10 AP/turn (~5 Skill1 uses)
- Defensive QTE grants ~3% max HP as shield per enemy attack (Good avg)
- Variance: +/-10% per hit

**Player Baselines (no upgrades, no elemental match):**

| Character | HP  | Avg Dmg | Resist | Skill1 Mult | Raw DPS/turn |
|-----------|-----|---------|--------|-------------|-------------|
| Fighter   | 160 | 15      | 15%    | 1.25x       | 93.75       |
| Mage      | 145 | 13      | 13%    | 1.20x       | 78 (+DoT)   |
| Archer    | 130 | 16      | 10%    | 1.20x       | 96 (+crit)  |
| Rogue     | 135 | 15      | 8%     | 1.10x       | 82.5 (+chain)|
| Cleric    | 145 | 13      | 13%    | 1.30x       | 84.5        |

**Enemies per encounter (from worldEncounter.json):**
- World 1: 1 regular, 1 elite, 1 boss
- World 2: 2 regular, 2 elite, 2 boss
- World 3: 3 regular, 3 elite, 2 boss
- World 4: 4 regular, 3 elite, 2 boss
- World 5: 5 regular, 4 elite, 3 boss

**Key metric:** TTD/TTK ratio (Turns to Die / Turns to Kill)
- \> 2.0 = Easy
- 1.2-2.0 = Fair
- 0.8-1.2 = Hard (requires upgrades/good play)
- < 0.8 = Unkillable

---

## Critical Issues Found (Pre-Balance)

### 1. Enemy base damage exceeded player damage
Thug avg 19.5 > Fighter avg 15. Every regular enemy hit harder than the player's base attacks.

### 2. Resistance too high across the board
Shieldbearer at 36% base resistance meant player dealt only 64% damage. StoneColossus at 48% = near-immunity. Combined with world scaling (+28 at W5), enemies became impervious.

### 3. World scaling far too aggressive
Old modifiers for regulars: HP [1.0, 1.8, 2.5, 3.2, 4.0], Dmg [1.0, 1.6, 2.1, 2.7, 3.4]. Combined with multi-enemy encounters:
- **World 2 (2 enemies):** Effective power = 1.6x dmg * 2 count = 3.2x. Player unchanged.
- **World 3 (3 enemies):** 2.1x * 3 = 6.3x. Completely unwinnable.
- 3x Thugs at W3: TTD=1.78 turns, TTK=9.7 turns. **Ratio = 0.18.**

### 4. Specific broken abilities
- **Pyromaniac Ignite:** 1.5x DoT for 3 turns = 96.75 total damage (60%+ of any character's HP from ONE ability)
- **Champion Strike:** 2.0x multiplier = 52 raw damage per basic attack
- **FallenChampion Reborn:** 100% HP + 100% damage bonus = effectively 1360 total HP with doubled DPS in phase 2
- **Monsoon:** Permanent HoT (duration -1) on all enemies = unkillable groups
- **Hydra Tail:** Stun + 50% Weaken + 50% Sunder simultaneously

---

## Changes Made

### Regular Enemy Stat Changes (enemies.json)

**Scaling modifiers (all regulars):**
| Modifier | Old | New |
|----------|-----|-----|
| healthModifiers | [1.0, 1.8, 2.5, 3.2, 4.0] | [1.0, 1.2, 1.5, 1.8, 2.2] |
| damageModifiers | [1.0, 1.6, 2.1, 2.7, 3.4] | [1.0, 1.15, 1.3, 1.5, 1.7] |
| resistanceModifiers | [0, 10, 16, 22, 28] | [0, 4, 7, 10, 14] |

**Thug:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseDamageMin/Max | 18-21 | 10-12 | Was higher than player damage |
| baseResistance | 18 | 12 | Too tanky for basic enemy |
| bonusResistance | 22 | 15 | Reduced proportionally |

**Archer:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 90 | 85 | Slight trim |
| baseDamageMin/Max | 17-20 | 10-13 | Was higher than player |
| baseResistance | 23 | 14 | Too high for ranged enemy |
| bonusResistance | 20 | 15 | Reduced |

**Pyromaniac:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 78 | 70 | Glass cannon identity |
| baseDamageMin/Max | 20-23 | 11-13 | Highest regular dmg was absurd |
| baseResistance | 18 | 10 | Glass cannon = low resist |
| bonusResistance | 28 | 18 | Reduced |

**Wizard:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 100 | 90 | Slight trim |
| baseDamageMin/Max | 16-19 | 9-11 | Low damage, relies on shield |
| baseResistance | 25 | 18 | Reduced |
| bonusResistance | 30 | 20 | Reduced |

**Frostcaller:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 100 | 90 | Slight trim |
| baseDamageMin/Max | 15-18 | 9-11 | Low damage, defensive enemy |
| baseResistance | 25 | 15 | Combined with Frost Shield was near-immune |
| bonusResistance | 35 | 20 | Was stacking to 80%+ with shield |

**TideShaman:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 110 | 95 | Trim for support enemy |
| baseDamageMin/Max | 15-18 | 9-11 | Support identity, low damage |
| baseResistance | 28 | 16 | Too tanky for healer |
| bonusResistance | 30 | 18 | Reduced |

**Deserter:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 120 | 100 | Still tankiest regular |
| baseDamageMin/Max | 14-17 | 10-12 | Moderate damage |
| baseResistance | 33 | 20 | Was extremely tanky |
| bonusResistance | 15 | 12 | Slight trim |

**Shieldbearer:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 150 | 120 | Still highest HP regular |
| baseDamageMin/Max | 13-16 | 8-10 | Low damage, tank identity. Retaliation counter now ~9 per hit instead of 14.5 |
| baseResistance | 36 | 22 | 36% base was elite-tier |
| bonusResistance | 15 | 12 | Slight trim |

---

### Elite Enemy Stat Changes

**Scaling modifiers (all elites):**
| Modifier | Old | New |
|----------|-----|-----|
| healthModifiers | [1.0, 2.1, 3.0, 3.9, 5.0] | [1.0, 1.3, 1.7, 2.1, 2.6] |
| damageModifiers | [1.0, 1.8, 2.4, 3.1, 4.0] | [1.0, 1.2, 1.4, 1.65, 1.9] |
| resistanceModifiers | [0, 16, 22, 28, 34] | [0, 5, 9, 13, 18] |

**Juggernaut:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 250 | 200 | Still tanky for elite |
| baseDamageMin/Max | 24-27 | 15-18 | ~Player damage level, appropriate for elite |
| baseResistance | 40 | 25 | Was near-immune |
| bonusResistance | 25 | 18 | Reduced |

**Spellbreaker:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 240 | 190 | Glass cannon elite |
| baseDamageMin/Max | 28-31 | 16-19 | Was 2x player damage |
| baseResistance | 30 | 20 | Reduced |
| bonusResistance | 35 | 22 | Reduced |

**StormCaptain:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 260 | 210 | Moderate elite |
| baseDamageMin/Max | 25-28 | 15-17 | Reduced to player-level |
| baseResistance | 35 | 22 | Reduced |
| bonusResistance | 32 | 20 | Reduced |

**StoneColossus:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 310 | 240 | Still highest HP elite |
| baseDamageMin/Max | 21-24 | 13-16 | Slow hitter identity |
| baseResistance | 48 | 28 | 48% was near-immune even at W1 |
| bonusResistance | 28 | 18 | Reduced |

---

### Boss Stat Changes

**Scaling modifiers (all bosses):**
| Modifier | Old | New |
|----------|-----|-----|
| healthModifiers | [1.0, 2.3, 3.2, 4.2, 5.5] | [1.0, 1.4, 1.8, 2.3, 2.8] |
| damageModifiers | [1.0, 2.1, 2.8, 3.6, 4.6] | [1.0, 1.25, 1.5, 1.75, 2.0] |
| resistanceModifiers | [0, 18, 24, 30, 36] | [0, 6, 11, 16, 22] |

**SlimeBoss:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 600 | 420 | Pre-split phase was too long |
| baseDamageMin/Max | 20-23 | 14-16 | Reduced |
| baseResistance | 48 | 28 | 48% was absurd |
| bonusResistance | 25 | 18 | Reduced |

**MadSlime / SadSlime (split spawns):**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 300 | 180 | Spawns should be weaker |
| baseDamageMin/Max | 10-12 | 8-10 | Slight trim |
| baseResistance | 24 | 14 | Reduced |
| bonusResistance | 13 | 10 | Reduced |

**MirrorBoss:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 480 | 380 | Moderate boss HP |
| baseDamageMin/Max | 29-33 | 16-19 | Was highest in entire game (avg 31!) |
| baseResistance | 35 | 24 | Reduced |
| bonusResistance | 35 | 20 | Was stacking to 70%+ |

**ElementalHydra:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 550 | 400 | Reduced |
| baseDamageMin/Max | 26-29 | 16-19 | Was too high |
| baseResistance | 40 | 28 | Reduced |
| bonusResistance | 40 | 22 | Was stacking to 80%+ |

**FallenChampion:**
| Stat | Old | New | Reason |
|------|-----|-----|--------|
| baseHealth | 680 | 450 | Effective HP was 1360 with reborn |
| baseDamageMin/Max | 24-28 | 15-18 | Reduced |
| baseResistance | 44 | 30 | Was near-immune |
| bonusResistance | 30 | 20 | Reduced |
| rebornHealthPercent | 100 | 60 | 100% = full reset was unfun |
| rebornDamageBonus | 1.0 | 0.5 | +100% was overkill, +50% still scary |

---

### Skill Changes (skills.json)

| Skill | Change | Old | New | Reason |
|-------|--------|-----|-----|--------|
| enemy_pyro_ignite | DoT multiplier | 1.5x | 0.8x | Was dealing 97 total damage (60%+ player HP) |
| enemy_wizard_arcane_shield | Shield % | 50% max HP | 25% max HP | Effectively doubled Wizard HP |
| enemy_tide_monsoon | HoT duration | -1 (permanent) | 3 turns | Permanent heal on all enemies = unkillable groups |
| enemy_jugg_sunder_smash | Vulnerable max stacks | 50% | 30% | 50% bonus damage taken was too punishing |
| enemy_hydra_breath | DoT multiplier | 1.5x | 0.8x | Same issue as Pyro Ignite |
| enemy_hydra_tail | Weaken magnitude | 50% | 30% | Stun+50% weaken+50% sunder was devastating |
| enemy_hydra_tail | Sunder magnitude | 50% | 30% | Reduced to match |
| enemy_champion_strike | Damage multiplier | 2.0x | 1.3x | 2.0x on a basic attack = 52 raw damage |
| enemy_champion_short_combo | Combo multiplier | 1.2x | 0.8x | Short combo should be weaker than basic |
| enemy_champion_long_combo | Combo multiplier/hits | 1.5x/6 hits | 1.2x/5 hits | Reduced intensity, still hardest attack |
| enemy_mirror_strike_copy | Damage multiplier | 1.5x | 1.2x | 1.5x on avg 31 damage was 46.5 per hit |

---

## Post-Balance Verification

### Fighter vs 3x Thug at World 3 (new stats)
- Thug W3: HP=120, Dmg=14.3, Resist=19%
- Fighter DPS after resist: 93.75 * 0.81 = 75.9
- TTK: 360/75.9 = **4.74 turns**
- Net enemy DPS: 3 * 14.3 * 0.85 - 14.4 (QTE) = 22.1
- TTD: 160/22.1 = **7.24 turns**
- **Ratio: 1.53 (Fair)**

### Rogue vs 3x Shieldbearer at World 3 (worst case)
- Shieldbearer W3: HP=180, Dmg=11.7, Resist=29%
- TTK: 540/58.6 = **9.22 turns**
- TTD: 135/20.15 = **6.70 turns**
- **Ratio: 0.73 (Hard - requires CC/upgrades)**

### Fighter vs SlimeBoss at World 1
- Phase 1 (to split): ~2.1 turns, ~17 damage taken
- Phase 2 (2 slimes): ~3.4 turns, ~20 damage taken
- Total: ~5.5 turns, ~37 damage out of 160 HP
- **Comfortable boss fight**

### Fighter vs FallenChampion at World 1
- Phase 1: 6.9 turns, ~73 damage taken
- Phase 2 (reborn at 60% HP, +50% dmg): 4.1 turns, ~77 damage taken
- Total: ~11 turns, ~150 damage out of 160 HP
- **Nail-biter. Requires good defensive QTE play or upgrades. Appropriate for hardest boss.**

---

## Design Notes

- Regular enemies now deal 55-75% of player base damage (was 85-140%)
- Elite enemies deal ~100% of player base damage (was 150-200%)
- Bosses deal 95-115% of player base damage (was 130-210%)
- World scaling is ~40-50% gentler across the board
- Resistance caps realistically at ~40-50% in late worlds (was 60-80%)
- Player power growth from sigils/relics/perks provides the margin needed for W4-W5