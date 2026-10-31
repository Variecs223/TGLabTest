using UnityEngine;

namespace TGLabTest.Simulation
{
    public readonly struct RotorSample
    {
        public readonly Vector3 ShaftUp;
        public readonly Vector3 ShaftForward;
        public readonly Vector3 ShaftRight;
        public readonly Vector3 HubVelocity;

        public RotorSample(Vector3 shaftUp, Vector3 shaftForward, Vector3 shaftRight, Vector3 hubVelocity)
        {
            ShaftUp = shaftUp;
            ShaftForward = shaftForward;
            ShaftRight = shaftRight;
            HubVelocity = hubVelocity;
        }
    }
}