# Gravity Rocket — Unity Port Design Spec

## Overview

Port of a WebGL/Canvas 2D gravity-based rocket game to Unity 6 (URP 2D, Android, portrait mode). The player drags to aim, launches a rocket, and it flies under gravitational influence of planets. Land in the goal zone for stars. 50 procedurally generated levels with escalating difficulty.

## Target Platform

- **Engine:** Unity 6 (6000.3.10f1)
- **Render Pipeline:** URP 2D
- **Platform:** Android, portrait mode
- **Input:** Touch (new Input System)
- **Aspect Ratios:** 16:9 through 20:9+ (variable width, fixed height)

---

## Game Mechanics

### Core Loop

1. Player sees the level: rocket at bottom, goal zone at top, planets in between
2. Player drags on screen to set aim direction
3. On release, rocket launches at fixed speed in aimed direction
4. Rocket is pulled by gravitational forces from all planets (inverse-square law)
5. Rocket either: lands in goal (success), crashes into a planet (fail), or flies off-screen (fail)
6. On success: earn 1-3 stars based on accuracy, advance to next sector
7. On fail: lose 1 shield, retry

### Star Rating

Based on horizontal hit position within the goal zone:
- **3 stars:** Center 20% of goal width (hitRatio 0.4–0.6)
- **2 stars:** Middle zones (hitRatio 0.2–0.4 and 0.6–0.8)
- **1 star:** Outer edges (hitRatio 0.0–0.2 and 0.8–1.0)

Best score per level is tracked persistently.

### Shield System

- Maximum 5 shields
- Lose 1 shield on any failure (crash or off-screen)
- Recover 1 shield on successful docking (capped at max)
- At 0 shields: player must "refill shields" (placeholder for ad/IAP) or retreat to previous sector
- Shields reset to max on game restart

### Game States

```
MENU → AIMING → FLYING → LEVEL_COMPLETE → AIMING (next level)
                   ↓                              ↑
                GAMEOVER → AIMING (retry) ────────┘
                   ↓
            SHIELD_DEPLETED → AIMING (retreat/refill)
```

Final level completion transitions to VICTORY state.

---

## Architecture

### Script Overview

| Script | Responsibility |
|--------|---------------|
| `GameManager` | Singleton. State machine, level lifecycle, shield/star tracking |
| `LevelGenerator` | Generates 50 deterministic `LevelData` configs |
| `LevelData` | Data struct: start pos, goal pos, list of `PlanetConfig` |
| `Rocket` | Rigidbody2D, launch velocity, collision callbacks, warp animation |
| `GravityField` | MonoBehaviour on each planet. Applies gravitational AddForce to rocket in FixedUpdate |
| `GoalZone` | BoxCollider2D trigger. Calculates hit ratio on contact |
| `AimController` | Touch/mouse drag input, draws aim line, triggers launch |
| `UIManager` | Manages Canvas panels (menu, game over, shield depleted, level complete, victory), HUD |
| `StarField` | Generates background star sprites/texture |
| `ChromaticAberrationFeature` | Custom URP ScriptableRendererFeature + pass + shader |
| `RocketParticles` | Manages exhaust ParticleSystem, explosion burst, references on Rocket |
| `ShockwavePulse` | Expanding ring effect on successful docking (sprite scale+fade) |
| `ObjectPool<T>` | Generic pooling utility for planets, particles |
| `AudioManager` | Singleton. Background music loop, volume/mute control, persists settings |
| `GameEvent` / `GameEvent<T>` | ScriptableObject event channels (parameterless and typed) |
| `GameEventListener` / `GameEventListener<T>` | MonoBehaviour listeners for Inspector wiring |
| `AnalyticsService` | Subscribes to game events, formats and dispatches analytics payloads |
| `AnalyticsEventConfig` | SO mapping a GameEvent to an analytics event name + parameter schema |
| `AnalyticsConfig` | SO master list of all tracked analytics events |

### Scene Hierarchy

