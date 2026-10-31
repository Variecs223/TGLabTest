namespace TGLabTest.Simulation
{
    public sealed class SkidGearParameters
    {
        public float Stiffness { get; }
        public float Damping { get; }
        public float SuspensionLength { get; }
        public float FrictionCoefficient { get; }
        public float FrictionStiffness { get; }
        public float FrictionDamping { get; }

        public SkidGearParameters(
            float stiffness,
            float damping,
            float suspensionLength,
            float frictionCoefficient,
            float frictionStiffness,
            float frictionDamping)
        {
            Stiffness = stiffness;
            Damping = damping;
            SuspensionLength = suspensionLength;
            FrictionCoefficient = frictionCoefficient;
            FrictionStiffness = frictionStiffness;
            FrictionDamping = frictionDamping;
        }
    }
}