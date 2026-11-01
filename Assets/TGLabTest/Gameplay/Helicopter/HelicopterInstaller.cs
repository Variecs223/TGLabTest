using TGLabTest.Simulation;
using UnityEngine;
using Zenject;

namespace TGLabTest.Gameplay.Helicopter
{
    public sealed class HelicopterInstaller : MonoInstaller
    {
        private const float GizmoRadius = 0.15f;
        private const float TailThrustGizmoLength = 1.5f;

        [SerializeField] private HelicopterConfig _config;
        [SerializeField] private Rigidbody _body;

        public override void InstallBindings()
        {
            Container.Bind<HelicopterParameters>().FromInstance(new HelicopterParameters(_config));
            Container.Bind<Rigidbody>().FromInstance(_body);
            Container.BindInterfacesAndSelfTo<HelicopterState>().AsSingle();
            Container.Bind<IGroundProbe>().To<RaycastGroundProbe>().AsSingle();
            Container.BindInterfacesTo<HelicopterSimulation>().AsSingle();
            Container.BindInterfacesTo<HelicopterRegistration>().AsSingle();
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null)
            {
                return;
            }

            Transform body = _body != null ? _body.transform : transform;
            Vector3 centerOfMass = body.TransformPoint(_config.CenterOfMass);
            Vector3 mainHub = PointAt(body, _config.MainHubOffset);
            Vector3 tailHub = PointAt(body, _config.TailHubOffset);
            Vector3 tailThrust = body.right * -(int)_config.SpinDirection;

            Gizmos.color = Color.white;
            Gizmos.DrawSphere(centerOfMass, GizmoRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(centerOfMass, mainHub);
            Gizmos.DrawSphere(mainHub, GizmoRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(centerOfMass, tailHub);
            Gizmos.DrawSphere(tailHub, GizmoRadius);
            Gizmos.DrawRay(tailHub, tailThrust * TailThrustGizmoLength);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(PointAt(body, _config.CenterOfPressureOffset), GizmoRadius);
            Gizmos.DrawSphere(PointAt(body, _config.StabilizerOffset), GizmoRadius);

            Gizmos.color = Color.green;
            foreach (Vector3 skidContact in _config.SkidContactOffsets)
            {
                Gizmos.DrawSphere(PointAt(body, skidContact), GizmoRadius);
            }
        }

        private Vector3 PointAt(Transform body, Vector3 offsetFromCenterOfMass) =>
            body.TransformPoint(_config.CenterOfMass + offsetFromCenterOfMass);
    }
}
