# Helicopter Flight Simulation — Project Context

This file is the agreed plan for this project, written as a handoff from an earlier design discussion. It describes the physics model, the controls, and how everything maps onto a Unity Rigidbody. Read it fully before writing code.

## Project

- **Engine:** Unity 6.3, HDRP.
- **Goal:** a helicopter flight model that *feels* realistic and is hard to fly, like the real aircraft. It does not need a 1:1 match of real-world physics.
- **Developer:** strong programming (CS) background, limited physics background. When introducing or changing physics, explain the concept in plain terms and say what each variable means.
- **Units:** SI everywhere (m, kg, s, N, N·m, W). Angles in **radians** internally; convert only at the UI or config boundary.

## Scope decisions (agreed)

In scope:
- Two rotors: a main rotor and a tail rotor. Each produces thrust and drag torque from its **blade pitch** and **rotor speed**.
- An engine that delivers torque to the rotors and compensates RPM loss at a limited rate.
- Rotor drag loads the engine. Heavy collective or pedal use droops rotor RPM until the engine catches up.
- Cyclic tilts the rotor disk, which tilts the thrust vector. Torque reaction yaws the fuselage. The tail rotor counters the yaw.
- All forces applied to a single Rigidbody.
- **No pilot assists.** No stability augmentation, no auto-hover, no auto-trim. The player must manage the coupling between collective, pedals and cyclic, as real pilots do.

Out of scope (deliberately):
- **Fuel.** Infinite fuel; mass is constant.
- **Detailed aerodynamics:** inflow and momentum theory, translational lift, blowback (aerodynamic disk tilt with speed), ground effect, vortex ring state, retreating blade stall.
- **Autorotation.** It needs the inflow model, so with the engine off the rotor simply slows down.

See the appendix for the upgrade path if detailed aerodynamics is added later.

## How a helicopter flies (key facts the model relies on)

- **Rotor speed is roughly constant.** Thrust is controlled by **blade pitch**, not RPM. A governor holds RPM.
- **Collective** changes the pitch of all main rotor blades equally. Result: more or less total thrust, and more or less drag torque (so more engine load).
- **Cyclic** changes blade pitch once per revolution: higher on one side, lower on the other. The blades flap on their hinges, and **the plane the blades spin in (the rotor disk) physically tilts relative to the mast**, typically up to about 8–10°. The mast itself stays fixed to the fuselage. Thrust is perpendicular to the disk, so it tilts too. The tilted thrust, acting above the center of mass, rotates the fuselage to follow, and part of the thrust then pulls the helicopter in that direction.
  - Gyroscopic precession (a 90° phase lag) is cancelled by how the swashplate is rigged. **Stick forward = disk tilts forward.** Do not simulate precession.
- **Torque reaction.** The engine turns the rotor, so the rotor turns the fuselage the opposite way. The tail rotor pushes the tail sideways to cancel this. **Pedals** change tail rotor pitch, so they control yaw.
- **Coupling** (this is what makes it hard, and it should emerge from the physics):
  - Raising collective adds torque, so the nose yaws, so the pilot needs pedal.
  - More tail rotor thrust also pushes the whole helicopter sideways ("translating tendency"), so the pilot needs a little opposite cyclic.
  - More load droops RPM, which costs thrust until the governor recovers.
- A hovering helicopter is naturally unstable and needs constant small corrections.

## Controls

| Key | Control | Behavior |
|---|---|---|
| W / S | Cyclic, longitudinal | W = disk forward (nose down, accelerate forward). Ramps toward ±1 while held and returns to 0 when released (e.g. 3 /s both ways). |
| A / D | Cyclic, lateral | Same ramping as W/S. |
| Space / Left Ctrl | Collective | Space raises, Ctrl lowers, at ~0.4–0.5 /s. **Holds its position when released**, like a real collective lever. Range [0, 1]. |
| Q / E | Pedals (yaw) | Q = nose left, E = nose right. Ramps ±1 (~2 /s) and returns to 0. Pedal 0 = tail pitch that balances hover torque. |

- Use the Unity **Input System** package.
- Keep input smoothing (ramps) in the input layer only; the physics consumes control positions.
- Optional later controls:
  - engine on/off or governor target (idle vs. flight RPM);
  - **force trim** (a key that stores the current cyclic position as the new neutral), which real helicopters have;
  - gamepad or joystick axes.

## Physics model

### Notation