```
Main Camera (ortho, ChromaticAberrationFeature on renderer)
EventSystem
GameManager
  └── UIManager (Canvas)
       ├── HUD (top-left, safe area offset)
       │    ├── SectorText
       │    ├── StarsText
       │    └── ShieldBar
       ├── MenuPanel
       ├── GameOverPanel
       ├── ShieldDepletedPanel
       ├── LevelCompletePanel
       └── VictoryPanel
StarField (background layer)
--- Spawned at runtime ---
Rocket (prefab)
  ├── SpriteRenderer (rocket shape)
  ├── Rigidbody2D (dynamic, no gravity scale)
  ├── CircleCollider2D
  ├── TrailRenderer
  ├── RocketParticles (exhaust ParticleSystem child)
  └── AimController
Planets (pooled prefabs)
  ├── SpriteRenderer (circle + crosshair)
  ├── CircleCollider2D
  ├── GravityField
  └── GravityWaveRing (child, animated)
GoalZone (prefab)
  ├── BoxCollider2D (trigger)
  └── GoalZoneRenderer (draws segmented zones, brackets, pulsing center)
```

---

## Physics

### Approach: Rigidbody2D with AddForce

- Rocket has `Rigidbody2D` with `gravityScale = 0` (no Unity default gravity)
- Each `GravityField` component (on planets) applies force to the rocket every `FixedUpdate`
- Force formula: `F = G * planetMass / distanceSq` (inverse-square, direction toward planet)
- Force applied via `Rigidbody2D.AddForce(direction * forceMagnitude)`
- Collision detection via Unity colliders:
  - Planets: `CircleCollider2D` (non-trigger) — rocket's `OnCollisionEnter2D` triggers crash
  - Goal: `BoxCollider2D` (trigger) — rocket's `OnTriggerEnter2D` triggers success
  - Screen bounds: checked manually each frame

### Constants (to be tuned)

Working in a 10×20 unit world:
- `G_CONSTANT`: ~15 (gravitational constant, tuned for feel)
- `LAUNCH_SPEED`: ~9 units/sec
- `ROCKET_RADIUS`: ~0.2 units
- Planet radii: 0.3–0.8 units
- Planet masses: 50–300 (scaled proportionally with radius squared)

These will need play-testing to match the original feel.

---

## Level Generation

### World Space

- **Logical play area:** 10 units wide × 20 units tall, centered at (5, 10)
- **Start zone:** bottom 3 units (y: 0–3) — no planets allowed
- **Goal zone:** top 2 units (y: 18–20) — no planets allowed
- **Planet zone:** y: 3–18 (15 units of playable space)

### Generation Algorithm

50 levels, deterministic (seeded by level index). Difficulty factor `diff = index / 50.0` (0.0 to 1.0).

**Start position:** `x = 5.0 + sin(index * 1.3) * 2.5`, `y = 1.5`
**Goal position:** `x = 5.0 + cos(index * 1.7) * 2.0 - goalWidth/2`, `y = 19.0`
**First 3 levels:** Centered start and goal (tutorials).

### Layout Types (cycle every 5 levels)

| Type | Name | Description | Planet Count |
|------|------|-------------|-------------|
| 0 | Corridor | Pairs of planets forming gates to fly through | 2–6 |
| 1 | Blockade | Planets blocking direct path, requiring curves | 1–4 |
| 2 | Zigzag | Alternating left-right planets | 3–8 |
| 3 | Slingshot | Medium planets placed for gravity-assist chains | 1–3 |
| 4 | Field | Scattered small planets | 3–10 |

### Planet Sizing

