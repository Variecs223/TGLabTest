# Project Notes

Implementation specifics for the helicopter simulator. Structure and component roles are in
`ARCHITECTURE.md`; intended behaviour is in `GAME-DESIGN.md`.

## Current state

The build-out is finished. The full flight model flies in six degrees of freedom: simulation core,
config inspector, controls, gameplay wiring, rotor visuals, telemetry overlay, restart (R) and quit
(Esc), the helicopter prefab with the HeloMarine model, and a test scene with a terrain collider.
All five milestones pass: hover at mid collective, steady ~9 m/s climb with rotor speed held, RPM
droop and recovery, collective-to-yaw coupling, a hover that needs constant small corrections,
take-off and landing on the skids, and tuned handling. At low speed with careful taps the
helicopter is controllable; holding a cyclic key still pitches it over before the stabilizer can
catch it. High-speed flight was descoped from testing — too hard to control on a keyboard.

**Open items**
- **`kvt` override.** The UH-1 asset has the `kvt` override enabled at 0.924 (40 % of the derived
  2.309), which felt better — less yaw upset from sideways motion. Not folded into the derivation;
  see *Derivation* → yaw damping split.
- **Flapback 1.0° per 10 m/s** (closer to a textbook UH-1 estimate) is untried; it would stiffen
  speed stability and shorten the phugoid. Fuselage `AngularDamping` is the remaining keyboard
  concession if holding a key must stop flipping the aircraft from a hover.
- **Location** — the real location is still to be built.
- **`Pmax`** — see *Derivation* → climb rate vs. power.

## Project setup

| Item | Value |
|---|---|
| Unity | 6000.3.26f1 |
| Render pipeline | HDRP 17.3.0 (quality assets in `Assets/Settings/`) |
| Input | Input System 1.20.0. Player Settings → Active Input Handling = Input System Package (New) only (`activeInputHandler: 1`); the legacy `Input` API is unavailable. |
| DI | Extenject, from the Mathijs-Bakker fork (git URL in `manifest.json`). |
| Async | UniTask, embedded in `Assets/Plugins/UniTask/` (not a UPM package). |
| Scene | `Assets/OutdoorsScene.unity`, from the HDRP template. The real location comes later. |
| Fixed timestep | 0.01 s (100 Hz). Drop to 0.005 s if disk lag or skid springs turn out unstable. |

## Conventions

- **SI units everywhere**: m, kg, s, N, N·m, W. **Angles in radians** internally; degrees appear
  only in the inspector, converted at the config boundary.
- **Vectors, not Euler angles**, throughout the physics. Unity's `Vector3.Cross` and its torque
  convention are consistent with each other.
- Unity is left-handed: X right, Y up, Z forward. A positive rotation about +Y is **clockwise seen
  from above** (nose right).
- Torque reaction turns the fuselage opposite to the rotor: a CCW rotor (`spinSign = −1`) yaws the
  nose right, and the tail rotor must push the tail right to cancel it.

## Physics model

### Notation

| Symbol | Meaning | Source |
|---|---|---|
| `c` | collective position, [0, 1] | controls |
| `cx`, `cy` | cyclic lateral / longitudinal, [−1, 1]; +`cx` = D (right), +`cy` = W (forward) | controls |
| `p` | pedal position, [−1, 1]; −1 = Q (nose left), +1 = E (nose right) | controls |
| `θ` | main rotor blade pitch, rad | from `c` |
| `θt` | tail rotor blade pitch, rad | from `p` |
| `Ω` | main rotor angular speed, rad/s | **state**, integrated each step |
| `Ωt = gt·Ω` | tail rotor speed, geared to the main rotor | derived |
| `n` | main rotor disk normal (thrust direction), world space | **state**, lags its target |
| `T`, `Q` | main rotor thrust (N), drag torque (N·m) | computed |
| `Tt`, `Qt` | tail rotor thrust, drag torque | computed |
| `Vup` | hub velocity along the thrust axis: `Dot(rb.GetPointVelocity(hubPos), n)` | sampled |
| `spinSign` | +1 = main rotor clockwise from above (Russian designs); −1 = counterclockwise (US designs, UH-1) | config |

### Main rotor

```
θ = lerp(θmin, θmax, c)
T  = kT·Ω²·θ − kv·Ω·Vup
vi = √((Vup/2)² + vh²·|T|/Th) − Vup/2
Q  = Ω²·q0 + |T|·(vi + Vup)/Ω                    // Vup taken along the thrust direction; Q = Ω²·q0 at Ω = 0
```

