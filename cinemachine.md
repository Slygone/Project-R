# Cinemachine Setup

This project uses **Cinemachine 3.1.5** (`com.unity.cinemachine`) with two virtual cameras managed by `CinemachineCameraManager.cs`.

---

## Architecture

| Component | Role |
|-----------|------|
| **Main Camera** | Has `CinemachineBrain` — automatically blends between virtual cameras |
| **FreeroamCamera** | Follows the player during exploration with a fixed offset |
| **CombatCamera** | Snaps to the combat arena center when a fight starts |
| **CinemachineCameraManager** | Script that switches priority between the two cameras |

The `CinemachineBrain` on the Main Camera reads the highest-priority `CinemachineCamera` and blends to it. Our manager script flips priorities to switch between freeroam and combat.

---

## Old Camera Script

`CameraFollow.cs` is **deprecated** and fully replaced. After completing setup:
1. Select the **Main Camera** in the Hierarchy.
2. Remove the `CameraFollow` component if present (right-click → Remove Component).
3. Delete `Assets/Scripts/Core/CameraFollow.cs` and its `.meta` file from the Project window.

No other scripts reference `CameraFollow` anymore.

---

## Step-by-Step Setup

### Step 1 — Main Camera (CinemachineBrain)

1. In the **Hierarchy**, select your **Main Camera** (the one tagged `MainCamera`).
2. In the **Inspector**, click **Add Component** → search for **Cinemachine Brain** → add it.
3. Configure the Brain:
   - **Default Blend**: `EaseInOut`, **Time**: `1.0` (smooth 1-second transition between cameras).
   - **Update Method**: `Smart Update` (default is fine).
   - **Default Mode Override**: `None`.
4. If a `CameraFollow` component is still attached, **remove it** (right-click → Remove Component).
5. **Do NOT** change the Main Camera's Transform — Cinemachine will control it.

### Step 2 — Create the FreeroamCamera

1. In the **Hierarchy**, right-click → **Create Empty** → name it **`FreeroamCamera`**.
2. Set its **Transform**:
   - **Position**: `(0, 12, -10)` — this is a starting hint; Cinemachine will override it at runtime.
   - **Rotation**: `(50, 0, 0)` — angled down to match your isometric/top-down view. Adjust to taste.
3. Click **Add Component** → search for **Cinemachine Camera** → add it.
   - **Priority**: `20` (will be the default active camera).
   - **Follow**: leave empty — the script assigns this at runtime.
   - **Look At**: leave empty — we use a fixed rotation, no aim tracking.
4. Click **Add Component** → search for **Cinemachine Follow** → add it.
   - **Tracker Settings > Binding Mode**: `World Space`.
   - **Tracker Settings > Position Damping**: `(0.5, 0.5, 0.5)` — smooth follow. Lower = snappier, higher = floatier.
   - **Follow Offset**: `(0, 12, -10)` — matches the old `CameraFollow` offset exactly.
5. **Do NOT** add any Aim component (like `CinemachineRotationComposer`). The camera keeps the fixed rotation you set in step 2.

### Step 3 — Create the CombatCamera

1. In the **Hierarchy**, right-click → **Create Empty** → name it **`CombatCamera`**.
2. Set its **Transform**:
   - **Position**: `(0, 14, -18)` — starting hint, overridden at runtime.
   - **Rotation**: `(50, 0, 0)` — same angle as freeroam, or tweak for a wider combat view.
3. Click **Add Component** → search for **Cinemachine Camera** → add it.
   - **Priority**: `10` (inactive by default — lower than FreeroamCamera).
   - **Follow**: leave empty — the script assigns this at runtime.
   - **Look At**: leave empty.
4. Click **Add Component** → search for **Cinemachine Follow** → add it.
   - **Tracker Settings > Binding Mode**: `World Space`.
   - **Tracker Settings > Position Damping**: `(0.3, 0.3, 0.3)` — slightly faster snap for combat.
   - **Follow Offset**: `(0, 14, -18)` — pulled back further to frame both player and enemies. Adjust to taste.
5. **Do NOT** add any Aim component.

### Step 4 — Create the CinemachineCameraManager

1. In the **Hierarchy**, right-click → **Create Empty** → name it **`CinemachineCameraManager`**.
2. Click **Add Component** → search for **Cinemachine Camera Manager** → add it.
3. In the Inspector, assign:
   - **Freeroam Camera**: drag the **FreeroamCamera** GameObject from the Hierarchy.
   - **Combat Camera**: drag the **CombatCamera** GameObject from the Hierarchy.
   - **Active Priority**: `20` (default).
   - **Inactive Priority**: `10` (default).

### Step 5 — Clean Up

1. Select the **Main Camera** → confirm `CinemachineBrain` is present and `CameraFollow` is **removed**.
2. Check the **Player** GameObject — if it has a `CameraFollow` component, **remove it**.
3. Delete `Assets/Scripts/Core/CameraFollow.cs` and its `.meta` file from the **Project** window.
4. Enter **Play Mode** and verify:
   - Camera follows the player smoothly in free roam.
   - Camera transitions to the combat arena when a fight starts.
   - Camera transitions back to the player after combat ends.

---

## Hierarchy Summary

After setup, your Hierarchy should include:

```
Main Camera              (Camera, CinemachineBrain)
FreeroamCamera           (CinemachineCamera, CinemachineFollow)
CombatCamera             (CinemachineCamera, CinemachineFollow)
CinemachineCameraManager (CinemachineCameraManager script)
```

---

## Tweaking Guide

| What to adjust | Where |
|----------------|-------|
| **Camera angle** | FreeroamCamera / CombatCamera → Transform → Rotation X |
| **Camera distance** | CinemachineFollow → Follow Offset (Y = height, Z = distance) |
| **Follow smoothness** | CinemachineFollow → Tracker Settings → Position Damping |
| **Blend speed** | Main Camera → CinemachineBrain → Default Blend → Time |
| **Blend curve** | Main Camera → CinemachineBrain → Default Blend → Style |
| **Combat framing** | CombatCamera → CinemachineFollow → Follow Offset (increase -Z to pull back) |

---

## How It Works (Code)

- **`CinemachineCameraManager.cs`** — sits on the CinemachineCameraManager GameObject. Holds serialized references to both cameras. Exposes `SetFreeroamTarget()`, `EnterCombat()`, and `ExitCombat()`.
- **`Referencer.cs`** — on Awake, finds the manager and calls `SetFreeroamTarget(playerTransform)`.
- **`CombatArena.cs`** — on combat enter, calls `cameraManager.EnterCombat(combatCenter)`. On combat exit, calls `cameraManager.ExitCombat()`.