| Symbol | Meaning | Source |
|---|---|---|
| `c` | collective position [0,1] | player |
| `cx, cy` | cyclic lateral / longitudinal [−1,1] | player |
| `p` | pedal position [−1,1] (−1 = Q = nose left) | player |
| `θ` | main rotor blade pitch, rad | `lerp(θmin, θmax, c)` |
| `θt` | tail rotor blade pitch, rad | from pedals (below) |
| `Ω` | main rotor angular speed, rad/s | **state variable**, integrated each step |
| `Ωt = gt·Ω` | tail rotor speed (geared to main) | derived |
| `n` | main rotor disk normal (thrust direction), world space | **state variable**, lags its target |
| `T, Q` | main rotor thrust (N), drag torque (N·m) | computed |
| `Tt, Qt` | tail rotor thrust, drag torque | computed |
| `Vup` | hub velocity along the thrust axis, m/s | `Vector3.Dot(rb.GetPointVelocity(hubPos), n)` |

### Main rotor

```
θ   = lerp(θmin, θmax, c)
T   = kT · Ω² · θ  −  kv · Ω · Vup        // thrust
Q   = Ω² · (q0 + q1 · θ²)                 // drag torque (loads the engine)
```

- `kT·Ω²·θ`: thrust grows with blade pitch and with the square of rotor speed.
- `−kv·Ω·Vup`: the one aerodynamic term we keep. Moving up the thrust axis (climbing) reduces thrust, and descending increases it.
  - This gives vertical damping and a natural maximum climb rate.
  - Without it, any collective above hover accelerates the helicopter upward forever.
- `q0`: drag of the blades spinning even at zero pitch. A rotor always needs some power.
- `q1·θ²`: extra drag from producing lift. It grows faster than thrust, so big collective pulls load the engine hard.

**Disk tilt from cyclic, with lag:**
```
nTarget = normalize( shaftUp + shaftFwd·(cy·maxTiltLon) + shaftRight·(cx·maxTiltLat) )
n       = Slerp(n, nTarget, 1 − exp(−dt/τ))      // τ ≈ 0.08–0.12 s
```
- Store `n` **in world space**. When the fuselage rotates, the disk briefly lags behind and tilts the thrust against the motion. That gives the pitch and roll damping a real rotor provides.
- Without it the helicopter oscillates uncontrollably.

**Optional hub moment** (for articulated or hingeless rotors; 0 for two-blade teetering rotors like the UH-1):
```
M_hub = Kβ · Vector3.Cross(shaftUp, n)
```

### Tail rotor

```
θt = θtNeutral + p · Δθt · (−spinSign)     // see sign conventions; pick Δ per side if asymmetric
Tt = kTt · Ωt² · θt  −  kvt · Ωt · Vaxial   // Vaxial = Dot(rb.GetPointVelocity(tailHubPos), tailAxis)
Qt = Ωt² · (q0t + q1t · θt²)
```
- `GetPointVelocity` includes rotation. When the helicopter yaws, the tail hub moves along the tail axis and the `kvt` term opposes it. That is **yaw damping** for free.
- `Tt` can be negative when pitch is negative; that's fine.
- The tail rotor's thrust is applied above or below the CG, so it produces a small roll moment and translating tendency. Both are real effects; keep them.

### Engine, governor and rotor speed

The engine delivers torque toward a commanded value, limited in how fast it can change (spool lag) and in maximum power. There is no fuel: power is always available up to `Pmax`.

```
Qcmd    = Kgov · (Ωtarget − Ω)                         // governor demand (P controller; add I if steady error bothers)
Qmax    = Pmax / max(Ω, Ωmin)                          // power-limited torque
Qcmd    = clamp(Qcmd, 0, Qmax)                         // ≥ 0: freewheel, the engine never brakes the rotor
Qeng   += clamp(Qcmd − Qeng, −rate·dt, rate·dt)        // limited compensation rate
Ω      += (Qeng − Q − gt·Qt − Qfriction) / Itotal · dt
Ω       = max(Ω, 0)
Itotal  = Imain + gt²·Itail
```
- Tail rotor drag, referred to the main shaft, is `gt·Qt`. Heavy pedal use also steals engine power.
- Engine off means `Qcmd = 0`, so the rotor spins down and thrust fades. (No autorotation in this scope.)

### Fuselage

- **Drag**, computed in local space and applied at a center of pressure slightly *behind* the CG (this gives a cheap weathervane effect):
  ```
  vLocal = transform.InverseTransformDirection(rb.GetPointVelocity(cpPos))
  Flocal = −0.5 · ρ · Vector3.Scale(fAreas, Vector3.Scale(vLocal, Abs(vLocal)))   // per-axis Cd·A
  ```
  Without drag, the helicopter accelerates horizontally forever.
- **Optional extra angular damping:** `rb.AddTorque(−kω ⊙ angularVelocity)` per body axis. Keep it small and use it only if the disk lag alone isn't enough.
- **Gravity:** Unity applies it (`useGravity = true`). Do not add weight manually.

### Landing gear

- Raycast spring-dampers at 4 skid points, pushing along the hit normal: `F = k·compression + c·compressionRate`, never negative.
- Add friction (opposing tangential velocity).
- Do **not** use WheelColliders for skids.

