using UnityEngine;

namespace TGLabTest.Simulation
{
    public interface IHelicopterState
    {
        ControlPositions Controls { get; }
        float MainRotorPitch { get; }
        float TailRotorPitch { get; }
        float RotorSpeed { get; }
        float RotorSpeedRatio { get; }
        Vector3 DiskNormal { get; }
        Vector3 LocalDiskNormal { get; }
        float MainRotorThrust { get; }
        float MainRotorDrag { get; }
        float TailRotorThrust { get; }
        float TailRotorDrag { get; }
        float StabilizerForce { get; }
        float EngineTorque { get; }
        float ShaftTorque { get; }
        float EnginePower { get; }
        Vector3 Velocity { get; }
        bool HasGroundContact { get; }
    }
}