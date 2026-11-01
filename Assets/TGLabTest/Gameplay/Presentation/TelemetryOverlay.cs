using TGLabTest.Simulation;
using UnityEngine;
using Zenject;

namespace TGLabTest.Gameplay.Presentation
{
    public sealed class TelemetryOverlay : MonoBehaviour
    {
        private const float RpmPerRadianPerSecond = 60f / (2f * Mathf.PI);
        private const float Kilo = 0.001f;

        [SerializeField] private Rect _area = new Rect(10f, 10f, 280f, 400f);

        private IActiveHelicopter _activeHelicopter;

        [Inject]
        public void Construct(IActiveHelicopter activeHelicopter)
        {
            _activeHelicopter = activeHelicopter;
        }

        private void OnGUI()
        {
            if (!_activeHelicopter.IsPresent)
            {
                return;
            }

            IHelicopterState state = _activeHelicopter.State;
            ControlPositions controls = state.Controls;
            Vector3 velocity = state.Velocity;

            GUILayout.BeginArea(_area, GUI.skin.box);

            Line("Collective", $"{controls.Collective:P0}");
            Line("Cyclic lat / lon", $"{controls.CyclicLateral:+0.00;-0.00} / {controls.CyclicLongitudinal:+0.00;-0.00}");
            Line("Pedals", $"{controls.Pedals:+0.00;-0.00}");
            Line("Main / tail pitch", $"{state.MainRotorPitch * Mathf.Rad2Deg:F1}° / {state.TailRotorPitch * Mathf.Rad2Deg:F1}°");
            Line("Rotor", $"{state.RotorSpeed * RpmPerRadianPerSecond:F0} rpm ({state.RotorSpeedRatio:P1})");
            Line("Main thrust", $"{state.MainRotorThrust * Kilo:F1} kN");
            Line("Tail thrust", $"{state.TailRotorThrust * Kilo:F2} kN");
            Line("Stabilizer", $"{state.StabilizerForce * Kilo:+0.00;-0.00} kN");
            Line("Engine torque", $"{state.EngineTorque * Kilo:F1} kN·m");
            Line("Shaft torque", $"{state.ShaftTorque * Kilo:F1} kN·m");
            Line("Engine power", $"{state.EnginePower * Kilo:F0} kW");
            Line("Vertical speed", $"{velocity.y:+0.0;-0.0} m/s");
            Line("Ground speed", $"{new Vector2(velocity.x, velocity.z).magnitude:F1} m/s");
            Line("Ground contact", state.HasGroundContact ? "yes" : "no");
            GUILayout.FlexibleSpace();
            Line("Restart", "R");
            Line("Quit", "Esc");

            GUILayout.EndArea();
        }

        private static void Line(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            GUILayout.FlexibleSpace();
            GUILayout.Label(value);
            GUILayout.EndHorizontal();
        }
    }
}