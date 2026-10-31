using TGLabTest.Simulation;
using UnityEngine;

namespace TGLabTest.Gameplay.Helicopter
{
    public sealed class RaycastGroundProbe : IGroundProbe
    {
        private const int HitBufferSize = 8;

        private readonly Rigidbody _body;
        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];

        public RaycastGroundProbe(Rigidbody body)
        {
            _body = body;
        }

        public bool Cast(Vector3 origin, Vector3 direction, float maxDistance, out GroundHit hit)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, _hits, maxDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            int nearest = -1;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i].rigidbody == _body)
                {
                    continue;
                }

                if (nearest < 0 || _hits[i].distance < _hits[nearest].distance)
                {
                    nearest = i;
                }
            }

            if (nearest < 0)
            {
                hit = default;
                return false;
            }

            RaycastHit ground = _hits[nearest];
            hit = new GroundHit(ground.point, ground.normal, ground.distance);
            return true;
        }
    }
}