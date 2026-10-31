# TGLabTest — Helicopter Flight Simulation

A realistic, hard-to-fly helicopter flight model in Unity 6.3 (HDRP): one helicopter, one location,
third-person view, keyboard controls through the Input System. Extenject for DI, UniTask for async.
Namespace root `TGLabTest.*`. Standalone side project — the company template rules don't apply.

## Developer

Strong programming background, limited physics background. When introducing or changing physics,
explain the concept in plain terms and say what each variable means.

## Documentation

Open the relevant file when a task calls for it; they are not loaded automatically.

- **`.claude/GAME-DESIGN.md`** — how the simulator is meant to work: concept, end goal, flight
  model parts, controls and their effect on the aircraft, control coupling, scope, milestones,
  glossary. Every item carries a status (planned / in progress / complete).
- **`.claude/ARCHITECTURE.md`** — project structure: assemblies and folders, components and their
  roles, Zenject contexts, timing loops, communication rules, extension points.
- **`.claude/PROJECT-NOTES.md`** — implementation specifics: current state, project setup,
  conventions and sign rules, the physics model with all equations, coefficient derivation and
  starting values, control rates, Rigidbody and Unity setup, telemetry overlay, upgrade path to
  detailed aerodynamics, pitfalls, open decisions.