- `kT·Ω²·θ` — thrust grows with blade pitch and with the square of rotor speed.
- `−kv·Ω·Vup` — the one aerodynamic term kept in thrust. Climbing along the thrust axis reduces
  thrust, descending increases it. This is the vertical damping and the natural climb-rate cap;
  without it any collective above hover accelerates upward forever.
- `Ω²·q0` — **profile** torque: drag of the blades spinning through the air, independent of lift.
- `|T|·(vi + Vup)` — the power the rotor spends on the air (momentum theory), divided by `Ω` to give
  torque. Two parts: `T·vi` — **induced** power, the cost of pushing air down to make lift; `T·Vup`
  — **climb** power, the cost of lifting the weight.
- `vi` — **induced velocity**, the speed of the downwash through the disk. `vh` is its value in a
  hover at hover thrust `Th`; it grows with √thrust. Climbing feeds air into the disk from above,
  so the rotor needs to push less of its own and `vi` drops.
- **Why not torque from blade pitch.** An earlier `Q = Ω²·(q0 + q1·θ²)` charged power for pitch as
  if the helicopter were stationary. In a climb the blades meet the air at a smaller angle, so it
  overstated full-collective torque ~3× (≈2,170 kW vs. 1,000 kW available): the rotor drooped to
  ~75 % and climb stalled at ~2 m/s. The power-based form is consistent with the thrust formula's
  climb term.
- Pulling more collective than the engine can power still droops the rotor — overpitching, real
  behaviour. Full collective on the ground (no climb yet, `T` ≈ 2× weight) does this briefly until
  the climb speed builds and the power demand falls.
- Descent lowers the power demand smoothly; `vi + Vup` never goes negative, so the air never drives
  the rotor (no autorotation in this scope; no vortex ring state).

**Disk tilt from cyclic, rigging and flapback, with lag:**
```
vPlane  = ProjectOnPlane(vHub, shaftUp)                       // hub velocity across the disk
nTarget = normalize(shaftUp
                    + shaftFwd   · (cy·maxTiltLon)
                    + shaftRight · (cx·maxTiltLat + rig)
                    − Kfb · vPlane)
n       = Slerp(n, nTarget, 1 − exp(−dt/τ))
```

`n` is stored **in world space**. When the fuselage rotates, the disk briefly lags behind and tilts
the thrust against the motion — the pitch and roll damping a real rotor provides. Without it the
helicopter oscillates uncontrollably.

- **Cyclic commands rotation rate, not attitude.** A tilt of the disk relative to the mast moves the
  thrust line off the centre of mass, so the fuselage rotates until the disk lines up with the mast
  again. Releasing the cyclic leaves the attitude where it is. That is how a teetering rotor
  behaves, and a real one also pitches over under held full cyclic. The steady rate is
  `(cy·maxTiltLon − Kfb·vPlane)/τ`: with the 4° limit, up to ~40°/s at low speed. A held key flies
  forward only once flapback and the horizontal stabilizer (see *Fuselage drag*) catch the rotation,
  which needs airspeed.
- **`rig` — lateral rigging.** A constant disk tilt with the cyclic centred, derived so that a hover
  needs no lateral cyclic (see *Derivation*). It cancels the roll moment of the tail rotor thrust,
  which acts above the CG. The sideways force of the tail rotor is then cancelled by the whole
  aircraft hanging rolled ~1.5° (left skid low for a CCW rotor), as real UH-1s do. Real helicopters
  get the same effect from cyclic rigging or mast tilt. Exact only in a hover.
- **`Kfb` — flapback** (blowback), rad of disk tilt per m/s. A rotor moving through the air flaps
  away from the airflow: the advancing blade gets more lift than the retreating one. Forward speed
  tilts the disk back and pitches the nose up; a sideways slide rolls the aircraft against the
  slide. This is the speed stability that lets a moving helicopter slow and level itself.
  Climb and descent are excluded (`vPlane` lies in the disk plane); they are handled by `kv`.
- **Phugoid.** Flapback turns speed into nose-up pitch, pitch into deceleration, deceleration into
  nose-down pitch: a slow oscillation, period `≈ 2π / √(Kfb·W·h·g / D)` ≈ 20 s, with `h` the hub
  height above the CG and `D = W·h·τ` the pitch damping from disk lag. Roughly neutral from the
  rotor alone; the horizontal stabilizer damps it in pitch at speed. The nose overshoots level while
  the aircraft is still moving; the pilot damps it by correcting ahead of the swing, as with a real
  hover. Roll has no stabilizer, so a sideways slide still swings lightly damped.
- Lateral flapping in forward flight (from blade coning) is not modelled.

