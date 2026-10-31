using System;
using TGLabTest.Simulation;
using UnityEngine;
using Zenject;

namespace TGLabTest.Gameplay.Helicopter
{
    public sealed class HelicopterRegistration : IInitializable, IDisposable
    {
        private readonly ActiveHelicopter _activeHelicopter;
        private readonly Transform _transform;
        private readonly IHelicopterState _state;

        public HelicopterRegistration(ActiveHelicopter activeHelicopter, Rigidbody body, IHelicopterState state)
        {
            _activeHelicopter = activeHelicopter;
            _transform = body.transform;
            _state = state;
        }

        public void Initialize()
        {
            _activeHelicopter.Register(_transform, _state);
        }

        public void Dispose()
        {
            _activeHelicopter.Unregister(_transform);
        }
    }
}