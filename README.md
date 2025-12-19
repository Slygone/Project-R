# Project R — Turn-Based Isometric Roguelike

Project R is a turn-based, isometric roguelike RPG built around a node-based world map. Each run is different, with encounters, upgrades, relics, and unlocks that shape your build as you progress through worlds and boss fights.

## Core Loop
- Travel between map nodes.
- Node types:
  - **Fight**: battle enemies
  - **Rest**: heal or upgrade
  - **Shop**: buy run-specific upgrades
- After fights you choose rewards (e.g., upgrades, unlocks, relics/trinkets).
- After **10+ encounters**, a **World Boss** appears.
- Beat the boss to advance to the next world; lose and the run ends.

## Progression
- **Run progression**: temporary upgrades and relics that last for the current run.
- **Meta progression**: resources earned from enemies/encounters that unlock characters and long-term progression.
- Party growth: unlock and invite more characters to accompany you over time.

## Combat
- Turn-based combat with **skill selection + target selection**.
- After selecting a skill, the player chooses a target:
  - A highlight **arrow** appears above the selected enemy.
  - For AoE skills: a **main arrow** marks the primary target and smaller/different arrows mark secondary targets.

### QTE Execution - WIP
Attacks use a quick execution check to scale damage:
- **Bad** → `Damage * 0.8`
- **Good** → `Damage * 1.0`
- **Perfect** → `Damage * 1.2`

### Elemental Mark & Reaction System - WIP
Elements: **Fire, Ice, Water, Wind, Rock**.

- Elemental attacks apply **marks (stacks)** to enemies.
- Enemies can have only **one mark type** at a time, with multiple stacks.
- Applying the **same element** adds stacks.
- Applying a **different element** triggers a **reaction** using **ReactedStacks**:
  - Paired stacks are consumed, and any leftover stacks remain as the active mark type.



Please note that the game is still in prototyping and R&D so some elements of the game might change during development.