**Hub moment** `M = Kβ·Cross(shaftUp, n)` — 0 for a two-blade teetering rotor like the UH-1. Kept
as a config value for articulated or hingeless rotors.

### Tail rotor

```
u  = p·spinSign                                  // +1 = more tail thrust, whatever the rotor direction
θt = θtNeutral + u·(θtMax − θtNeutral)   if u ≥ 0
θt = θtNeutral + u·(θtNeutral − θtMin)   if u < 0
Tt = kTt·Ωt²·θt − kvt·Ωt·Vaxial                  // Vaxial = Dot(rb.GetPointVelocity(tailHubPos), tailAxis)
Qt = Ωt²·q0t + |Tt|·(vit + Vaxial)/Ωt            // vit as vi, with vht and the tail's hover thrust
tailAxis = −spinSign·transform.right
```

- **Pedal sign.** Q must yaw the nose left (counterclockwise from above, a −Y moment), E right.
  The handoff's `(−spinSign)` factor did the opposite for both rotor directions; `+spinSign` is the
  corrected form. Check for a CCW rotor: Q → `u = +1` → more tail thrust pushing the tail right →
  `Cross((0,0,−L), (F,0,0)) = (0, −L·F, 0)`, nose left.
- Pedal travel is split per side so full pedal reaches the ends of the tail pitch range.
- `GetPointVelocity` includes rotation, so when the fuselage yaws the tail hub moves along the tail
  axis and the `kvt` term opposes it — yaw damping for free.
- `Tt` may go negative at negative pitch; that is correct.
- The tail rotor has **no disk lag and no tilt**: its thrust axis is always `tailAxis`. A lagging
  tail disk would tilt the tail thrust fore or aft while yawing (~6° at 1 rad/s), adding a false
  longitudinal force. In code it is the same `RotorModel` with zero tilt range and zero lag.
- Tail thrust acts above or below the CG, giving a small roll moment and the translating tendency.
  Both are real; keep them. The hover case is cancelled by the main rotor's lateral rigging (see
  *Main rotor*); off-hover, the pilot corrects with cyclic.
- **Sideways motion costs tail thrust.** `Vaxial` includes the whole aircraft's lateral velocity,
  not only yaw: drifting in the direction the tail rotor pushes loses `kvt·Ωt` of thrust per m/s
  (~400 N with the derived `kvt`). With `kvt` derived from the max yaw rate, the tail rotor carries
  all of the yaw damping, which overstates this ~3× against a real tail rotor.

### Engine, governor and rotor speed

```
Qcmd   = Kgov·(Ωtarget − Ω)
Qmax   = Pmax / max(Ω, Ωmin)                 // power-limited torque
Qcmd   = clamp(Qcmd, 0, Qmax)                // the engine never brakes the rotor
Qeng  += clamp(Qcmd − Qeng, −rate·dt, rate·dt)
Ω̇      = (Qeng − Q − gt·Qt − Qfriction) / Itotal
Ω      = max(Ω + Ω̇·dt, 0)
Itotal = Imain + gt²·Itail
```

- `Kgov` — governor gain: how much torque it asks for per rad/s of rotor speed error. P-only, so it
  settles below its target by `torque / Kgov` under load (~2 % at hover with the defaults).
- `Ωtarget` is therefore **not** `Ωnom`: it is raised by the hover droop,
  `Ωtarget = Ωnom + Qhover_total / Kgov`, where `Qhover_total` is main rotor drag + `gt`·tail rotor
  drag + friction at hover. A steady hover then settles exactly at `Ωnom`, so mid collective still
  hovers. Side effect: at low collective the rotor runs ~2 % fast, as real P governors do. Computed
  from the effective (post-override) coefficients.
- `Qmax` — the engine can give at most `Pmax` of power, so the torque available falls as rotor
  speed rises. `Ωmin` stops it dividing by zero at standstill.
- `rate` — how fast engine torque can change (spool lag).
- `gt·Qt` — tail rotor drag referred to the main shaft; heavy pedal steals engine power.
- The engine always runs; engine on/off is out of scope.

### Torque reaction

```
Qshaft = Q + Imain·Ω̇            // Ω̇ treated as 0 on a step where Ω is clamped at 0
M      = −Qshaft·spinSign·shaftUp
```

The fuselage is turned by the torque the transmission puts into the main shaft, not by the rotor's
drag directly. The handoff used `Q` alone; the shaft torque replaces it.

- In steady flight `Ω̇ = 0`, so `Qshaft = Q` and all tuning below holds unchanged.
- A collective pull first slows the rotor; the yaw kick builds as the engine spools up instead of
  arriving instantly.

