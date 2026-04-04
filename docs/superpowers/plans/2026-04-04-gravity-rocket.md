# Gravity Rocket Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port a WebGL gravity-rocket game to Unity 6 (URP 2D, Android portrait, touch input) with chromatic aberration shader, procedural levels, analytics event system, and background music.

**Architecture:** Single-scene game. GameManager singleton drives a state machine (Menu/Aiming/Flying/LevelComplete/GameOver/Victory). Rigidbody2D physics with per-planet gravity via AddForce. ScriptableObject-based event channels decouple analytics from gameplay. Custom URP Renderer Feature for chromatic aberration post-process.

**Tech Stack:** Unity 6 (6000.3.10f1), URP 2D, C#, New Input System, Rigidbody2D, ScriptableObjects, PlayerPrefs

**Spec:** `docs/superpowers/specs/2026-04-04-gravity-rocket-design.md`

---

## File Map

### Scripts to Create

| Path | Responsibility |
|------|---------------|
| `Assets/Scripts/Core/GameManager.cs` | Singleton. State machine, level lifecycle, shields, stars, persistence |
| `Assets/Scripts/Core/LevelGenerator.cs` | Generates 50 deterministic LevelData configs |
| `Assets/Scripts/Core/LevelData.cs` | Data structs: LevelData, PlanetConfig |
| `Assets/Scripts/Core/ObjectPool.cs` | Generic object pool |
| `Assets/Scripts/Gameplay/Rocket.cs` | Rigidbody2D rocket: launch, collisions, warp animation, bounds check |
| `Assets/Scripts/Gameplay/GravityField.cs` | Planet component: applies gravity to rocket via AddForce |
| `Assets/Scripts/Gameplay/GoalZone.cs` | Trigger collider, calculates hit ratio |
| `Assets/Scripts/Gameplay/GoalZoneRenderer.cs` | Draws segmented goal zone with LineRenderers |
| `Assets/Scripts/Gameplay/AimController.cs` | Touch/mouse drag input, aim line, launch trigger |
| `Assets/Scripts/VFX/ChromaticAberrationFeature.cs` | URP ScriptableRendererFeature |
| `Assets/Scripts/VFX/ChromaticAberrationPass.cs` | URP ScriptableRenderPass |
| `Assets/Scripts/VFX/RocketParticles.cs` | Exhaust + explosion particle control |
| `Assets/Scripts/VFX/ShockwavePulse.cs` | Expanding ring effect |
| `Assets/Scripts/VFX/StarField.cs` | Background stars generator |
| `Assets/Scripts/UI/UIManager.cs` | Canvas panels, HUD, safe area |
| `Assets/Scripts/Audio/AudioManager.cs` | Music singleton, volume/mute, PlayerPrefs |
| `Assets/Scripts/Events/GameEvent.cs` | Parameterless SO event channel |
| `Assets/Scripts/Events/GameEventListener.cs` | MonoBehaviour listener for GameEvent |
| `Assets/Scripts/Events/LevelEvents.cs` | Typed event SOs + payload structs for level start/complete/fail |
| `Assets/Scripts/Events/AnalyticsService.cs` | Subscribes to level events, logs analytics |
| `Assets/Scripts/Events/AnalyticsEventConfig.cs` | SO: maps event to analytics name |
| `Assets/Scripts/Events/AnalyticsConfig.cs` | SO: master list of analytics configs |
| `Assets/Shaders/ChromaticAberration.shader` | Full-screen RGB split shader |

### Assets to Create (via MCP tools in Unity Editor)

| Path | Type |
|------|------|
| `Assets/Prefabs/Rocket.prefab` | Rocket prefab with all components |
| `Assets/Prefabs/Planet.prefab` | Planet prefab with GravityField, visuals |
| `Assets/Prefabs/GoalZone.prefab` | Goal zone prefab |
| `Assets/Prefabs/ShockwaveRing.prefab` | Expanding ring sprite |
| `Assets/Materials/ChromaticAberration.mat` | Material using the CA shader |
| `Assets/Scenes/GameScene.unity` | Main game scene |
| `Assets/Data/Events/*.asset` | SO event channel instances |
| `Assets/Data/Analytics/*.asset` | Analytics config instances |

---

## Task 1: Project Folder Structure & Data Types

**Files:**
- Create: `Assets/Scripts/Core/LevelData.cs`

- [ ] **Step 1: Create folder structure**

```bash
mkdir -p "Assets/Scripts/Core"
mkdir -p "Assets/Scripts/Gameplay"
mkdir -p "Assets/Scripts/VFX"
mkdir -p "Assets/Scripts/UI"
mkdir -p "Assets/Scripts/Audio"
mkdir -p "Assets/Scripts/Events"
mkdir -p "Assets/Shaders"
mkdir -p "Assets/Prefabs"
mkdir -p "Assets/Materials"
mkdir -p "Assets/Data/Events"
mkdir -p "Assets/Data/Analytics"
```

- [ ] **Step 2: Create LevelData.cs with data structs**

```csharp
// Assets/Scripts/Core/LevelData.cs
using System;
using UnityEngine;

namespace GravityRocket.Core
{
    [Serializable]
    public struct PlanetConfig
    {
        public Vector2 Position;
        public float Radius;
        public float Mass;
    }

    [Serializable]
    public struct LevelData
    {
        public int LevelIndex;
        public int LayoutType;
        public Vector2 StartPosition;
        public Vector2 GoalPosition;
        public float GoalWidth;
        public PlanetConfig[] Planets;
    }
}
```

- [ ] **Step 3: Verify compilation**

Use `console-get-logs` MCP tool to check for compile errors. Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core/LevelData.cs
git commit -m "feat: add LevelData and PlanetConfig data structs"
```

---

## Task 2: Level Generator

**Files:**
- Create: `Assets/Scripts/Core/LevelGenerator.cs`

- [ ] **Step 1: Create LevelGenerator.cs**

```csharp
// Assets/Scripts/Core/LevelGenerator.cs
using UnityEngine;
using System.Collections.Generic;

namespace GravityRocket.Core
{
    public static class LevelGenerator
    {
        public const int TotalLevels = 50;
        public const float WorldWidth = 10f;
        public const float WorldHeight = 20f;
        public const float GoalWidth = 4f;
        public const float GoalHeight = 0.75f;

        // Buffer zones
        private const float StartBufferY = 3f;
        private const float GoalBufferY = 2f;
        private const float PlanetMinY = 3f;
        private const float PlanetMaxY = 18f;
        private const float MinPlanetEdgeGap = 1.5f;

        // Planet sizing
        private const float MinRadius = 0.3f;
        private const float MaxRadius = 0.8f;
        private const float MassPerRadiusSq = 500f;

        /// <summary>
        /// Generates all 50 levels deterministically.
        /// </summary>
        public static LevelData[] GenerateAllLevels()
        {
            var levels = new LevelData[TotalLevels];
            for (int i = 0; i < TotalLevels; i++)
            {
                levels[i] = GenerateLevel(i);
            }
            return levels;
        }

        /// <summary>
        /// Generates a single level by index.
        /// </summary>
        public static LevelData GenerateLevel(int index)
        {
            float diff = (float)index / TotalLevels;
            int layoutType = index % 5;

            // Start and goal positions
            float startX = WorldWidth / 2f + Mathf.Sin(index * 1.3f) * 2.5f;
            float goalX = WorldWidth / 2f + Mathf.Cos(index * 1.7f) * 2f - GoalWidth / 2f;

            // First 3 levels are centered tutorials
            if (index < 3)
            {
                startX = WorldWidth / 2f;
                goalX = WorldWidth / 2f - GoalWidth / 2f;
            }

            // Clamp goal so it stays within world bounds
            goalX = Mathf.Clamp(goalX, 0.5f, WorldWidth - GoalWidth - 0.5f);

            var planetList = GeneratePlanetsForLayout(layoutType, diff, index);
            var validPlanets = EnforceBuffers(planetList, new Vector2(startX, 1.5f));

            return new LevelData
            {
                LevelIndex = index,
                LayoutType = layoutType,
                StartPosition = new Vector2(startX, 1.5f),
                GoalPosition = new Vector2(goalX, 19f),
                GoalWidth = GoalWidth,
                Planets = validPlanets
            };
        }

        private static List<PlanetConfig> GeneratePlanetsForLayout(int type, float diff, int index)
        {
            var planets = new List<PlanetConfig>();

            switch (type)
            {
                case 0: // Corridor - pairs forming gates
                    int gatePairs = 1 + Mathf.FloorToInt(diff * 2.5f);
                    for (int g = 0; g < gatePairs; g++)
                    {
                        float gapY = PlanetMinY + (PlanetMaxY - PlanetMinY) * ((g + 1f) / (gatePairs + 1f));
                        float offset = Mathf.Sin(index + g) * 1.5f;
                        float r = Mathf.Lerp(MinRadius, MinRadius + 0.2f, diff);
                        planets.Add(MakePlanet(WorldWidth / 2f - 2f + offset, gapY, r));
                        planets.Add(MakePlanet(WorldWidth / 2f + 2f + offset, gapY, r));
                    }
                    break;

                case 1: // Blockade - planets blocking direct path
                    int blockCount = 1 + Mathf.FloorToInt(diff * 3f);
                    for (int b = 0; b < blockCount; b++)
                    {
                        float by = PlanetMinY + (PlanetMaxY - PlanetMinY) * ((b + 1f) / (blockCount + 1f));
                        float bx = WorldWidth / 2f + Mathf.Sin(index + b * 2.1f) * 2.5f;
                        float r = Mathf.Lerp(MinRadius + 0.1f, MaxRadius - 0.1f, diff);
                        planets.Add(MakePlanet(bx, by, r));
                    }
                    break;

                case 2: // Zigzag - alternating left-right
                    int zigCount = 3 + Mathf.FloorToInt(diff * 5f);
                    for (int z = 0; z < zigCount; z++)
                    {
                        float zy = PlanetMinY + (PlanetMaxY - PlanetMinY) * ((z + 1f) / (zigCount + 1f));
                        float zx = (z % 2 == 0) ? 2.5f : WorldWidth - 2.5f;
                        zx += Mathf.Sin(index * 0.7f + z) * 0.5f;
                        float r = Mathf.Lerp(MinRadius, MinRadius + 0.15f, diff);
                        planets.Add(MakePlanet(zx, zy, r));
                    }
                    break;

                case 3: // Slingshot - medium planets for gravity assists
                    int slingCount = 1 + Mathf.FloorToInt(diff * 2f);
                    for (int s = 0; s < slingCount; s++)
                    {
                        float sy = PlanetMinY + (PlanetMaxY - PlanetMinY) * ((s + 1f) / (slingCount + 1f));
                        float sx = WorldWidth / 2f + ((index + s) % 2 == 0 ? 1.5f : -1.5f);
                        float r = Mathf.Lerp(0.5f, MaxRadius, diff);
                        planets.Add(MakePlanet(sx, sy, r));
                    }
                    break;

                case 4: // Field - scattered small planets
                    int fieldCount = 3 + Mathf.FloorToInt(diff * 7f);
                    for (int f = 0; f < fieldCount; f++)
                    {
                        float fx = 1f + ((index * f * 31) % ((int)((WorldWidth - 2f) * 100))) / 100f;
                        float fy = PlanetMinY + ((index * f * 47) % ((int)((PlanetMaxY - PlanetMinY) * 100))) / 100f;
                        float r = Mathf.Lerp(MinRadius, MinRadius + 0.1f, diff) + (f % 2) * 0.05f;
                        planets.Add(MakePlanet(fx, fy, r));
                    }
                    break;
            }

            return planets;
        }

