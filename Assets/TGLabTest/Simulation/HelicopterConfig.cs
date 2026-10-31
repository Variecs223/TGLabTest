using System.Collections.Generic;
using UnityEngine;

namespace TGLabTest.Simulation
{
    [CreateAssetMenu(fileName = "HelicopterConfig", menuName = "TGLabTest/Helicopter Config")]
    public sealed class HelicopterConfig : ScriptableObject
    {
        private const float RpmToRadiansPerSecond = 2f * Mathf.PI / 60f;
        private const float WattsPerKilowatt = 1000f;
        private const float Percent = 0.01f;

        [Header("Body")]
        [SerializeField, Min(1f)] private float _mass = 4300f;
        [SerializeField] private Vector3 _inertia = new Vector3(24700f, 21000f, 6500f);
        [SerializeField] private Vector3 _centerOfMass = new Vector3(0f, 1.4f, 0f);

        [Header("Geometry, relative to the centre of mass")]
        [SerializeField] private Vector3 _mainHubOffset = new Vector3(0f, 1.8f, 0f);
        [SerializeField] private Vector3 _tailHubOffset = new Vector3(0f, 1f, -8.6f);
        [SerializeField] private Vector3 _centerOfPressureOffset = new Vector3(0f, 0f, -0.5f);
        [SerializeField] private Vector3[] _skidContactOffsets =
        {
            new Vector3(-1.3f, -1.4f, 1.5f),
            new Vector3(1.3f, -1.4f, 1.5f),
            new Vector3(-1.3f, -1.4f, -1.5f),
            new Vector3(1.3f, -1.4f, -1.5f)
        };

        [Header("Main rotor")]
        [SerializeField] private RotorSpinDirection _spinDirection = RotorSpinDirection.Counterclockwise;
        [SerializeField, Min(1f)] private float _nominalRotorSpeedRpm = 324f;
        [SerializeField] private Vector2 _collectivePitchRangeDegrees = new Vector2(0f, 15f);
        [SerializeField, Min(0f)] private float _maxTiltLongitudinalDegrees = 8f;
        [SerializeField, Min(0f)] private float _maxTiltLateralDegrees = 6f;
        [SerializeField, Min(0f)] private float _diskLagSeconds = 0.1f;
        [SerializeField, Min(0f)] private float _hubStiffness;
        [SerializeField, Min(1f)] private float _mainRotorInertia = 6000f;

        [Header("Tail rotor")]
        [SerializeField, Min(0.1f)] private float _tailGearRatio = 5.1f;
        [SerializeField] private Vector2 _tailPitchRangeDegrees = new Vector2(-8f, 20f);
        [SerializeField] private float _tailNeutralPitchDegrees = 8f;
        [SerializeField, Min(0.1f)] private float _tailRotorInertia = 15f;

        [Header("Engine")]
        [SerializeField, Min(1f)] private float _hoverPowerKilowatts = 700f;
        [SerializeField, Min(1f)] private float _maxPowerKilowatts = 1000f;
        [SerializeField, Min(0.01f)] private float _spoolTimeSeconds = 1f;
        [SerializeField, Min(0.1f)] private float _governorDroopPercent = 3f;
        [SerializeField, Min(0f)] private float _drivetrainFrictionTorque = 200f;
        [SerializeField, Min(1f)] private float _minPowerRotorSpeedPercent = 10f;

        [Header("Handling targets")]
        [SerializeField, Min(0.1f)] private float _maxClimbRate = 10f;
        [SerializeField, Min(0.1f)] private float _tailPowerSharePercent = 12f;
        [SerializeField, Min(1f)] private float _maxYawRateDegrees = 60f;

        [Header("Fuselage")]
        [SerializeField] private Vector3 _dragAreas = new Vector3(10f, 15f, 2f);
        [SerializeField] private Vector3 _angularDamping = Vector3.zero;
        [SerializeField, Min(0f)] private float _airDensity = 1.225f;

        [Header("Skids")]
        [SerializeField, Min(0f)] private float _skidStiffness = 2.1e5f;
        [SerializeField, Min(0f)] private float _skidDamping = 1.8e4f;
        [SerializeField, Min(0.01f)] private float _skidSuspensionLength = 0.3f;
        [SerializeField, Min(0f)] private float _skidFrictionCoefficient = 0.6f;
        [SerializeField, Min(1f)] private float _skidFrictionStiffness = 2e5f;
        [SerializeField, Min(0f)] private float _skidFrictionDamping = 2e4f;

