using TGLabTest.Simulation;
using UnityEngine;
using Zenject;

namespace TGLabTest.Gameplay.Helicopter
{
    public sealed class HelicopterInstaller : MonoInstaller
    {
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
    }
}