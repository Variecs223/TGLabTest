using UnityEngine;

namespace TGLabTest.Simulation
{
    public sealed class RotorModel
    {
        private readonly RotorParameters _parameters;

        public Vector3 DiskNormal { get; private set; } = Vector3.up;
        public float Thrust { get; private set; }
        public float DragTorque { get; private set; }
        public Vector3 HubMoment { get; private set; }

        public Vector3 ThrustForce => DiskNormal * Thrust;

        public RotorModel(RotorParameters parameters)
        {
            _parameters = parameters;
        }

        public void ResetDisk(Vector3 shaftUp)
        {
            DiskNormal = shaftUp;
        }

        public void Step(in RotorSample sample, float pitch, float cyclicLateral, float cyclicLongitudinal, float rotorSpeed, float deltaTime)
        {
            UpdateDiskNormal(sample, cyclicLateral, cyclicLongitudinal, deltaTime);

            float axialVelocity = Vector3.Dot(sample.HubVelocity, DiskNormal);
            Thrust = _parameters.Thrust(rotorSpeed, pitch, axialVelocity);
            DragTorque = _parameters.DragTorque(rotorSpeed, Thrust, axialVelocity);
            HubMoment = _parameters.HubStiffness * Vector3.Cross(sample.ShaftUp, DiskNormal);
        }

        private void UpdateDiskNormal(in RotorSample sample, float cyclicLateral, float cyclicLongitudinal, float deltaTime)
        {
            Vector3 target = (sample.ShaftUp
                              + sample.ShaftForward * (cyclicLongitudinal * _parameters.MaxTiltLongitudinal)
                              + sample.ShaftRight * (cyclicLateral * _parameters.MaxTiltLateral)).normalized;

            DiskNormal = _parameters.DiskLag > 0f
                ? Vector3.Slerp(DiskNormal, target, 1f - Mathf.Exp(-deltaTime / _parameters.DiskLag))
                : target;
        }
    }
}