### Fuselage drag

```
vLocal = transform.InverseTransformDirection(rb.GetPointVelocity(cpPos))
Flocal = −0.5·ρ·Scale(fAreas, Scale(vLocal, Abs(vLocal)))     // fAreas = per-axis Cd·A
```

Applied at a centre of pressure slightly **behind** the CG, which gives a cheap weathervane effect.
Without drag the helicopter accelerates horizontally forever. Optional extra angular damping
`−kω ⊙ angularVelocity` per body axis — 0 on the asset; it has no real-world basis at the values
that would stop a held key from flipping the aircraft, so it is a keyboard concession if used.

**Horizontal stabilizer** — the small wing on the tail boom:
```
vLocal = transform.InverseTransformDirection(rb.GetPointVelocity(stabilizerPos))
Fup    = −0.5·ρ·a·S·vLocal.y·|vLocal.z|           // along body up, applied at the stabilizer
```
- `a` — lift slope, lift per radian of airflow angle (~3.5 for a short wing); `S` — area (2 m²).
- `vLocal.y` is the airflow across the wing, `vLocal.z` along it. Flying forward nose-down tilts
  the body up axis into the flight path, so the tail is pushed down and the nose lifted: pitch
  stability. Because the velocity is sampled at the tail, pitch rotation also feeds `vLocal.y` and
  is resisted: pitch damping (∝ `S·arm²·speed`). Zero in a hover, growing with airspeed.
- `y·|z|` equals `V²·sinα·cosα`, the textbook lift with the airflow angle `α`, so the force stays
  bounded at large angles (peaks at 45°). No stall and no rotor downwash on the stabilizer —
  detailed aerodynamics.
- Area 0 turns it off. In a 2D pitch study, it was what turned a held cyclic into a settled
  cruise rather than an endless swing.

Gravity comes from Unity (`useGravity = true`); weight is never added manually.

### Landing gear

Raycast spring-dampers at four skid points, pushing along the hit normal:
`F = k·compression + c·compressionRate`, clamped at ≥ 0 (the ground never pulls). **No
WheelColliders** for skids.

- Each ray starts `SuspensionLength` (0.3 m) above its contact point along body up and is cast that
  far down. `compression = SuspensionLength − hitDistance`: zero when the ground is exactly at the
  contact point, positive as the skid sinks in. Contact points therefore sit at the skid's unloaded
  bottom; at rest the skids sink ~5 cm.
- `compressionRate = −Dot(contactVelocity, hitNormal)` — from velocity, so the normal force needs
  no state between steps.
- **Friction is anchored** (stick-slip), so the skids can hold the aircraft perfectly still:
  ```
  anchor   = hit point at touchdown, per contact, world space
  d        = ProjectOnPlane(hitPoint − anchor, normal)          // how far the skid has been pushed
  F        = −kf·d − cf·slipVelocity                            // stick: spring back to the anchor + damping
  if |F| > μ·N:  F = μ·N·F/|F|,  anchor = hitPoint + F/kf      // slip: cap the force, drag the anchor along
  ```
  `kf` is the friction stiffness (2e5 N/m per contact), `cf` the friction damping (2e4 N·s/m). The
  anchor is cleared when the contact leaves the ground. Isotropic — sliding along the skids is as
  hard as sideways.
- **Why anchored.** Purely viscous friction (`−cf·slipVelocity`, capped) only resists motion that is
  already happening, so any steady load makes the skids creep. With the rotor turning at flat pitch
  the tail rotor leaves ~14 kN·m of nose-left yaw (tail thrust is set to balance hover torque, the
  main rotor makes only profile torque at 0° pitch), which crept the helicopter round at ~2.6°/s on
  the ground. Anchored, the same load costs ~9 mm of give per contact.
- As collective rises, the load on the skids — and so the friction limit — falls while the yaw
  imbalance shrinks toward zero at hover collective; around lift-off the nose becomes free and needs
  pedal, as in a real helicopter.
- The skids replace collision response with the ground only. Movement is still Unity's Rigidbody,
  the ground is found with `Physics.Raycast`, and the hull collider handles crashes.

### Applying forces (each fixed step)

1. Read control positions.
2. Sample the body: point velocities at the hubs and centre of pressure, shaft axes from the hub
   transform.
