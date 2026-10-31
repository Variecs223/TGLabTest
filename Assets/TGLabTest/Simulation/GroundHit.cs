using UnityEngine;

namespace TGLabTest.Simulation
{
    public readonly struct GroundHit
    {
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly float Distance;

        public GroundHit(Vector3 point, Vector3 normal, float distance)
        {
            Point = point;
            Normal = normal;
            Distance = distance;
        }
    }
}