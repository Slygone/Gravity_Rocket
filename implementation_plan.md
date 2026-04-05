# Gravity Rocket - Unity Implementation Plan

## Overview
Port of the HTML5 "Gravity Rocket: Monochrome" game to Unity 6000.3.10f1 (2D).
Core mechanics only: 1 level on repeat, win/lose conditions, drag-to-aim, gravity physics, star scoring.

## Coordinate System
- Original HTML canvas: 400x800 pixels, Y-axis pointing down
- Unity: World units matching pixel scale (400x800), Y-axis pointing up (flipped)
- Conversion: `unity_y = 800 - html_y`
- Camera: Orthographic, position (200, 400, -10), size = 400

## Level 1 Data (Tutorial - Centered)
- **Rocket Start**: (200, 60) — bottom center
- **Docking Gate**: x=120, y=730, width=160, height=30 — top center
- **Planet 1**: (80, 400), radius=30, mass=100
- **Planet 2**: (320, 400), radius=30, mass=100

## Core Constants (from original)
- G (gravity constant) = 15
- LAUNCH_SPEED = 9 (units/frame at 60fps)
- ROCKET_RADIUS = 8
- GOAL_WIDTH = 160 (400/2.5)
- GOAL_HEIGHT = 30

## Game States
1. **MENU** — Title screen, "Initiate" button
2. **AIMING** — Player drags to set trajectory angle
3. **FLYING** — Rocket in flight, gravity applied each frame
4. **GAMEOVER** — Hit planet or out-of-bounds
5. **LEVEL_COMPLETE** — Docked successfully, show stars

## Physics (Custom — NOT Unity physics)
Per-frame Euler integration (normalized to 60fps):
```
dt = Time.deltaTime * 60
for each planet:
    dx = planet.x - rocket.x
    dy = planet.y - rocket.y
    distSq = dx*dx + dy*dy
    dist = sqrt(distSq)
    if dist < planet.radius + ROCKET_RADIUS: CRASH
    force = (G * planet.mass) / distSq
    ax += force * (dx / dist)
    ay += force * (dy / dist)
vx += ax * dt
vy += ay * dt
x += vx * dt
y += vy * dt
```

## Collision Detection
- **Planet**: Circle vs Circle — `dist < planet.radius + ROCKET_RADIUS`
- **Goal**: Circle vs AABB — closest point on rect to rocket center
- **Out of Bounds**: `x < -20 || x > 420 || y < -20 || y > 820`

## Star Scoring
hitRatio = (closestX - goalX) / GOAL_WIDTH mapped to zones [1][2][3][2][1]:
- 0.4 < ratio < 0.6 → 3 stars (center)
- 0.2 < ratio < 0.8 → 2 stars
- else → 1 star

## Scripts to Create

### 1. GameManager.cs
- Singleton managing game state (MENU/AIMING/FLYING/GAMEOVER/LEVEL_COMPLETE)
- Creates and manages all UI (Canvas, panels, buttons, HUD)
- Handles level loading/restarting
- References to RocketController, GravitySource[], DockingGate

### 2. RocketController.cs
- Custom gravity physics in Update()
- Drag-to-aim input (mouse down/move/up)
- Launch on release
- Trail rendering (LineRenderer with chromatic aberration simulated via 3 LineRenderers)
- Particle spawning (exhaust + explosion)
- Rocket visual (triangle mesh)
- Collision checks against planets, goal, and bounds

### 3. GravitySource.cs
- Stores mass, radius
- Draws planet visuals: black circle with white outline, crosshair, gravity wave ring
- Uses LineRenderer or GL calls for rendering

### 4. DockingGate.cs
- Stores position, width, height
- Draws the 5-zone target with bracket corners
- Pulsing center zone animation
- Star scoring calculation

### 5. StarField.cs
- Generates 150 random background star positions
- Renders small white squares at varying alpha

## Editor Setup Steps (ZERO — Fully Automatic)

The GameManager uses `[RuntimeInitializeOnLoadMethod]` to auto-create itself when the scene loads. **No manual editor steps are required.** Just press Play in the existing SampleScene.