3. Main rotor: update `n`, compute `T`, `Q`.
4. Tail rotor: compute `Tt`, `Qt`.
5. Drivetrain: update `Qeng`, integrate `Ω`.
6. Apply:
   ```csharp
   rb.AddForceAtPosition(T * n, mainHubPos);              // lift and control moments; hub above CG
   rb.AddTorque(-Qshaft * spinSign * shaftUp);            // torque reaction
   rb.AddTorque(Kβ * Vector3.Cross(shaftUp, n));          // hub moment, 0 for teetering rotors
   rb.AddForceAtPosition(Tt * tailAxis, tailHubPos);      // anti-torque
   rb.AddForceAtPosition(fuselageDragWorld, cpPos);
   rb.AddForceAtPosition(stabilizerForceWorld, stabilizerPos);
   // + skid contact forces
   ```
7. Write `HelicopterState`, including `LocalDiskNormal` (`n` in the body frame of this step) for
   the rotor visual.

## Configuration and coefficients

`HelicopterConfig` holds tuning **targets**. `HelicopterCoefficients.Derive` computes the
coefficients from them; `WithOverrides` replaces any coefficient whose override is enabled with its
manual value; `HelicopterParameters` packs the result per part (`RotorParameters` ×2,
`DrivetrainParameters`, `FuselageParameters`, `SkidGearParameters`).

- **Units.** Serialized fields are in inspector units — degrees, rpm, kW, % — and carry the unit in
  their name. The config's public getters return SI and radians; conversion happens only there.
- **Geometry** lives in the config as offsets **relative to the centre of mass**: main hub, tail
  hub, centre of pressure, stabilizer, skid contacts. The centre of mass itself is relative to the
  prefab root.
  `Larm = −TailHubOffset.z`. **The config is the only source of physics geometry** — forces are
  applied at these offsets, never at prefab transforms, so the derivation and the simulation can't
  disagree. Prefab transforms only place visuals; if the model and the config disagree, correct
  the config.
- **Blade pitch from controls** is `HelicopterParameters.MainRotorPitch(collective)` and
  `TailRotorPitch(pedals)`; the pedal sign rule lives there.
- **Inspector** (`HelicopterConfigEditor`): the config fields, then a *Coefficients* table — derived
  value, override toggle, manual value. Enabling an override seeds it with the derived value. Below
  it a *Hover balance* readout from the effective parameters: hover pitch, thrust vs. weight, main
  rotor torque, tail thrust, yaw imbalance, drivetrain power vs. max, tail rotor speed, governor
  target. With no overrides thrust/weight is 100 % and yaw imbalance 0; an override shows how far it
  moves the aircraft off its targets.
- Parameters are built once when the helicopter starts. Changing the config in play mode does not
  reach the running simulation.

**Overriding a coefficient breaks the target it was derived from** — overriding `kT` means mid
collective no longer hovers. The inspector shows the derived value beside the override so the
deviation stays visible.

### Derivation

| # | Target | Derived |
|---|---|---|
| 1 | Hover at `c = 0.5`, `Ω = Ωnom`, `Vup = 0`: `T = m·g` | `θh = lerp(θmin, θmax, 0.5)`; `kT = m·g / (Ωnom²·θh)` |
| 2 | Main rotor hover power `Phover`, 30 % profile / 70 % induced | `Qh = Phover / Ωnom`; `q0 = 0.3·Qh / Ωnom²`; `vh = 0.7·Phover / (m·g)` |
| 3 | Max climb rate `Vclimb` at full collective (fuselage drag ignored) | `kv = kT·Ωnom·(θmax − θh) / Vclimb` |
| 4 | Tail rotor balances hover torque at centred pedals: `Tt·Larm = Qh` (`Larm` = horizontal CG-to-tail-hub distance) | `Ωt_nom = gt·Ωnom`; `kTt = (Qh / Larm) / (Ωt_nom²·θtNeutral)` |
| 5 | Tail rotor uses a share `st` of hover power (10–15 %), split 30/70 like the main rotor | `q0t = 0.3·(st·Phover / Ωt_nom) / Ωt_nom²`; `vht = 0.7·st·Phover / Tt_hover`, `Tt_hover = Qh / Larm` |
| 6 | Max yaw rate `rmax` at full pedal in hover | `kvt = kTt·Ωt_nom·(θtMax − θtNeutral) / (rmax·Larm)` |
| 7 | Engine power `Pmax ≈ 1.3–1.5 × Phover` | `Qmax_nom = Pmax / Ωnom` |
| 8 | Spool time `tspool` from hover to full torque (0.5–2 s) | `rate = (Qmax_nom − Qh) / tspool` |
| 9 | Governor droop `δΩ` (fraction of `Ωnom`) at which it commands full torque | `Kgov = Qmax_nom / (δΩ·Ωnom)` |
| 10 | Hover with centred lateral cyclic: main rotor tilt cancels the tail thrust's roll moment | `rig = spinSign·Tt_hover·tailHub.y / (m·g·mainHub.y)` (offsets relative to the CG) |