        private static PlanetConfig MakePlanet(float x, float y, float radius)
        {
            return new PlanetConfig
            {
                Position = new Vector2(x, y),
                Radius = radius,
                Mass = MassPerRadiusSq * radius * radius
            };
        }

        private static PlanetConfig[] EnforceBuffers(List<PlanetConfig> planets, Vector2 startPos)
        {
            var valid = new List<PlanetConfig>();

            foreach (var p in planets)
            {
                // Buffer from start position
                if (Vector2.Distance(p.Position, startPos) < 3f)
                    continue;

                // Buffer from goal zone top
                if (p.Position.y > PlanetMaxY)
                    continue;

                // Buffer from bottom
                if (p.Position.y < PlanetMinY)
                    continue;

                // Clamp X to world bounds
                if (p.Position.x < p.Radius + 0.2f || p.Position.x > WorldWidth - p.Radius - 0.2f)
                    continue;

                // Minimum edge gap between planets
                bool tooClose = false;
                foreach (var v in valid)
                {
                    float edgeDist = Vector2.Distance(p.Position, v.Position) - p.Radius - v.Radius;
                    if (edgeDist < MinPlanetEdgeGap)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                valid.Add(p);
            }

            return valid.ToArray();
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Use `console-get-logs` to check for errors. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/LevelGenerator.cs
git commit -m "feat: add procedural level generator with 50 levels and 5 layout types"
```

---

## Task 3: Object Pool

**Files:**
- Create: `Assets/Scripts/Core/ObjectPool.cs`

- [ ] **Step 1: Create ObjectPool.cs**

```csharp
// Assets/Scripts/Core/ObjectPool.cs
using System.Collections.Generic;
using UnityEngine;

namespace GravityRocket.Core
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _initialSize = 10;

        private readonly Queue<GameObject> _pool = new();
        private Transform _poolParent;

        private void Awake()
        {
            _poolParent = new GameObject($"Pool_{_prefab.name}").transform;
            _poolParent.SetParent(transform);

            for (int i = 0; i < _initialSize; i++)
            {
                CreateInstance();
            }
        }

        private void CreateInstance()
        {
            var obj = Instantiate(_prefab, _poolParent);
            obj.SetActive(false);
            _pool.Enqueue(obj);
        }

        /// <summary>
        /// Gets an object from the pool, activating it.
        /// </summary>
        public GameObject Get()
        {
            if (_pool.Count == 0)
            {
                CreateInstance();
            }

            var obj = _pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Returns an object to the pool, deactivating it.
        /// </summary>
        public void Return(GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            obj.transform.SetParent(_poolParent);
            _pool.Enqueue(obj);
        }

        /// <summary>
        /// Returns all active children to the pool.
        /// </summary>
        public void ReturnAll()
        {
            // Collect active objects that came from this pool
            var toReturn = new List<GameObject>();
            for (int i = 0; i < _poolParent.childCount; i++)
            {
                var child = _poolParent.GetChild(i).gameObject;
                if (child.activeSelf)
                {
                    toReturn.Add(child);
                }
            }
            foreach (var obj in toReturn)
            {
                Return(obj);
            }
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/ObjectPool.cs
git commit -m "feat: add generic GameObject object pool"
```

---

## Task 4: Event System (ScriptableObject Channels)

**Files:**
- Create: `Assets/Scripts/Events/GameEvent.cs`
- Create: `Assets/Scripts/Events/GameEventListener.cs`
- Create: `Assets/Scripts/Events/LevelEvents.cs`

- [ ] **Step 1: Create GameEvent.cs (parameterless + generic typed)**

```csharp
// Assets/Scripts/Events/GameEvent.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GravityRocket.Events
{
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "Gravity Rocket/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<Action> _listeners = new();

        /// <summary>
        /// Raises this event, notifying all listeners.
        /// </summary>
        public void Raise()
        {
            // Iterate in reverse so listeners can safely unsubscribe during callback
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i]?.Invoke();
            }
        }

        public void Subscribe(Action listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unsubscribe(Action listener)
        {
            _listeners.Remove(listener);
        }
    }

    public abstract class GameEvent<T> : ScriptableObject
    {
        private readonly List<Action<T>> _listeners = new();

        /// <summary>
        /// Raises this event with payload data.
        /// </summary>
        public void Raise(T data)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i]?.Invoke(data);
            }
        }

        public void Subscribe(Action<T> listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unsubscribe(Action<T> listener)
        {
            _listeners.Remove(listener);
        }
    }
}
```

- [ ] **Step 2: Create GameEventListener.cs**

```csharp
// Assets/Scripts/Events/GameEventListener.cs
using UnityEngine;
using UnityEngine.Events;

namespace GravityRocket.Events
{
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent _event;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_event != null)
                _event.Subscribe(OnEventRaised);
        }

        private void OnDisable()
        {
            if (_event != null)
                _event.Unsubscribe(OnEventRaised);
        }

        private void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}
```

- [ ] **Step 3: Create LevelEvents.cs (typed event SOs + payload structs)**

```csharp
// Assets/Scripts/Events/LevelEvents.cs
using System;
using UnityEngine;

namespace GravityRocket.Events
{
    // --- Payload Structs ---

    [Serializable]
    public struct LevelEventData
    {
        public int LevelIndex;
        public int LevelType;
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

    // --- Concrete SO Types (needed because Unity can't serialize generic SOs) ---

    [CreateAssetMenu(fileName = "LevelStartEvent", menuName = "Gravity Rocket/Events/Level Start Event")]
    public class LevelStartEvent : GameEvent<LevelEventData> { }

    [CreateAssetMenu(fileName = "LevelCompleteEvent", menuName = "Gravity Rocket/Events/Level Complete Event")]
    public class LevelCompleteEvent : GameEvent<LevelCompleteData> { }

    [CreateAssetMenu(fileName = "LevelFailEvent", menuName = "Gravity Rocket/Events/Level Fail Event")]
    public class LevelFailEvent : GameEvent<LevelFailData> { }
}
```

- [ ] **Step 4: Verify compilation**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Events/
git commit -m "feat: add ScriptableObject event system with typed level events"
```

---

## Task 5: Analytics Service

**Files:**
- Create: `Assets/Scripts/Events/AnalyticsEventConfig.cs`
- Create: `Assets/Scripts/Events/AnalyticsConfig.cs`
- Create: `Assets/Scripts/Events/AnalyticsService.cs`

- [ ] **Step 1: Create AnalyticsEventConfig.cs**

```csharp
// Assets/Scripts/Events/AnalyticsEventConfig.cs
using UnityEngine;

namespace GravityRocket.Events
{
    [CreateAssetMenu(fileName = "AnalyticsEventConfig", menuName = "Gravity Rocket/Analytics/Event Config")]
    public class AnalyticsEventConfig : ScriptableObject
    {
        [Tooltip("The analytics event name sent to the backend (e.g. 'level_start')")]
        public string EventName;

        [Tooltip("Which event channel this tracks")]
        public EventChannelType ChannelType;
    }

    public enum EventChannelType
    {
        LevelStart,
        LevelComplete,
        LevelFail
    }
}
```

- [ ] **Step 2: Create AnalyticsConfig.cs**

```csharp
// Assets/Scripts/Events/AnalyticsConfig.cs
using UnityEngine;

namespace GravityRocket.Events
{
    [CreateAssetMenu(fileName = "AnalyticsConfig", menuName = "Gravity Rocket/Analytics/Config")]
    public class AnalyticsConfig : ScriptableObject
    {
        [Tooltip("All analytics event configurations")]
        public AnalyticsEventConfig[] TrackedEvents;
    }
}
```

- [ ] **Step 3: Create AnalyticsService.cs**

```csharp
// Assets/Scripts/Events/AnalyticsService.cs
using UnityEngine;
using System.Collections.Generic;

namespace GravityRocket.Events
{
    public class AnalyticsService : MonoBehaviour
    {
        [SerializeField] private AnalyticsConfig _config;
        [SerializeField] private LevelStartEvent _levelStartEvent;
        [SerializeField] private LevelCompleteEvent _levelCompleteEvent;
        [SerializeField] private LevelFailEvent _levelFailEvent;

        private readonly Dictionary<EventChannelType, string> _eventNames = new();

        private void Awake()
        {
            if (_config == null) return;

            foreach (var tracked in _config.TrackedEvents)
            {
                if (tracked != null)
                {
                    _eventNames[tracked.ChannelType] = tracked.EventName;
                }
            }
        }

        private void OnEnable()
        {
            if (_levelStartEvent != null)
                _levelStartEvent.Subscribe(OnLevelStart);
            if (_levelCompleteEvent != null)
                _levelCompleteEvent.Subscribe(OnLevelComplete);
            if (_levelFailEvent != null)
                _levelFailEvent.Subscribe(OnLevelFail);
        }

        private void OnDisable()
        {
            if (_levelStartEvent != null)
                _levelStartEvent.Unsubscribe(OnLevelStart);
            if (_levelCompleteEvent != null)
                _levelCompleteEvent.Unsubscribe(OnLevelComplete);
            if (_levelFailEvent != null)
                _levelFailEvent.Unsubscribe(OnLevelFail);
        }

        private void OnLevelStart(LevelEventData data)
        {
            string eventName = GetEventName(EventChannelType.LevelStart);
            LogEvent(eventName, new Dictionary<string, object>
            {
                { "level_index", data.LevelIndex },
                { "level_type", data.LevelType },
                { "shields_remaining", data.ShieldsRemaining }
            });
        }

        private void OnLevelComplete(LevelCompleteData data)
        {
            string eventName = GetEventName(EventChannelType.LevelComplete);
            LogEvent(eventName, new Dictionary<string, object>
            {
                { "level_index", data.LevelIndex },
                { "level_type", data.LevelType },
                { "stars_earned", data.StarsEarned },
                { "shields_remaining", data.ShieldsRemaining },
                { "is_new_best", data.IsNewBest }
            });
        }

        private void OnLevelFail(LevelFailData data)
        {
            string eventName = GetEventName(EventChannelType.LevelFail);
            LogEvent(eventName, new Dictionary<string, object>
            {
                { "level_index", data.LevelIndex },
                { "level_type", data.LevelType },
                { "fail_reason", data.FailReason },
                { "shields_remaining", data.ShieldsRemaining }
            });
        }

        private string GetEventName(EventChannelType channelType)
        {
            return _eventNames.TryGetValue(channelType, out string name) ? name : channelType.ToString();
        }

        /// <summary>
        /// Dispatches an analytics event. Currently logs to console.
        /// Replace this method body to integrate Firebase/Unity Analytics.
        /// </summary>
        private void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            string paramStr = "";
            foreach (var kvp in parameters)
            {
                paramStr += $"  {kvp.Key}: {kvp.Value}\n";
            }
            Debug.Log($"[Analytics] {eventName}\n{paramStr}");
        }
    }
}
```

- [ ] **Step 4: Verify compilation**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Events/AnalyticsEventConfig.cs Assets/Scripts/Events/AnalyticsConfig.cs Assets/Scripts/Events/AnalyticsService.cs
git commit -m "feat: add data-driven analytics service with SO configs"
```

---

## Task 6: AudioManager

**Files:**
- Create: `Assets/Scripts/Audio/AudioManager.cs`

- [ ] **Step 1: Create AudioManager.cs**

```csharp
// Assets/Scripts/Audio/AudioManager.cs
using UnityEngine;

namespace GravityRocket.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioClip _backgroundMusic;

