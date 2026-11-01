# Game Design

How the simulator is meant to work. This is intent, not a description of the current build.
Equations, constants and tuning live in `PROJECT-NOTES.md`; code structure lives in
`ARCHITECTURE.md`.

**Status legend** — every item carries one:
- **Planned** — agreed, not started.
- **In progress** — partly built, or built but not yet behaving as described here (the item says which).
- **Complete** — built and matching this file.

## Concept

**In progress** — flyable with rotor visuals; the location is missing. A realistic helicopter flight simulator. The player flies a single helicopter around
one location, seen from a third-person camera. There are no missions, objectives or scoring — the
experience is the flying itself.

### End goal

**Complete** — the handling reads as a helicopter at low speed and was accepted after milestone 5 tuning. Holding a cyclic key from a hover still pitches the aircraft over, as full cyclic would in a real one. The helicopter must **feel** like a real one and be **hard to fly** in the same ways a
real one is. A 1:1 match of real-world physics is not the goal: in-depth aerodynamics is
deliberately left out (see *Scope*), and the remaining model is tuned so the handling still reads
as authentic.

The difficulty must come from the physics, never from scripted behaviour:
- A hover is naturally unstable and needs constant small corrections.
- The controls are coupled — moving one forces the pilot to correct with the others.
- There are **no pilot assists**: no stability augmentation, no auto-hover, no auto-trim.

## World

| Item | Status | Description |
|---|---|---|
| Location | Planned | A single simple outdoor location to take off from, fly around and land in. Built later. |
| Helicopter | Complete | One aircraft with a simple visual model. Medium utility helicopter in the UH-1 class: two-blade main rotor turning counterclockwise seen from above, tail rotor on the left of the tail boom, skid landing gear. The rotors spin at the simulated speed and the main rotor visibly tilts with the disk. |
| Camera | Complete | Third-person view following the helicopter's heading. |
| HUD | Planned (not now) | No HUD in the current scope. A developer telemetry overlay is built for testing and tuning, but it is not part of the player experience. |

## Flight model

**Complete.** The helicopter is one rigid body. Everything that acts on it — rotor thrust, torque
reaction, tail rotor thrust, fuselage drag, gravity, ground contact — is a force or moment applied
to that body each physics step. The rotors are not physical objects; they exist only as a model
whose state drives the forces, and their visuals follow that state.

The model simulates these parts:

| Part | Status | What it simulates |
|---|---|---|
| Main rotor | Complete | Thrust from blade pitch and rotor speed; drag torque from the power the rotor delivers. Climbing reduces thrust and descending increases it, which damps vertical motion and caps the climb rate. The rotor disk tilts with cyclic, and the tilt lags slightly behind the fuselage, which damps pitch and roll. The cyclic is rigged so that centred cyclic holds a hover without sideways drift. Moving through the air tilts the disk away from the airflow (flapback), so a moving helicopter tends to slow and level itself — with a slow overshooting swing the pilot has to damp. |
| Tail rotor | Complete | The same model, smaller, geared to the main rotor. Its thrust opposes the main rotor's torque reaction. Its response to the fuselage swinging provides yaw damping. |
| Engine and governor | Complete | Delivers torque to the rotors to hold a target rotor speed. Its output is limited in both maximum power and how fast it can change, so heavy loads droop rotor RPM until it catches up. Fuel is infinite. |
| Fuselage | Complete | Air drag on all three axes, acting slightly behind the centre of mass, so the helicopter limits its own top speed and tends to weathervane into its direction of travel. |
| Horizontal stabilizer | Complete | A small wing on the tail boom. In forward flight it pushes the tail down when the nose drops and resists pitching, so a moving helicopter settles at a steady attitude and the slow swing dies out. No effect in a hover. |
| Landing gear | Complete | Four spring-damped contact points on the skids with ground friction, allowing take-off, landing and sitting on the ground. |

## Controls

**Complete.** Keyboard only, through the Unity Input System. Keys move control **positions**; the
physics only ever reads those positions, never the keys.

**Keyboard limitation.** A real cyclic and real pedals stay where the pilot leaves them. Keys can't
hold an analogue position, so cyclic and pedals ramp toward full deflection while held and return
to centre when released. This is a deliberate concession to the keyboard. Any future analogue
device (joystick, gamepad) should drive absolute positions instead.

| Keys | Control | Control behaviour | What it changes | What the pilot sees |
|---|---|---|---|---|
| Space / Left Ctrl | Collective | Space raises, Ctrl lowers, at a moderate rate. **Holds its position when released**, like a real collective lever. Range from fully down to fully up. | Pitch of all main rotor blades together. | More pitch: more thrust, so the helicopter climbs — and more rotor drag, so the engine is loaded harder. Mid-travel holds a hover. |
| W / S | Cyclic, fore/aft | Ramps toward full deflection while held, returns to centre when released. | Tilts the main rotor disk forward or back. | The tilted thrust pitches the nose down (W) or up (S), and the helicopter accelerates in that direction. |
| A / D | Cyclic, left/right | Same as W / S. | Tilts the main rotor disk left or right. | The helicopter rolls and slides toward that side. |
| Q / E | Pedals | Ramp toward full deflection while held, return to centre when released. Centred pedals balance the torque of a steady hover. | Pitch of the tail rotor blades. | The nose yaws left (Q) or right (E). |

