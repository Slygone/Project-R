# Status Effects Test Plan
## DoT Stacking & Stun Mechanics

---

## DoT (Damage over Time) Stacking

### Design Rules
- **Base DoT**: 20% of direct damage per turn for 2 turns
- **Stacking**: When same DoT type is reapplied, new damage = base + 50% of old remaining per-turn damage
- **Duration**: Always resets to full (2 turns) on reapplication
- **Stack Cap**: Maximum 3 stacks (no further damage increase after cap, only duration refresh)
- **Tick Timing**: Start of enemy turn (DoT ticks even when stunned)

### Test Cases

#### TC-DOT-01: Basic DoT Application
1. Bolt hits enemy for 100 damage
2. DoT applied: 20/turn for 2 turns
3. **Expected**: Enemy health bar shows "-20 DoT (2t)"

#### TC-DOT-02: DoT Tick After 1 Turn
1. Apply DoT (20/turn, 2 turns)
2. End player turn, enemy turn starts
3. **Expected**: Enemy takes 20 DoT damage, duration becomes 1 turn

#### TC-DOT-03: DoT Stacking (Reapply While Active)
1. Turn 1: Bolt hits 100, DoT = 20/turn for 2 turns
2. Turn 2 start: DoT ticks 20 (1 turn remains)
3. Turn 2: Bolt hits 100 again
4. **Expected**: New DoT = 20 + (20 * 0.5) = 30/turn for 2 turns, stacks = 2
5. **UI**: Shows "-30 DoTx2 (2t)"

#### TC-DOT-04: Triple Stack (Max)
1. Apply DoT 3 times consecutively
2. Stack 1: 20/turn
3. Stack 2: 20 + 10 = 30/turn
4. Stack 3: 20 + 15 = 35/turn
5. **Expected**: At stack 3, no further damage increase on 4th application

#### TC-DOT-05: Max Stack Cap Behavior
1. Apply DoT 4 times
2. **Expected**: 4th application only refreshes duration, damage stays at stack 3 value

#### TC-DOT-06: Reapply Before First Tick
1. Turn 1: Apply DoT (20/turn, 2 turns)
2. Turn 1: Apply DoT again before turn ends
3. **Expected**: 20 + 10 = 30/turn for 2 turns (stacked immediately)

#### TC-DOT-07: DoT Expires Correctly
1. Apply DoT (20/turn, 2 turns)
2. Wait 2 enemy turns
3. **Expected**: DoT removed after 2nd tick

#### TC-DOT-08: Different DoT Sources Stack Separately
1. Apply "Bolt" DoT (20/turn)
2. Apply "Poison" DoT (15/turn) - hypothetical future source
3. **Expected**: Both DoTs exist separately, total damage = 35/turn

---

## Stun Mechanics

### Design Rules
- **Effect**: Stunned unit skips their entire turn (no attack, no skills)
- **Check Timing**: START of unit's turn, BEFORE DoT ticks
- **Duration Decrement**: Happens when stun is consumed (turn is skipped)
- **Stacking**: Add durations (capped at 2 turns max)
- **DoT Interaction**: DoT still ticks on stunned enemies

### Test Cases

#### TC-STUN-01: Basic Stun Application
1. CheapShot hits enemy, applies 1-turn stun
2. **Expected**: Enemy health bar shows "STUNNED (1t)"

#### TC-STUN-02: Stunned Enemy Skips Turn
1. Apply 1-turn stun to enemy
2. End player turn
3. **Expected**: Enemy does NOT attack, player takes 0 damage from that enemy

#### TC-STUN-03: Stun Duration Decrement
1. Apply 1-turn stun
2. Enemy turn starts
3. **Expected**: Stun consumed, enemy skips turn, stun expires

#### TC-STUN-04: Multi-Turn Stun
1. Apply 2-turn stun (e.g., via stacking or Meteor on boss)
2. Turn 1: Enemy skips, stun becomes 1 turn
3. Turn 2: Enemy skips, stun expires
4. Turn 3: Enemy attacks normally