        private AudioSource _audioSource;
        private bool _isMuted;
        private int _volume = 80;

        private const string PrefKeyEnabled = "MusicEnabled";
        private const string PrefKeyVolume = "MusicVolume";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;

            LoadPrefs();
            ApplySettings();

            if (_backgroundMusic != null)
            {
                _audioSource.clip = _backgroundMusic;
                _audioSource.Play();
            }
        }

        /// <summary>
        /// Sets the volume (0-100). Saves to PlayerPrefs.
        /// </summary>
        public void SetVolume(int volume)
        {
            _volume = Mathf.Clamp(volume, 0, 100);
            ApplySettings();
            SavePrefs();
        }

        /// <summary>
        /// Sets mute state. Saves to PlayerPrefs.
        /// </summary>
        public void SetMuted(bool muted)
        {
            _isMuted = muted;
            ApplySettings();
            SavePrefs();
        }

        /// <summary>
        /// Toggles mute on/off.
        /// </summary>
        public void ToggleMute()
        {
            SetMuted(!_isMuted);
        }

        public bool IsMuted => _isMuted;
        public int Volume => _volume;

        private void ApplySettings()
        {
            if (_audioSource == null) return;
            _audioSource.volume = _isMuted ? 0f : _volume / 100f;
        }

        private void LoadPrefs()
        {
            _isMuted = PlayerPrefs.GetInt(PrefKeyEnabled, 1) == 0;
            _volume = PlayerPrefs.GetInt(PrefKeyVolume, 80);
        }

        private void SavePrefs()
        {
            PlayerPrefs.SetInt(PrefKeyEnabled, _isMuted ? 0 : 1);
            PlayerPrefs.SetInt(PrefKeyVolume, _volume);
            PlayerPrefs.Save();
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Audio/AudioManager.cs
git commit -m "feat: add AudioManager with looping music and persistent volume/mute settings"
```

---

## Task 7: Chromatic Aberration Shader + URP Renderer Feature

**Files:**
- Create: `Assets/Shaders/ChromaticAberration.shader`
- Create: `Assets/Scripts/VFX/ChromaticAberrationFeature.cs`
- Create: `Assets/Scripts/VFX/ChromaticAberrationPass.cs`

- [ ] **Step 1: Create ChromaticAberration.shader**

```hlsl
// Assets/Shaders/ChromaticAberration.shader
Shader "GravityRocket/ChromaticAberration"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ChromaticAberration"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float2 _AberrationOffset;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                half r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + _AberrationOffset).r;
                half g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                half b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - _AberrationOffset).b;
                half a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;

                return half4(r, g, b, a);
            }
            ENDHLSL
        }
    }
}
```

- [ ] **Step 2: Create ChromaticAberrationPass.cs**

```csharp
// Assets/Scripts/VFX/ChromaticAberrationPass.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace GravityRocket.VFX
{
    public class ChromaticAberrationPass : ScriptableRenderPass
    {
        private Material _material;
        private static readonly int AberrationOffsetId = Shader.PropertyToID("_AberrationOffset");

        public ChromaticAberrationPass(Material material)
        {
            _material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        private class PassData
        {
            public TextureHandle Source;
            public Material Material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var source = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(source);
            desc.name = "ChromaticAberrationTemp";
            var tempTex = renderGraph.CreateTexture(desc);

            // Blit source -> temp with aberration shader
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("ChromaticAberration_Blit", out var passData))
            {
                passData.Source = source;
                passData.Material = _material;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(tempTex, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    data.Material.SetTexture("_MainTex", data.Source);
                    Blitter.BlitTexture(ctx.cmd, data.Source, new Vector4(1, 1, 0, 0), data.Material, 0);
                });
            }

            // Copy temp back to source
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("ChromaticAberration_CopyBack", out var passData))
            {
                passData.Source = tempTex;

                builder.UseTexture(tempTex, AccessFlags.Read);
                builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.Source, new Vector4(1, 1, 0, 0), 0);
                });
            }
        }
    }
}
```

- [ ] **Step 3: Create ChromaticAberrationFeature.cs**

```csharp
// Assets/Scripts/VFX/ChromaticAberrationFeature.cs
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GravityRocket.VFX
{
    public class ChromaticAberrationFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _shader;

        private Material _material;
        private ChromaticAberrationPass _pass;

        private static readonly int AberrationOffsetId = Shader.PropertyToID("_AberrationOffset");
        private static Vector2 _currentOffset;

        /// <summary>
        /// Call from Rocket script to update the aberration offset each frame.
        /// </summary>
        public static void SetOffset(Vector2 offset)
        {
            _currentOffset = offset;
            Shader.SetGlobalVector(AberrationOffsetId, offset);
        }

        public override void Create()
        {
            if (_shader == null) return;

            _material = CoreUtils.CreateEngineMaterial(_shader);
            _pass = new ChromaticAberrationPass(_material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_material == null || _pass == null) return;
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            if (_material != null)
            {
                CoreUtils.Destroy(_material);
                _material = null;
            }
        }
    }
}
```

- [ ] **Step 4: Verify compilation**

Use `console-get-logs`. Expected: no errors. If there are URP API issues (Unity 6 RenderGraph API may differ), check the specific error and adjust the pass code accordingly.

- [ ] **Step 5: Commit**

```bash
git add Assets/Shaders/ChromaticAberration.shader Assets/Scripts/VFX/ChromaticAberrationFeature.cs Assets/Scripts/VFX/ChromaticAberrationPass.cs
git commit -m "feat: add chromatic aberration URP renderer feature and shader"
```

---

## Task 8: StarField Background

**Files:**
- Create: `Assets/Scripts/VFX/StarField.cs`

- [ ] **Step 1: Create StarField.cs**

```csharp
// Assets/Scripts/VFX/StarField.cs
using UnityEngine;

namespace GravityRocket.VFX
{
    public class StarField : MonoBehaviour
    {
        [SerializeField] private int _starCount = 150;
        [SerializeField] private float _minSize = 0.02f;
        [SerializeField] private float _maxSize = 0.06f;
        [SerializeField] private float _minAlpha = 0.2f;
        [SerializeField] private float _maxAlpha = 0.8f;

        private void Start()
        {
            GenerateStars();
        }

