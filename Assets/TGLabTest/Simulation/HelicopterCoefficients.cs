using UnityEngine;

namespace TGLabTest.Simulation
{
    public readonly struct HelicopterCoefficients
    {
        private const float BaseTorqueShare = 0.3f;
        private const float PitchTorqueShare = 0.7f;

        public readonly float ThrustCoefficient;
        public readonly float AxialDamping;
        public readonly float BaseTorqueCoefficient;
        public readonly float PitchTorqueCoefficient;
        public readonly float TailThrustCoefficient;
        public readonly float TailAxialDamping;
        public readonly float TailBaseTorqueCoefficient;
        public readonly float TailPitchTorqueCoefficient;
        public readonly float EngineTorqueRate;
        public readonly float GovernorGain;

        public HelicopterCoefficients(
            float thrustCoefficient,
            float axialDamping,
            float baseTorqueCoefficient,
            float pitchTorqueCoefficient,
            float tailThrustCoefficient,
            float tailAxialDamping,
            float tailBaseTorqueCoefficient,
            float tailPitchTorqueCoefficient,
            float engineTorqueRate,
            float governorGain)
        {
            ThrustCoefficient = thrustCoefficient;
            AxialDamping = axialDamping;
            BaseTorqueCoefficient = baseTorqueCoefficient;
            PitchTorqueCoefficient = pitchTorqueCoefficient;
            TailThrustCoefficient = tailThrustCoefficient;
            TailAxialDamping = tailAxialDamping;
            TailBaseTorqueCoefficient = tailBaseTorqueCoefficient;
            TailPitchTorqueCoefficient = tailPitchTorqueCoefficient;
            EngineTorqueRate = engineTorqueRate;
            GovernorGain = governorGain;
        }

        public static HelicopterCoefficients Derive(HelicopterConfig config)
        {
            float rotorSpeed = config.NominalRotorSpeed;
            float hoverPitch = HoverPitch(config);
            float hoverTorque = config.HoverPower / rotorSpeed;
            float thrustCoefficient = config.Mass * Physics.gravity.magnitude / (rotorSpeed * rotorSpeed * hoverPitch);

            float tailRotorSpeed = config.TailGearRatio * rotorSpeed;
            float tailArm = TailArm(config);
            float tailPitch = config.TailNeutralPitch;
            float tailHoverTorque = config.TailPowerShare * config.HoverPower / tailRotorSpeed;
            float tailThrustCoefficient = hoverTorque / tailArm / (tailRotorSpeed * tailRotorSpeed * tailPitch);

            float maxEngineTorque = config.MaxPower / rotorSpeed;

            return new HelicopterCoefficients(
                thrustCoefficient: thrustCoefficient,
                axialDamping: thrustCoefficient * rotorSpeed * (config.MaxCollectivePitch - hoverPitch) / config.MaxClimbRate,
                baseTorqueCoefficient: BaseTorqueShare * hoverTorque / (rotorSpeed * rotorSpeed),
                pitchTorqueCoefficient: PitchTorqueShare * hoverTorque / (rotorSpeed * rotorSpeed * hoverPitch * hoverPitch),
                tailThrustCoefficient: tailThrustCoefficient,
                tailAxialDamping: tailThrustCoefficient * tailRotorSpeed * (config.MaxTailPitch - tailPitch) / (config.MaxYawRate * tailArm),
                tailBaseTorqueCoefficient: BaseTorqueShare * tailHoverTorque / (tailRotorSpeed * tailRotorSpeed),
                tailPitchTorqueCoefficient: PitchTorqueShare * tailHoverTorque / (tailRotorSpeed * tailRotorSpeed * tailPitch * tailPitch),
                engineTorqueRate: (maxEngineTorque - hoverTorque) / config.SpoolTime,
                governorGain: maxEngineTorque / (config.GovernorDroop * rotorSpeed));
        }

        public static float HoverPitch(HelicopterConfig config) =>
            Mathf.Lerp(config.MinCollectivePitch, config.MaxCollectivePitch, 0.5f);

        public static float TailArm(HelicopterConfig config) => -config.TailHubOffset.z;

        public HelicopterCoefficients WithOverrides(HelicopterConfig config) =>
            new HelicopterCoefficients(
                thrustCoefficient: config.ThrustCoefficientOverride.Resolve(ThrustCoefficient),
                axialDamping: config.AxialDampingOverride.Resolve(AxialDamping),
                baseTorqueCoefficient: config.BaseTorqueCoefficientOverride.Resolve(BaseTorqueCoefficient),
                pitchTorqueCoefficient: config.PitchTorqueCoefficientOverride.Resolve(PitchTorqueCoefficient),
                tailThrustCoefficient: config.TailThrustCoefficientOverride.Resolve(TailThrustCoefficient),
                tailAxialDamping: config.TailAxialDampingOverride.Resolve(TailAxialDamping),
                tailBaseTorqueCoefficient: config.TailBaseTorqueCoefficientOverride.Resolve(TailBaseTorqueCoefficient),
                tailPitchTorqueCoefficient: config.TailPitchTorqueCoefficientOverride.Resolve(TailPitchTorqueCoefficient),
                engineTorqueRate: config.EngineTorqueRateOverride.Resolve(EngineTorqueRate),
                governorGain: config.GovernorGainOverride.Resolve(GovernorGain));
    }
}