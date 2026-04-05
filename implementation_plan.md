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

## Flow (1 Level on Repeat)
```
MENU → [Start Button] → AIMING → [Drag & Release] → FLYING
  → Hit Planet/OOB → GAMEOVER → [Retry] → AIMING (same level)
  → Hit Gate → LEVEL_COMPLETE (show stars) → [Replay] → AIMING (same level)
```