#### TC-STUN-05: Stun Stacking (Add Durations)
1. Apply 1-turn stun
2. Before enemy turn, apply another 1-turn stun
3. **Expected**: Total stun = 2 turns (capped)

#### TC-STUN-06: Stun Cap at 2 Turns
1. Apply stun 3 times (1 turn each)
2. **Expected**: Total stun duration capped at 2 turns

#### TC-STUN-07: DoT Ticks While Stunned
1. Apply DoT (20/turn, 2 turns) to enemy
2. Apply stun (1 turn) to same enemy
3. Enemy turn starts
4. **Expected**: 
   - Enemy is stunned (skips attack)
   - DoT still ticks (enemy takes 20 damage)
   - Both effects update correctly

#### TC-STUN-08: Multiple Enemies, Mixed Stun State
1. 3 enemies on field
2. Stun enemy A (1 turn), leave B and C unstunned
3. Enemy turn
4. **Expected**: 
   - Enemy A skips turn
   - Enemy B and C attack normally

#### TC-STUN-09: Meteor AoE Stun
1. Use Meteor (stuns all enemies 1 turn)
2. Enemy turn
3. **Expected**: All enemies skip their turn

---

## Edge Cases

#### TC-EDGE-01: Enemy Dies from DoT While Stunned
1. Enemy at 15 HP
2. Apply DoT (20/turn) and stun (1 turn)
3. Enemy turn starts
4. **Expected**: 
   - Stun checked first (would skip turn)
   - DoT ticks (20 damage)
   - Enemy dies from DoT
   - No attack occurs

#### TC-EDGE-02: All Enemies Die from DoT
1. All enemies low HP
2. All have DoT applied
3. Enemy turn starts
4. **Expected**: Combat ends in victory after DoT kills all

#### TC-EDGE-03: Stun Applied Mid-Combat
1. Player uses CheapShot mid-combat
2. **Expected**: Stun takes effect NEXT enemy turn (not current)

---

## Console Log Verification
When testing, check Unity console for these log patterns:

### DoT Logs
```
[StatusEffect] Added DoT (Value: 20, Duration: 2, Source: Bolt)
[StatusEffect] DoT STACKED! Base: 20, Carryover: 10.0, New total: 30.0/turn, Stacks: 2/3
[StatusEffect] DoT tick: 30 damage from Bolt (Stacks: 2, Duration left: 1)
[StatusEffect] DoT from Bolt expired
```

### Stun Logs
```
[CombatEnemy] {Name} is stunned for 1 turn(s)!
[StatusEffect] Unit is STUNNED! Turns remaining before decrement: 1
[CombatEnemy] {Name} is STUNNED and skips their turn!
[CombatManager] {Name} turn skipped due to STUN!
[StatusEffect] Stun expired after this turn skip
```

---

## Manual Test Checklist
- [ ] TC-DOT-01: Basic DoT Application
- [ ] TC-DOT-02: DoT Tick After 1 Turn
- [ ] TC-DOT-03: DoT Stacking (Reapply While Active)
- [ ] TC-DOT-04: Triple Stack (Max)
- [ ] TC-DOT-05: Max Stack Cap Behavior
- [ ] TC-DOT-06: Reapply Before First Tick
- [ ] TC-DOT-07: DoT Expires Correctly
- [ ] TC-STUN-01: Basic Stun Application
- [ ] TC-STUN-02: Stunned Enemy Skips Turn
- [ ] TC-STUN-03: Stun Duration Decrement
- [ ] TC-STUN-04: Multi-Turn Stun
- [ ] TC-STUN-05: Stun Stacking
- [ ] TC-STUN-06: Stun Cap at 2 Turns
- [ ] TC-STUN-07: DoT Ticks While Stunned
- [ ] TC-STUN-08: Multiple Enemies, Mixed Stun State
- [ ] TC-STUN-09: Meteor AoE Stun
- [ ] TC-EDGE-01: Enemy Dies from DoT While Stunned
- [ ] TC-EDGE-02: All Enemies Die from DoT