## Applying forces to the Rigidbody (each FixedUpdate)

Order:
1. Read control positions from the input layer.
2. Sample state: `rb.GetPointVelocity` at the hubs, the shaft axes from the hub transform, `Ω`.
3. Main rotor: update `n` (lag), compute `T`, `Q`.
4. Tail rotor: compute `Tt`, `Qt`.
5. Engine/governor: update `Qeng`, integrate `Ω`.
6. Apply:
   ```csharp
   rb.AddForceAtPosition(T * n, mainHubPos);                 // lift + control moments (hub is above CG)
   rb.AddTorque(-Q * spinSign * shaftUp);                    // torque reaction on fuselage
   rb.AddTorque(Kβ * Vector3.Cross(shaftUp, n));             // optional hub moment
   rb.AddForceAtPosition(Tt * tailAxis, tailHubPos);         // anti-torque
   rb.AddForceAtPosition(fuselageDragWorld, cpPos);
   // + landing gear forces
   ```

### Sign conventions (Unity is left-handed, Y up, Z forward, X right)

- A positive rotation about +Y is **clockwise when seen from above**.
- `spinSign = +1`: main rotor turns clockwise seen from above (Soviet/Russian designs, e.g. Mi-8).
- `spinSign = −1`: main rotor turns counterclockwise seen from above (US designs, e.g. UH-1, UH-60).
- Torque reaction on the fuselage is `−Q · spinSign · shaftUp`. A CCW rotor yaws the nose right.
- Tail thrust axis: `tailAxis = −spinSign · transform.right`.
  - Example: CCW rotor → tail rotor pushes the tail to the right → nose-left moment, which cancels the reaction.
  - Check: `Cross((0,0,−L), (F,0,0)) = (0, −L·F, 0)`, a −Y torque (nose left).
- Stay in vectors with `Vector3.Cross`. Unity's cross product and torque conventions are consistent with each other; avoid Euler angles in the physics.

## Unity setup rules

- **Fixed timestep 0.01 s (100 Hz)**, or 0.005 if tilt lag or springs are unstable (Project Settings → Time).
- Rigidbody settings:
  - `automaticCenterOfMass = false`, `automaticInertiaTensor = false`. Set `centerOfMass`, `inertiaTensor` and `inertiaTensorRotation` manually; collider-derived values are badly wrong for helicopters.
  - `linearDamping = 0`, `angularDamping = 0` (Unity 6 names). All damping comes from the model.
  - `interpolation = Interpolate`. Camera follows in `LateUpdate`.
- The rotors are **not** Rigidbodies. They exist only as math. Visual meshes are spun by `Ω` and tilted to match `n`. At high RPM, swap in a blur-disk mesh, since HDRP motion blur handles rotors poorly.
- If flying more than ~5–10 km from the origin, plan a floating origin. HDRP camera-relative rendering fixes visual jitter but not physics precision.

## Tuning: derive constants from targets, not physics

Pick the targets, then solve for the constants:

1. **Hover:** at `c = 0.5`, `Ω = Ωnom`, `Vup = 0`, require `T = m·g`.
   → `θh = lerp(θmin, θmax, 0.5)`, `kT = m·g / (Ωnom² · θh)`.
2. **Hover power** `Phover` → `Qh = Phover / Ωnom`.
   → `q0 = 0.3·Qh / Ωnom²`, `q1 = 0.7·Qh / (Ωnom² · θh²)`.
3. **Max climb rate** `Vclimb` at full collective (ignoring fuselage drag).
   → `kv = kT · Ωnom · (θmax − θh) / Vclimb`.
4. **Tail rotor at hover:** require `Tt · Larm = Qh` (`Larm` = horizontal distance from CG to the tail hub) with pedals neutral.
   → `kTt = (Qh / Larm) / (Ωt_nom² · θtNeutral)`.
   → Choose `q0t`, `q1t` so the tail rotor uses roughly 10–15% of hover power.
5. **Engine:** `Pmax ≈ 1.3–1.5 × Phover`. Set the spool rate so recovering from a sharp collective pull takes about 0.5–2 s.

### Starting values (UH-1H-like, approximate)

| Parameter | Value |
|---|---|
| mass | 4,300 kg |
| inertia (Unity axes X pitch, Y yaw, Z roll) | ≈ (24,700, 21,000, 3,750) kg·m² |
| main hub above CG | ~1.8 m |
| tail hub behind CG (`Larm`) | ~8.6 m |
| `Ωnom` | 33.9 rad/s (324 rpm) |
| `gt` (tail/main gear ratio) | ~5.1 |
| `θmin..θmax` (collective) | 0 .. 0.26 rad (0–15°) |
| `θt` range | −0.14 .. 0.35 rad (−8° .. +20°) |
| `maxTiltLon / maxTiltLat` | 0.14 / 0.10 rad (8° / 6°) |
| `τ` disk lag | 0.1 s |
| `Phover` / `Pmax` | ~700 kW / ~1,000 kW |
| `Imain` (rotor inertia) | ~6,000 kg·m² |
| `Vclimb` at full collective | ~10 m/s |
| fuselage `fAreas` (Cd·A, x/y/z local) | ~(10, 15, 2) m² |
| `spinSign` | −1 (CCW, US) |
| skid spring `k` per contact | ~2.1e5 N/m (≈5 cm static compression with 4 contacts) |
| skid damper `c` per contact | ~1.8e4 N·s/m |

