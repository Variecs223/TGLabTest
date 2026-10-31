# Architecture

What the project consists of, what each part is for, and how the parts interact. Implementation
specifics, the physics model, Unity settings and pitfalls live in `PROJECT-NOTES.md`. Intended
behaviour lives in `GAME-DESIGN.md`.

Nothing is implemented yet; this is the agreed structure.

## Overview

```
 Input System ──► FlightControls ─────────────┐   per frame: ramps, hold
                  IFlightControls             │   control positions
                                              ▼
                ┌──────── Helicopter (GameObjectContext) ───────────────┐
                │  HelicopterSimulation  (fixed tick)                   │
                │    samples body ─────► RotorModel (main)              │
                │                        RotorModel (tail)              │
                │                        Drivetrain                     │
                │                        FuselageAero                   │
                │                        SkidGear ──► IGroundProbe      │
                │    applies forces ───► Rigidbody                      │
                │    writes ───────────► HelicopterState                │
                │                                                       │
                │  RotorVisual ◄── IHelicopterState  (per frame)        │
                └───────────────┬───────────────────────────────────────┘
                                │ registers itself
                                ▼
                     IActiveHelicopter (scene) ──► HelicopterCamera
                                              └─► TelemetryOverlay
```

The project is split into three concerns, each kept away from the others:
- **Controls** turn player input into control positions. Only this layer knows keys exist.
- **Simulation** turns control positions and the body's motion into forces. It is plain C# and
  knows nothing about the scene, DI or input devices.
- **Presentation** reads the simulation's state and shows it: rotor visuals, camera, and later a
  developer telemetry overlay.

Data flows one way: controls → simulation → state → presentation. Nothing downstream writes back
upstream.

## Assemblies and folders

| Assembly | Folder | Namespace root | References | Contents |
|---|---|---|---|---|
| `TGLabTest.Simulation` | `Assets/TGLabTest/Simulation/` | `TGLabTest.Simulation` | `UnityEngine` only | Physics models, config, derived parameters, state types. |
| `TGLabTest.Gameplay` | `Assets/TGLabTest/Gameplay/` | `TGLabTest.Gameplay` | Simulation, Zenject, Input System, UniTask | Installers, controls, the simulation orchestrator, presentation. |
| `TGLabTest.EditorTools` | `Assets/TGLabTest/EditorTools/` | `TGLabTest.EditorTools` | Simulation, `UnityEditor` | Custom inspectors. Editor-only. |

Namespaces follow folders below the root. `TGLabTest.Gameplay` holds the scene-level pieces
(`FlightSceneInstaller`, `ActiveHelicopter`); `.Controls` the input; `.Helicopter` everything bound
in the helicopter's context; `.Presentation` camera, overlay and visuals.

The assembly split is what keeps the simulation core free of scene, DI and input dependencies — a
reference that would break that rule doesn't compile.

## Components

### Simulation (`TGLabTest.Simulation`)

| Component | Kind | Purpose |
|---|---|---|
| `HelicopterConfig` | ScriptableObject | Everything that describes one aircraft: mass and inertia, geometry offsets, control ranges, tuning **targets** (hover power, climb rate, …), plus an optional manual override per derived coefficient. |
| `HelicopterCoefficients` | Struct | Derives every coefficient from the config's targets, and applies the config's overrides. The single source of coefficients for both the simulation and the inspector. |
| `HelicopterParameters` | Plain C# | The runtime values the models use, built from a config: effective coefficients split per part (`RotorParameters`, `DrivetrainParameters`, `FuselageParameters`, `SkidGearParameters`), geometry, and the mapping from control positions to blade pitch. |
| `ControlPositions` | Struct | The four control positions: collective, cyclic lateral, cyclic longitudinal, pedals. The only input the simulation accepts. |
| `RotorSample`, `SkidContactSample` | Structs | The sampled body state a model needs for one step: shaft axes and hub velocity; contact position and velocity. |
| `RotorModel` | Plain C# | Thrust, drag torque and disk tilt for one rotor. Two instances: main and tail. |
| `Drivetrain` | Plain C# | Engine torque, governor and rotor speed. Takes the rotors' drag torques, integrates rotor speed. |
| `FuselageAero` | Plain C# | Fuselage drag at the centre of pressure. |
| `SkidGear` | Plain C# | Spring-damper contacts with friction at the skid points. Queries the ground through `IGroundProbe`. |
| `IGroundProbe` | Interface | Ground query (raycast) used by `SkidGear`, so the gear model needs no scene access of its own. |
| `HelicopterState` / `IHelicopterState` | Plain C# / read interface | Snapshot of the last simulation step: control positions, blade pitches, rotor speed, disk normal, thrusts, torques, engine torque, power. Written by the orchestrator, read by everything else. |

