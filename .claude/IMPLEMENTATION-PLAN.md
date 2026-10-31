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
- [ ] Delete `Assets/InputSystem_Actions.inputactions` in the Editor **(you)**

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
- [ ] Compile confirmed in Unity **(you)**

## 5. Gameplay wiring (`TGLabTest.Gameplay`)

- [ ] `HelicopterRig` — prefab references
- [ ] `RaycastGroundProbe`
- [ ] `HelicopterSimulation` — fixed-step order from `PROJECT-NOTES.md`, shaft-torque reaction
- [ ] `ActiveHelicopter` — registration on initialize/dispose
- [ ] `HelicopterInstaller` (GameObjectContext), `FlightSceneInstaller` (SceneContext)
- [ ] Verify `IFixedTickable` inside the GameObjectContext is driven each fixed step
- [ ] `HelicopterCamera` — basic follow
- [ ] `TelemetryOverlay` — minimal IMGUI readout

## 6. Scene and prefab **(you, to spec)**

- [ ] Spec handed over: prefab hierarchy, components, transform offsets from the config
- [ ] Helicopter prefab — GameObjectContext + `HelicopterInstaller`, Rigidbody, `HelicopterRig` with main hub, tail hub, centre of pressure, four skid points; placeholder mesh; hull collider
- [ ] Scene — SceneContext + `FlightSceneInstaller`, camera with `HelicopterCamera`, `TelemetryOverlay`

## 7. Flight milestones

Acceptance checks are in `GAME-DESIGN.md` → *Milestones*.

- [ ] Milestone 1 — vertical only (all rotations frozen)
- [ ] Milestone 2 — yaw (Y rotation unfrozen)
- [ ] Milestone 3 — full six degrees of freedom
- [ ] Milestone 4 — fuselage drag and skids

## 8. Presentation and tuning

- [ ] `RotorVisual` — spin, disk tilt, blur disk
- [ ] Camera polish
- [ ] Milestone 5 — tuning against the targets **(you, mostly)**

## 9. Close-out

- [ ] Documentation sweep: `GAME-DESIGN.md` statuses, `ARCHITECTURE.md`, `PROJECT-NOTES.md`
- [ ] Delete this file