        private void GenerateStars()
        {
            float worldWidth = Core.LevelGenerator.WorldWidth;
            float worldHeight = Core.LevelGenerator.WorldHeight;

            for (int i = 0; i < _starCount; i++)
            {
                var star = new GameObject($"Star_{i}");
                star.transform.SetParent(transform);

                float x = Random.Range(0f, worldWidth);
                float y = Random.Range(0f, worldHeight);
                star.transform.position = new Vector3(x, y, 1f); // z=1 behind gameplay

                var sr = star.AddComponent<SpriteRenderer>();
                sr.sprite = CreatePixelSprite();
                sr.sortingLayerName = "Default";
                sr.sortingOrder = -100;

                float size = Random.Range(_minSize, _maxSize);
                star.transform.localScale = new Vector3(size, size, 1f);

                float alpha = Random.Range(_minAlpha, _maxAlpha);
                sr.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private Sprite _cachedPixelSprite;

        private Sprite CreatePixelSprite()
        {
            if (_cachedPixelSprite != null) return _cachedPixelSprite;

            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            _cachedPixelSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _cachedPixelSprite;
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/VFX/StarField.cs
git commit -m "feat: add procedural starfield background"
```

---

## Task 9: GravityField (Planet Component)

**Files:**
- Create: `Assets/Scripts/Gameplay/GravityField.cs`

- [ ] **Step 1: Create GravityField.cs**

```csharp
// Assets/Scripts/Gameplay/GravityField.cs
using UnityEngine;

namespace GravityRocket.Gameplay
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class GravityField : MonoBehaviour
    {
        [SerializeField] private float _mass = 100f;
        [SerializeField] private float _gravityConstant = 15f;

        private Rigidbody2D _rocketRb;
        private Transform _rocketTransform;
        private bool _hasRocket;

        /// <summary>
        /// Configures this planet's properties. Called by GameManager when spawning.
        /// </summary>
        public void Configure(float mass, float radius)
        {
            _mass = mass;

            var collider = GetComponent<CircleCollider2D>();
            if (collider != null)
            {
                collider.radius = radius;
            }

            transform.localScale = Vector3.one * (radius * 2f);
        }

        /// <summary>
        /// Registers the rocket so this planet can apply gravity to it.
        /// </summary>
        public void SetRocket(Rigidbody2D rocketRb)
        {
            _rocketRb = rocketRb;
            _rocketTransform = rocketRb != null ? rocketRb.transform : null;
            _hasRocket = rocketRb != null;
        }

        public float Mass => _mass;

        private void FixedUpdate()
        {
            if (!_hasRocket || _rocketRb == null) return;

            Vector2 direction = (Vector2)transform.position - _rocketRb.position;
            float distSq = direction.sqrMagnitude;

            if (distSq < 0.01f) return; // Avoid division by near-zero

            float dist = Mathf.Sqrt(distSq);
            float forceMag = _gravityConstant * _mass / distSq;

            Vector2 force = direction.normalized * forceMag;
            _rocketRb.AddForce(force);
        }

        /// <summary>
        /// Gets the current gravitational acceleration magnitude at the rocket's position.
        /// Used by the chromatic aberration effect.
        /// </summary>
        public Vector2 GetGravityAcceleration()
        {
            if (!_hasRocket || _rocketRb == null) return Vector2.zero;

            Vector2 direction = (Vector2)transform.position - _rocketRb.position;
            float distSq = direction.sqrMagnitude;
            if (distSq < 0.01f) return Vector2.zero;

            float dist = Mathf.Sqrt(distSq);
            float forceMag = _gravityConstant * _mass / distSq;
            return direction.normalized * forceMag;
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Gameplay/GravityField.cs
git commit -m "feat: add GravityField planet component with inverse-square gravity"
```

---

## Task 10: GoalZone + GoalZoneRenderer

**Files:**
- Create: `Assets/Scripts/Gameplay/GoalZone.cs`
- Create: `Assets/Scripts/Gameplay/GoalZoneRenderer.cs`

- [ ] **Step 1: Create GoalZone.cs**

```csharp
// Assets/Scripts/Gameplay/GoalZone.cs
using UnityEngine;

namespace GravityRocket.Gameplay
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class GoalZone : MonoBehaviour
    {
        private float _goalWidth;
        private float _goalHeight = 0.75f;
        private System.Action<float> _onRocketArrived;

        /// <summary>
        /// Configures the goal zone position and size.
        /// </summary>
        public void Configure(Vector2 position, float width, System.Action<float> onRocketArrived)
        {
            _goalWidth = width;
            _onRocketArrived = onRocketArrived;

            transform.position = new Vector3(position.x + width / 2f, position.y + _goalHeight / 2f, 0f);

            var collider = GetComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(width, _goalHeight);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Rocket")) return;

            // Calculate hit ratio: 0.0 = left edge, 1.0 = right edge
            float localX = other.transform.position.x - (transform.position.x - _goalWidth / 2f);
            float hitRatio = Mathf.Clamp01(localX / _goalWidth);

            _onRocketArrived?.Invoke(hitRatio);
        }
    }
}
```

- [ ] **Step 2: Create GoalZoneRenderer.cs**

```csharp
// Assets/Scripts/Gameplay/GoalZoneRenderer.cs
using UnityEngine;

namespace GravityRocket.Gameplay
{
    public class GoalZoneRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer _outerBorder;
        [SerializeField] private LineRenderer _centerZone;
        [SerializeField] private SpriteRenderer _centerFill;

        private float _goalWidth;
        private float _goalHeight = 0.75f;
        private Vector2 _goalPosition;

        /// <summary>
        /// Sets up the visual representation of the goal zone.
        /// </summary>
        public void Configure(Vector2 position, float width)
        {
            _goalPosition = position;
            _goalWidth = width;

            SetupOuterBorder();
            SetupCenterZone();
            SetupCenterFill();
        }

        private void SetupOuterBorder()
        {
            if (_outerBorder == null) return;

            _outerBorder.positionCount = 5;
            _outerBorder.loop = false;
            _outerBorder.startWidth = 0.03f;
            _outerBorder.endWidth = 0.03f;
            _outerBorder.startColor = Color.white;
            _outerBorder.endColor = Color.white;
            _outerBorder.useWorldSpace = true;

            float x = _goalPosition.x;
            float y = _goalPosition.y;
            _outerBorder.SetPositions(new Vector3[]
            {
                new(x, y, 0),
                new(x + _goalWidth, y, 0),
                new(x + _goalWidth, y + _goalHeight, 0),
                new(x, y + _goalHeight, 0),
                new(x, y, 0)
            });
        }

        private void SetupCenterZone()
        {
            if (_centerZone == null) return;

            float centerX = _goalPosition.x + _goalWidth * 0.4f;
            float centerW = _goalWidth * 0.2f;
            float y = _goalPosition.y;

            _centerZone.positionCount = 5;
            _centerZone.loop = false;
            _centerZone.startWidth = 0.05f;
            _centerZone.endWidth = 0.05f;
            _centerZone.startColor = Color.white;
            _centerZone.endColor = Color.white;
            _centerZone.useWorldSpace = true;

            _centerZone.SetPositions(new Vector3[]
            {
                new(centerX, y, 0),
                new(centerX + centerW, y, 0),
                new(centerX + centerW, y + _goalHeight, 0),
                new(centerX, y + _goalHeight, 0),
                new(centerX, y, 0)
            });
        }

        private void SetupCenterFill()
        {
            if (_centerFill == null) return;

            float centerX = _goalPosition.x + _goalWidth * 0.5f;
            float centerY = _goalPosition.y + _goalHeight / 2f;
            _centerFill.transform.position = new Vector3(centerX, centerY, 0);
            _centerFill.transform.localScale = new Vector3(_goalWidth * 0.2f, _goalHeight, 1f);
        }

        private void Update()
        {
            // Pulse the center fill alpha
            if (_centerFill != null)
            {
                float pulse = (Mathf.Sin(Time.time * 5f) + 1f) * 0.15f;
                _centerFill.color = new Color(1f, 1f, 1f, pulse);
            }
        }
    }
}
```

- [ ] **Step 3: Verify compilation**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Gameplay/GoalZone.cs Assets/Scripts/Gameplay/GoalZoneRenderer.cs
git commit -m "feat: add GoalZone with hit ratio detection and visual renderer"
```

---

## Task 11: Rocket Particles + Shockwave

**Files:**
- Create: `Assets/Scripts/VFX/RocketParticles.cs`
- Create: `Assets/Scripts/VFX/ShockwavePulse.cs`

- [ ] **Step 1: Create RocketParticles.cs**

```csharp
// Assets/Scripts/VFX/RocketParticles.cs
using UnityEngine;

namespace GravityRocket.VFX
{
    public class RocketParticles : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _exhaustSystem;

        /// <summary>
        /// Enables or disables the exhaust particle emission.
        /// </summary>
        public void SetExhaustActive(bool active)
        {
            if (_exhaustSystem == null) return;

            var emission = _exhaustSystem.emission;
            emission.enabled = active;

            if (active && !_exhaustSystem.isPlaying)
                _exhaustSystem.Play();
        }

        /// <summary>
        /// Triggers an explosion burst at the rocket's current position.
        /// </summary>
        public void Explode()
        {
            if (_exhaustSystem == null) return;

            // Stop normal exhaust
            var emission = _exhaustSystem.emission;
            emission.enabled = false;

            // Emit burst with radial velocity
            var burstParams = new ParticleSystem.EmitParams();
            for (int i = 0; i < 40; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(2f, 7f);
                burstParams.velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
                burstParams.startLifetime = Random.Range(0.5f, 1.0f);
                burstParams.position = transform.position;
                _exhaustSystem.Emit(burstParams, 1);
            }
        }

        /// <summary>
        /// Stops all particles and clears them.
        /// </summary>
        public void Clear()
        {
            if (_exhaustSystem == null) return;
            _exhaustSystem.Stop();
            _exhaustSystem.Clear();
        }
    }
}
```

- [ ] **Step 2: Create ShockwavePulse.cs**

```csharp
// Assets/Scripts/VFX/ShockwavePulse.cs
using System.Collections;
using UnityEngine;

namespace GravityRocket.VFX
{
    public class ShockwavePulse : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _ringRenderer;
        [SerializeField] private float _startScale = 0.5f;
        [SerializeField] private float _endScale = 5f;
        [SerializeField] private float _duration = 1f;

        /// <summary>
        /// Spawns two staggered shockwave pulses at the given position.
        /// </summary>
        public void TriggerPulse(Vector2 position)
        {
            StartCoroutine(PulseSequence(position));
        }

        private IEnumerator PulseSequence(Vector2 position)
        {
            StartCoroutine(AnimatePulse(position));
            yield return new WaitForSeconds(0.15f);
            StartCoroutine(AnimatePulse(position));
        }

        private IEnumerator AnimatePulse(Vector2 position)
        {
            if (_ringRenderer == null) yield break;

            // Create a temporary copy for this pulse
            var pulseObj = new GameObject("Pulse");
            pulseObj.transform.position = new Vector3(position.x, position.y, 0f);

            var sr = pulseObj.AddComponent<SpriteRenderer>();
            sr.sprite = _ringRenderer.sprite;
            sr.sortingOrder = _ringRenderer.sortingOrder;

            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _duration;

                float scale = Mathf.Lerp(_startScale, _endScale, t);
                pulseObj.transform.localScale = new Vector3(scale, scale, 1f);

                float alpha = 1f - t;
                sr.color = new Color(1f, 1f, 1f, alpha);

                yield return null;
            }

            Destroy(pulseObj);
        }
    }
}
```

- [ ] **Step 3: Verify compilation**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/VFX/RocketParticles.cs Assets/Scripts/VFX/ShockwavePulse.cs
git commit -m "feat: add rocket exhaust/explosion particles and shockwave pulse"
```

---

## Task 12: Rocket Script

**Files:**
- Create: `Assets/Scripts/Gameplay/Rocket.cs`

- [ ] **Step 1: Create Rocket.cs**

```csharp
// Assets/Scripts/Gameplay/Rocket.cs
using UnityEngine;
using GravityRocket.VFX;

namespace GravityRocket.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Rocket : MonoBehaviour
    {
        [SerializeField] private RocketParticles _particles;
        [SerializeField] private TrailRenderer _trail;
        [SerializeField] private float _launchSpeed = 9f;
        [SerializeField] private float _aberrationMultiplier = 0.003f;

        private Rigidbody2D _rb;
        private CircleCollider2D _collider;
        private bool _isFlying;
        private bool _isWarping;
        private int _warpTimer;
        private float _warpX;
        private Vector2 _warpTarget;

        // Bounds
        private const float BoundsPadding = 2f;

        private System.Action<string> _onOutOfBounds;
        private System.Action _onPlanetCrash;

        public Rigidbody2D Rb => _rb;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();

            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        /// <summary>
        /// Resets the rocket to starting position for a new level attempt.
        /// </summary>
        public void ResetToPosition(Vector2 position, System.Action<string> onOutOfBounds, System.Action onPlanetCrash)
        {
            _onOutOfBounds = onOutOfBounds;
            _onPlanetCrash = onPlanetCrash;

            transform.position = new Vector3(position.x, position.y, 0f);
            transform.rotation = Quaternion.identity;

            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.bodyType = RigidbodyType2D.Kinematic;

            _isFlying = false;
            _isWarping = false;
            _warpTimer = 0;

            if (_trail != null)
            {
                _trail.Clear();
                _trail.emitting = false;
            }

            if (_particles != null)
            {
                _particles.Clear();
                _particles.SetExhaustActive(false);
            }

            ChromaticAberrationFeature.SetOffset(Vector2.zero);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Launches the rocket in the given direction.
        /// </summary>
        public void Launch(Vector2 direction)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = direction.normalized * _launchSpeed;
            _isFlying = true;

            if (_trail != null) _trail.emitting = true;
            if (_particles != null) _particles.SetExhaustActive(true);
        }

        /// <summary>
        /// Starts the warp/docking animation after hitting the goal.
        /// </summary>
        public void StartWarp(Vector2 goalCenter)
        {
            _isFlying = false;
            _isWarping = true;
            _warpTimer = 0;
            _warpX = transform.position.x;
            _warpTarget = goalCenter;

            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
        }

        /// <summary>
        /// Triggers crash explosion and disables the rocket.
        /// </summary>
        public void Crash()
        {
            _isFlying = false;
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;

            if (_particles != null) _particles.Explode();
            if (_trail != null) _trail.emitting = false;

            // Hide rocket sprite but keep particles visible
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }

        private void Update()
        {
            if (_isFlying)
            {
                UpdateRotation();
                UpdateAberration();
                CheckBounds();
            }
            else if (_isWarping)
            {
                UpdateWarp();
            }
        }

        private void UpdateRotation()
        {
            if (_rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        private void UpdateAberration()
        {
            // Calculate total gravitational acceleration for aberration effect
            var fields = FindObjectsByType<GravityField>(FindObjectsSortMode.None);
            Vector2 totalAccel = Vector2.zero;
            foreach (var field in fields)
            {
                totalAccel += field.GetGravityAcceleration();
            }

            Vector2 offset = totalAccel * _aberrationMultiplier;
            ChromaticAberrationFeature.SetOffset(offset);
        }

        private void CheckBounds()
        {
            float worldW = Core.LevelGenerator.WorldWidth;
            float worldH = Core.LevelGenerator.WorldHeight;
            Vector2 pos = transform.position;

            if (pos.x < -BoundsPadding || pos.x > worldW + BoundsPadding ||
                pos.y < -BoundsPadding || pos.y > worldH + BoundsPadding)
            {
                _isFlying = false;
                _onOutOfBounds?.Invoke("out_of_bounds");
            }
        }

        private void UpdateWarp()
        {
            _warpTimer++;

            if (_warpTimer < 30)
            {
                // Phase 1: Align to goal center
                Vector3 pos = transform.position;
                pos.x += (_warpX - pos.x) * 0.15f;
                pos.y += (_warpTarget.y - pos.y) * 0.15f;
                transform.position = pos;

                // Rotate toward up
                float currentAngle = transform.eulerAngles.z;
                float targetAngle = 90f; // pointing up
                float da = Mathf.DeltaAngle(currentAngle, targetAngle);
                transform.rotation = Quaternion.Euler(0, 0, currentAngle + da * 0.15f);

                // Reduce aberration
                Vector2 currentOffset = Vector2.Lerp(
                    ChromaticAberrationFeature.GetOffset(),
                    Vector2.zero,
                    0.06f
                );
                ChromaticAberrationFeature.SetOffset(currentOffset);
            }
            else
            {
                // Phase 2: Warp upward
                Vector3 pos = transform.position;
                pos.y += (_warpTimer - 20) * 1.5f * Time.deltaTime * 60f;
                transform.position = pos;

                // Increase aberration for dramatic warp effect
                ChromaticAberrationFeature.SetOffset(new Vector2(0f, _warpTimer * 0.0005f));
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_isFlying) return;

            if (collision.gameObject.GetComponent<GravityField>() != null)
            {
                _isFlying = false;
                _onPlanetCrash?.Invoke();
            }
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Use `console-get-logs`. Expected: no errors. Note: `ChromaticAberrationFeature.GetOffset()` is referenced but not yet defined — we need to add it.

- [ ] **Step 3: Add GetOffset to ChromaticAberrationFeature.cs**

Add this method to `ChromaticAberrationFeature.cs` alongside the existing `SetOffset`:

```csharp
        public static Vector2 GetOffset()
        {
            return _currentOffset;
        }
```

- [ ] **Step 4: Verify compilation again**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Gameplay/Rocket.cs Assets/Scripts/VFX/ChromaticAberrationFeature.cs
git commit -m "feat: add Rocket with physics, warp animation, and chromatic aberration integration"
```

---

## Task 13: AimController (Touch/Mouse Input)

**Files:**
- Create: `Assets/Scripts/Gameplay/AimController.cs`

- [ ] **Step 1: Create AimController.cs**

```csharp
// Assets/Scripts/Gameplay/AimController.cs
using UnityEngine;
using UnityEngine.InputSystem;

namespace GravityRocket.Gameplay
{
    public class AimController : MonoBehaviour
    {
        [SerializeField] private LineRenderer _aimLine;
        [SerializeField] private float _aimLineMaxLength = 5f;

        private Camera _mainCamera;
        private bool _isDragging;
        private Vector2 _dragWorldPos;
        private bool _aimingEnabled;

        private Rocket _rocket;

        private System.Action<Vector2> _onLaunch;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _rocket = GetComponent<Rocket>();
        }

        /// <summary>
        /// Enables aiming mode. Call when level is ready for player input.
        /// </summary>
        public void EnableAiming(System.Action<Vector2> onLaunch)
        {
            _onLaunch = onLaunch;
            _aimingEnabled = true;
            _isDragging = false;

            if (_aimLine != null)
            {
                _aimLine.positionCount = 0;
                _aimLine.enabled = false;
            }
        }

        /// <summary>
        /// Disables aiming mode.
        /// </summary>
        public void DisableAiming()
        {
            _aimingEnabled = false;
            _isDragging = false;

            if (_aimLine != null)
                _aimLine.enabled = false;
        }

        private void Update()
        {
            if (!_aimingEnabled) return;

            // Read pointer state (works for both mouse and touch via Input System)
            bool pointerDown = Mouse.current != null && Mouse.current.leftButton.isPressed;
            Vector2 pointerScreenPos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;

            // Also check touch
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointerDown = true;
                pointerScreenPos = Touchscreen.current.primaryTouch.position.ReadValue();
            }

            if (pointerDown)
            {
                _isDragging = true;
                _dragWorldPos = _mainCamera.ScreenToWorldPoint(pointerScreenPos);
                UpdateAimLine();
            }
            else if (_isDragging)
            {
                // Released — launch
                _isDragging = false;
                if (_aimLine != null) _aimLine.enabled = false;

                Vector2 rocketPos = transform.position;
                Vector2 direction = (_dragWorldPos - rocketPos).normalized;
                _onLaunch?.Invoke(direction);
            }
        }

        private void UpdateAimLine()
        {
            if (_aimLine == null) return;

            Vector2 rocketPos = transform.position;
            Vector2 direction = _dragWorldPos - rocketPos;

            // Clamp aim line length
            if (direction.magnitude > _aimLineMaxLength)
            {
                direction = direction.normalized * _aimLineMaxLength;
            }

            _aimLine.enabled = true;
            _aimLine.positionCount = 2;
            _aimLine.SetPosition(0, new Vector3(rocketPos.x, rocketPos.y, 0));
            _aimLine.SetPosition(1, new Vector3(rocketPos.x + direction.x, rocketPos.y + direction.y, 0));

            // Dashed line style
            _aimLine.startWidth = 0.03f;
            _aimLine.endWidth = 0.03f;
            _aimLine.startColor = new Color(1f, 1f, 1f, 0.4f);
            _aimLine.endColor = new Color(1f, 1f, 1f, 0.4f);
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Gameplay/AimController.cs
git commit -m "feat: add AimController with touch and mouse drag-to-aim input"
```

---

## Task 14: UIManager

**Files:**
- Create: `Assets/Scripts/UI/UIManager.cs`

- [ ] **Step 1: Create UIManager.cs**

```csharp
// Assets/Scripts/UI/UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GravityRocket.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _menuPanel;
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private GameObject _shieldDepletedPanel;
        [SerializeField] private GameObject _levelCompletePanel;
        [SerializeField] private GameObject _victoryPanel;

        [Header("HUD")]
        [SerializeField] private TMP_Text _sectorText;
        [SerializeField] private TMP_Text _starsText;
        [SerializeField] private TMP_Text _shieldText;
        [SerializeField] private TMP_Text _instructionText;
        [SerializeField] private GameObject _hudPanel;

        [Header("Level Complete")]
        [SerializeField] private TMP_Text _levelStarsDisplay;
        [SerializeField] private TMP_Text _totalStarsDisplay;

        [Header("Victory")]
        [SerializeField] private TMP_Text _finalStarsDisplay;

        [Header("Buttons")]
        [SerializeField] private Button _startBtn;
        [SerializeField] private Button _retryBtn;
        [SerializeField] private Button _refillBtn;
        [SerializeField] private Button _retreatBtn;
        [SerializeField] private Button _replayBtn;
        [SerializeField] private Button _nextBtn;
        [SerializeField] private Button _restartBtn;

        [Header("Audio Settings")]
        [SerializeField] private Button _muteToggleBtn;
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private TMP_Text _muteToggleText;

        [Header("Safe Area")]
        [SerializeField] private RectTransform _safeAreaRect;

        // Callbacks
        private System.Action _onStart;
        private System.Action _onRetry;
        private System.Action _onRefill;
        private System.Action _onRetreat;
        private System.Action _onReplay;
        private System.Action _onNext;
        private System.Action _onRestart;

        private void Awake()
        {
            ApplySafeArea();
        }

        /// <summary>
        /// Wires up all button callbacks. Called once by GameManager.
        /// </summary>
        public void Initialize(
            System.Action onStart,
            System.Action onRetry,
            System.Action onRefill,
            System.Action onRetreat,
            System.Action onReplay,
            System.Action onNext,
            System.Action onRestart)
        {
            _onStart = onStart;
            _onRetry = onRetry;
            _onRefill = onRefill;
            _onRetreat = onRetreat;
            _onReplay = onReplay;
            _onNext = onNext;
            _onRestart = onRestart;

            _startBtn.onClick.AddListener(() => _onStart?.Invoke());
            _retryBtn.onClick.AddListener(() => _onRetry?.Invoke());
            _refillBtn.onClick.AddListener(() => _onRefill?.Invoke());
            _retreatBtn.onClick.AddListener(() => _onRetreat?.Invoke());
            _replayBtn.onClick.AddListener(() => _onReplay?.Invoke());
            _nextBtn.onClick.AddListener(() => _onNext?.Invoke());
            _restartBtn.onClick.AddListener(() => _onRestart?.Invoke());

            // Audio controls
            if (_muteToggleBtn != null)
                _muteToggleBtn.onClick.AddListener(OnMuteToggle);
            if (_volumeSlider != null)
            {
                _volumeSlider.minValue = 0;
                _volumeSlider.maxValue = 100;
                _volumeSlider.wholeNumbers = true;
                _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

                // Initialize from AudioManager
                if (Audio.AudioManager.Instance != null)
                {
                    _volumeSlider.value = Audio.AudioManager.Instance.Volume;
                    UpdateMuteButtonText();
                }
            }
        }

        private void OnMuteToggle()
        {
            if (Audio.AudioManager.Instance == null) return;
            Audio.AudioManager.Instance.ToggleMute();
            UpdateMuteButtonText();
        }

        private void OnVolumeChanged(float value)
        {
            if (Audio.AudioManager.Instance == null) return;
            Audio.AudioManager.Instance.SetVolume((int)value);
        }

        private void UpdateMuteButtonText()
        {
            if (_muteToggleText == null || Audio.AudioManager.Instance == null) return;
            _muteToggleText.text = Audio.AudioManager.Instance.IsMuted ? "SOUND: OFF" : "SOUND: ON";
        }

        public void ShowMenu()
        {
            HideAllPanels();
            _menuPanel.SetActive(true);
            _hudPanel.SetActive(false);
        }

        public void ShowAiming(int level, int totalStars, int shields, int maxShields)
        {
            HideAllPanels();
            _hudPanel.SetActive(true);
            UpdateHUD(level, totalStars, shields, maxShields);
            ShowInstruction("DRAG TO PLOT TRAJECTORY");
        }

        public void ShowGameOver()
        {
            _gameOverPanel.SetActive(true);
            HideInstruction();
        }

        public void ShowShieldDepleted()
        {
            _shieldDepletedPanel.SetActive(true);
            HideInstruction();
        }

        public void ShowLevelComplete(int starsEarned, int totalStars)
        {
            HideInstruction();

            string starText = "";
            for (int i = 0; i < 3; i++)
                starText += (i < starsEarned) ? "\u2605" : "\u2606"; // ★ or ☆
            _levelStarsDisplay.text = starText;
            _totalStarsDisplay.text = $"Total Stars: {totalStars} / 150";

            _levelCompletePanel.SetActive(true);
        }

        public void ShowVictory(int totalStars)
        {
            HideAllPanels();
            _hudPanel.SetActive(false);
            _finalStarsDisplay.text = $"Total Stars: {totalStars} / 150";
            _victoryPanel.SetActive(true);
        }

        public void UpdateHUD(int level, int totalStars, int shields, int maxShields)
        {
            _sectorText.text = $"Sector {level + 1}/50";
            _starsText.text = $"\u2605 {totalStars}";

            string shieldStr = "";
            for (int i = 0; i < maxShields; i++)
                shieldStr += (i < shields) ? "\u2588" : "\u2592"; // █ or ▒
            _shieldText.text = $"SHIELD: {shieldStr}";

            if (shields > 2)
                _shieldText.color = Color.white;
            else if (shields > 0)
                _shieldText.color = new Color(0.67f, 0.67f, 0.67f);
            else
                _shieldText.color = new Color(0.33f, 0.33f, 0.33f);
        }

        public void HideInstruction()
        {
            if (_instructionText != null)
                _instructionText.gameObject.SetActive(false);
        }

        private void ShowInstruction(string text)
        {
            if (_instructionText != null)
            {
                _instructionText.text = text;
                _instructionText.gameObject.SetActive(true);
            }
        }

        public void HideAllPanels()
        {
            _menuPanel.SetActive(false);
            _gameOverPanel.SetActive(false);
            _shieldDepletedPanel.SetActive(false);
            _levelCompletePanel.SetActive(false);
            _victoryPanel.SetActive(false);
        }

        private void ApplySafeArea()
        {
            if (_safeAreaRect == null) return;

            var safeArea = Screen.safeArea;
            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _safeAreaRect.anchorMin = anchorMin;
            _safeAreaRect.anchorMax = anchorMax;
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Use `console-get-logs`. Expected: no errors. If TextMeshPro namespace is not found, ensure `com.unity.textmeshpro` is installed (it ships with Unity 6 by default).

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/UIManager.cs
git commit -m "feat: add UIManager with all panels, HUD, safe area, and audio controls"
```

---

## Task 15: GameManager

**Files:**
- Create: `Assets/Scripts/Core/GameManager.cs`

- [ ] **Step 1: Create GameManager.cs**

```csharp
// Assets/Scripts/Core/GameManager.cs
using UnityEngine;
using GravityRocket.Gameplay;
using GravityRocket.Events;
using GravityRocket.UI;
using GravityRocket.VFX;

namespace GravityRocket.Core
{
    public enum GameState
    {
        Menu,
        Aiming,
        Flying,
        LevelComplete,
        GameOver,
        ShieldDepleted,
        Victory
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private Rocket _rocket;
        [SerializeField] private AimController _aimController;
        [SerializeField] private ObjectPool _planetPool;
        [SerializeField] private GoalZone _goalZone;
        [SerializeField] private GoalZoneRenderer _goalZoneRenderer;
        [SerializeField] private ShockwavePulse _shockwavePulse;
        [SerializeField] private Camera _mainCamera;

        [Header("Events")]
        [SerializeField] private LevelStartEvent _levelStartEvent;
        [SerializeField] private LevelCompleteEvent _levelCompleteEvent;
        [SerializeField] private LevelFailEvent _levelFailEvent;

        [Header("Settings")]
        [SerializeField] private int _maxShields = 5;

        private GameState _state = GameState.Menu;
        private LevelData[] _levels;
        private LevelData _currentLevelData;
        private int _currentLevel;
        private int _currentShields;
        private int[] _levelStars;
        private GravityField[] _activePlanets;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _levels = LevelGenerator.GenerateAllLevels();
            _levelStars = new int[LevelGenerator.TotalLevels];
            _currentShields = _maxShields;

            LoadProgress();
            SetupCamera();
        }

        private void Start()
        {
            _uiManager.Initialize(
                onStart: OnStartPressed,
                onRetry: OnRetryPressed,
                onRefill: OnRefillPressed,
                onRetreat: OnRetreatPressed,
                onReplay: OnReplayPressed,
                onNext: OnNextPressed,
                onRestart: OnRestartPressed
            );

            _uiManager.ShowMenu();
        }

        private void SetupCamera()
        {
            if (_mainCamera == null) return;

            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = LevelGenerator.WorldHeight / 2f;
            _mainCamera.transform.position = new Vector3(
                LevelGenerator.WorldWidth / 2f,
                LevelGenerator.WorldHeight / 2f,
                -10f
            );

            // Ensure full play area width is visible on narrow screens
            float aspect = (float)Screen.width / Screen.height;
            float requiredOrthoSize = LevelGenerator.WorldWidth / (2f * aspect);
            if (requiredOrthoSize > _mainCamera.orthographicSize)
            {
                _mainCamera.orthographicSize = requiredOrthoSize;
                // Re-center vertically
                _mainCamera.transform.position = new Vector3(
                    LevelGenerator.WorldWidth / 2f,
                    _mainCamera.orthographicSize,
                    -10f
                );
            }
        }

        // --- State Transitions ---

        private void LoadLevel(int index)
        {
            _currentLevel = index;
            _currentLevelData = _levels[_currentLevel];

            // Return all active planets to pool
            if (_planetPool != null)
                _planetPool.ReturnAll();

            // Spawn planets
            _activePlanets = new GravityField[_currentLevelData.Planets.Length];
            for (int i = 0; i < _currentLevelData.Planets.Length; i++)
            {
                var config = _currentLevelData.Planets[i];
                var planetObj = _planetPool.Get();
                planetObj.transform.position = new Vector3(config.Position.x, config.Position.y, 0f);

                var gravityField = planetObj.GetComponent<GravityField>();
                if (gravityField != null)
                {
                    gravityField.Configure(config.Mass, config.Radius);
                    gravityField.SetRocket(_rocket.Rb);
                }

                _activePlanets[i] = gravityField;
            }

            // Setup goal zone
            if (_goalZone != null)
            {
                _goalZone.gameObject.SetActive(true);
                _goalZone.Configure(_currentLevelData.GoalPosition, _currentLevelData.GoalWidth, OnGoalReached);
            }
            if (_goalZoneRenderer != null)
            {
                _goalZoneRenderer.Configure(_currentLevelData.GoalPosition, _currentLevelData.GoalWidth);
            }

            // Setup rocket
            _rocket.ResetToPosition(
                _currentLevelData.StartPosition,
                onOutOfBounds: OnRocketOutOfBounds,
                onPlanetCrash: OnRocketCrash
            );

            // Enable aiming
            _aimController.EnableAiming(OnLaunch);

            _state = GameState.Aiming;

            int totalStars = GetTotalStars();
            _uiManager.ShowAiming(_currentLevel, totalStars, _currentShields, _maxShields);

            // Fire analytics event
            if (_levelStartEvent != null)
            {
                _levelStartEvent.Raise(new LevelEventData
                {
                    LevelIndex = _currentLevel,
                    LevelType = _currentLevelData.LayoutType,
                    ShieldsRemaining = _currentShields
                });
            }
        }

        private void OnLaunch(Vector2 direction)
        {
            _state = GameState.Flying;
            _aimController.DisableAiming();
            _rocket.Launch(direction);
            _uiManager.HideInstruction();
        }

        private void OnGoalReached(float hitRatio)
        {
            if (_state != GameState.Flying) return;

            _state = GameState.LevelComplete;
            _aimController.DisableAiming();

            // Calculate stars
            int starsEarned;
            if (hitRatio > 0.4f && hitRatio < 0.6f)
                starsEarned = 3;
            else if (hitRatio > 0.2f && hitRatio < 0.8f)
                starsEarned = 2;
            else
                starsEarned = 1;

            bool isNewBest = starsEarned > _levelStars[_currentLevel];
            _levelStars[_currentLevel] = Mathf.Max(_levelStars[_currentLevel], starsEarned);

            // Recover shield
            _currentShields = Mathf.Min(_currentShields + 1, _maxShields);

            int totalStars = GetTotalStars();
            _uiManager.UpdateHUD(_currentLevel, totalStars, _currentShields, _maxShields);

            // Warp animation
            float goalCenterY = _currentLevelData.GoalPosition.y + 0.375f; // goalHeight/2
            _rocket.StartWarp(new Vector2(_rocket.transform.position.x, goalCenterY));

            // Shockwave
            if (_shockwavePulse != null)
            {
                _shockwavePulse.TriggerPulse(new Vector2(_rocket.transform.position.x, goalCenterY));
            }

            SaveProgress();

            // Show UI after warp delay
            Invoke(nameof(ShowLevelCompleteUI), 1.5f);

            // Store for delayed UI
            _pendingStars = starsEarned;
            _pendingTotalStars = totalStars;
            _pendingIsNewBest = isNewBest;

            // Fire analytics event
            if (_levelCompleteEvent != null)
            {
                _levelCompleteEvent.Raise(new LevelCompleteData
                {
                    LevelIndex = _currentLevel,
                    LevelType = _currentLevelData.LayoutType,
                    StarsEarned = starsEarned,
                    ShieldsRemaining = _currentShields,
                    IsNewBest = isNewBest
                });
            }
        }

        private int _pendingStars;
        private int _pendingTotalStars;
        private bool _pendingIsNewBest;

        private void ShowLevelCompleteUI()
        {
            if (_currentLevel >= LevelGenerator.TotalLevels - 1)
            {
                _uiManager.ShowVictory(_pendingTotalStars);
                _state = GameState.Victory;
            }
            else
            {
                _uiManager.ShowLevelComplete(_pendingStars, _pendingTotalStars);
            }
        }

        private void OnRocketCrash()
        {
            HandleFailure("crash", true);
        }

        private void OnRocketOutOfBounds(string reason)
        {
            HandleFailure(reason, false);
        }

        private void HandleFailure(string reason, bool crashed)
        {
            if (_state != GameState.Flying) return;

            _state = GameState.GameOver;
            _aimController.DisableAiming();

            if (crashed)
                _rocket.Crash();

            _currentShields--;
            _uiManager.UpdateHUD(_currentLevel, GetTotalStars(), _currentShields, _maxShields);

            SaveProgress();

            float delay = crashed ? 0.8f : 0.2f;

            if (_currentShields > 0)
            {
                Invoke(nameof(ShowGameOverUI), delay);
            }
            else
            {
                _state = GameState.ShieldDepleted;
                Invoke(nameof(ShowShieldDepletedUI), delay);
            }

            // Fire analytics event
            if (_levelFailEvent != null)
            {
                _levelFailEvent.Raise(new LevelFailData
                {
                    LevelIndex = _currentLevel,
                    LevelType = _currentLevelData.LayoutType,
                    FailReason = reason,
                    ShieldsRemaining = _currentShields
                });
            }
        }

        private void ShowGameOverUI() => _uiManager.ShowGameOver();
        private void ShowShieldDepletedUI() => _uiManager.ShowShieldDepleted();

        // --- Button Handlers ---

        private void OnStartPressed()
        {
            _currentShields = _maxShields;
            LoadLevel(0);
        }

        private void OnRetryPressed() => LoadLevel(_currentLevel);
        private void OnReplayPressed() => LoadLevel(_currentLevel);
        private void OnNextPressed() => LoadLevel(_currentLevel + 1);

        private void OnRefillPressed()
        {
            _currentShields = _maxShields;
            LoadLevel(_currentLevel);
        }

        private void OnRetreatPressed()
        {
            _currentShields = _maxShields;
            LoadLevel(Mathf.Max(0, _currentLevel - 1));
        }

        private void OnRestartPressed()
        {
            _levelStars = new int[LevelGenerator.TotalLevels];
            _currentShields = _maxShields;
            SaveProgress();
            LoadLevel(0);
        }

        // --- Persistence ---

        private int GetTotalStars()
        {
            int total = 0;
            foreach (int s in _levelStars) total += s;
            return total;
        }

        private void SaveProgress()
        {
            for (int i = 0; i < _levelStars.Length; i++)
                PlayerPrefs.SetInt($"Stars_{i}", _levelStars[i]);
            PlayerPrefs.SetInt("CurrentLevel", _currentLevel);
            PlayerPrefs.SetInt("Shields", _currentShields);
            PlayerPrefs.Save();
        }

        private void LoadProgress()
        {
            for (int i = 0; i < _levelStars.Length; i++)
                _levelStars[i] = PlayerPrefs.GetInt($"Stars_{i}", 0);
            _currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
            _currentShields = PlayerPrefs.GetInt("Shields", _maxShields);
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/GameManager.cs
git commit -m "feat: add GameManager with full state machine, level lifecycle, and persistence"
```

---

## Task 16: Create Scene and Wire Up GameObjects (via MCP)

This task uses Unity MCP tools to create the game scene and set up all GameObjects, prefabs, and component references.

- [ ] **Step 1: Create a new game scene**

Use MCP `scene-create` to create `Assets/Scenes/GameScene.unity`. Then use `scene-open` to open it.

- [ ] **Step 2: Set up Main Camera**

Use `gameobject-find` to find "Main Camera". Then use `gameobject-component-modify` to set the Camera component:
- `orthographic: true`
- `orthographicSize: 10`
- `backgroundColor: black (0,0,0,1)`

Use `gameobject-modify` to set position to `(5, 10, -10)`.

- [ ] **Step 3: Create AudioManager GameObject**

Use `gameobject-create` to create "AudioManager". Use `gameobject-component-add` to add `GravityRocket.Audio.AudioManager`. Then use `gameobject-component-modify` to set `_backgroundMusic` to the audio clip at `Assets/BackgroundMusic/PPK - Resurrection full versionmp4.mp3`.

- [ ] **Step 4: Create StarField GameObject**

Use `gameobject-create` to create "StarField". Use `gameobject-component-add` to add `GravityRocket.VFX.StarField`.

- [ ] **Step 5: Create Rocket prefab**

1. Use `gameobject-create` to create "Rocket" in the scene
2. Add components: `SpriteRenderer`, `Rigidbody2D`, `CircleCollider2D`, `TrailRenderer`, `LineRenderer`, `GravityRocket.Gameplay.Rocket`, `GravityRocket.Gameplay.AimController`
3. Set Rigidbody2D: `gravityScale: 0`, `collisionDetectionMode: Continuous`, `interpolation: Interpolate`
4. Set CircleCollider2D: `radius: 0.2`
5. Set SpriteRenderer to use a white circle sprite (use `assets-find-built-in` to find "Circle" sprite)
6. Set `transform.localScale` to `(0.4, 0.4, 1)` for proper visual size
7. Set TrailRenderer: `time: 2`, `startWidth: 0.15`, `endWidth: 0.02`, start/end color white with fade
8. Set LineRenderer (for aim line): `startWidth: 0.03`, `endWidth: 0.03`, color white with 0.4 alpha
9. Create child "ExhaustParticles" with a ParticleSystem configured for exhaust (white, small, short lifetime)
10. Add `GravityRocket.VFX.RocketParticles` to the Rocket and wire `_exhaustSystem` to the child
11. Wire `_trail` and `_aimLine` references on Rocket and AimController
12. Tag the Rocket GameObject with tag "Rocket" (create the tag first if needed)
13. Use `assets-prefab-create` to save as `Assets/Prefabs/Rocket.prefab`

- [ ] **Step 6: Create Planet prefab**

1. Use `gameobject-create` to create "Planet"
2. Add: `SpriteRenderer` (circle sprite, white color), `CircleCollider2D`, `GravityRocket.Gameplay.GravityField`
3. Set SpriteRenderer: sprite to built-in Circle, color to black, with white outline material if available, otherwise white
4. Create child "GravityWaveRing" with SpriteRenderer (circle sprite, white, low alpha)
5. Use `assets-prefab-create` to save as `Assets/Prefabs/Planet.prefab`

- [ ] **Step 7: Create GoalZone prefab**

1. Use `gameobject-create` to create "GoalZone"
2. Add: `BoxCollider2D` (isTrigger: true), `GravityRocket.Gameplay.GoalZone`, `GravityRocket.Gameplay.GoalZoneRenderer`
3. Create child LineRenderers: "OuterBorder", "CenterZone"
4. Create child SpriteRenderer: "CenterFill" (white square sprite, low alpha)
5. Wire the renderer references
6. Use `assets-prefab-create` to save as `Assets/Prefabs/GoalZone.prefab`

- [ ] **Step 8: Create ShockwaveRing prefab**

1. Use `gameobject-create` to create "ShockwaveRing"
2. Add: `SpriteRenderer` (circle sprite, white), `GravityRocket.VFX.ShockwavePulse`
3. Wire `_ringRenderer` reference
4. Use `assets-prefab-create` to save as `Assets/Prefabs/ShockwaveRing.prefab`

- [ ] **Step 9: Create GameManager GameObject**

1. Use `gameobject-create` to create "GameManager"
2. Add `GravityRocket.Core.GameManager` component
3. Add `GravityRocket.Core.ObjectPool` component (set `_prefab` to Planet prefab, `_initialSize: 12`)

- [ ] **Step 10: Create Canvas and UI hierarchy**

1. Create a Canvas with `CanvasScaler` set to Scale With Screen Size, reference 1080x1920, match height
2. Create child panels: MenuPanel, GameOverPanel, ShieldDepletedPanel, LevelCompletePanel, VictoryPanel
3. Each panel: Background image (black, 85% alpha), border, TMP_Text children for headers/content, Button children
4. Create HUD panel anchored top-left with SafeArea RectTransform parent
5. Add TMP_Text elements for sector, stars, shields, instruction
6. Add mute toggle button and volume slider to MenuPanel
7. Add `GravityRocket.UI.UIManager` to the Canvas
8. Wire ALL serialized references on UIManager (panels, texts, buttons, slider)

- [ ] **Step 11: Create AnalyticsService GameObject**

1. Use `gameobject-create` to create "AnalyticsService"
2. Add `GravityRocket.Events.AnalyticsService` component

- [ ] **Step 12: Create ScriptableObject event assets**

Using `script-execute` or the CreateAssetMenu in Unity:
1. Create `Assets/Data/Events/LevelStartEvent.asset` (LevelStartEvent SO)
2. Create `Assets/Data/Events/LevelCompleteEvent.asset` (LevelCompleteEvent SO)
3. Create `Assets/Data/Events/LevelFailEvent.asset` (LevelFailEvent SO)
4. Create `Assets/Data/Analytics/Track_LevelStart.asset` (AnalyticsEventConfig, EventName: "level_start", ChannelType: LevelStart)
5. Create `Assets/Data/Analytics/Track_LevelComplete.asset` (AnalyticsEventConfig, EventName: "level_complete", ChannelType: LevelComplete)
6. Create `Assets/Data/Analytics/Track_LevelFail.asset` (AnalyticsEventConfig, EventName: "level_fail", ChannelType: LevelFail)
7. Create `Assets/Data/Analytics/AnalyticsConfig.asset` (AnalyticsConfig, wire TrackedEvents to the three above)

- [ ] **Step 13: Wire all cross-references**

1. Wire GameManager: `_uiManager`, `_rocket`, `_aimController`, `_planetPool`, `_goalZone`, `_goalZoneRenderer`, `_shockwavePulse`, `_mainCamera`, and all three event SO references
2. Wire AnalyticsService: `_config`, `_levelStartEvent`, `_levelCompleteEvent`, `_levelFailEvent`
3. Wire AimController: `_aimLine` → Rocket's LineRenderer

- [ ] **Step 14: Add ChromaticAberrationFeature to the Renderer**

1. Use `assets-get-data` on `Assets/Settings/Renderer2D.asset`
2. Use `assets-modify` or Unity Editor to add `ChromaticAberrationFeature` to the renderer's feature list
3. Set the `_shader` reference to `Assets/Shaders/ChromaticAberration.shader`

- [ ] **Step 15: Save the scene**

Use `scene-save` to save GameScene.

- [ ] **Step 16: Verify compilation and check console**

Use `console-get-logs`. Expected: no errors.

- [ ] **Step 17: Commit**

```bash
git add Assets/Scenes/GameScene.unity Assets/Prefabs/ Assets/Materials/ Assets/Data/
git commit -m "feat: create game scene with all GameObjects, prefabs, and wired references"
```

---

## Task 17: Integration Testing in Play Mode

- [ ] **Step 1: Enter Play mode**

Use `editor-application-set-state` to enter Play mode. Wait for the scene to load.

- [ ] **Step 2: Take screenshot of the menu**

Use `screenshot-game-view` to capture the menu screen. Verify:
- Black background with white stars
- Menu panel visible with "GRAVITY ROCKET" title and "INITIATE" button
- Music should be playing (not verifiable via screenshot)

- [ ] **Step 3: Check console for runtime errors**

Use `console-get-logs` to verify no runtime exceptions.

- [ ] **Step 4: Test level loading**

Click the "INITIATE" button (or simulate via `script-execute`). Use `screenshot-game-view` to verify:
- Rocket visible at bottom
- Planets visible in the middle area
- Goal zone visible at top
- HUD showing "Sector 1/50"

- [ ] **Step 5: Test aiming and launching**

Simulate a drag-and-launch via `script-execute` if needed. Verify rocket moves and trail renders.

- [ ] **Step 6: Check analytics logs**

Use `console-get-logs` and search for "[Analytics] level_start" to confirm the event fired correctly.

- [ ] **Step 7: Exit Play mode**

Use `editor-application-set-state` to stop Play mode.

- [ ] **Step 8: Fix any issues found during testing**

Address any runtime errors, visual issues, or missing references. Re-test after fixes.

- [ ] **Step 9: Commit fixes**

```bash
git add -A
git commit -m "fix: resolve integration issues from play mode testing"
```

---

## Task 18: Planet Visual Polish (Gravity Wave Ring)

**Files:**
- Create: `Assets/Scripts/VFX/GravityWaveRing.cs`

- [ ] **Step 1: Create GravityWaveRing.cs**

```csharp
// Assets/Scripts/VFX/GravityWaveRing.cs
using UnityEngine;
using GravityRocket.Gameplay;

namespace GravityRocket.VFX
{
    public class GravityWaveRing : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _ringRenderer;
        [SerializeField] private float _waveSpeed = 1f;
        [SerializeField] private float _maxScaleMultiplier = 0.2f;

        private float _baseMass;
        private float _baseScale;

        /// <summary>
        /// Configures the wave ring based on the parent planet's mass.
        /// </summary>
        public void Configure(float planetMass, float planetRadius)
        {
            _baseMass = planetMass;
            _baseScale = planetRadius * 2f;
        }

        private void Update()
        {
            if (_ringRenderer == null) return;

            float wave = (Time.time * _waveSpeed) % 1f;
            float scale = _baseScale + (_baseMass * _maxScaleMultiplier * wave);
            transform.localScale = new Vector3(scale, scale, 1f);

            float alpha = 1f - wave;
            _ringRenderer.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
```

- [ ] **Step 2: Add GravityWaveRing to planet prefab**

Open Planet prefab, add `GravityWaveRing` component to the "GravityWaveRing" child, wire `_ringRenderer`.

- [ ] **Step 3: Update GravityField.Configure to also configure the wave ring**

Add to `GravityField.Configure()`:

```csharp
var waveRing = GetComponentInChildren<GravityWaveRing>();
if (waveRing != null)
{
    waveRing.Configure(mass, radius);
}
```

- [ ] **Step 4: Verify compilation and test**

Use `console-get-logs`. Take a screenshot in play mode to verify the pulsing gravity rings around planets.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/VFX/GravityWaveRing.cs Assets/Scripts/Gameplay/GravityField.cs
git commit -m "feat: add animated gravity wave ring effect on planets"
```

---

## Task 19: Launch Pad Visual Indicator

**Files:**
- Modify: `Assets/Scripts/Gameplay/AimController.cs`

- [ ] **Step 1: Add launch pad circle to AimController**

Add a `[SerializeField] private LineRenderer _launchPadCircle;` field and a method to draw a dashed circle around the start position:

```csharp
        [SerializeField] private LineRenderer _launchPadCircle;

        public void ShowLaunchPad(Vector2 position)
        {
            if (_launchPadCircle == null) return;

            int segments = 32;
            float radius = 0.5f;
            _launchPadCircle.positionCount = segments + 1;
            _launchPadCircle.loop = false;
            _launchPadCircle.startWidth = 0.02f;
            _launchPadCircle.endWidth = 0.02f;
            _launchPadCircle.startColor = new Color(1f, 1f, 1f, 0.3f);
            _launchPadCircle.endColor = new Color(1f, 1f, 1f, 0.3f);
            _launchPadCircle.enabled = true;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = position.x + Mathf.Cos(angle) * radius;
                float y = position.y + Mathf.Sin(angle) * radius;
                _launchPadCircle.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        public void HideLaunchPad()
        {
            if (_launchPadCircle != null)
                _launchPadCircle.enabled = false;
        }
```

- [ ] **Step 2: Call ShowLaunchPad in EnableAiming and HideLaunchPad in DisableAiming**

In `EnableAiming`:
```csharp
ShowLaunchPad(transform.position);
```

In `DisableAiming`:
```csharp
HideLaunchPad();
```

- [ ] **Step 3: Add a second LineRenderer to the Rocket prefab for launch pad**

Use MCP tools to add another LineRenderer to the Rocket prefab and wire it to `_launchPadCircle` on AimController.

- [ ] **Step 4: Verify and commit**

```bash
git add Assets/Scripts/Gameplay/AimController.cs
git commit -m "feat: add visual launch pad indicator circle"
```

---

## Task 20: Final Play Test & Polish

- [ ] **Step 1: Enter Play mode and play through first 3 tutorial levels**

Use `editor-application-set-state` to enter Play mode. Manually test or use `script-execute` to simulate gameplay through levels 0-2.

- [ ] **Step 2: Take screenshots at each game state**

Capture `screenshot-game-view` for:
- Menu screen
- Aiming state (with drag line visible)
- Flying state (rocket mid-flight with trail)
- Level complete (star display)
- Game over (after crashing)

- [ ] **Step 3: Verify analytics events in console**

Use `console-get-logs` and search for "[Analytics]". Verify all three event types fire with correct parameters.

- [ ] **Step 4: Test shield system**

Deliberately crash multiple times to verify shield decrement. Reach 0 shields to verify the "Shields Critical" panel appears with refill and retreat options.

- [ ] **Step 5: Test audio controls**

Verify music plays on start. Test mute toggle and volume slider if UI is set up correctly.

- [ ] **Step 6: Test screen scaling**

Use Unity Game View aspect ratio presets to test different mobile sizes (9:16, 9:19, 9:21). Verify play area and UI scale correctly.

- [ ] **Step 7: Fix any issues found**

Address visual, gameplay, or scaling issues. Re-test after each fix.

- [ ] **Step 8: Final commit**

```bash
git add -A
git commit -m "feat: complete Gravity Rocket initial port with all systems functional"
```

---

## Summary

| Task | Description | Dependencies |
|------|-------------|-------------|
| 1 | Folder structure + data types | None |
| 2 | Level generator | Task 1 |
| 3 | Object pool | None |
| 4 | Event system | None |
| 5 | Analytics service | Task 4 |
| 6 | Audio manager | None |
| 7 | Chromatic aberration shader | None |
| 8 | Star field background | Task 2 (for constants) |
| 9 | Gravity field (planet) | None |
| 10 | Goal zone + renderer | None |
| 11 | Rocket particles + shockwave | None |
| 12 | Rocket script | Tasks 7, 9 |
| 13 | Aim controller (input) | None |
| 14 | UI manager | Task 6 |
| 15 | Game manager | Tasks 2-14 |
| 16 | Scene setup (MCP) | Tasks 1-15 |
| 17 | Integration testing | Task 16 |
| 18 | Gravity wave ring polish | Task 9 |
| 19 | Launch pad visual | Task 13 |
| 20 | Final play test & polish | Tasks 17-19 |

**Parallelizable:** Tasks 1-4, 6-11, 13 can all be created in parallel (no inter-dependencies). Tasks 3-6 are independent of each other.
