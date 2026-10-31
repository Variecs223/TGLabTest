using TGLabTest.Simulation;
using UnityEngine;

namespace TGLabTest.Gameplay
{
    public sealed class ActiveHelicopter : IActiveHelicopter
    {
        public bool IsPresent => Transform != null;
        public Transform Transform { get; private set; }
        public IHelicopterState State { get; private set; }

        public void Register(Transform transform, IHelicopterState state)
        {
            Transform = transform;
            State = state;
        }

        public void Unregister(Transform transform)
        {
            if (Transform != transform)
            {
                return;
            }

            Transform = null;
            State = null;
        }
    }
}