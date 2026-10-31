using UnityEngine;

namespace TGLabTest.Simulation
{
    public interface IGroundProbe
    {
        bool Cast(Vector3 origin, Vector3 direction, float maxDistance, out GroundHit hit);
    }
}