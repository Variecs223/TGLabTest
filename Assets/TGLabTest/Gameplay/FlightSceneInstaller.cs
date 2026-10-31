using System;
using TGLabTest.Gameplay.Controls;
using Zenject;

namespace TGLabTest.Gameplay
{
    public sealed class FlightSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<FlightInput>().AsSingle();
            Container.Bind<IDisposable>().To<FlightInput>().FromResolve();
            Container.BindInterfacesTo<FlightControls>().AsSingle();
            Container.BindInterfacesAndSelfTo<ActiveHelicopter>().AsSingle();
        }
    }
}