Rows 1–5 and 7 come from the original plan. Rows 6, 8 and 9 are proposed here to keep every
coefficient target-driven; they replace hand-picked `kvt`, `rate` and `Kgov`. Row 10 was added in
milestone 3, when the unopposed translating tendency made the hover unflyable on a keyboard.
Flapback `Kfb` is a plain config value, not derived.

`Phover` is the **main rotor's** hover power; the drivetrain total at hover adds the tail share and
friction (~791 kW with the defaults). Each rotor's hover thrust `Th` is computed from its effective
coefficients at neutral pitch and nominal speed, so a `kT` override keeps `vh` meaning "downwash at
this rotor's hover thrust".

Derived values with the defaults: `kT ≈ 279.9`, `kv ≈ 124.3`, `q0 ≈ 5.376`, `vh ≈ 11.6 m/s`,
`rig ≈ −0.0316 rad` (1.81° left), `kTt ≈ 0.5738`, `kvt ≈ 2.309`, `q0t ≈ 0.004863`, `vht ≈ 24.5 m/s`, `rate ≈ 8842`, `Kgov ≈ 28956`.
Hover: main torque ≈ 20.63 kN·m, tail thrust ≈ 2.399 kN, drivetrain power ≈ 791 kW, governor
target ≈ 331.7 rpm.

**Climb rate vs. power.** Row 3 sets `kv` assuming rotor speed holds at full collective, but a
sustained 10 m/s climb needs ~1,045 kW against the 1,000 kW `Pmax`, so the rotor droops slightly and
the climb settles a little lower (~9 m/s observed). Raise `Pmax` to ~1,060 kW if the full target
matters.

**Yaw damping split (proposed, not built).** Row 6 puts all yaw damping on the tail rotor, which
makes it overly sensitive to sideways drift (see *Tail rotor*). A real aircraft gets much of its
yaw damping from the tail boom and fin, which react to rotation but not to drift. Proposal: a
config share `sy` (~40 %) for the tail rotor; `kvt` from row 6 scaled by `sy`, and the remainder
`(1 − sy)·kvt·Ωt_nom·Larm²` added as fuselage yaw damping (`AngularDamping.y`), keeping the total —
and so the max yaw rate and yaw response time — unchanged. With the defaults: `kvt ≈ 0.924`, yaw
damping ≈ 17,700 N·m·s/rad. Currently approximated on the asset by the `kvt` override alone.

### Starting values (UH-1H-like, approximate)

| Parameter | Value |
|---|---|
| mass | 4,300 kg |
| inertia (Unity axes: X pitch, Y yaw, Z roll) | ≈ (24,700, 21,000, 6,500) kg·m² — roll raised from 3,750 in milestone 3 to match published UH-1H estimates; calmer, less twitchy roll |
| main hub above CG | ~1.8 m |
| `Larm` (tail hub behind CG) | ~8.6 m |
| `Ωnom` | 33.9 rad/s (324 rpm) |
| `gt` | ~5.1 |
| `θmin..θmax` | 0 .. 0.26 rad (0–15°) |
| `θtMin..θtMax` | −0.14 .. 0.35 rad (−8° .. +20°) |
| `θtNeutral` | 8° — sets how much pedal authority lies each side of hover |
| `maxTiltLon / maxTiltLat` | 0.070 / 0.052 rad (4° / 3°) — lowered from 8° / 6° in milestone 5 so a held key is a smaller input |
| `τ` disk lag | 0.1 s (0.08–0.12) |
| `Kfb` flapback | 0.6° per 10 m/s (≈ 0.001 rad per m/s); lower gives a slower, gentler phugoid |
| `Phover` / `Pmax` | ~700 kW / ~1,000 kW |
| `Imain` | ~6,000 kg·m² |
| `Vclimb` | ~10 m/s |
| `fAreas` (Cd·A, local x/y/z) | ~(10, 15, 2) m² |
| `spinSign` | −1 (CCW, US) |
| `Kβ` | 0 (teetering) |
| skid spring `k` per contact | ~2.1e5 N/m (≈ 5 cm static compression on 4 contacts) |
| skid damper `c` per contact | ~1.8e4 N·s/m |
| `st` tail power share | 12 % |
| `rmax` max yaw rate | 60°/s |
| `tspool` | 1 s |
| `δΩ` governor droop | 3 % |
| `Itail` | 15 kg·m² |
| `Qfriction` | 200 N·m |
| `Ωmin` power-limit floor | 10 % of `Ωnom` |
| centre of mass | (0, 1.4, 0) from the prefab root |
| main hub / tail hub / centre of pressure / stabilizer offsets | (0, 1.8, 0) / (0, 1, −8.6) / (0, 0, −0.5) / (0, 0, −6) |
| stabilizer area / lift slope | 2 m² / 3.5 per rad |
| skid contact offsets | (±1.3, −1.4, ±1.5) |
| skid suspension length | 0.3 m |
| skid friction `μ` / stiffness / damping | 0.6 / 2e5 N/m / 2e4 N·s/m |
| `kvt` override on the UH-1 asset | 0.924 (derived 2.309) — see *Current state* |
| `ρ` air density | 1.225 kg/m³ |

