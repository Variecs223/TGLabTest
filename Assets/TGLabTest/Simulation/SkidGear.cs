using System.Collections.Generic;
using UnityEngine;

namespace TGLabTest.Simulation
{
    public sealed class SkidGear
    {
        private readonly SkidGearParameters _parameters;
        private readonly IGroundProbe _groundProbe;
        private readonly SkidContactForce[] _forces;
        private readonly Vector3[] _anchors;
        private readonly bool[] _isAnchored;

        public IReadOnlyList<SkidContactForce> Forces => _forces;
        public bool HasGroundContact { get; private set; }

        public SkidGear(SkidGearParameters parameters, IGroundProbe groundProbe, int contactCount)
        {
            _parameters = parameters;
            _groundProbe = groundProbe;
            _forces = new SkidContactForce[contactCount];
            _anchors = new Vector3[contactCount];
            _isAnchored = new bool[contactCount];
        }

        public void Step(IReadOnlyList<SkidContactSample> contacts, Vector3 up)
        {
            HasGroundContact = false;

            for (int i = 0; i < _forces.Length; i++)
            {
                _forces[i] = ContactForce(i, contacts[i], up);
            }
        }

        private SkidContactForce ContactForce(int index, in SkidContactSample contact, Vector3 up)
        {
            Vector3 origin = contact.Position + up * _parameters.SuspensionLength;

            if (!_groundProbe.Cast(origin, -up, _parameters.SuspensionLength, out GroundHit hit))
            {
                _isAnchored[index] = false;
                return new SkidContactForce(Vector3.zero, contact.Position);
            }

            HasGroundContact = true;

            float compression = _parameters.SuspensionLength - hit.Distance;
            float compressionRate = -Vector3.Dot(contact.Velocity, hit.Normal);
            float normalForce = Mathf.Max(0f, _parameters.Stiffness * compression + _parameters.Damping * compressionRate);
            Vector3 friction = FrictionForce(index, hit, contact.Velocity, normalForce);

            return new SkidContactForce(hit.Normal * normalForce + friction, hit.Point);
        }

        private Vector3 FrictionForce(int index, in GroundHit hit, Vector3 contactVelocity, float normalForce)
        {
            if (!_isAnchored[index])
            {
                _anchors[index] = hit.Point;
                _isAnchored[index] = true;
            }

            Vector3 displacement = Vector3.ProjectOnPlane(hit.Point - _anchors[index], hit.Normal);
            Vector3 slipVelocity = Vector3.ProjectOnPlane(contactVelocity, hit.Normal);
            Vector3 friction = -displacement * _parameters.FrictionStiffness - slipVelocity * _parameters.FrictionDamping;

            float maxFriction = _parameters.FrictionCoefficient * normalForce;

            if (friction.sqrMagnitude <= maxFriction * maxFriction)
            {
                return friction;
            }

            friction = friction.normalized * maxFriction;
            _anchors[index] = hit.Point + friction / _parameters.FrictionStiffness;
            return friction;
        }
    }
}