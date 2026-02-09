# Enemy Rebalance — Project R

## Balance Analysis

### Player Damage Output (World 1 Baseline)

All characters have **10 AP/turn**. Realistic turn = 4–5 skill uses.

| Character | Avg Base Dmg | Typical Raw DPT | Notes |
|-----------|-------------|-----------------|-------|
| Fighter   | 15          | ~75             | Slash×3 + Riposte, WarCry burst turns |
| Mage      | 13          | ~65             | Bolt×3 + FrostNova, plus DoT ticks |
| Archer    | 16          | ~80             | AimedShot×3 + PiercingArrow, high crit uptime |
| Rogue     | 15          | ~75             | DirtyStab×5 with chain scaling |
| Cleric    | 13          | ~70             | Shock×3 + Smite, self-sustain offsets lower DPT |

**Average raw DPT across characters: ~75**

### Player Survivability

| Character | HP  | baseResist | Sustain Tools |
|-----------|-----|-----------|---------------|
| Fighter   | 160 | 15%       | Riposte (50% block), ShieldBash (weaken), Bladestorm AoE |
| Mage      | 145 | 13%       | ArcaneShield (30 shield), FrostNova (slow), Meteor (stun) |
| Archer    | 130 | 10%       | EvasiveRoll (100% dodge), PiercingArrow (true dmg) |
| Rogue     | 135 | 8%        | SmokeBomb (50% blind), CheapShot (stun), Backstab (burst) |
| Cleric    | 145 | 13%       | DivineShield (25 shield + 15 heal), Judgement (20% lifesteal), HolyNova (shield) |

Plus: potions (25/50/75 heal), rest nodes (20% max HP heal), elemental reaction shields.

### Current Enemy Problems (World 1)

**Regular enemies die in under 2 turns — no tactical pressure:**

| Enemy        | HP | baseResist | Eff DPT vs them | Turns to Kill | Enemy Avg Dmg |
|-------------|----|-----------:|----------------:|--------------:|--------------:|
| Rogue        | 50 | 15%       | 63.8            | **0.8**       | 14            |
| Archer       | 55 | 20%       | 60.0            | **0.9**       | 13            |
| Pyromaniac   | 55 | 15%       | 63.8            | **0.9**       | 15            |
| Wizard       | 60 | 20%       | 60.0            | **1.0**       | 12            |
| Frostcaller  | 60 | 20%       | 60.0            | **1.0**       | 11            |
| TideShaman   | 65 | 25%       | 56.3            | **1.2**       | 11            |
| Grunt        | 70 | 30%       | 52.5            | **1.3**       | 10            |
| Shieldbearer | 85 | 30%       | 52.5            | **1.6**       | 9             |

**Elites die in 2–5 turns — should be mini-boss encounters:**

| Elite         | HP  | baseResist | Turns to Kill | Enemy Avg Dmg |
|--------------|----:|-----------:|--------------:|--------------:|
| Juggernaut    | 160 | 35%       | **3.3**       | 18            |
| Spellbreaker  | 130 | 25%       | **2.3**       | 22            |
| StormCaptain  | 145 | 30%       | **2.8**       | 20            |
| StoneColossus | 190 | 45%       | **4.6**       | 16            |

**Bosses are too safe — player barely needs to heal:**

| Boss            | HP  | baseResist | Turns to Kill | Total Dmg to Player |
|----------------|----:|-----------:|--------------:|--------------------:|
| SlimeBoss       | 420 | 45%       | **10.2**      | ~107                |
| RainbowBoss     | 320 | 30%       | **6.1**       | ~118                |
| ElementalHydra  | 380 | 35%       | **7.8**       | ~131                |
| FallenChampion  | 460 | 40%       | **10.2**      | ~163                |

Bosses deal only 100–163 total damage over the entire fight. Players have 130–160 HP plus heals, shields, and potions. **Zero kill threat.**

---

## Balance Targets

| Tier    | Turns to Kill (solo) | Enemy Damage Pressure | Design Intent |
|---------|---------------------:|----------------------:|--------------|
| Regular | 2–3                  | Drain HP across encounters | Multiple fights should chip player down |
| Elite   | 5–7                  | Force defensive skill use | Risk/reward — worth the relic/sigil reward |
| Boss    | 12–18                | Threaten a kill | Force potions, shields, tactical cooldown management |

---