### What happens automatically on Play:
1. `GameManager.AutoBootstrap()` fires after scene load, creates the GameManager object
2. Camera is repositioned to (200, 400, -10), set to orthographic size 400, black background
3. Global Light 2D is disabled
4. EventSystem is created for UI interaction
5. StarField background is generated (150 random stars)
6. Rocket GameObject with mesh, trail renderers, aim line
7. Two planets at (80, 400) and (320, 400) with mass=100, radius=30
8. Docking gate at (200, 745) with width=160, height=30
9. Full UI canvas with Menu, GameOver, and LevelComplete panels
10. Menu panel displayed — click INITIATE to play

### Optional (if manually adding GameManager to scene):
1. Create an Empty GameObject, name it "GameManager"
2. Attach GameManager.cs — the auto-bootstrap will skip if one already exists

### Optional: Physics2D Settings
- Go to **Edit → Project Settings → Physics 2D** → Set Gravity to **(0, 0)**
- (Not critical since we use custom gravity, not Unity's built-in physics)

## Visual Style
- **Monochrome**: Black background, white/gray elements only
- **Rocket**: White triangle with flame line when flying
- **Planets**: Black filled circle, white outline, crosshair lines, animated gravity wave ring
- **Docking Gate**: 5 rectangular zones with dashed/solid borders, bracket corners, pulsing center
- **Trail**: 3 colored lines (R/G/B) offset by acceleration for chromatic aberration effect
- **Particles**: Small white squares fading out
- **Stars**: Tiny white dots at random positions

## Flow (1 Level on Repeat — Legacy)
```
MENU → [Start Button] → AIMING → [Drag & Release] → FLYING
  → Hit Planet/OOB → GAMEOVER → [Retry] → AIMING (same level)
  → Hit Gate → LEVEL_COMPLETE (show stars) → [Replay] → AIMING (same level)
```

---

# Feature Implementation — Full Game (FeatureImplementation.md)

## Architecture Overview

### Data-Driven Approach
All game data is defined in static classes, separated from logic:

| File | Purpose |
|------|---------|
| `Assets/Scripts/Data/GameData.cs` | All static data: ships, trails, battle pass tiers, sector mechanics, shop items, level generation algorithm |
| `Assets/Scripts/Data/PlayerState.cs` | Persistent player state: scores, unlocks, equipped items, credits, shields. Uses PlayerPrefs for saving. |
| `Assets/Scripts/UI/UIManager.cs` | All UI screens created programmatically at runtime |
| `Assets/Scripts/GameManager.cs` | Game flow orchestrator: level loading, state machine, session management |
| `Assets/Scripts/RocketController.cs` | Rocket physics, input, rendering — updated for multi-ship gravity, trail colors, nebula, touch input |
| `Assets/Scripts/GravitySource.cs` | Planet rendering (unchanged) |
| `Assets/Scripts/DockingGate.cs` | Docking gate rendering (unchanged) |
| `Assets/Scripts/StarField.cs` | Background stars (unchanged) |

### Folder Structure
```
Assets/
  Scripts/
    Data/
      GameData.cs        — Static data definitions
      PlayerState.cs     — Persistent player progression
    UI/
      UIManager.cs       — All UI screens and overlays
    GameManager.cs       — Game flow controller
    RocketController.cs  — Rocket gameplay
    GravitySource.cs     — Planet visuals
    DockingGate.cs       — Gate visuals
    StarField.cs         — Background
```

## Features Implemented

### 1. Multiple Ships with Perks
- **Starter Cruiser** (shipA): Standard issue, white, default gravity (G=50)
- **Anti-Grav Skiff** (shipB): Purple, reduced gravity (G=30), recommended for High Gravity sectors
- **Nebula Piercer** (shipC): Green, immune to toxic nebula hull damage
- Ships unlocked via Battle Pass progression
- Multi-select fleet system: equip multiple ships, switch in-game during AIMING state

### 2. Trail System (Chromatic Aberration)
- 7 trail types: Default (chroma RGB), Basic, Neon, Plasma, Stardust, Comet, Supernova
- Chromatic trails use 3 offset channels (R/G/B) with ABERRATION_MULT=12
- Non-chromatic trails use ship hex color with reduced offset (mult=3)
- Trails unlocked via Battle Pass, selectable in Hangar

### 3. Procedural Level Generation
- 50 levels total: 2 universes × 10 sectors × 5 levels each
- 5 level archetypes (type 0–4) with varying planet count, size, mass
- Difficulty scales with `diff = i/25 + u*0.3`
- Start position and goal position vary per level
- Tutorial levels (U1, first 3) are centered for easier play
- Algorithm matches the original HTML5 `generateLevels()` exactly

### 4. Sector Mechanics
- **Normal Space**: Standard physics (sectors 1, and others not listed)
- **High Gravity**: Sectors 2, 4, 5–7. Ship B recommended.
- **Toxic Nebula**: Sectors 3, 4, 8–10. Ship C required or hull takes 0.5 damage/frame
- Hull integrity starts at 100, drops to 0 = game over
- Purple nebula overlay + red damage overlay rendered via GL

### 5. Shield System
- Start with 5 shields (or 6 with buff)
- Lose 1 shield on game over
- Gain 1 shield on level complete
- When shields depleted: option to watch ad to refill or retreat to sector
- Shield buff from Battle Pass tier 3 premium reward

### 6. Battle Pass (12 Tiers)
- Free track: ships, trails, powerups, skins, credits
- Premium track: skins, trails, buffs, powerups, credits
- Tiers unlock based on account level (totalStars / 10)
- Stars required: 10, 20, 30... up to 120
- Claim rewards to receive items + bonus credits
- Premium upgrade button (simulated)

### 7. Shop / Currency Terminal
- 3 credit packs: 100/$4.99, 300/$9.99, 900/$14.99
- Simulated purchase overlay (2 second processing animation)
- Power-up items: Magnet, Gate Extender, Placeholders
- Credits earned from Battle Pass rewards

### 8. UI Screens (All Programmatic)
- **Top Navigation Bar**: Stars count, Account Level, Credits (clickable), Map/Pass/Hangar/Shop tabs
- **Map View**: Universe selector (U1/U2), 10 sector cards in 2-column grid, mechanics tags, star/level progress
- **Sector View**: Back button, hazard warnings, 5 level rows with stars, play/lock buttons
- **Hangar**: Fleet ships (multi-select toggle), Trails (single-select)
- **Battle Pass**: Tier cards with free/premium sections, claim buttons, premium upgrade
- **Shop**: Credit packs, power-up items with pricing
- **Pre-Flight Loadout**: Squadron details, hazard analysis, launch/abort buttons
- **Sector Summary**: Typewriter terminal effect showing per-level stars, total, completion button
- **In-Game HUD**: Location, shield bar, fleet name, drag instruction message
- **In-Game Fleet Panel**: Quick ship switch during AIMING state
- **Game Over Overlay**: Recalculate (if shields), Refill Shields (Ad), Retreat to Sector
- **Success Overlay**: Stars display, Continue button (auto-advances to next level)
- **Ad Overlay**: Simulated "Establishing Warp Link..." for 1.5s
- **Purchase Overlay**: Simulated "Secure Link Established" for 2s

### 9. Game Flow
```
MENU (Map View) → Select Sector → Select Level → Pre-Flight Loadout → Launch
  → AIMING (drag to aim, Fleet button to switch ships)
  → FLYING (gravity, collisions, nebula damage)
  → Hit Planet/OOB/Hull=0 → GAMEOVER
    → Recalculate (retry, costs 1 shield)
    → Refill Shields (simulated ad)
    → Retreat to Sector (back to map)
  → Hit Gate → LEVEL_COMPLETE (1-3 stars)
    → Continue → Next Level (auto-advance within sector)
    → After Level 5 → Sector Summary Terminal → Back to Map
```

### 10. Persistence
- All player data saved via PlayerPrefs: level scores, equipped ships/trails, credits, premium status, claimed rewards, shields
- Loaded on startup, saved on return to menu

### 11. Mobile/Android Ready
- Touch input support via InputSystem Touchscreen
- 1080×1920 CanvasScaler reference resolution
- Full HD text rendering with Courier New monospace font
- All UI elements sized for mobile screens

## Editor Setup (ZERO — Fully Automatic)
Same as before: `[RuntimeInitializeOnLoadMethod]` auto-creates GameManager. Press Play in SampleScene. The UIManager is created as a child of GameManager automatically. All UI, game objects, and levels are created programmatically.