## Controls

| Control | Keys | Behaviour | Rate |
|---|---|---|---|
| Collective | Space / Left Ctrl | Holds position when released; range [0, 1] | ~0.4–0.5 /s |
| Cyclic | W / S, A / D | Ramps toward ±1 while held, returns to 0 on release | ~3 /s both ways |
| Pedals | Q / E | Ramps toward ±1 while held, returns to 0 on release | ~2 /s |
| Restart / quit | R / Esc | Not flight controls; `Session` map | — |

- Asset `Assets/TGLabTest/Gameplay/Controls/FlightInput.inputactions`, with **Generate C# Class**
  on (namespace `TGLabTest.Gameplay.Controls`). It generates `FlightInput.cs` next to the asset —
  never edit that file. `Flight` map: `Cyclic` (2D composite, WASD, `mode=2` so diagonals are
  not normalized and each axis reaches ±1), `Collective` (1D axis, Space +, Ctrl −), `Pedals` (1D
  axis, Q −, E +). `Session` map: `Restart` (R), `Quit` (Esc). One control scheme, `Keyboard`.
  After editing the asset, Unity regenerates the wrapper on reimport; code using a new action
  fails to compile until it has.
- `SessionControls` enables the `Session` map. Restart reloads the active scene by build index —
  Zenject rebuilds both contexts, so everything resets; the scene must be in the build list. Quit
  calls `Application.Quit()`, or leaves play mode in the Editor.
- **Asset name ≠ `<MapName>Actions`.** The generator names each map's struct `<MapName>Actions`; an
  asset called `FlightActions` would generate a `FlightActions` struct inside a `FlightActions`
  class (CS0542). Hence `FlightInput`.
- The wrapper is bound once in the SceneContext with `BindInterfacesAndSelfTo`, so Zenject disposes
  it. `FlightControls` enables the `Flight` map on initialize and disables it on dispose. Each
  wrapper instance owns its own copy of the actions, so Project-wide Actions are irrelevant to it;
  the template asset has been deleted and Project-wide Actions is empty.
- Ramps and hold live only in `FlightControls`, ticked in `Update`; the simulation consumes
  positions. Rates are named constants there (collective 0.45 /s, cyclic 3 /s, pedals 2 /s).
- **Analogue devices later.** Bindings are per device — a gamepad does nothing until `<Gamepad>`
  bindings are added. Sticks mean absolute positions while keys mean rate commands, and both read
  1.0 at full deflection, so they need separate control schemes (`Keyboard`, `Gamepad`) with
  `FlightControls` choosing ramp or pass-through by the active scheme.

## Unity setup

**Rigidbody** — `HelicopterSimulation.Initialize` applies all of these from the config, overriding
whatever the prefab's Rigidbody says; only `constraints` are left to the prefab (used to freeze
rotations for milestones 1–2):
- `mass` from the config.
- `automaticCenterOfMass = false`, `automaticInertiaTensor = false`; set `centerOfMass`,
  `inertiaTensor` and `inertiaTensorRotation` from the config. Collider-derived values are badly
  wrong for helicopters.
- `linearDamping = 0`, `angularDamping = 0` — all damping comes from the model.
- `useGravity = true`.
- `interpolation = Interpolate`; the camera follows in `LateUpdate`.

**Hull collider.** A simple collider on the prefab from milestone 1, so the helicopter can sit on
the ground before the skids exist. Once the skids work, their springs hold it above the ground and
the collider is only a crash hull.

**Camera.** `HelicopterCamera` sits behind and above the helicopter along its **heading only** —
pitch and roll are not followed, so attitude stays readable against the horizon. The heading
follows with an exponential lag. Distance, height, look height and lag are serialized fields.