## Proposed Changes — Regular Enemies

### Rogue (Glass Cannon)
Fast and lethal but fragile. Should punish slow play.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 50       | **80**   | +60%     |
| baseDamageMin    | 13       | **18**   | +38%     |
| baseDamageMax    | 15       | **21**   | +40%     |
| baseResistance   | 15       | **18**   | +3       |
| bonusResistance  | 20       | **22**   | +2       |
| healthModifiers  | [1.0, 1.7, 2.3, 3.0, 3.8] | [1.0, 1.8, 2.5, 3.2, 4.0] | Steeper |
| damageModifiers  | [1.0, 1.5, 1.9, 2.4, 3.0] | [1.0, 1.6, 2.1, 2.7, 3.4] | Steeper |
| resistModifiers  | [0, 10, 15, 20, 25] | [0, 10, 16, 22, 28] | Steeper |

*Projected: ~1.3 turns to kill. In 2-enemy fights: ~2.6 turns total. Deals ~17 effective dmg/turn to player.*

### Archer (Ranged, Moderate)
Consistent damage from range. Moderate survivability.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 55       | **90**   | +64%     |
| baseDamageMin    | 12       | **17**   | +42%     |
| baseDamageMax    | 14       | **20**   | +43%     |
| baseResistance   | 20       | **23**   | +3       |
| bonusResistance  | 15       | **20**   | +5       |
| healthModifiers  | [1.0, 1.7, 2.3, 3.0, 3.8] | [1.0, 1.8, 2.5, 3.2, 4.0] | Steeper |
| damageModifiers  | [1.0, 1.5, 1.9, 2.4, 3.0] | [1.0, 1.6, 2.1, 2.7, 3.4] | Steeper |
| resistModifiers  | [0, 10, 15, 20, 25] | [0, 10, 16, 22, 28] | Steeper |

*Projected: ~1.6 turns to kill. Deals ~16 effective dmg/turn to player.*

### Pyromaniac (Glass Cannon, Highest Regular Damage)
Burns hard and fast. Must be prioritized in multi-enemy fights.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 55       | **78**   | +42%     |
| baseDamageMin    | 14       | **20**   | +43%     |
| baseDamageMax    | 16       | **23**   | +44%     |
| baseResistance   | 15       | **18**   | +3       |
| bonusResistance  | 25       | **28**   | +3       |
| healthModifiers  | [1.0, 1.7, 2.3, 3.0, 3.8] | [1.0, 1.8, 2.5, 3.2, 4.0] | Steeper |
| damageModifiers  | [1.0, 1.5, 1.9, 2.4, 3.0] | [1.0, 1.6, 2.1, 2.7, 3.4] | Steeper |
| resistModifiers  | [0, 10, 15, 20, 25] | [0, 10, 16, 22, 28] | Steeper |

*Projected: ~1.3 turns to kill. Deals ~19 effective dmg/turn — highest among regulars.*

### Wizard (Moderate Caster)
Tanky caster with high elemental resistance.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 60       | **100**  | +67%     |
| baseDamageMin    | 11       | **16**   | +45%     |
| baseDamageMax    | 13       | **19**   | +46%     |
| baseResistance   | 20       | **25**   | +5       |
| bonusResistance  | 25       | **30**   | +5       |
| healthModifiers  | [1.0, 1.7, 2.3, 3.0, 3.8] | [1.0, 1.8, 2.5, 3.2, 4.0] | Steeper |
| damageModifiers  | [1.0, 1.5, 1.9, 2.4, 3.0] | [1.0, 1.6, 2.1, 2.7, 3.4] | Steeper |
| resistModifiers  | [0, 10, 15, 20, 25] | [0, 10, 16, 22, 28] | Steeper |

*Projected: ~1.8 turns to kill. Punishes elemental attackers hard (25+30=55% resist if element matches).*

### Frostcaller (Defensive Caster)
Slow but tanky. High conditional resistance.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 60       | **100**  | +67%     |
| baseDamageMin    | 10       | **15**   | +50%     |
| baseDamageMax    | 12       | **18**   | +50%     |
| baseResistance   | 20       | **25**   | +5       |
| bonusResistance  | 30       | **35**   | +5       |
| healthModifiers  | [1.0, 1.7, 2.3, 3.0, 3.8] | [1.0, 1.8, 2.5, 3.2, 4.0] | Steeper |
| damageModifiers  | [1.0, 1.5, 1.9, 2.4, 3.0] | [1.0, 1.6, 2.1, 2.7, 3.4] | Steeper |
| resistModifiers  | [0, 10, 15, 20, 25] | [0, 10, 16, 22, 28] | Steeper |

