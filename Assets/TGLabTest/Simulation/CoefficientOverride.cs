using System;
using UnityEngine;

namespace TGLabTest.Simulation
{
    [Serializable]
    public struct CoefficientOverride
    {
        [SerializeField] private bool _enabled;
        [SerializeField] private float _value;

        public float Resolve(float derived) => _enabled ? _value : derived;
    }
}