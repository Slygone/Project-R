# Project R - 2D Turn-Based RPG

A 2D top-down turn-based RPG built in Unity, inspired by classic titles like Final Fantasy and Pokémon.

## Features

### Combat System
- **Turn-based battles** with classic FF/Pokémon-style mechanics
- **Health and damage system** with real-time HP tracking
- **Visual health bars** displayed above player and enemies during combat
- Enemy spawns only during battle encounters

### Player Mechanics
- **2D top-down movement** with WASD/Arrow key controls
- **Persistent health** - HP carries over between battles
- **Strategic upgrades** - Choose between healing or boosting attack power

### Interaction System
- **Trigger-based encounters** - Touch markers (X) to interact
- **Choice menu** with multiple options:
  - **Fight** - Enter turn-based combat
  - **Rest** - Heal +30 HP or boost attack +10 damage
  - **Shop** - (Coming soon)

### Visual Style
- **Pixel art aesthetic** (16 PPU)
- **World space UI** - Health text follows characters
- **2D sprites** - Player (square), Enemy (triangle)

## Getting Started

### Requirements
- Unity 2023.x or later
- 2D Sprite package
- New Input System (set to "Both" mode)

### Setup
1. Clone the repository
2. Open in Unity
3. Make sure Project Settings → Editor → Default Behavior Mode is set to **2D**
4. Open `Assets/Scenes/MainMap` scene
5. Press Play!

## How to Play

1. **Move** around using WASD or Arrow Keys
2. Walk into the **yellow markers** to trigger interaction menus
3. Choose **Fight** to battle enemies
4. Click **Attack** during your turn
5. **Defeat enemies** by reducing their HP to 0
6. Use **Rest** to heal or permanently boost your attack
7. Your HP persists between battles - play strategically!

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # GameManager, InteractionTrigger
│   ├── Characters/     # PlayerController
│   └── Combat/         # BattleSystem, Fighter classes, Stats
├── Scenes/
│   └── MainMap         # Main game scene
└── ...
```

## Technical Highlights

- **State management** using GameManager with GameState enum
- **Component-based architecture** with Fighter base class
- **Separation of concerns** - Movement, Combat, and Stats are separate systems
- **World Space Canvas** for UI that follows game objects

## Future Plans

- [ ] Multiple enemy types with varying stats
- [ ] Shop system for buying items and upgrades
- [ ] Multiple maps/areas to explore
- [ ] Save/Load functionality
- [ ] Special attacks and abilities
- [ ] Inventory system

## Credits

Built with Unity and inspired by classic JRPGs.

---

**Play the game. Make strategic choices. Survive.**
