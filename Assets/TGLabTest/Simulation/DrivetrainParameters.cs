namespace TGLabTest.Simulation
{
    public sealed class DrivetrainParameters
    {
        public float NominalRotorSpeed { get; }
        public float GovernorTargetSpeed { get; }
        public float GovernorGain { get; }
        public float MaxPower { get; }
        public float MinPowerRotorSpeed { get; }
        public float EngineTorqueRate { get; }
        public float MainRotorInertia { get; }
        public float TailRotorInertia { get; }
        public float TailGearRatio { get; }
        public float FrictionTorque { get; }

        public float TotalInertia => MainRotorInertia + TailGearRatio * TailGearRatio * TailRotorInertia;

        public DrivetrainParameters(
            float nominalRotorSpeed,
            float governorTargetSpeed,
            float governorGain,
            float maxPower,
            float minPowerRotorSpeed,
            float engineTorqueRate,
            float mainRotorInertia,
            float tailRotorInertia,
            float tailGearRatio,
            float frictionTorque)
        {
            NominalRotorSpeed = nominalRotorSpeed;
            GovernorTargetSpeed = governorTargetSpeed;
            GovernorGain = governorGain;
            MaxPower = maxPower;
            MinPowerRotorSpeed = minPowerRotorSpeed;
            EngineTorqueRate = engineTorqueRate;
            MainRotorInertia = mainRotorInertia;
            TailRotorInertia = tailRotorInertia;
            TailGearRatio = tailGearRatio;
            FrictionTorque = frictionTorque;
        }
    }
}