## Suggested code structure

```
Assets/Scripts/Helicopter/
  HelicopterConfig.cs      ScriptableObject: all constants above (show degrees in the inspector, convert to rad)
  HelicopterInput.cs       Input System → control positions with ramps/hold behavior
  RotorModel.cs            plain C#: thrust, torque, disk normal lag (used for both rotors)
  Drivetrain.cs            plain C#: engine torque, governor, rotor-speed integration
  FuselageAero.cs          plain C#: drag at center of pressure
  SkidGear.cs              raycast spring-damper contacts
  HelicopterController.cs  MonoBehaviour: FixedUpdate order above; applies forces
  RotorVisual.cs           spins/tilts meshes from Ω and n; blur-disk swap
  HelicopterDebugHUD.cs    on-screen telemetry + gizmos
```

- Keep the physics in plain C# classes (testable without a scene). The MonoBehaviour only orchestrates.
- **Build the debug HUD early.** It should show `c, cx, cy, p, θ, θt, Ω (rpm and %), Qeng, Q, Qt, T, Tt`, power, vertical speed, airspeed, and gizmo arrows for thrust vectors.

## Implementation roadmap (with acceptance checks)

1. **Vertical only** (freeze rotation constraints): collective + main thrust + engine/Ω.
   - `T ≈ m·g` at `c = 0.5`.
   - Full collective → climb settles near `Vclimb`.
   - Fast collective pull → visible RPM droop, then recovery.
2. **Yaw** (unfreeze Y rotation): torque reaction + tail rotor + pedals.
   - Collective changes cause yaw that needs pedal.
   - Releasing pedals at hover collective → slow yaw drift only.
3. **Full 6DOF:** cyclic disk tilt with lag.
   - Hover is possible but needs constant small corrections.
   - Cyclic forward → nose down → accelerate.
4. **Fuselage drag + skids:** stable top speed; takeoff and landing from the ground.
5. **Visuals and tuning:** rotor visuals, camera, HUD polish, tune against the targets above.

## Appendix: upgrade path to detailed aerodynamics (not in current scope)

If realism is extended later, replace the simple thrust/torque formulas with a blade-element + momentum-theory rotor. The rest of the architecture stays the same.

```
μ  = |V in disk plane| / (ΩR)              // advance ratio
λc = (V · n) / (ΩR)                         // climb inflow
Ct = (σa/2)·[θ0(1/3 + μ²/2) + θtw(1+μ²)/4 − λ/2]   // blade element
λi = Ct / (2√(μ² + λ²)),  λ = λc + λi      // momentum theory; iterate Ct ⇄ λ 5–10×
T  = Ct·ρ·A·(ΩR)²
Cq = κ·Ct·λi + Ct·λc + (σ·Cd0/8)(1 + 4.6μ²)
Q  = Cq·ρ·A·(ΩR)²·R
```

This adds, without special-case code: translational lift, forward-flight power savings, and autorotation (`Ct·λc` going negative drives the rotor).
- Ground effect: multiply thrust by `1 / (1 − (R/4z)²)`.
- Vortex ring state: an empirical correction to `λi`.
- Reference: NASA TM-84281, *A Mathematical Model of a Single Main Rotor Helicopter for Piloted Simulation* (Talbot et al., 1982).

## Glossary

- **Collective:** lever changing pitch of all main blades together (up/down thrust).
- **Cyclic:** stick tilting the rotor disk (pitch/roll, direction of travel).
- **Pedals:** tail rotor pitch (yaw).
- **Blade pitch (θ):** angle of the blade relative to its plane of rotation.
- **Rotor disk / tip-path plane:** the plane the blade tips sweep. Thrust is perpendicular to it.
- **Swashplate:** mechanism translating collective/cyclic into blade pitch.
- **Flapping:** blades moving up/down on hinges; this is what tilts the disk.
- **Torque reaction:** fuselage pushed opposite to rotor rotation.
- **Governor:** engine controller holding rotor RPM.
- **RPM droop:** rotor slowing when load exceeds what the engine can supply at that moment.
- **Translating tendency:** sideways drift caused by tail rotor thrust.
- **Teetering / articulated rotor:** two-blade rotor rocking on the mast (no hub moment) / multi-blade rotor with hinges (has hub moment).