- **Radius range:** 0.3 to 0.8 units (smaller than original's 0.6–1.75 equivalent)
- **Mass:** proportional to `radius^2` (mass = baseMultiplier * radius * radius)
- Gravity wave visualization radius scales with mass
- Difficulty increases planet count primarily, with modest size increases

### Buffer Enforcement

After generating planet positions, validate:
- No planet center within 3 units of start position
- No planet center within 2 units of goal zone top edge
- Minimum 1.5 units between any two planet edges

---

## Visual Effects

### Chromatic Aberration (Post-Process Shader)

**Implementation:** Custom URP `ScriptableRendererFeature` + `ScriptableRenderPass`.

- Inserted after all opaque and transparent rendering, before UI
- Renders to a temporary RT, then blits back with the aberration shader
- Shader samples the screen texture 3 times at different UV offsets (R, G, B channels)
- Offset controlled by global shader property `_AberrationOffset` (Vector2)
- `Rocket` script updates `_AberrationOffset` each frame based on current Rigidbody2D acceleration magnitude and direction
- At rest: zero offset. Under heavy gravity: offset increases. During warp: maximum offset.
- Multiplier constant `ABERRATION_MULT` tuned visually (~0.003 in UV space)

### Trail

- Unity `TrailRenderer` component on the Rocket
- White color, width ~0.15 units, fading alpha over lifetime
- Time-based lifetime (~2 seconds)
- Chromatic aberration shader handles the RGB splitting automatically

### Particles

**Exhaust (while flying):**
- Unity Particle System, child of Rocket
- Emits from rocket rear, local space offset
- White color, small size (0.05–0.1), short lifetime (0.3s)
- Rate: ~20/sec, slight velocity spread opposite to rocket direction
- Emission enabled only during FLYING and LEVEL_COMPLETE states

**Explosion (on crash):**
- Burst emission: 40 particles
- Radial velocity in all directions, speed 2–7 units/sec
- White, lifetime 0.5–1.0s, fading alpha
- Triggered by `RocketParticles.Explode()` call from GameManager

**Shockwave Pulse (on success):**
- Spawned at goal hit position
- Simple sprite (white ring) that scales up from 0.5 to 5 units over 1 second, alpha fading from 1 to 0
- Two pulses staggered 150ms apart
- Managed by `ShockwavePulse` script with coroutine

### Planet Visuals

- Circle sprite, black fill, white 2px outline
- Crosshair lines through center (thin white lines at 0.5 opacity)
- **Gravity wave ring:** child object with ring sprite, looping animation:
  - Scale from 1.0 to 1.0 + (mass * waveMultiplier)
  - Alpha from 1.0 to 0.0
  - Repeat every ~1 second
  - Shows gravitational influence radius visually

### Goal Zone Rendering

- Custom `GoalZoneRenderer` using `LineRenderer` components or `GL` drawing
- 5 segments: left 1-star, left 2-star, center 3-star, right 2-star, right 1-star
- Outer zones: dashed lines (animated dash offset)
- Center zone: solid lines + pulsing white fill (sin wave alpha 0.0–0.3)
- Corner brackets at full zone extents
- All white/monochrome

### Background

- `StarField`: generates ~150 small white square sprites at random positions
- Random sizes (0.02–0.06 units) and alpha (0.2–0.8)
- Static, no movement — sits on a background sorting layer

---

## Mobile & Screen Scaling

### Camera

- Orthographic, fixed height = 20 units (`orthographicSize = 10`)
- Camera centered at `(5, 10, -10)`
- Width varies by device aspect ratio:
  - 9:16 → ~11.25 units wide (play area 10 fits with margin)
  - 9:20 → ~9 units wide (slightly tight, play area still fits)
  - 3:4 tablet → 15 units wide (generous margins)
- If screen aspect is narrower than 1:2 (width < 10 units at default ortho size), dynamically increase `orthographicSize` so that `orthographicSize * aspect * 2 >= 10`, ensuring the full play area width is always visible

### UI (Canvas)

- Canvas Scaler: **Scale With Screen Size**
- Reference resolution: 1080 × 1920
- Screen Match Mode: match height (1.0)
- All panels centered with max-width constraint
- HUD anchored top-left with `Screen.safeArea` offset for notches/punch-holes
- Buttons minimum 48dp (130 units at reference res) touch targets
- Font: monospace (Courier New or similar bundled font)

### Input

- New Input System with `EnhancedTouch` or `Touchscreen` bindings
- `primaryTouch/position` for drag position
- `primaryTouch/press` for press/release detection
- Also support mouse for Editor testing
- Drag from anywhere on screen to aim (no virtual joystick)

### Performance

- Object pooling for planets (max ~10 active at once, pool of 12)
- Particle systems use pooling internally
- Single extra full-screen blit for chromatic aberration (acceptable on mobile)
- No `Find()` or `GetComponent()` in Update loops — all references cached
- Target 60 FPS

---

## Audio

### Background Music

- **Track:** `Assets/BackgroundMusic/PPK - Resurrection full versionmp4.mp3`
- **Behavior:** Loops continuously from game launch. Never stops or restarts on level start, level complete, level fail, or game over. Seamless loop across all states.
- **Implementation:** `AudioManager` singleton with a single `AudioSource` component set to `loop = true`, `playOnAwake = true`. Marked `DontDestroyOnLoad` (future-proofing for multi-scene, but currently single scene so it simply persists).

### Sound Settings

- **Mute toggle:** On/Off button in the menu panel and HUD (small speaker icon or text toggle)
- **Volume slider:** 0–100 range, controls `AudioSource.volume` (mapped to 0.0–1.0)
- **Persistence:** Both settings saved to `PlayerPrefs`:
  - `"MusicEnabled"` (int, 0 or 1)
  - `"MusicVolume"` (int, 0–100)
- **Defaults:** Enabled, volume 80
- **UI location:** Settings accessible from the menu panel. Small mute button also visible on the HUD during gameplay for quick toggle.

### Script

| Script | Purpose |
|--------|---------|
| `AudioManager` | Singleton. Owns the AudioSource, exposes `SetVolume(int)`, `SetMuted(bool)`. Loads/saves prefs. |

---

## Data Persistence

- `levelStars[50]` array — best star count per level
- `currentShields` — current shield count
- Music preferences (enabled, volume)
- Saved via `PlayerPrefs` (simple key-value):
  - `"Stars_0"` through `"Stars_49"` (int, 0–3)
  - `"CurrentLevel"` (int, last unlocked level)
  - `"Shields"` (int, current shield count)
  - `"MusicEnabled"` (int, 0 or 1, default 1)
  - `"MusicVolume"` (int, 0–100, default 80)
- Load on game start, save after each level completion

---

## Analytics & Event System

### Design Principles

- **Data-driven:** Events are defined as ScriptableObject assets, not hardcoded strings. Adding a new event means creating a new asset, not editing code.
- **Reusable:** The event system is generic — not analytics-specific. Any system (analytics, audio, UI, achievements) can listen to the same events.
- **Decoupled:** Event producers (GameManager, Rocket) don't know about consumers (analytics logger, future audio manager). Communication flows through ScriptableObject event channels.

### Architecture: ScriptableObject Event Channels

```
GameManager fires →  GameEvent SO  ← AnalyticsListener subscribes
                         ↑
               GameEventWithData<T> SO  ← (typed variant for payloads)
```

**Core types:**

| Class | Purpose |
|-------|---------|
| `GameEvent` | ScriptableObject. Parameterless event channel. Has `Raise()` method and `OnRaised` C# event. |
| `GameEventListener` | MonoBehaviour. References a `GameEvent` SO, invokes a `UnityEvent` when raised. For wiring in Inspector. |
| `GameEvent<T>` | Generic typed variant. `Raise(T data)` for events with payloads. |
| `GameEventListener<T>` | Generic typed listener with `UnityEvent<T>` response. |
| `AnalyticsPayload` | Serializable struct carrying event parameters as `Dictionary<string, object>`. |
| `AnalyticsService` | MonoBehaviour singleton. Subscribes to analytics-relevant GameEvents, formats and dispatches payloads. Currently logs to console/file; backend pluggable later. |
| `AnalyticsEventConfig` | ScriptableObject. Maps a `GameEvent` to an analytics event name string + parameter schema. One asset per tracked event. |
| `AnalyticsConfig` | ScriptableObject. Master list of all `AnalyticsEventConfig` assets. `AnalyticsService` reads this at startup to auto-subscribe. |

### Data Flow

1. `GameManager` calls `levelStartEvent.Raise(levelStartData)` when a level begins
2. The `GameEvent<LevelStartData>` SO broadcasts to all subscribers
3. `AnalyticsService` receives it, looks up the matching `AnalyticsEventConfig` to get the event name and parameter mapping
4. `AnalyticsService` formats the payload and dispatches (currently: `Debug.Log` + write to local JSON log; later: Firebase/Unity Analytics/custom backend)

### Events to Track

| Event SO Asset | Trigger | Payload Type | Parameters |
|---------------|---------|-------------|------------|
| `LevelStart` | `GameManager.LoadLevel()` | `LevelEventData` | `levelIndex`, `levelType`, `shieldsRemaining` |
| `LevelComplete` | `GameManager.LevelComplete()` | `LevelCompleteData` | `levelIndex`, `levelType`, `starsEarned` (1-3), `shieldsRemaining`, `isNewBest` |
| `LevelFail` | `GameManager.GameOver()` | `LevelFailData` | `levelIndex`, `levelType`, `failReason` ("crash" or "out_of_bounds"), `shieldsRemaining` |

### Payload Structs

```csharp
[Serializable]
public struct LevelEventData
{
    public int LevelIndex;
    public int LevelType; // 0-4 layout type
    public int ShieldsRemaining;
}

[Serializable]
public struct LevelCompleteData
{
    public int LevelIndex;
    public int LevelType;
    public int StarsEarned;
    public int ShieldsRemaining;
    public bool IsNewBest;
}

[Serializable]
public struct LevelFailData
{
    public int LevelIndex;
    public int LevelType;
    public string FailReason;
    public int ShieldsRemaining;
}
```

### ScriptableObject Assets (created in Editor)

```
Assets/Data/Events/
├── LevelStartEvent.asset        (GameEvent<LevelEventData>)
├── LevelCompleteEvent.asset     (GameEvent<LevelCompleteData>)
└── LevelFailEvent.asset         (GameEvent<LevelFailData>)

Assets/Data/Analytics/
├── AnalyticsConfig.asset         (master config, references all below)
├── Track_LevelStart.asset        (AnalyticsEventConfig: maps LevelStartEvent → "level_start")
├── Track_LevelComplete.asset     (AnalyticsEventConfig: maps LevelCompleteEvent → "level_complete")
└── Track_LevelFail.asset         (AnalyticsEventConfig: maps LevelFailEvent → "level_fail")
```

### Extensibility

- **New event:** Create a new payload struct, a new `GameEvent<T>` SO asset, and a new `AnalyticsEventConfig` SO asset. Zero code changes to `AnalyticsService`.
- **New consumer:** Any MonoBehaviour can subscribe to existing `GameEvent` SOs. E.g., a future audio manager subscribes to `LevelCompleteEvent` to play a success sound.
- **New backend:** Swap `AnalyticsService` dispatch method from local logging to Firebase/Unity Analytics. Event definitions unchanged.

---

## File Organization

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs
│   │   ├── LevelGenerator.cs
│   │   ├── LevelData.cs
│   │   └── ObjectPool.cs
│   ├── Gameplay/
│   │   ├── Rocket.cs
│   │   ├── GravityField.cs
│   │   ├── GoalZone.cs
│   │   ├── GoalZoneRenderer.cs
│   │   └── AimController.cs
│   ├── VFX/
│   │   ├── ChromaticAberrationFeature.cs
│   │   ├── ChromaticAberrationPass.cs
│   │   ├── RocketParticles.cs
│   │   ├── ShockwavePulse.cs
│   │   └── StarField.cs
│   ├── UI/
│   │   └── UIManager.cs
│   ├── Audio/
│   │   └── AudioManager.cs
│   └── Events/
│       ├── GameEvent.cs
│       ├── GameEventT.cs
│       ├── GameEventListener.cs
│       ├── GameEventListenerT.cs
│       ├── AnalyticsService.cs
│       ├── AnalyticsEventConfig.cs
│       └── AnalyticsConfig.cs
├── Data/
│   ├── Events/
│   │   ├── LevelStartEvent.asset
│   │   ├── LevelCompleteEvent.asset
│   │   └── LevelFailEvent.asset
│   └── Analytics/
│       ├── AnalyticsConfig.asset
│       ├── Track_LevelStart.asset
│       ├── Track_LevelComplete.asset
│       └── Track_LevelFail.asset
├── Shaders/
│   └── ChromaticAberration.shader
├── Prefabs/
│   ├── Rocket.prefab
│   ├── Planet.prefab
│   ├── GoalZone.prefab
│   └── ShockwaveRing.prefab
├── Materials/
│   └── (auto-generated materials)
├── Scenes/
│   └── GameScene.unity
└── Settings/
    └── (existing URP settings)
```

---

## Out of Scope (for initial port)

- Ads/IAP integration (shield refill is a placeholder button)
- Cloud save
- Localization
- Level select screen