*Projected: ~1.8 turns to kill. Highest conditional resist among regulars (25+35=60%).*

### TideShaman (Tanky Caster)
Durable magic user. Moderate across all stats.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 65       | **110**  | +69%     |
| baseDamageMin    | 10       | **15**   | +50%     |
| baseDamageMax    | 12       | **18**   | +50%     |
| baseResistance   | 25       | **28**   | +3       |
| bonusResistance  | 25       | **30**   | +5       |
| healthModifiers  | [1.0, 1.7, 2.3, 3.0, 3.8] | [1.0, 1.8, 2.5, 3.2, 4.0] | Steeper |
| damageModifiers  | [1.0, 1.5, 1.9, 2.4, 3.0] | [1.0, 1.6, 2.1, 2.7, 3.4] | Steeper |
| resistModifiers  | [0, 10, 15, 20, 25] | [0, 10, 16, 22, 28] | Steeper |

*Projected: ~2.0 turns to kill. Consistent and durable.*

### Grunt (Frontline Tank)
The wall. High HP, high base resist, lower damage.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 70       | **120**  | +71%     |
| baseDamageMin    | 9        | **14**   | +56%     |
| baseDamageMax    | 11       | **17**   | +55%     |
| baseResistance   | 30       | **33**   | +3       |
| bonusResistance  | 10       | **15**   | +5       |
| healthModifiers  | [1.0, 1.7, 2.3, 3.0, 3.8] | [1.0, 1.8, 2.5, 3.2, 4.0] | Steeper |
| damageModifiers  | [1.0, 1.5, 1.9, 2.4, 3.0] | [1.0, 1.6, 2.1, 2.7, 3.4] | Steeper |
| resistModifiers  | [0, 10, 15, 20, 25] | [0, 10, 16, 22, 28] | Steeper |

*Projected: ~2.4 turns to kill. Soaks damage for squishier allies.*

### Shieldbearer (Heavy Tank)
Maximum durability. Rewards burst damage and armor penetration.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 85       | **150**  | +76%     |
| baseDamageMin    | 8        | **13**   | +63%     |
| baseDamageMax    | 10       | **16**   | +60%     |
| baseResistance   | 30       | **36**   | +6       |
| bonusResistance  | 10       | **15**   | +5       |
| healthModifiers  | [1.0, 1.7, 2.3, 3.0, 3.8] | [1.0, 1.8, 2.5, 3.2, 4.0] | Steeper |
| damageModifiers  | [1.0, 1.5, 1.9, 2.4, 3.0] | [1.0, 1.6, 2.1, 2.7, 3.4] | Steeper |
| resistModifiers  | [0, 10, 15, 20, 25] | [0, 10, 16, 22, 28] | Steeper |

*Projected: ~3.1 turns to kill. Tankiest regular enemy. Forces multiple turns.*

---

## Proposed Changes — Elite Enemies

### Juggernaut (Tanky Bruiser)
High HP, high damage, high resistance. The classic stat-check fight.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 160      | **250**  | +56%     |
| baseDamageMin    | 17       | **24**   | +41%     |
| baseDamageMax    | 19       | **27**   | +42%     |
| baseResistance   | 35       | **40**   | +5       |
| bonusResistance  | 20       | **25**   | +5       |
| healthModifiers  | [1.0, 2.0, 2.8, 3.6, 4.6] | [1.0, 2.1, 3.0, 3.9, 5.0] | Steeper |
| damageModifiers  | [1.0, 1.7, 2.2, 2.8, 3.6] | [1.0, 1.8, 2.4, 3.1, 4.0] | Steeper |
| resistModifiers  | [0, 15, 20, 25, 30] | [0, 16, 22, 28, 34] | Steeper |

*Projected: ~5.6 turns. Deals ~22 effective dmg/turn → ~123 total damage to player. Forces healing.*

