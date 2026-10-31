using UnityEngine;

namespace TGLabTest.Simulation
{
    public sealed class Drivetrain
    {
        private readonly DrivetrainParameters _parameters;

        public float RotorSpeed { get; private set; }
        public float RotorAcceleration { get; private set; }
        public float EngineTorque { get; private set; }
        public float ShaftTorque { get; private set; }

        public float TailRotorSpeed => RotorSpeed * _parameters.TailGearRatio;
        public float RotorSpeedRatio => RotorSpeed / _parameters.NominalRotorSpeed;
        public float EnginePower => EngineTorque * RotorSpeed;

        public Drivetrain(DrivetrainParameters parameters)
        {
            _parameters = parameters;
            RotorSpeed = parameters.NominalRotorSpeed;
        }

        public void Step(float mainRotorDrag, float tailRotorDrag, float deltaTime)
        {
            EngineTorque = Mathf.MoveTowards(EngineTorque, CommandedTorque(), _parameters.EngineTorqueRate * deltaTime);

            float netTorque = EngineTorque
                              - mainRotorDrag
                              - _parameters.TailGearRatio * tailRotorDrag
                              - _parameters.FrictionTorque;
            float acceleration = netTorque / _parameters.TotalInertia;
            float rotorSpeed = RotorSpeed + acceleration * deltaTime;

            if (rotorSpeed > 0f)
            {
                RotorSpeed = rotorSpeed;
                RotorAcceleration = acceleration;
            }
            else
            {
                RotorSpeed = 0f;
                RotorAcceleration = 0f;
            }

            ShaftTorque = mainRotorDrag + _parameters.MainRotorInertia * RotorAcceleration;
        }

        private float CommandedTorque()
        {
            float demand = _parameters.GovernorGain * (_parameters.GovernorTargetSpeed - RotorSpeed);
            float available = _parameters.MaxPower / Mathf.Max(RotorSpeed, _parameters.MinPowerRotorSpeed);
            return Mathf.Clamp(demand, 0f, available);
        }
    }
}