using UnityEngine;

namespace TGLabTest.Simulation
{
    public readonly struct SkidContactForce
    {
        public readonly Vector3 Force;
        public readonly Vector3 Point;

        public SkidContactForce(Vector3 force, Vector3 point)
        {
            Force = force;
            Point = point;
        }
    }
}