**Rotors** are not Rigidbodies; they exist only as math. `RotorVisual` spins each rotor mesh in
`Update` from `Ω` (tail: `gt·Ω`), main rotor in the `spinSign` direction, about a serialized
local axis per rotor; the main rotor is also tilted by `LocalDiskNormal` relative to the
interpolated body, so it doesn't jitter between physics steps. One transform per rotor, pivot at
the hub. No blur disk: blades only, with HDRP motion blur (raise its *Maximum Velocity* for
rotors). At 60 fps the tail rotor turns ~165° per frame and strobes. The HeloMarine FBX stores the
rotor hubs as Maya pivots with the nodes at the model origin; Unity normally bakes the pivot into
the transform. If a rotor orbits instead of spinning in place, it didn't — wrap the mesh in an
empty at the hub and assign that.

**Config gizmos.** With the helicopter selected, `HelicopterInstaller` draws the config's force
points in the Scene view: centre of mass (white), main hub (yellow), tail hub and thrust direction
(red), centre of pressure and stabilizer (cyan), skid contacts (green). The HeloMarine model is
lined up with them by eye — rotors roughly at their hubs, not an exact match. If the model is
replaced, line it up the same way; the config stays the source of physics geometry.

**Floating origin** is not needed for a single location. It becomes necessary beyond ~5–10 km
from the origin: HDRP camera-relative rendering fixes visual jitter but not physics precision.

## Telemetry overlay

A developer tool, not part of the player experience (no HUD in the current scope). IMGUI, reading
`IHelicopterState`: control positions, blade pitches, rotor rpm and %, main and tail thrust,
stabilizer force (negative pushes the tail down), engine and shaft torque, engine power, vertical
and ground speed, ground contact. The R / Esc hints sit at the bottom of the same box, so the
overlay is also the only place they are shown.

## Upgrade path: detailed aerodynamics

Not in the current scope. Replaces the simple thrust formula and the axial-only momentum-theory
torque inside `RotorModel` with a full blade-element and momentum-theory rotor; the rest of the
architecture stays the same.

```
μ  = |V in disk plane| / (ΩR)                              // advance ratio
λc = (V · n) / (ΩR)                                        // climb inflow
Ct = (σa/2)·[θ0(1/3 + μ²/2) + θtw(1 + μ²)/4 − λ/2]         // blade element
λi = Ct / (2√(μ² + λ²)),  λ = λc + λi                      // momentum theory; iterate Ct ⇄ λ 5–10×
T  = Ct·ρ·A·(ΩR)²
Cq = κ·Ct·λi + Ct·λc + (σ·Cd0/8)(1 + 4.6μ²)
Q  = Cq·ρ·A·(ΩR)²·R
```

This adds translational lift, forward-flight power savings and autorotation (`Ct·λc` going
negative drives the rotor) without special-case code.
- Ground effect: multiply thrust by `1 / (1 − (R/4z)²)`.
- Vortex ring state: an empirical correction to `λi`.
- Reference: NASA TM-84281, *A Mathematical Model of a Single Main Rotor Helicopter for Piloted
  Simulation* (Talbot et al., 1982).

## Pitfalls

- **Namespace segments that shadow Unity types.** A namespace ending in `.Input` makes `Input`
  resolve to the namespace; one ending in `.Editor` breaks `: Editor` base classes in inspectors.
  Hence `TGLabTest.Gameplay.Controls` and `TGLabTest.EditorTools`.
- **The scene can't see the helicopter's bindings.** A GameObjectContext inherits from the
  SceneContext, not the other way round. Scene-level objects reach the aircraft through
  `ActiveHelicopter`.
- **Fixed ticks inside a GameObjectContext** are driven by the context's own kernel: with the
  *Kernel* field empty, `GameObjectContext` adds a `DefaultGameObjectKernel` component whose
  `FixedUpdate` runs the context's `IFixedTickable`s. Order relative to other scripts is only
  controllable by assigning a custom `MonoKernel` subclass with a script execution order.
- **Sample the physics pose, not the transform.** With interpolation on, `transform` holds the
  interpolated render pose; `HelicopterSimulation` reads `Rigidbody.rotation` and
  `worldCenterOfMass` instead. Presentation reads the transform, which is what it should show.
- **The ground probe must skip the helicopter's own colliders** — the ray starts above the skid,
  possibly inside the hull. `RaycastGroundProbe` drops hits whose `rigidbody` is the helicopter's,
  so no layer setup is needed.
- **`n` must stay in world space.** Storing it relative to the shaft removes the disk lag that
  damps pitch and roll. Presentation uses the separate `LocalDiskNormal`; tilting a visual to the
  world `n` would jitter against the interpolated fuselage.
- **Overrides silently break targets** — see *Configuration and coefficients*.