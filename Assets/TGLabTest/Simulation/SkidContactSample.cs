using UnityEngine;

namespace TGLabTest.Simulation
{
    public readonly struct SkidContactSample
    {
        public readonly Vector3 Position;
        public readonly Vector3 Velocity;

        public SkidContactSample(Vector3 position, Vector3 velocity)
        {
            Position = position;
            Velocity = velocity;
        }
    }
}