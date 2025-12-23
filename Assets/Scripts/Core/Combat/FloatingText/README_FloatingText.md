# Floating Combat Text (FCT) System

## Setup Instructions

### 1. Create FloatingTextManager GameObject
1. In Unity, create an empty GameObject in your scene
2. Name it `FloatingTextManager`
3. Add the `FloatingTextManager` component to it
4. The manager will auto-create a Canvas if one isn't assigned

### 2. Create FloatingTextConfig Asset
1. Right-click in Project window → **Create → Combat → Floating Text Config**
2. Name it `FloatingTextConfig`
3. Assign it to the FloatingTextManager's `Config` field
4. Customize colors, sizes, durations per text type

### 3. (Optional) Custom Prefab
If you want custom styling beyond TextMeshPro:
1. Create a UI prefab with:
   - `RectTransform`
   - `CanvasGroup`
   - `TextMeshProUGUI` (child or same object)
   - `FloatingText` component
2. Assign prefab to FloatingTextManager's `Floating Text Prefab` field

---

## API Reference

### Damage Events
```csharp
FloatingTextManager.Instance.ShowDamage(targetTransform, amount, isCrit);
FloatingTextManager.Instance.ShowDamageTaken(targetTransform, amount);
FloatingTextManager.Instance.ShowDoTTick(targetTransform, amount, "Burn");
```

### Healing & Shields
```csharp
FloatingTextManager.Instance.ShowHeal(targetTransform, amount);
FloatingTextManager.Instance.ShowShield(targetTransform, amount, FloatingTextType.ShieldGain);
FloatingTextManager.Instance.ShowShield(targetTransform, amount, FloatingTextType.ShieldAbsorb);
FloatingTextManager.Instance.ShowShield(targetTransform, 0, FloatingTextType.ShieldBroken);
```

### Status Effects
```csharp
FloatingTextManager.Instance.ShowStatus(targetTransform, "Stunned", gained: true);
FloatingTextManager.Instance.ShowStatus(targetTransform, "Burn", gained: false); // Ended
FloatingTextManager.Instance.ShowStatus(targetTransform, "DoT", gained: true, stacksDelta: 1);
FloatingTextManager.Instance.ShowImmune(targetTransform, isImmune: true);
```

### Elemental Reactions
```csharp
FloatingTextManager.Instance.ShowOrbActivated(targetTransform, "Fire");
FloatingTextManager.Instance.ShowReaction(targetTransform, "Melt", multiplier: 1.75f, bonusDamage: 50);
```

### Turn Feedback
```csharp
FloatingTextManager.Instance.ShowTurnSkipped(targetTransform, "Stunned!");
FloatingTextManager.Instance.ShowEnergyChange(targetTransform, amount); // Disabled by default
```

### Generic
```csharp
FloatingTextManager.Instance.ShowText(targetTransform, "Custom Text", FloatingTextType.Generic);
FloatingTextManager.Instance.ShowTextAtPosition(worldPosition, "Text", FloatingTextType.Generic);
```

---

## FloatingTextType Enum

| Type | Description | Default Color |
|------|-------------|---------------|
| `DamageDealt` | Damage dealt to enemy | Red |
| `DamageTaken` | Damage taken by player | Dark Red |
| `CriticalHit` | Critical hit indicator | Gold |
| `DoTTick` | DoT damage tick | Orange |
| `Heal` | Healing received | Green |
| `ShieldGain` | Shield gained | Light Blue |
| `ShieldAbsorb` | Shield absorbed damage | Cyan |
| `ShieldBroken` | Shield broke | Blue |
| `StatusGain` | Status effect applied | Orange |
| `StatusLost` | Status effect ended | Gray |
| `StatusStack` | Stack count changed | Light Orange |
| `Immune` | Immunity feedback | Gray |
| `Resisted` | Status resisted | Dark Gray |
| `OrbActivated` | Elemental orb activated | Yellow |
| `ReactionTriggered` | Reaction triggered | Magenta |
| `ReactionDamage` | Bonus reaction damage | Pink |
| `TurnSkipped` | Turn skipped (stun) | Yellow |
| `EnergyChange` | Energy gained/spent | Light Blue |
| `Generic` | Generic text | White |

---

## Configuration Options (FloatingTextConfig)

### Global Settings
- **globalDelay**: Delay between queued texts (default: 0.15s)
- **verticalOffset**: Base offset above target (default: 1.5)
- **stackOffset**: Vertical offset per stacked text (default: 0.3)
- **maxConcurrentTexts**: Max texts on screen (default: 8)
- **combatPauseAfterPlayerAction**: Pause after player attacks (default: 0.5s)
- **combatPauseAfterEnemyAction**: Pause after enemy attacks (default: 0.4s)

### Per-Type Style Settings
- **color**: Text color
- **outlineColor**: Text outline color
- **fontSize**: Font size
- **duration**: How long text displays
- **floatSpeed**: Upward movement speed
- **fadeStartTime**: When fade begins (0-1)
- **shake**: Enable shake effect
- **shakeIntensity**: Shake amount
- **scalePunch**: Enable pop-in effect
- **scaleMultiplier**: Initial scale for punch
- **prefix**: Text prefix (e.g., "+")
- **suffix**: Text suffix (e.g., " Shield")
- **enabled**: Toggle on/off for experimentation

---

## Combat Flow Example

```
Player attacks enemy:
1. ShowDamage(enemyTransform, 100, isCrit: false)
   → "100" floats up from enemy

[0.5s pause for readability]

Enemy attacks player:
1. QTE happens
2. ShowShieldToPlayer(20, ShieldAbsorb) if shield absorbed
3. ShowDamageToPlayer(30) for actual HP damage
4. ShowHealToPlayer(15) from QTE healing

[0.4s pause]

Next enemy attacks...
```

---

## Toggling Text Types

Each text type can be enabled/disabled in the FloatingTextConfig for experimentation:

```csharp
// In FloatingTextConfig inspector, set 'enabled = false' for:
// - EnergyChange (disabled by default - too noisy)
// - Any type you want to hide temporarily
```
