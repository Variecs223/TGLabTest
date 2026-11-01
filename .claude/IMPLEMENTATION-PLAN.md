# Implementation Plan

Temporary. Tracks the build-out of the design in `GAME-DESIGN.md` and `ARCHITECTURE.md`, step by
step, so progress survives between sessions. Updated as work goes; deleted once every step is done.

Legend: `[x]` done · `[~]` in progress · `[ ]` not started · **(you)** = developer's task.

Every code step ends with an IDE diagnostics check and a hand-off for recompile; `Editor.log` is
read at the start of the next prompt, and errors are fixed before moving on.

## 1. Foundation

- [x] Fixed timestep 0.01 s **(you)**
- [x] Folders `Assets/TGLabTest/Simulation/`, `Gameplay/`, `EditorTools/`
- [x] `TGLabTest.Simulation.asmdef` — references `UnityEngine` only
- [x] `TGLabTest.Gameplay.asmdef` — references Simulation, Zenject, Input System, UniTask
- [x] `TGLabTest.EditorTools.asmdef` — Editor platform only; references Simulation
- [x] Delete `Assets/InputSystem_Actions.inputactions` in the Editor **(you)**

## 2. Simulation core (`TGLabTest.Simulation`)

- [x] `ControlPositions` — collective, cyclic lateral/longitudinal, pedals
- [x] Body samples — `RotorSample`, `SkidContactSample`; outputs `SkidContactForce`, `GroundHit`
- [x] `HelicopterState` / `IHelicopterState`
- [x] `HelicopterConfig` — mass, inertia, geometry, ranges, tuning targets, per-coefficient overrides; degrees in the inspector
- [x] `HelicopterCoefficients` (derivation, overrides) and `HelicopterParameters` with per-part `RotorParameters`, `DrivetrainParameters`, `FuselageParameters`, `SkidGearParameters`
- [x] `RotorModel` — thrust, drag torque, disk normal with lag (main and tail)
- [x] `Drivetrain` — governor, spool-limited engine torque, power limit, `Ω`, `Ω̇`, shaft torque
- [x] `FuselageAero` — per-axis drag at the centre of pressure, optional angular damping
- [x] `IGroundProbe`, `SkidGear` — spring-damper contacts with friction
- [x] Compile confirmed in Unity **(you)**

## 3. Config inspector (`TGLabTest.EditorTools`)

- [x] `HelicopterConfigEditor` — config fields; each coefficient's derived value, override toggle, manual value; hover-balance readout
- [x] Compile confirmed in Unity **(you)**
- [x] UH-1 config asset created **(you)**
- [x] Sanity check of derived values against hand calculation: `kT ≈ 279.9`, `kv ≈ 124.3`, `q0 ≈ 5.376`, `q1 ≈ 732.1`, `kTt ≈ 0.5738`, `kvt ≈ 2.309`, `q0t ≈ 0.004863`, `q1t ≈ 0.582`, `rate ≈ 8842`, `Kgov ≈ 28956`; hover: torque `≈ 20.63 kN·m`, tail thrust `≈ 2.399 kN`, drivetrain power `≈ 791 kW`, tail rotor speed `≈ 173.0 rad/s`, governor target `≈ 331.7 rpm`

## 4. Controls (`TGLabTest.Gameplay.Controls`)

- [x] `FlightInput.inputactions` — `Flight` map, `Keyboard` scheme, generated `FlightInput` class
- [x] `IFlightControls`, `FlightControls` — ramps, hold, rates from `PROJECT-NOTES.md`
- [x] Compile confirmed in Unity **(you)**

## 5. Gameplay wiring (`TGLabTest.Gameplay`)

- [x] ~~`HelicopterRig`~~ — dropped: forces use config offsets, so the installer's serialized Rigidbody is the only prefab reference needed
- [x] `RaycastGroundProbe`
- [x] `HelicopterSimulation` — fixed-step order from `PROJECT-NOTES.md`, shaft-torque reaction, Rigidbody setup
- [x] `IActiveHelicopter` / `ActiveHelicopter`, `HelicopterRegistration`
- [x] `HelicopterInstaller` (GameObjectContext), `FlightSceneInstaller` (SceneContext)
- [x] Verify `IFixedTickable` inside the GameObjectContext is driven each fixed step — yes, via `DefaultGameObjectKernel`
- [x] `HelicopterCamera` — basic follow
- [x] `TelemetryOverlay` — minimal IMGUI readout
- [x] Compile confirmed in Unity **(you)**

## 6. Scene and prefab **(you, to spec)**

- [x] Spec handed over
- [x] Helicopter prefab — GameObjectContext + `HelicopterInstaller` (config, Rigidbody), Rigidbody, placeholder mesh, hull collider
- [x] Scene — SceneContext + `FlightSceneInstaller`, camera with `HelicopterCamera`, `TelemetryOverlay`, terrain with collider

## 7. Flight milestones

Acceptance checks are in `GAME-DESIGN.md` → *Milestones*.

- [x] Milestone 1 — vertical only (all rotations frozen). Fix along the way: rotor torque now from momentum-theory power instead of blade pitch (overpitched at full collective before)
- [x] Rightward drift from tail rotor translating tendency — derived lateral rigging offset
- [x] Milestone 2 — yaw (Y rotation unfrozen)
- [x] Milestone 3 — full six degrees of freedom. Fixes along the way: anchored skid friction, lateral rigging, roll inertia 6,500 kg·m²
- [x] Milestone 4 — fuselage drag and skids. Take-off and landing pass; top-speed check descoped. Added along the way: flapback

## 8. Presentation and tuning

- [~] `RotorVisual` — spin and disk tilt; blur disk dropped (blades only, HDRP motion blur to try). Code written; prefab wiring and eye test **(you)**
- [x] ~~Camera polish~~ — current camera accepted
- [~] Milestone 5 — tuning against the targets **(you, mostly)**. Goal: holding W flies forward without flipping. Tilt reduced to 4° / 3° (not enough alone); horizontal stabilizer added, eye test pending; flapback 1.0 next if needed

## 9. Close-out

- [~] Documentation sweep: `GAME-DESIGN.md` statuses, `ARCHITECTURE.md`, `PROJECT-NOTES.md` — swept after milestone 4; repeat once step 8 is done
- [ ] Delete this file