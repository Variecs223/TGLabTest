using UnityEngine;

namespace TGLabTest.Simulation
{
    public sealed class RotorParameters
    {
        public float ThrustCoefficient { get; }
        public float AxialDamping { get; }
        public float BaseTorqueCoefficient { get; }
        public float HoverInducedVelocity { get; }
        public float HoverThrust { get; }
        public float MinPitch { get; }
        public float NeutralPitch { get; }
        public float MaxPitch { get; }
        public float MaxTiltLongitudinal { get; }
        public float MaxTiltLateral { get; }
        public float LateralRigging { get; }
        public float Flapback { get; }
        public float DiskLag { get; }
        public float HubStiffness { get; }

        public RotorParameters(
            float thrustCoefficient,
            float axialDamping,
            float baseTorqueCoefficient,
            float hoverInducedVelocity,
            float nominalRotorSpeed,
            float minPitch,
            float neutralPitch,
            float maxPitch,
            float maxTiltLongitudinal,
            float maxTiltLateral,
            float lateralRigging,
            float flapback,
            float diskLag,
            float hubStiffness)
        {
            ThrustCoefficient = thrustCoefficient;
            AxialDamping = axialDamping;
            BaseTorqueCoefficient = baseTorqueCoefficient;
            HoverInducedVelocity = hoverInducedVelocity;
            MinPitch = minPitch;
            NeutralPitch = neutralPitch;
            MaxPitch = maxPitch;
            MaxTiltLongitudinal = maxTiltLongitudinal;
            MaxTiltLateral = maxTiltLateral;
            LateralRigging = lateralRigging;
            Flapback = flapback;
            DiskLag = diskLag;
            HubStiffness = hubStiffness;
            HoverThrust = Mathf.Abs(Thrust(nominalRotorSpeed, neutralPitch, 0f));
        }

        public float Thrust(float rotorSpeed, float pitch, float axialVelocity) =>
            ThrustCoefficient * rotorSpeed * rotorSpeed * pitch - AxialDamping * rotorSpeed * axialVelocity;

        public float DragTorque(float rotorSpeed, float thrust, float axialVelocity)
        {
            float baseTorque = BaseTorqueCoefficient * rotorSpeed * rotorSpeed;

            if (rotorSpeed <= 0f)
            {
                return baseTorque;
            }

            return baseTorque + ThrustPower(thrust, axialVelocity) / rotorSpeed;
        }

        private float ThrustPower(float thrust, float axialVelocity)
        {
            float thrustMagnitude = Mathf.Abs(thrust);
            float halfInflow = 0.5f * (thrust >= 0f ? axialVelocity : -axialVelocity);
            float hoverVelocitySquared = HoverInducedVelocity * HoverInducedVelocity * thrustMagnitude / HoverThrust;
            float inducedVelocity = Mathf.Sqrt(halfInflow * halfInflow + hoverVelocitySquared) - halfInflow;

            return thrustMagnitude * (inducedVelocity + 2f * halfInflow);
        }
    }
}