### Spellbreaker (High-Damage Glass Cannon Elite)
Kills fast or gets killed fast. The DPS race.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 130      | **240**  | +85%     |
| baseDamageMin    | 21       | **28**   | +33%     |
| baseDamageMax    | 23       | **31**   | +35%     |
| baseResistance   | 25       | **30**   | +5       |
| bonusResistance  | 30       | **35**   | +5       |
| healthModifiers  | [1.0, 2.0, 2.8, 3.6, 4.6] | [1.0, 2.1, 3.0, 3.9, 5.0] | Steeper |
| damageModifiers  | [1.0, 1.7, 2.2, 2.8, 3.6] | [1.0, 1.8, 2.4, 3.1, 4.0] | Steeper |
| resistModifiers  | [0, 15, 20, 25, 30] | [0, 16, 22, 28, 34] | Steeper |

*Projected: ~4.6 turns. Deals ~26 effective dmg/turn → ~120 total. Nearly lethal if unhealed.*

### StormCaptain (Balanced Elite)
Well-rounded stats. Tests overall build quality.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 145      | **260**  | +79%     |
| baseDamageMin    | 19       | **25**   | +32%     |
| baseDamageMax    | 21       | **28**   | +33%     |
| baseResistance   | 30       | **35**   | +5       |
| bonusResistance  | 30       | **32**   | +2       |
| healthModifiers  | [1.0, 2.0, 2.8, 3.6, 4.6] | [1.0, 2.1, 3.0, 3.9, 5.0] | Steeper |
| damageModifiers  | [1.0, 1.7, 2.2, 2.8, 3.6] | [1.0, 1.8, 2.4, 3.1, 4.0] | Steeper |
| resistModifiers  | [0, 15, 20, 25, 30] | [0, 16, 22, 28, 34] | Steeper |

*Projected: ~5.3 turns. Deals ~23 effective dmg/turn → ~124 total. Solid mid-fight.*

### StoneColossus (Super Tank Elite)
War of attrition. Rewards sustained damage and pierce.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 190      | **310**  | +63%     |
| baseDamageMin    | 15       | **21**   | +40%     |
| baseDamageMax    | 17       | **24**   | +41%     |
| baseResistance   | 45       | **48**   | +3       |
| bonusResistance  | 25       | **28**   | +3       |
| healthModifiers  | [1.0, 2.0, 2.8, 3.6, 4.6] | [1.0, 2.1, 3.0, 3.9, 5.0] | Steeper |
| damageModifiers  | [1.0, 1.7, 2.2, 2.8, 3.6] | [1.0, 1.8, 2.4, 3.1, 4.0] | Steeper |
| resistModifiers  | [0, 15, 20, 25, 30] | [0, 16, 22, 28, 34] | Steeper |

*Projected: ~7.9 turns. Deals ~20 effective dmg/turn → ~156 total. Long grind, forces sustained healing.*

---

## Proposed Changes — Boss Enemies

### SlimeBoss (High-HP Tank Boss)
Massive health pool. Tests endurance and resource management.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 420      | **600**  | +43%     |
| baseDamageMin    | 13       | **20**   | +54%     |
| baseDamageMax    | 15       | **23**   | +53%     |
| baseResistance   | 45       | **48**   | +3       |
| bonusResistance  | 20       | **25**   | +5       |
| healthModifiers  | [1.0, 2.2, 3.1, 4.0, 5.2] | [1.0, 2.3, 3.2, 4.2, 5.5] | Steeper |
| damageModifiers  | [1.0, 2.0, 2.6, 3.3, 4.2] | [1.0, 2.1, 2.8, 3.6, 4.6] | Steeper |
| resistModifiers  | [0, 18, 23, 28, 33] | [0, 18, 24, 30, 36] | Steeper |

*Projected: ~15.4 turns. Deals ~19 effective dmg/turn → ~291 total. Player MUST use potions and shields.*

### RainbowBoss (High-Damage Burst Boss)
Lower HP but devastating attacks. Punishes greed.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 320      | **480**  | +50%     |
| baseDamageMin    | 21       | **29**   | +38%     |
| baseDamageMax    | 23       | **33**   | +43%     |
| baseResistance   | 30       | **35**   | +5       |
| bonusResistance  | 30       | **35**   | +5       |
| healthModifiers  | [1.0, 2.2, 3.1, 4.0, 5.2] | [1.0, 2.3, 3.2, 4.2, 5.5] | Steeper |
| damageModifiers  | [1.0, 2.0, 2.6, 3.3, 4.2] | [1.0, 2.1, 2.8, 3.6, 4.6] | Steeper |
| resistModifiers  | [0, 18, 23, 28, 33] | [0, 18, 24, 30, 36] | Steeper |

