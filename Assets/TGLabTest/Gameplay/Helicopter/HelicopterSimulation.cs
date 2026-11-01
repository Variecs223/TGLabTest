using TGLabTest.Gameplay.Controls;
using TGLabTest.Simulation;
using UnityEngine;
using Zenject;

namespace TGLabTest.Gameplay.Helicopter
{
    public sealed class HelicopterSimulation : IInitializable, IFixedTickable
    {
        private readonly HelicopterParameters _parameters;
        private readonly Rigidbody _body;
        private readonly IFlightControls _controls;
        private readonly HelicopterState _state;

        private readonly RotorModel _mainRotor;
        private readonly RotorModel _tailRotor;
        private readonly Drivetrain _drivetrain;
        private readonly FuselageAero _fuselage;
        private readonly SkidGear _skidGear;
        private readonly SkidContactSample[] _skidContacts;

        public HelicopterSimulation(
            HelicopterParameters parameters,
            Rigidbody body,
            IFlightControls controls,
            HelicopterState state,
            IGroundProbe groundProbe)
        {
            _parameters = parameters;
            _body = body;
            _controls = controls;
            _state = state;

            _mainRotor = new RotorModel(parameters.MainRotor);
            _tailRotor = new RotorModel(parameters.TailRotor);
            _drivetrain = new Drivetrain(parameters.Drivetrain);
            _fuselage = new FuselageAero(parameters.Fuselage);
            _skidContacts = new SkidContactSample[parameters.SkidContactOffsets.Count];
            _skidGear = new SkidGear(parameters.SkidGear, groundProbe, _skidContacts.Length);
        }

        public void Initialize()
        {
            _body.mass = _parameters.Mass;
            _body.automaticCenterOfMass = false;
            _body.centerOfMass = _parameters.CenterOfMass;
            _body.automaticInertiaTensor = false;
            _body.inertiaTensor = _parameters.Inertia;
            _body.inertiaTensorRotation = Quaternion.identity;
            _body.linearDamping = 0f;
            _body.angularDamping = 0f;
            _body.useGravity = true;
            _body.interpolation = RigidbodyInterpolation.Interpolate;

            _mainRotor.ResetDisk(_body.rotation * Vector3.up);
        }

        public void FixedTick()
        {
            float deltaTime = Time.fixedDeltaTime;
            ControlPositions controls = _controls.Positions;
            BodyFrame frame = new BodyFrame(_body);

            float mainRotorPitch = _parameters.MainRotorPitch(controls.Collective);
            float tailRotorPitch = _parameters.TailRotorPitch(controls.Pedals);
            Vector3 mainHub = frame.PointAt(_parameters.MainHubOffset);
            Vector3 tailHub = frame.PointAt(_parameters.TailHubOffset);
            Vector3 tailAxis = frame.Right * -_parameters.SpinSign;

            _mainRotor.Step(
                new RotorSample(frame.Up, frame.Forward, frame.Right, _body.GetPointVelocity(mainHub)),
                mainRotorPitch, controls.CyclicLateral, controls.CyclicLongitudinal,
                _drivetrain.RotorSpeed, deltaTime);

            _tailRotor.Step(
                new RotorSample(tailAxis, frame.Forward, frame.Up, _body.GetPointVelocity(tailHub)),
                tailRotorPitch, 0f, 0f,
                _drivetrain.TailRotorSpeed, deltaTime);

            _drivetrain.Step(_mainRotor.DragTorque, _tailRotor.DragTorque, deltaTime);

            _body.AddForceAtPosition(_mainRotor.ThrustForce, mainHub);
            _body.AddTorque(-_drivetrain.ShaftTorque * _parameters.SpinSign * frame.Up);
            _body.AddTorque(_mainRotor.HubMoment);
            _body.AddForceAtPosition(_tailRotor.ThrustForce, tailHub);

            ApplyFuselageForces(frame);
            ApplySkidForces(frame);
            WriteState(frame, controls, mainRotorPitch, tailRotorPitch);
        }

        private void ApplyFuselageForces(in BodyFrame frame)
        {
            Vector3 centerOfPressure = frame.PointAt(_parameters.CenterOfPressureOffset);
            Vector3 localVelocity = frame.ToLocal(_body.GetPointVelocity(centerOfPressure));
            Vector3 localAngularVelocity = frame.ToLocal(_body.angularVelocity);

            _body.AddForceAtPosition(frame.ToWorld(_fuselage.DragForce(localVelocity)), centerOfPressure);
            _body.AddTorque(frame.ToWorld(_fuselage.DampingTorque(localAngularVelocity)));
        }

        private void ApplySkidForces(in BodyFrame frame)
        {
            for (int i = 0; i < _skidContacts.Length; i++)
            {
                Vector3 contact = frame.PointAt(_parameters.SkidContactOffsets[i]);
                _skidContacts[i] = new SkidContactSample(contact, _body.GetPointVelocity(contact));
            }

            _skidGear.Step(_skidContacts, frame.Up);

            for (int i = 0; i < _skidGear.Forces.Count; i++)
            {
                SkidContactForce contactForce = _skidGear.Forces[i];
                _body.AddForceAtPosition(contactForce.Force, contactForce.Point);
            }
        }

        private void WriteState(in BodyFrame frame, in ControlPositions controls, float mainRotorPitch, float tailRotorPitch)
        {
            _state.Controls = controls;
            _state.MainRotorPitch = mainRotorPitch;
            _state.TailRotorPitch = tailRotorPitch;
            _state.RotorSpeed = _drivetrain.RotorSpeed;
            _state.RotorSpeedRatio = _drivetrain.RotorSpeedRatio;
            _state.DiskNormal = _mainRotor.DiskNormal;
            _state.LocalDiskNormal = frame.ToLocal(_mainRotor.DiskNormal);
            _state.MainRotorThrust = _mainRotor.Thrust;
            _state.MainRotorDrag = _mainRotor.DragTorque;
            _state.TailRotorThrust = _tailRotor.Thrust;
            _state.TailRotorDrag = _tailRotor.DragTorque;
            _state.EngineTorque = _drivetrain.EngineTorque;
            _state.ShaftTorque = _drivetrain.ShaftTorque;
            _state.EnginePower = _drivetrain.EnginePower;
            _state.Velocity = _body.linearVelocity;
            _state.HasGroundContact = _skidGear.HasGroundContact;
        }

        private readonly struct BodyFrame
        {
            private readonly Quaternion _rotation;
            private readonly Vector3 _centerOfMass;

            public readonly Vector3 Up;
            public readonly Vector3 Forward;
            public readonly Vector3 Right;

            public BodyFrame(Rigidbody body)
            {
                _rotation = body.rotation;
                _centerOfMass = body.worldCenterOfMass;
                Up = _rotation * Vector3.up;
                Forward = _rotation * Vector3.forward;
                Right = _rotation * Vector3.right;
            }

            public Vector3 PointAt(Vector3 offsetFromCenterOfMass) => _centerOfMass + _rotation * offsetFromCenterOfMass;

            public Vector3 ToLocal(Vector3 worldVector) => Quaternion.Inverse(_rotation) * worldVector;

            public Vector3 ToWorld(Vector3 localVector) => _rotation * localVector;
        }
    }
}