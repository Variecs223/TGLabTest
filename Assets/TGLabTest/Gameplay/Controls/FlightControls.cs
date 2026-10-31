using System;
using TGLabTest.Simulation;
using UnityEngine;
using Zenject;

namespace TGLabTest.Gameplay.Controls
{
    public sealed class FlightControls : IFlightControls, IInitializable, ITickable, IDisposable
    {
        private const float CollectiveRate = 0.45f;
        private const float CyclicRate = 3f;
        private const float PedalRate = 2f;

        private readonly FlightInput _input;

        private float _collective;
        private float _cyclicLateral;
        private float _cyclicLongitudinal;
        private float _pedals;

        public ControlPositions Positions =>
            new ControlPositions(_collective, _cyclicLateral, _cyclicLongitudinal, _pedals);

        public FlightControls(FlightInput input)
        {
            _input = input;
        }

        public void Initialize()
        {
            _input.Flight.Enable();
        }

        public void Dispose()
        {
            _input.Flight.Disable();
        }

        public void Tick()
        {
            float deltaTime = Time.deltaTime;
            FlightInput.FlightActions flight = _input.Flight;
            Vector2 cyclic = flight.Cyclic.ReadValue<Vector2>();

            _collective = Mathf.Clamp01(_collective + flight.Collective.ReadValue<float>() * CollectiveRate * deltaTime);
            _cyclicLateral = Mathf.MoveTowards(_cyclicLateral, cyclic.x, CyclicRate * deltaTime);
            _cyclicLongitudinal = Mathf.MoveTowards(_cyclicLongitudinal, cyclic.y, CyclicRate * deltaTime);
            _pedals = Mathf.MoveTowards(_pedals, flight.Pedals.ReadValue<float>(), PedalRate * deltaTime);
        }
    }
}