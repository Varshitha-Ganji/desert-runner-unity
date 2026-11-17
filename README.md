# Desert Runner - 3D Platform Runner for Unity 6

A complete, playable desert platform runner written in C#. **No art assets, prefabs or scene setup needed**:
the level, character, hazards, scenery, sound effects and UI are all generated from code at runtime.

## Quick start

1. Create (or open) a **Unity 6** project using the **3D (URP)** or 3D (Built-in) template.
2. Copy the `Assets/Scripts` folder from this package into your project's `Assets` folder.
3. Open any scene (the default `SampleScene` is fine) and press **Play**.

`GameBootstrap` starts automatically when the scene loads. It creates the sun, sky, fog, camera, player,
level, HUD and game manager. It also disables any existing directional light and reuses the scene's Main Camera.

> **Input:** works with the new Input System package, the legacy Input Manager, or both
> (Project Settings > Player > Active Input Handling). The default Unity 6 3D template already includes the Input System package.

## Controls

| Action | Keys |
|---|---|
| Move left / right (3 smooth lanes) | `A` / `D` or `Left` / `Right` |
| Jump (press again in the air for a double jump) | `Space`, `W` or `Up` |
| Pause / resume | `Esc` or `P` |
| Start / retry | `Space` / `Enter` (or `R` to restart) |

The runner moves forward automatically and gradually speeds up from 10 m/s to 14.5 m/s.

## The course (about 600 m)

1. **Warm-up dunes** - open ground and a coin trail.
2. **Cactus field** - change lanes (cacti are too tall for a normal jump).
3. **First gap** - jump to the next platform.
4. **Wooden barriers** - jump them, or use a free lane. The last one spans all lanes.
5. **Narrow rope bridge** - only the middle lane is walkable; switching lanes means a fall.
6. **Rolling boulders** - boulders start rolling toward you as you approach.
7. **Floating islands** - missing platforms of different heights (double jump helps).
8. **Sand pits** - pits cut into some lanes, ending with a full-width pit you must jump.
9. **Rotating obstacles** - spinning wooden sweeper bars; jump over them.
10. **Falling rocks** - a red warning disc marks the landing spot; rocks stay as obstacles.
11. **Final dash** - gap, long coin runs, then the **ancient temple** with a golden FINISH arch and checkered line.

The scenery is dunes, rocky mountains, pyramids, ruins, palms, cacti, dead trees and sandstone mesas,
all in golden yellow, orange, brown and sandy beige, with warm sun, fog and a procedural sky.

## Scoring

- **+10** per coin (rotating gold coins).
- **+1** per metre run (distance score).
- **+1000** bonus for reaching the temple.
- The **best score** is saved with `PlayerPrefs` and shown on the HUD, menu and end screens.
- The HUD shows the coin count, score, best score, distance and a progress bar to the temple.

## How you can die

Falling off a platform, touching a cactus, barrier, boulder, falling rock or spinner, sinking in a sand pit,
or running face-first into the side of a platform. The end screen says which one got you.

## Files (`Assets/Scripts`)

| File | Purpose |
|---|---|
| `GameBootstrap.cs` | Auto-start: lighting, sky, fog, camera, player, HUD |
| `GameManager.cs` | State machine (Menu / Playing / Paused / GameOver / Victory), scoring, best score |
| `PlayerController.cs` | `CharacterController` runner: lanes, jump, double jump, coyote time, gravity, fall detection, animation |
| `CameraFollow.cs` | Chase camera with smoothing, speed FOV and shake |
| `LevelBuilder.cs` | Course layout: platforms, bridge, islands, pits, obstacles, coins |
| `LevelScenery.cs` | Finish temple and all background scenery |
| `Hazards.cs` | Coin, KillZone, RollingRock, FallingRock, Rotator, FinishGate |
| `DesertFactory.cs` / `MeshUtil.cs` | Materials, explorer character, cacti, palms, ruins, pyramids, mountains |
| `GameHUD.cs` | HUD and menu screens |
| `GameInput.cs` | Input wrapper (both input systems) |
| `Sfx.cs` | Procedurally generated sound effects |

## Tweaking

- Speed, jump height, gravity and lane width: public fields on `PlayerController`.
- Reorder or edit segments: the `Build()` method in `LevelBuilder.cs` is a readable list of segments.
- Score values: the constants at the top of `GameManager.cs`.

## Troubleshooting

- **Pink materials:** the game borrows the project's default lit shader, so this should not happen.
  If it does, check that the render pipeline asset is assigned in Project Settings > Graphics.
- **Compile error about `UnityEngine.InputSystem`:** set Active Input Handling to "Both" or install the Input System package.
- **Low frame rate in the editor:** lower `QualitySettings.shadowDistance` in `GameBootstrap.cs`.