Models are **stateful but scene-free**. Each takes a sampled body state (velocities and axes at its
points, rotor speed) and returns forces and moments; none of them touches the Rigidbody. That keeps
them testable without a scene and replaceable one by one — the detailed-aerodynamics upgrade
replaces `RotorModel`'s internals and nothing else.

### Gameplay (`TGLabTest.Gameplay`)

| Component | Kind | Context | Purpose |
|---|---|---|---|
| `FlightSceneInstaller` | MonoInstaller | SceneContext | Binds the scene-level services: the `FlightInput` wrapper, controls, `ActiveHelicopter`. |
| `HelicopterInstaller` | MonoInstaller | Helicopter GameObjectContext | Serialized config and Rigidbody. Binds the aircraft: parameters, Rigidbody, state, ground probe, orchestrator, registration. |
| `IFlightControls` / `FlightControls` | Interface / plain C# | SceneContext | Reads the Input System `Flight` action map each frame, applies ramp and hold behaviour, exposes `ControlPositions`. Future joystick, gamepad or scripted sources are further implementations. |
| `HelicopterSimulation` | Plain C#, `IFixedTickable` | Helicopter | The orchestrator. Owns the models — they are parts of the simulation, not services, so it constructs them from `HelicopterParameters` rather than receiving them from DI. Each fixed step: reads controls, samples the body, steps the models, applies forces to the Rigidbody, writes `HelicopterState`. Also applies the Rigidbody settings on initialize. |
| `RaycastGroundProbe` | Plain C# | Helicopter | `IGroundProbe` over `Physics.RaycastNonAlloc`, ignoring the helicopter's own colliders. |
| `IActiveHelicopter` / `ActiveHelicopter` | Interface / plain C# | SceneContext | Registry of the helicopter currently being flown: its transform and `IHelicopterState`. Readers use the interface; only registration uses the class. Lets scene-level objects reach an aircraft that lives in a child context. |
| `HelicopterRegistration` | Plain C# | Helicopter | Registers the aircraft with `ActiveHelicopter` on initialize, unregisters on dispose. |
| `RotorVisual` | MonoBehaviour | Helicopter prefab | Spins rotor meshes from rotor speed, tilts the main rotor to the disk normal, swaps to a blur disk at high speed. Its transforms only place visuals — physics geometry comes from the config. |
| `HelicopterCamera` | MonoBehaviour | Scene | Third-person follow camera on the active helicopter. |
| `TelemetryOverlay` | MonoBehaviour | Scene | Developer-only IMGUI readout of the active helicopter's `IHelicopterState`. |

### EditorTools (`TGLabTest.EditorTools`)

| Component | Purpose |
|---|---|
| `HelicopterConfigEditor` | Inspector for `HelicopterConfig`: the config fields; every coefficient with its derived value, an override toggle and the manual value; a hover-balance readout. Values come from `HelicopterCoefficients` and `HelicopterParameters`, so the inspector shows exactly what the simulation uses. |

## Contexts and bindings

- **SceneContext** — the flight scene. Scene-wide services: `IFlightControls`, `ActiveHelicopter`.
- **GameObjectContext** — on the helicopter prefab. Everything that belongs to one aircraft. A
  second aircraft or a respawn is another instance of the prefab, with its own container.
- **ProjectContext** — not used. There is one scene and nothing to carry across scenes.

The child context sees the scene's bindings (the helicopter injects `IFlightControls`); the scene
does not see the child's. Scene objects that need the aircraft go through `ActiveHelicopter`.

## Timing

| Loop | Who runs | What happens |
|---|---|---|
| `Update` | `FlightControls` | Ramps control positions from held keys. |
| `FixedUpdate` | `HelicopterSimulation` | One simulation step, forces applied before Unity's physics step. |
| `Update` | `RotorVisual` | Rotor spin integrated at render rate, for smooth motion. |
| `LateUpdate` | `HelicopterCamera` | Follows the interpolated helicopter transform. |

The simulation reads the latest control positions at each fixed step; it never reads input
directly.

## Communication rules

- **Direct, read-only interfaces** for high-frequency data: `IFlightControls` into the simulation,
  `IHelicopterState` out of it.
- **SignalBus** for rare, loosely coupled events only (engine started or stopped, touchdown,
  lift-off). None exist yet; none are added until a consumer needs one.
- **UniTask** for asynchronous flow when it arrives — boot, scene loading, timed sequences such as
  an engine start-up spool. The simulation itself is synchronous and never awaits.
- **State machine** for the engine once engine on/off is built (off → starting → running →
  stopping). Nothing needs one yet.

## Extension points

| Change | Where it lands |
|---|---|
| Joystick or gamepad | A new `IFlightControls` implementation with absolute positions. |
| Force trim | `FlightControls`; the simulation is unaffected. |
| Detailed aerodynamics | `RotorModel` internals and its config section. |
| Engine on/off | `Drivetrain` input, an engine state machine, signals for start and stop. |
| Simulation tests | EditMode tests against `TGLabTest.Simulation`; no scene needed. |