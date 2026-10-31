# Implementation Plan

Temporary. Tracks the build-out of the design in `GAME-DESIGN.md` and `ARCHITECTURE.md`, step by
step, so progress survives between sessions. Updated as work goes; deleted once every step is done.

Legend: `[x]` done · `[~]` in progress · `[ ]` not started · **(you)** = developer's task.

Every code step ends with an IDE diagnostics check and a hand-off for recompile; `Editor.log` is
read at the start of the next prompt, and errors are fixed before moving on.

## 1. Foundation

- [x] Fixed timestep 0.01 s **(you)**
- [ ] Folders `Assets/TGLabTest/Simulation/`, `Gameplay/`, `EditorTools/`
- [ ] `TGLabTest.Simulation.asmdef` — references `UnityEngine` only
- [ ] `TGLabTest.Gameplay.asmdef` — references Simulation, Zenject, Input System, UniTask
- [ ] `TGLabTest.EditorTools.asmdef` — Editor platform only; references Simulation
- [ ] Delete `Assets/InputSystem_Actions.inputactions` in the Editor **(you)**

## 2. Simulation core (`TGLabTest.Simulation`)

- [ ] `ControlPositions` — collective, cyclic lateral/longitudinal, pedals
- [ ] Body-sample struct — point velocities and axes the models need
- [ ] `HelicopterState` / `IHelicopterState`
- [ ] `HelicopterConfig` — mass, inertia, geometry, ranges, tuning targets, per-coefficient overrides; degrees in the inspector
- [ ] `HelicopterParameters` — derivation table from `PROJECT-NOTES.md`, then overrides
- [ ] `RotorModel` — thrust, drag torque, disk normal with lag (main and tail)
- [ ] `Drivetrain` — governor, spool-limited engine torque, power limit, `Ω` and `Ω̇`
- [ ] `FuselageAero` — per-axis drag at the centre of pressure
- [ ] `IGroundProbe`, `SkidGear` — spring-damper contacts with friction

## 3. Config inspector (`TGLabTest.EditorTools`)

- [ ] `HelicopterConfigEditor` — targets; each coefficient's derived value, override toggle, manual value
- [ ] Create the UH-1 config asset with the starting values **(you)**
- [ ] Sanity check of derived values against hand calculation: `kT ≈ 282`, `kv ≈ 125`, `Qh ≈ 20.6 kN·m`, hover tail thrust `≈ 2.4 kN`, `Ωt_nom ≈ 173 rad/s`

## 4. Controls (`TGLabTest.Gameplay.Controls`)

- [ ] `FlightActions.inputactions` — `Flight` map, `Keyboard` scheme, generated C# class
- [ ] `IFlightControls`, `FlightControls` — ramps, hold, rates from `PROJECT-NOTES.md`

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