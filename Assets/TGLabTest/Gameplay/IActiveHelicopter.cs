using TGLabTest.Simulation;
using UnityEngine;

namespace TGLabTest.Gameplay
{
    public interface IActiveHelicopter
    {
        bool IsPresent { get; }
        Transform Transform { get; }
        IHelicopterState State { get; }
    }
}