using UnityEngine;

namespace TGLabTest.Simulation
{
    public sealed class FuselageParameters
    {
        public Vector3 DragAreas { get; }
        public Vector3 AngularDamping { get; }
        public float AirDensity { get; }

        public FuselageParameters(Vector3 dragAreas, Vector3 angularDamping, float airDensity)
        {
            DragAreas = dragAreas;
            AngularDamping = angularDamping;
            AirDensity = airDensity;
        }
    }
}