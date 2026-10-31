using UnityEngine;

namespace TGLabTest.Simulation
{
    public readonly struct HelicopterCoefficients
    {
        private const float BaseTorqueShare = 0.3f;
        private const float InducedPowerShare = 0.7f;

        public readonly float ThrustCoefficient;
        public readonly float AxialDamping;
        public readonly float BaseTorqueCoefficient;
        public readonly float HoverInducedVelocity;
        public readonly float TailThrustCoefficient;
        public readonly float TailAxialDamping;
        public readonly float TailBaseTorqueCoefficient;
        public readonly float TailHoverInducedVelocity;
        public readonly float EngineTorqueRate;
        public readonly float GovernorGain;

        public HelicopterCoefficients(
            float thrustCoefficient,
            float axialDamping,
            float baseTorqueCoefficient,
            float hoverInducedVelocity,
            float tailThrustCoefficient,
            float tailAxialDamping,
            float tailBaseTorqueCoefficient,
            float tailHoverInducedVelocity,
            float engineTorqueRate,
            float governorGain)
        {
            ThrustCoefficient = thrustCoefficient;
            AxialDamping = axialDamping;
            BaseTorqueCoefficient = baseTorqueCoefficient;
            HoverInducedVelocity = hoverInducedVelocity;
            TailThrustCoefficient = tailThrustCoefficient;
            TailAxialDamping = tailAxialDamping;
            TailBaseTorqueCoefficient = tailBaseTorqueCoefficient;
            TailHoverInducedVelocity = tailHoverInducedVelocity;
            EngineTorqueRate = engineTorqueRate;
            GovernorGain = governorGain;
        }

        public static HelicopterCoefficients Derive(HelicopterConfig config)
        {
            float rotorSpeed = config.NominalRotorSpeed;
            float hoverPitch = HoverPitch(config);
            float hoverTorque = config.HoverPower / rotorSpeed;
            float hoverThrust = config.Mass * Physics.gravity.magnitude;
            float thrustCoefficient = hoverThrust / (rotorSpeed * rotorSpeed * hoverPitch);

            float tailRotorSpeed = config.TailGearRatio * rotorSpeed;
            float tailArm = TailArm(config);
            float tailPitch = config.TailNeutralPitch;
            float tailHoverPower = config.TailPowerShare * config.HoverPower;
            float tailHoverTorque = tailHoverPower / tailRotorSpeed;
            float tailHoverThrust = hoverTorque / tailArm;
            float tailThrustCoefficient = tailHoverThrust / (tailRotorSpeed * tailRotorSpeed * tailPitch);

            float maxEngineTorque = config.MaxPower / rotorSpeed;

            return new HelicopterCoefficients(
                thrustCoefficient: thrustCoefficient,
                axialDamping: thrustCoefficient * rotorSpeed * (config.MaxCollectivePitch - hoverPitch) / config.MaxClimbRate,
                baseTorqueCoefficient: BaseTorqueShare * hoverTorque / (rotorSpeed * rotorSpeed),
                hoverInducedVelocity: InducedPowerShare * config.HoverPower / hoverThrust,
                tailThrustCoefficient: tailThrustCoefficient,
                tailAxialDamping: tailThrustCoefficient * tailRotorSpeed * (config.MaxTailPitch - tailPitch) / (config.MaxYawRate * tailArm),
                tailBaseTorqueCoefficient: BaseTorqueShare * tailHoverTorque / (tailRotorSpeed * tailRotorSpeed),
                tailHoverInducedVelocity: InducedPowerShare * tailHoverPower / tailHoverThrust,
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
                hoverInducedVelocity: config.HoverInducedVelocityOverride.Resolve(HoverInducedVelocity),
                tailThrustCoefficient: config.TailThrustCoefficientOverride.Resolve(TailThrustCoefficient),
                tailAxialDamping: config.TailAxialDampingOverride.Resolve(TailAxialDamping),
                tailBaseTorqueCoefficient: config.TailBaseTorqueCoefficientOverride.Resolve(TailBaseTorqueCoefficient),
                tailHoverInducedVelocity: config.TailHoverInducedVelocityOverride.Resolve(TailHoverInducedVelocity),
                engineTorqueRate: config.EngineTorqueRateOverride.Resolve(EngineTorqueRate),
                governorGain: config.GovernorGainOverride.Resolve(GovernorGain));
    }
}