Two session keys sit outside the flight controls: **R** restarts the simulation and **Esc** quits.
Both are listed on screen, since they aren't part of a helicopter's controls.

### Control coupling

**Complete.** None of this is scripted; all of it must emerge from the model:
- **Collective → yaw.** Raising collective loads the rotor harder and increases the torque
  reaction, so the nose yaws and needs pedal to hold heading. Lowering it does the opposite.
- **Pedals → drift and roll.** More tail rotor thrust also pushes the whole helicopter sideways
  (translating tendency) and adds a small roll, so the pilot holds a little opposite cyclic.
- **Pedals → power.** The tail rotor is driven by the same engine, so heavy pedal use costs power
  too.
- **Load → rotor speed.** A sharp collective pull or heavy pedal outpaces the engine. Rotor RPM
  droops, thrust drops with it, and both recover as the engine catches up.

### Optional controls

| Control | Status | Description |
|---|---|---|
| Joystick / gamepad | Planned (later) | Analogue axes with absolute positions, replacing the keyboard ramps. |

## Scope

### In scope

The flight model and controls above, the third-person camera, one helicopter and one location.

### Deliberately out of scope

- **Fuel** — infinite, mass is constant.
- **Engine on/off** — the engine always runs at the governed flight rotor speed.
- **Extra flight controls** such as force trim — the simulator flies on the keys above and nothing
  else.
- **Detailed aerodynamics** — full inflow modelling (only a vertical momentum-theory estimate of
  rotor power is used), translational lift, ground effect, vortex ring state, retreating blade
  stall. Flapback is in, but as a single linear coefficient, not a blade model; the horizontal
  stabilizer has no stall and ignores the rotor downwash.
- **Rotor blur disk** — the blades are shown spinning, with the render pipeline's motion blur.
- **High-speed flight tuning** — top speed is limited by fuselage drag but was not tuned or
  verified; fast flight is too hard to control on a keyboard to be a goal.
- **Autorotation** — it depends on the inflow model above.
- **Gyroscopic precession** — real swashplates are rigged to cancel it; stick forward simply tilts
  the disk forward.

### Upgrade path

**Planned (not in current scope).** The simple rotor model can later be replaced with a
blade-element and momentum-theory rotor without changing the rest of the design. That would bring
translational lift, forward-flight power savings and autorotation, with ground effect and vortex
ring state as add-ons.

## Milestones

Each stage is accepted when its checks hold.

| # | Stage | Status | Acceptance checks |
|---|---|---|---|
| 1 | Vertical only (rotation frozen) | Complete | Mid collective holds a hover. Full collective climbs and settles at a steady maximum climb rate. A fast collective pull shows visible RPM droop, then recovery. |
| 2 | Yaw | Complete | Collective changes cause yaw that needs pedal to correct. With pedals released at hover collective, the helicopter only drifts slowly in yaw. |
| 3 | Full six degrees of freedom | Complete | Hover is possible but needs constant small corrections. Forward cyclic drops the nose and the helicopter accelerates forward. |
| 4 | Fuselage drag and skids | Complete | Take-off from and landing on the ground. A moving helicopter slows down again (the top-speed check was descoped). |
| 5 | Visuals and tuning | Complete, except the location (planned) | Rotor visuals, third-person camera, simple helicopter model and location; handling tuned against the targets in `PROJECT-NOTES.md`. Tuning added the horizontal stabilizer and lowered the cyclic's disk tilt range. |

## Glossary

- **Collective** — lever changing the pitch of all main rotor blades together (up/down thrust).
- **Cyclic** — stick tilting the rotor disk (pitch, roll, direction of travel).
- **Pedals** — tail rotor pitch (yaw).
- **Blade pitch** — angle of a blade relative to the plane it spins in.
- **Rotor disk** — the plane the blade tips sweep. Thrust acts perpendicular to it.
- **Swashplate** — mechanism turning collective and cyclic inputs into blade pitch.
- **Flapping** — blades moving up and down on their hinges; this is what tilts the disk. The mast
  itself stays fixed to the fuselage.
- **Teetering / articulated rotor** — a two-blade rotor rocking on the mast as one piece, which
  transmits no hub moment / a multi-blade rotor with hinges per blade, which does.
- **Torque reaction** — the fuselage being turned opposite to the rotor's rotation.
- **Governor** — engine controller holding rotor speed.
- **RPM droop** — the rotor slowing when the load exceeds what the engine can deliver at that moment.
- **Translating tendency** — sideways drift caused by tail rotor thrust.
- **Rigging** — a built-in offset of the cyclic, so centred controls already hold a hover.
- **Flapback** — the rotor disk tilting away from the airflow when the helicopter moves.
- **Phugoid** — the slow swing between speeding up and slowing down that flapback produces.
- **Horizontal stabilizer** — small fixed wing on the tail boom giving pitch stability in forward flight.
- **Weathervaning** — the fuselage turning to point into the airflow, like a weathervane.
