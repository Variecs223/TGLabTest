using UnityEngine;

namespace TGLabTest.Simulation
{
    public sealed class HelicopterState : IHelicopterState
    {
        public ControlPositions Controls { get; set; }
        public float MainRotorPitch { get; set; }
        public float TailRotorPitch { get; set; }
        public float RotorSpeed { get; set; }
        public float RotorSpeedRatio { get; set; }
        public Vector3 DiskNormal { get; set; } = Vector3.up;
        public Vector3 LocalDiskNormal { get; set; } = Vector3.up;
        public float MainRotorThrust { get; set; }
        public float MainRotorDrag { get; set; }
        public float TailRotorThrust { get; set; }
        public float TailRotorDrag { get; set; }
        public float EngineTorque { get; set; }
        public float ShaftTorque { get; set; }
        public float EnginePower { get; set; }
        public Vector3 Velocity { get; set; }
        public bool HasGroundContact { get; set; }
    }
}