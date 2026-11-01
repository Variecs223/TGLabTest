using System;
using TGLabTest.Simulation;
using UnityEngine;
using Zenject;

namespace TGLabTest.Gameplay.Presentation
{
    public sealed class RotorVisual : MonoBehaviour
    {
        [SerializeField] private Spinner _mainRotor = new Spinner(Vector3.up);
        [SerializeField] private Spinner _tailRotor = new Spinner(Vector3.right);

        private IHelicopterState _state;
        private Transform _body;
        private float _mainRotorDirection;
        private float _tailGearRatio;

        [Inject]
        public void Construct(IHelicopterState state, HelicopterParameters parameters, Rigidbody body)
        {
            _state = state;
            _body = body.transform;
            _mainRotorDirection = parameters.SpinSign;
            _tailGearRatio = parameters.Drivetrain.TailGearRatio;

            _mainRotor.CaptureRest(_body);
            _tailRotor.CaptureRest(_body);
        }

        private void Update()
        {
            float rotorAngle = _state.RotorSpeed * Mathf.Rad2Deg * Time.deltaTime;
            Quaternion diskTilt = Quaternion.FromToRotation(Vector3.up, _state.LocalDiskNormal);

            _mainRotor.Advance(_body, diskTilt, _mainRotorDirection * rotorAngle);
            _tailRotor.Advance(_body, Quaternion.identity, _tailGearRatio * rotorAngle);
        }

        [Serializable]
        private sealed class Spinner
        {
            [SerializeField] private Transform _transform;
            [SerializeField] private Vector3 _localAxis;

            private Quaternion _restRotationInBody;
            private float _angle;

            public Spinner(Vector3 localAxis)
            {
                _localAxis = localAxis;
            }

            public void CaptureRest(Transform body)
            {
                _restRotationInBody = Quaternion.Inverse(body.rotation) * _transform.rotation;
            }

            public void Advance(Transform body, Quaternion tiltInBody, float deltaAngle)
            {
                _angle = Mathf.Repeat(_angle + deltaAngle, 360f);
                _transform.rotation = body.rotation * tiltInBody * _restRotationInBody * Quaternion.AngleAxis(_angle, _localAxis);
            }
        }
    }
}