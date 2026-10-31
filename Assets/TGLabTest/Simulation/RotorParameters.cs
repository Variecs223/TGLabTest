namespace TGLabTest.Simulation
{
    public sealed class RotorParameters
    {
        public float ThrustCoefficient { get; }
        public float AxialDamping { get; }
        public float BaseTorqueCoefficient { get; }
        public float PitchTorqueCoefficient { get; }
        public float MinPitch { get; }
        public float NeutralPitch { get; }
        public float MaxPitch { get; }
        public float MaxTiltLongitudinal { get; }
        public float MaxTiltLateral { get; }
        public float DiskLag { get; }
        public float HubStiffness { get; }

        public RotorParameters(
            float thrustCoefficient,
            float axialDamping,
            float baseTorqueCoefficient,
            float pitchTorqueCoefficient,
            float minPitch,
            float neutralPitch,
            float maxPitch,
            float maxTiltLongitudinal,
            float maxTiltLateral,
            float diskLag,
            float hubStiffness)
        {
            ThrustCoefficient = thrustCoefficient;
            AxialDamping = axialDamping;
            BaseTorqueCoefficient = baseTorqueCoefficient;
            PitchTorqueCoefficient = pitchTorqueCoefficient;
            MinPitch = minPitch;
            NeutralPitch = neutralPitch;
            MaxPitch = maxPitch;
            MaxTiltLongitudinal = maxTiltLongitudinal;
            MaxTiltLateral = maxTiltLateral;
            DiskLag = diskLag;
            HubStiffness = hubStiffness;
        }

        public float Thrust(float rotorSpeed, float pitch, float axialVelocity) =>
            ThrustCoefficient * rotorSpeed * rotorSpeed * pitch - AxialDamping * rotorSpeed * axialVelocity;

        public float DragTorque(float rotorSpeed, float pitch) =>
            rotorSpeed * rotorSpeed * (BaseTorqueCoefficient + PitchTorqueCoefficient * pitch * pitch);
    }
}