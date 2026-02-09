# Refactor Plan - Unused Code Analysis

This document lists code that is **not currently used** in the project and can be safely removed or refactored.

---

## Summary

After analyzing all 63 C# files in the project, the following unused/deprecated code was identified:

| Category | Items Found |
|----------|-------------|
| Deprecated Methods | 4 |
| Legacy UI Classes | 2 |
| Unused Methods | 3 |
| Unused Public Getters | 1 |

---

## Deprecated Methods

### 1. `TriggerReactionAction` in CombatManager.cs
**Location:** `Assets/Scripts/Core/Combat/CombatManager.cs` (Lines 309-315)

**Purpose:** Old orb-based reaction system trigger. Was used to trigger reactions based on elemental orb mechanics.

**Status:** Marked `DEPRECATED` in code comments. Now replaced by the new mark-based reaction system (`TriggerMarkReaction`).

```csharp
// DEPRECATED: Old orb-based reaction system - now using new mark system
private void TriggerReactionAction(CombatEnemy target, int skillNumber, bool isAttack)
```

**Recommendation:** Delete this method entirely.

---

### 2. `StartAffinitySelection` in GameManager.cs
**Location:** `Assets/Scripts/Core/GameManager.cs` (Lines 184-188)

**Purpose:** Started the elemental orb pair selection screen at the beginning of a run.

**Status:** Marked `DEPRECATED`. The new elemental mark system doesn't use orb pairs - skills are enchanted with sigils dropped from enemies instead.

```csharp
private void StartAffinitySelection()
{
    // DEPRECATED: Old orb pair selection - now skipped
    SkipAffinitySelection();
}
```

**Recommendation:** Delete and replace any calls with `SkipAffinitySelection()` directly.

---

### 3. `OnAffinityChosen` in GameManager.cs
**Location:** `Assets/Scripts/Core/GameManager.cs` (Lines 218-238)

**Purpose:** Callback handler when player selected an elemental affinity from the old selection UI.

**Status:** Never called - `StartAffinitySelection` now skips directly to `SkipAffinitySelection()`.

```csharp
private void OnAffinityChosen(Element element)
```

**Recommendation:** Delete this method.

---

### 4. `HasAffinityBeenChosen` in GameManager.cs
**Location:** `Assets/Scripts/Core/GameManager.cs` (Line 240)

**Purpose:** Getter to check if player has chosen an affinity.

**Status:** Defined but never queried anywhere in the codebase.

```csharp
public bool HasAffinityBeenChosen() => affinityChosen;
```

**Recommendation:** Delete this getter.

---

## Legacy UI Classes (Entire Files)

### 1. `AffinitySelectionUI.cs`
**Location:** `Assets/Scripts/Core/AffinitySelectionUI.cs` (222 lines)

**Purpose:** Full-screen UI for selecting elemental orb pair at run start. Created selection panel with 3 random element buttons.

**Status:** Legacy - referenced in `Referencer.cs` but `Show()` method is never called. The new system uses sigil drops instead of pre-selected orb pairs.

**Key Methods:**
- `Show(Action<Element> callback)` - Display element selection
- `GetRandomElements(int count)` - Pick random elements to choose from
- `CreateAffinityButton()` - Create clickable element buttons
- `GetElementDescription()` - Return flavor text for each element

**Recommendation:** Delete entire file and remove reference from `Referencer.cs`.

---

### 2. `CharacterSelectionUI.cs`
**Location:** `Assets/Scripts/Core/CharacterSelectionUI.cs` (246 lines)

**Purpose:** Old character selection UI shown at run start. Displayed 3 random characters.

**Status:** Legacy - replaced by `NewRunCharacterSelectUI.cs` and `CharacterLoadoutUI.cs`. Only called from `StartCharacterSelection()` which is itself a fallback for when `NewRunCharacterSelectUI` is not found.

**Key Methods:**
- `Show(Action<CharacterData> callback)` - Display character selection
- `GetRandomCharacters(int count)` - Pick random characters to choose from  
- `CreateCharacterButton()` - Create clickable character cards

**Recommendation:** Delete entire file if new UI flow is always available. Otherwise keep as fallback.

---

## Unused Methods (Legacy Flow)

### 1. `StartCharacterSelection` in GameManager.cs
**Location:** `Assets/Scripts/Core/GameManager.cs` (Lines 190-202)

**Purpose:** Fallback to show old `CharacterSelectionUI` when new UI not found.

**Status:** Only called when `NewRunCharacterSelectUI` is null (fallback).

```csharp
private void StartCharacterSelection()
```

**Recommendation:** Keep as fallback OR delete if new UI is guaranteed.

---

### 2. `OnCharacterChosen` in GameManager.cs
**Location:** `Assets/Scripts/Core/GameManager.cs` (Lines 204-216)

**Purpose:** Callback for legacy `CharacterSelectionUI`.

**Status:** Only passed to legacy UI - not used by new flow.

```csharp
private void OnCharacterChosen(CharacterData character)
```

**Recommendation:** Keep with `StartCharacterSelection` or delete both.

---

### 3. `ShowVictory` in GameManager.cs
**Location:** `Assets/Scripts/Core/GameManager.cs` (Lines 348-359)

**Purpose:** Simple victory panel display (sets panel active).