*Projected: ~9.8 turns. Deals ~27 effective dmg/turn → ~267 total. Fastest boss but extremely dangerous.*

### ElementalHydra (Balanced, High Resist Boss)
Punishes elemental builds. Rewards physical/mixed damage.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 380      | **550**  | +45%     |
| baseDamageMin    | 19       | **26**   | +37%     |
| baseDamageMax    | 21       | **29**   | +38%     |
| baseResistance   | 35       | **40**   | +5       |
| bonusResistance  | 35       | **40**   | +5       |
| healthModifiers  | [1.0, 2.2, 3.1, 4.0, 5.2] | [1.0, 2.3, 3.2, 4.2, 5.5] | Steeper |
| damageModifiers  | [1.0, 2.0, 2.6, 3.3, 4.2] | [1.0, 2.1, 2.8, 3.6, 4.6] | Steeper |
| resistModifiers  | [0, 18, 23, 28, 33] | [0, 18, 24, 30, 36] | Steeper |

*Projected: ~12.2 turns. Deals ~24 effective dmg/turn → ~295 total. Full resource test.*

### FallenChampion (Final Boss, Well-Rounded)
The ultimate test. High HP, solid damage, strong resistance.

| Stat             | Current  | Proposed | Change   |
|-----------------|----------|----------|----------|
| baseHealth       | 460      | **680**  | +48%     |
| baseDamageMin    | 17       | **24**   | +41%     |
| baseDamageMax    | 19       | **28**   | +47%     |
| baseResistance   | 40       | **44**   | +4       |
| bonusResistance  | 25       | **30**   | +5       |
| healthModifiers  | [1.0, 2.2, 3.1, 4.0, 5.2] | [1.0, 2.3, 3.2, 4.2, 5.5] | Steeper |
| damageModifiers  | [1.0, 2.0, 2.6, 3.3, 4.2] | [1.0, 2.1, 2.8, 3.6, 4.6] | Steeper |
| resistModifiers  | [0, 18, 23, 28, 33] | [0, 18, 24, 30, 36] | Steeper |

*Projected: ~16.2 turns. Deals ~23 effective dmg/turn → ~371 total. Marathon fight, must manage every resource.*

---

## Summary of Fight Duration Changes

### Regular Enemies (World 1)
| Enemy        | Old Turns | New Turns | Old Dmg/Turn | New Dmg/Turn |
|-------------|----------:|----------:|-------------:|-------------:|
| Rogue        | 0.8       | **1.3**   | 12           | **17**       |
| Archer       | 0.9       | **1.6**   | 11           | **16**       |
| Pyromaniac   | 0.9       | **1.3**   | 13           | **19**       |
| Wizard       | 1.0       | **1.8**   | 11           | **15**       |
| Frostcaller  | 1.0       | **1.8**   | 10           | **15**       |
| TideShaman   | 1.2       | **2.0**   | 10           | **15**       |
| Grunt        | 1.3       | **2.4**   | 9            | **14**       |
| Shieldbearer | 1.6       | **3.1**   | 8            | **13**       |

### Elite Enemies (World 1)
| Elite         | Old Turns | New Turns | Old Total Dmg to Player | New Total Dmg to Player |
|--------------|----------:|----------:|------------------------:|------------------------:|
| Juggernaut    | 3.3       | **5.6**   | ~52                     | **~123**                |
| Spellbreaker  | 2.3       | **4.6**   | ~44                     | **~120**                |
| StormCaptain  | 2.8       | **5.3**   | ~49                     | **~124**                |
| StoneColossus | 4.6       | **7.9**   | ~65                     | **~156**                |

### Boss Enemies (World 1)
| Boss            | Old Turns | New Turns | Old Total Dmg to Player | New Total Dmg to Player |
|----------------|----------:|----------:|------------------------:|------------------------:|
| SlimeBoss       | 10.2      | **15.4**  | ~107                    | **~291**                |
| RainbowBoss     | 6.1       | **9.8**   | ~118                    | **~267**                |
| ElementalHydra  | 7.8       | **12.2**  | ~131                    | **~295**                |
| FallenChampion  | 10.2      | **16.2**  | ~163                    | **~371**                |