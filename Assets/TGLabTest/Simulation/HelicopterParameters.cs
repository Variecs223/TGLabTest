using System.Collections.Generic;
using UnityEngine;

namespace TGLabTest.Simulation
{
    public sealed class HelicopterParameters
    {
        public float Mass { get; }
        public Vector3 Inertia { get; }
        public Vector3 CenterOfMass { get; }
        public Vector3 MainHubOffset { get; }
        public Vector3 TailHubOffset { get; }
        public Vector3 CenterOfPressureOffset { get; }
        public IReadOnlyList<Vector3> SkidContactOffsets { get; }
        public int SpinSign { get; }
        public float HoverDrivetrainTorque { get; }

        public RotorParameters MainRotor { get; }
        public RotorParameters TailRotor { get; }
        public DrivetrainParameters Drivetrain { get; }
        public FuselageParameters Fuselage { get; }
        public SkidGearParameters SkidGear { get; }

        public HelicopterParameters(HelicopterConfig config)
        {
            HelicopterCoefficients coefficients = HelicopterCoefficients.Derive(config).WithOverrides(config);

            Mass = config.Mass;
            Inertia = config.Inertia;
            CenterOfMass = config.CenterOfMass;
            MainHubOffset = config.MainHubOffset;
            TailHubOffset = config.TailHubOffset;
            CenterOfPressureOffset = config.CenterOfPressureOffset;
            SkidContactOffsets = new List<Vector3>(config.SkidContactOffsets);
            SpinSign = (int)config.SpinDirection;

            float rotorSpeed = config.NominalRotorSpeed;

            MainRotor = new RotorParameters(
                thrustCoefficient: coefficients.ThrustCoefficient,
                axialDamping: coefficients.AxialDamping,
                baseTorqueCoefficient: coefficients.BaseTorqueCoefficient,
                hoverInducedVelocity: coefficients.HoverInducedVelocity,
                nominalRotorSpeed: rotorSpeed,
                minPitch: config.MinCollectivePitch,
                neutralPitch: HelicopterCoefficients.HoverPitch(config),
                maxPitch: config.MaxCollectivePitch,
                maxTiltLongitudinal: config.MaxTiltLongitudinal,
                maxTiltLateral: config.MaxTiltLateral,
                lateralRigging: coefficients.LateralRigging,
                diskLag: config.DiskLag,
                hubStiffness: config.HubStiffness);

            TailRotor = new RotorParameters(
                thrustCoefficient: coefficients.TailThrustCoefficient,
                axialDamping: coefficients.TailAxialDamping,
                baseTorqueCoefficient: coefficients.TailBaseTorqueCoefficient,
                hoverInducedVelocity: coefficients.TailHoverInducedVelocity,
                nominalRotorSpeed: config.TailGearRatio * rotorSpeed,
                minPitch: config.MinTailPitch,
                neutralPitch: config.TailNeutralPitch,
                maxPitch: config.MaxTailPitch,
                maxTiltLongitudinal: 0f,
                maxTiltLateral: 0f,
                lateralRigging: 0f,
                diskLag: 0f,
                hubStiffness: 0f);

            HoverDrivetrainTorque =
                MainRotor.DragTorque(rotorSpeed, MainRotor.HoverThrust, 0f)
                + config.TailGearRatio * TailRotor.DragTorque(config.TailGearRatio * rotorSpeed, TailRotor.HoverThrust, 0f)
                + config.DrivetrainFrictionTorque;

            Drivetrain = new DrivetrainParameters(
                nominalRotorSpeed: rotorSpeed,
                governorTargetSpeed: rotorSpeed + HoverDrivetrainTorque / coefficients.GovernorGain,
                governorGain: coefficients.GovernorGain,
                maxPower: config.MaxPower,
                minPowerRotorSpeed: config.MinPowerRotorSpeed,
                engineTorqueRate: coefficients.EngineTorqueRate,
                mainRotorInertia: config.MainRotorInertia,
                tailRotorInertia: config.TailRotorInertia,
                tailGearRatio: config.TailGearRatio,
                frictionTorque: config.DrivetrainFrictionTorque);

            Fuselage = new FuselageParameters(config.DragAreas, config.AngularDamping, config.AirDensity);

            SkidGear = new SkidGearParameters(
                stiffness: config.SkidStiffness,
                damping: config.SkidDamping,
                suspensionLength: config.SkidSuspensionLength,
                frictionCoefficient: config.SkidFrictionCoefficient,
                frictionStiffness: config.SkidFrictionStiffness,
                frictionDamping: config.SkidFrictionDamping);
        }

        public float MainRotorPitch(float collective) =>
            Mathf.Lerp(MainRotor.MinPitch, MainRotor.MaxPitch, collective);

        public float TailRotorPitch(float pedals)
        {
            float deflection = pedals * SpinSign;
            return deflection >= 0f
                ? Mathf.Lerp(TailRotor.NeutralPitch, TailRotor.MaxPitch, deflection)
                : Mathf.Lerp(TailRotor.NeutralPitch, TailRotor.MinPitch, -deflection);
        }
    }
}