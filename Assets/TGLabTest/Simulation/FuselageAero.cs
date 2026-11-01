using UnityEngine;

namespace TGLabTest.Simulation
{
    public sealed class FuselageAero
    {
        private readonly FuselageParameters _parameters;

        public FuselageAero(FuselageParameters parameters)
        {
            _parameters = parameters;
        }

        public Vector3 DragForce(Vector3 localVelocity)
        {
            Vector3 signedSquare = new Vector3(
                localVelocity.x * Mathf.Abs(localVelocity.x),
                localVelocity.y * Mathf.Abs(localVelocity.y),
                localVelocity.z * Mathf.Abs(localVelocity.z));

            return -0.5f * _parameters.AirDensity * Vector3.Scale(_parameters.DragAreas, signedSquare);
        }

        public Vector3 StabilizerForce(Vector3 localVelocity) =>
            Vector3.up * (-0.5f * _parameters.AirDensity * _parameters.StabilizerLiftSlope * _parameters.StabilizerArea
                          * localVelocity.y * Mathf.Abs(localVelocity.z));

        public Vector3 DampingTorque(Vector3 localAngularVelocity) =>
            -Vector3.Scale(_parameters.AngularDamping, localAngularVelocity);
    }
}