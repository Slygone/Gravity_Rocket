# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Gravity_Rocket** is a Unity 6 (6000.3.10f1) 2D game project using:
- **Universal Render Pipeline (URP)** with 2D renderer
- **Input System** (new, not legacy) — input actions defined in `Assets/InputSystem_Actions.inputactions`
- **2D packages**: Animation, Sprite, SpriteShape, Tilemap (+ extras), PSD Importer, Aseprite
- **Physics2D** for 2D physics

## Build & Test Commands

This is a Unity project — there is no CLI build by default. Use Unity Editor for builds and play testing.

```bash
# Run all tests (EditMode + PlayMode) via Unity CLI
"C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe" -runTests -batchmode -projectPath . -testResults results.xml

# Run EditMode tests only
"C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe" -runTests -batchmode -projectPath . -testPlatform EditMode -testResults results.xml

# Run a single test by name
"C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe" -runTests -batchmode -projectPath . -testFilter "TestClassName.TestMethodName" -testResults results.xml
```

## Architecture Notes

- All game code goes in `Assets/` — organize scripts under `Assets/Scripts/`
- Scenes are in `Assets/Scenes/` (currently only `SampleScene.unity`)
- URP settings are in `Assets/Settings/`
- Use the new Input System (`UnityEngine.InputSystem`) — do not use legacy `Input.GetKey`/`Input.GetAxis`
- Target render pipeline is URP 2D — use `Sprite-Lit-Default` or `Sprite-Unlit-Default` materials, not Standard shader