**Status:** Called only as error fallback when `CombatManager` or `Player` not found during boss fight. Replaced by `ShowRunComplete()` for normal flow.

```csharp
private void ShowVictory()
```

**Recommendation:** Keep for error handling or merge into `ShowRunComplete()`.

---

## Additional Findings (Discovered During Refactor)

### 5. Empty GameObject Spawning in `Referencer.cs`
**Location:** `Assets/Scripts/Core/Referencer.cs` (Lines 71-83)

**Problem:** `Referencer.Awake()` creates empty `AffinitySelectionUI` and `CharacterSelectionUI` GameObjects when they aren't found in the scene. Since these classes are never used, this spawns useless empty UI components every run.

**Recommendation:** Remove both the fields and the spawning blocks entirely.

---

### 6. Fallback Chain Dependency in `GameManager.cs`
**Location:** `Assets/Scripts/Core/GameManager.cs` (Lines 114-124)

**Problem:** `StartNewRunCharacterSelect()` falls back to `StartCharacterSelection()` (line 123) which itself calls `StartAffinitySelection()` (line 200). Deleting the legacy methods breaks this chain. The fallback should log an error instead since the new UI is always available.

**Recommendation:** Replace `StartCharacterSelection()` fallback call with an error log.

---

### 7. `affinityChosen` Field in `GameManager.cs`
**Location:** `Assets/Scripts/Core/GameManager.cs` (Line 9)

**Problem:** The `affinityChosen` field is set in multiple places but only ever read by `HasAffinityBeenChosen()` which is itself unused. With the old affinity system removed, this field serves no purpose.

**Recommendation:** Remove the field. Keep it only if future systems need it.

---

### 8. Duplicated `GetElementColor` Across 4 Files
**Files:**
- `CombatUI.cs` (private method)
- `PlayerStatsUI.cs` (private method)
- `CombatArena.cs` (private method)
- `ElementAscensionDetailUI.cs` (private method)

**Problem:** Each file has its own private `GetElementColor` with slightly different signatures but same logic. This is duplicated code.

**Recommendation:** Future refactor — consolidate into a shared utility. Not blocking for this pass.

---

## Action Items

1. **High Priority (Safe to Delete):**
   - [x] `TriggerReactionAction()` in CombatManager.cs
   - [x] `StartAffinitySelection()` in GameManager.cs
   - [x] `OnAffinityChosen()` in GameManager.cs
   - [x] `HasAffinityBeenChosen()` in GameManager.cs
   - [x] `affinityChosen` field in GameManager.cs
   - [x] `AffinitySelectionUI.cs` (entire file + .meta)
   - [x] Remove `affinitySelectionUI` field + spawning from `Referencer.cs`
   - [x] `CharacterSelectionUI.cs` (entire file + .meta) — new UI is always available
   - [x] `StartCharacterSelection()` in GameManager.cs
   - [x] `OnCharacterChosen()` in GameManager.cs
   - [x] Remove `characterSelectionUI` field + spawning from `Referencer.cs`
   - [x] Fix `StartNewRunCharacterSelect()` fallback to not call deleted method

2. **Medium Priority (Now Completed):**
   - [x] `ShowVictory()` - Removed entirely (was error fallback, now logs error)
   - [x] Consolidate duplicated `GetElementColor` across 4 files into shared `ElementColors` utility

3. **Phase 2 — Remove Fallbacks & Old Combat UI (Completed):**
   - [x] Remove `ShowVictory()` from GameManager.cs — replaced with error log
   - [x] Strip `CombatUI.cs` down to loot-panel-only (1341 → 493 lines)
   - [x] Remove all `combatUI` combat display calls from CombatManager.cs (~30 call sites)
   - [x] Remove `usingInWorldCombat` fallback logic — CombatArena is the only combat UI
   - [x] Add new floating text helpers: `ShowReactionToEnemy`, `ShowStatusToEnemy`, `ShowTurnSkippedToEnemy`, `ShowDoTTickToEnemy`
   - [x] Remove `combatUI` field + reference from PotionUI.cs
   - [x] Create shared `ElementColors.cs` utility (Element→Color, string→Color, Element→hex)
   - [x] Update `PlayerStatsUI.cs`, `CombatArena.cs`, `ElementAscensionDetailUI.cs` to use `ElementColors`
   - [x] Remove floating text CombatUI fallbacks from CombatManager.cs helpers
   - [x] Unguard defensive QTE floating text calls (were behind `if (combatUI != null)`)

---

## Notes

- The `affinity` field and getter methods (`GetAffinity()`, `HasAffinity()`, `GetAffinityBonus()`) in `Player.cs` are **still used** throughout the combat system and should NOT be deleted.
- The deprecated affinity *selection* system can be removed, but the affinity *mechanics* are still active.
- The new run flow uses: `MainMenuUI` → `NewRunCharacterSelectUI` → `CharacterLoadoutUI` → `SkipAffinitySelection()`
- `CombatUI.cs` now only handles loot panel (gold, sigil enchantment, relic collection). All combat display is handled by `CombatArena` (in-world combat).
- `EnemyWorldUnit.Update()` auto-detects health changes on `CombatEnemy` — no explicit UI update calls needed.
- `ElementColors.cs` is the single source of truth for element colors across all UI systems.
