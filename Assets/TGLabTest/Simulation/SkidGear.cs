using System.Collections.Generic;
using UnityEngine;

namespace TGLabTest.Simulation
{
    public sealed class SkidGear
    {
        private readonly SkidGearParameters _parameters;
        private readonly IGroundProbe _groundProbe;
        private readonly SkidContactForce[] _forces;

        public IReadOnlyList<SkidContactForce> Forces => _forces;
        public bool HasGroundContact { get; private set; }

        public SkidGear(SkidGearParameters parameters, IGroundProbe groundProbe, int contactCount)
        {
            _parameters = parameters;
            _groundProbe = groundProbe;
            _forces = new SkidContactForce[contactCount];
        }

        public void Step(IReadOnlyList<SkidContactSample> contacts, Vector3 up)
        {
            HasGroundContact = false;

            for (int i = 0; i < _forces.Length; i++)
            {
                _forces[i] = ContactForce(contacts[i], up);
            }
        }

        private SkidContactForce ContactForce(in SkidContactSample contact, Vector3 up)
        {
            Vector3 origin = contact.Position + up * _parameters.SuspensionLength;

            if (!_groundProbe.Cast(origin, -up, _parameters.SuspensionLength, out GroundHit hit))
            {
                return new SkidContactForce(Vector3.zero, contact.Position);
            }

            HasGroundContact = true;

            float compression = _parameters.SuspensionLength - hit.Distance;
            float compressionRate = -Vector3.Dot(contact.Velocity, hit.Normal);
            float normalForce = Mathf.Max(0f, _parameters.Stiffness * compression + _parameters.Damping * compressionRate);

            Vector3 slipVelocity = Vector3.ProjectOnPlane(contact.Velocity, hit.Normal);
            Vector3 friction = Vector3.ClampMagnitude(
                -slipVelocity * _parameters.FrictionDamping,
                _parameters.FrictionCoefficient * normalForce);

            return new SkidContactForce(hit.Normal * normalForce + friction, hit.Point);
        }
    }
}