        [Header("Coefficient overrides")]
        [SerializeField] private CoefficientOverride _thrustCoefficient;
        [SerializeField] private CoefficientOverride _axialDamping;
        [SerializeField] private CoefficientOverride _baseTorqueCoefficient;
        [SerializeField] private CoefficientOverride _hoverInducedVelocity;
        [SerializeField] private CoefficientOverride _lateralRigging;
        [SerializeField] private CoefficientOverride _tailThrustCoefficient;
        [SerializeField] private CoefficientOverride _tailAxialDamping;
        [SerializeField] private CoefficientOverride _tailBaseTorqueCoefficient;
        [SerializeField] private CoefficientOverride _tailHoverInducedVelocity;
        [SerializeField] private CoefficientOverride _engineTorqueRate;
        [SerializeField] private CoefficientOverride _governorGain;

        public float Mass => _mass;
        public Vector3 Inertia => _inertia;
        public Vector3 CenterOfMass => _centerOfMass;

        public Vector3 MainHubOffset => _mainHubOffset;
        public Vector3 TailHubOffset => _tailHubOffset;
        public Vector3 CenterOfPressureOffset => _centerOfPressureOffset;
        public IReadOnlyList<Vector3> SkidContactOffsets => _skidContactOffsets;

        public RotorSpinDirection SpinDirection => _spinDirection;
        public float NominalRotorSpeed => _nominalRotorSpeedRpm * RpmToRadiansPerSecond;
        public float MinCollectivePitch => _collectivePitchRangeDegrees.x * Mathf.Deg2Rad;
        public float MaxCollectivePitch => _collectivePitchRangeDegrees.y * Mathf.Deg2Rad;
        public float MaxTiltLongitudinal => _maxTiltLongitudinalDegrees * Mathf.Deg2Rad;
        public float MaxTiltLateral => _maxTiltLateralDegrees * Mathf.Deg2Rad;
        public float DiskLag => _diskLagSeconds;
        public float HubStiffness => _hubStiffness;
        public float MainRotorInertia => _mainRotorInertia;

        public float TailGearRatio => _tailGearRatio;
        public float MinTailPitch => _tailPitchRangeDegrees.x * Mathf.Deg2Rad;
        public float MaxTailPitch => _tailPitchRangeDegrees.y * Mathf.Deg2Rad;
        public float TailNeutralPitch => _tailNeutralPitchDegrees * Mathf.Deg2Rad;
        public float TailRotorInertia => _tailRotorInertia;

        public float HoverPower => _hoverPowerKilowatts * WattsPerKilowatt;
        public float MaxPower => _maxPowerKilowatts * WattsPerKilowatt;
        public float SpoolTime => _spoolTimeSeconds;
        public float GovernorDroop => _governorDroopPercent * Percent;
        public float DrivetrainFrictionTorque => _drivetrainFrictionTorque;
        public float MinPowerRotorSpeed => _minPowerRotorSpeedPercent * Percent * NominalRotorSpeed;

        public float MaxClimbRate => _maxClimbRate;
        public float TailPowerShare => _tailPowerSharePercent * Percent;
        public float MaxYawRate => _maxYawRateDegrees * Mathf.Deg2Rad;

        public Vector3 DragAreas => _dragAreas;
        public Vector3 AngularDamping => _angularDamping;
        public float AirDensity => _airDensity;

        public float SkidStiffness => _skidStiffness;
        public float SkidDamping => _skidDamping;
        public float SkidSuspensionLength => _skidSuspensionLength;
        public float SkidFrictionCoefficient => _skidFrictionCoefficient;
        public float SkidFrictionStiffness => _skidFrictionStiffness;
        public float SkidFrictionDamping => _skidFrictionDamping;

        public CoefficientOverride ThrustCoefficientOverride => _thrustCoefficient;
        public CoefficientOverride AxialDampingOverride => _axialDamping;
        public CoefficientOverride BaseTorqueCoefficientOverride => _baseTorqueCoefficient;
        public CoefficientOverride HoverInducedVelocityOverride => _hoverInducedVelocity;
        public CoefficientOverride LateralRiggingOverride => _lateralRigging;
        public CoefficientOverride TailThrustCoefficientOverride => _tailThrustCoefficient;
        public CoefficientOverride TailAxialDampingOverride => _tailAxialDamping;
        public CoefficientOverride TailBaseTorqueCoefficientOverride => _tailBaseTorqueCoefficient;
        public CoefficientOverride TailHoverInducedVelocityOverride => _tailHoverInducedVelocity;
        public CoefficientOverride EngineTorqueRateOverride => _engineTorqueRate;
        public CoefficientOverride GovernorGainOverride => _governorGain;
    }
}