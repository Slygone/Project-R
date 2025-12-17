# Game Prototype Setup Instructions

## Scene Setup

### 1. Create Basic Scene Elements

#### Terrain
1. Create a **Plane** GameObject (GameObject > 3D Object > Plane)
2. Name it "Terrain"
3. Set Scale to (10, 1, 10) for a larger play area
4. Position at (0, 0, 0)

#### Player
1. Create a **Cube** GameObject (GameObject > 3D Object > Cube)
2. Name it "Player"
3. Add tag "Player" (Tag dropdown > Add Tag > Create "Player" tag)
4. Position at (0, 1, 0)
5. Add the **PlayerController** script component
6. Add a **Rigidbody** component (will be auto-configured by script)
   - Constraints: Freeze Rotation X, Y, Z and Freeze Position Y

#### Camera Setup
1. Select Main Camera
2. Set Position to (0, 20, -15)
3. Set Rotation to (45, 0, 0) for isometric view
4. Set Projection to Orthographic
5. Set Orthographic Size to 15

### 2. Create UI Canvas

#### Canvas Setup
1. Create a **Canvas** (GameObject > UI > Canvas)
2. Set Canvas Scaler component:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080

#### Node Counter (Top Left)
1. Right-click Canvas > UI > Text - TextMeshPro
2. Name it "NodeCounter"
3. Set Anchor Preset: Top-Left
4. Position: X=150, Y=-30
5. Set text: "Nodes Completed: 0/10"
6. Font Size: 24
7. Color: White

#### Node Popup Panel
1. Right-click Canvas > UI > Panel
2. Name it "NodePopup"
3. Set Anchor Preset: Center
4. Width: 500, Height: 300
5. Add child elements:
   - **Title** (Text - TextMeshPro):
     - Position: Y=80
     - Font Size: 32
     - Alignment: Center
     - Text: "Node Title"
   - **ContinueButton** (Button - TextMeshPro):
     - Position: Y=-80
     - Width: 200, Height: 60
     - Button Text: "Continue"

#### Victory Panel
1. Right-click Canvas > UI > Panel
2. Name it "VictoryPanel"
3. Set Anchor Preset: Center
4. Width: 600, Height: 400
5. Add child element:
   - **Text - TextMeshPro**:
     - Position: Y=0
     - Font Size: 48
     - Alignment: Center
     - Text: "You Finished This Level!"
     - Color: Gold/Yellow

### 3. Add Core Scripts to Scene

#### Referencer
1. Create an empty GameObject
2. Name it "Referencer"
3. Add the **Referencer** script component

#### UIManager
1. Create an empty GameObject
2. Name it "UIManager"
3. Add the **UIManager** script component

#### NodeSpawner
1. Create an empty GameObject
2. Name it "NodeSpawner"
3. Add the **NodeSpawner** script component

### 4. Node Configuration

The NodeSpawner will automatically create:
- 2 Shop nodes (Yellow spheres)
- 3 Combat nodes (Red spheres)
- 2 Rest nodes (Green spheres)
- 3 Mystery nodes (Cyan spheres)

All nodes will spawn randomly within a 20-unit radius and automatically turn gray when completed.

## How It Works

### Player Movement
- Use **WASD** or **Arrow Keys** to move the player cube around the terrain
- Movement is disabled when a popup is active

### Node Interaction
- Walk into any colored sphere to trigger its popup
- Each node type shows a different title:
  - **Shop**: "Shop - Buy Items Here!"
  - **Combat**: "Combat - Fight an Enemy!"
  - **Rest**: "Rest - Heal or Gain +10 Damage"
  - **Mystery**: "Mystery - Unknown Event"
- Click **Continue** button to close popup and mark node as completed
- Completed nodes turn gray and cannot be triggered again

### Win Condition
- Complete all 10 nodes
- Victory panel appears when counter reaches 10/10

## Testing the Game

1. Press **Play** in Unity
2. Use WASD or Arrow Keys to move around
3. Collide with spheres to trigger events
4. Click Continue on each popup
5. Complete all 10 nodes to see the victory screen

## Node Types Breakdown

- **Shop (2 minimum)**: Yellow - Future: Buy items
- **Combat (3 minimum)**: Red - Future: Fight enemies
- **Rest (2 minimum)**: Green - Future: Heal or gain damage boost
- **Mystery (3 minimum)**: Cyan - Future: Random events

Total: 10 nodes spawned per level
