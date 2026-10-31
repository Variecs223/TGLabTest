using System;
using TGLabTest.Simulation;
using UnityEditor;
using UnityEngine;

namespace TGLabTest.EditorTools
{
    [CustomEditor(typeof(HelicopterConfig))]
    public sealed class HelicopterConfigEditor : Editor
    {
        private const string ValueFormat = "G5";
        private const float DerivedWidth = 90f;
        private const float ToggleWidth = 16f;
        private const float RpmPerRadianPerSecond = 60f / (2f * Mathf.PI);

        private static readonly CoefficientRow[] Rows =
        {
            new CoefficientRow("_thrustCoefficient", "kT", "Main thrust",
                "Thrust per rotor speed² per radian of blade pitch.", c => c.ThrustCoefficient),
            new CoefficientRow("_axialDamping", "kv", "Main climb damping",
                "Thrust lost per rotor speed per m/s of climb along the thrust axis.", c => c.AxialDamping),
            new CoefficientRow("_baseTorqueCoefficient", "q0", "Main base torque",
                "Drag torque per rotor speed² at zero blade pitch.", c => c.BaseTorqueCoefficient),
            new CoefficientRow("_pitchTorqueCoefficient", "q1", "Main pitch torque",
                "Extra drag torque per rotor speed² per blade pitch².", c => c.PitchTorqueCoefficient),
            new CoefficientRow("_tailThrustCoefficient", "kTt", "Tail thrust",
                "Tail thrust per tail rotor speed² per radian of blade pitch.", c => c.TailThrustCoefficient),
            new CoefficientRow("_tailAxialDamping", "kvt", "Tail yaw damping",
                "Tail thrust lost per tail rotor speed per m/s of tail hub motion along its axis.", c => c.TailAxialDamping),
            new CoefficientRow("_tailBaseTorqueCoefficient", "q0t", "Tail base torque",
                "Tail drag torque per tail rotor speed² at zero blade pitch.", c => c.TailBaseTorqueCoefficient),
            new CoefficientRow("_tailPitchTorqueCoefficient", "q1t", "Tail pitch torque",
                "Extra tail drag torque per tail rotor speed² per blade pitch².", c => c.TailPitchTorqueCoefficient),
            new CoefficientRow("_engineTorqueRate", "rate", "Engine torque rate",
                "How fast engine torque can change, N·m/s.", c => c.EngineTorqueRate),
            new CoefficientRow("_governorGain", "Kgov", "Governor gain",
                "Engine torque requested per rad/s of rotor speed error, N·m·s/rad.", c => c.GovernorGain)
        };

        private static readonly string[] OverridePropertyNames = Array.ConvertAll(Rows, row => row.PropertyName);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, OverridePropertyNames);
            serializedObject.ApplyModifiedProperties();

            HelicopterConfig config = (HelicopterConfig)target;

            DrawCoefficients(HelicopterCoefficients.Derive(config));
            DrawHoverBalance(new HelicopterParameters(config));
        }

        private void DrawCoefficients(HelicopterCoefficients derived)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Coefficients", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(" ");
                EditorGUILayout.LabelField("Derived", EditorStyles.miniBoldLabel, GUILayout.Width(DerivedWidth));
                EditorGUILayout.LabelField("Override", EditorStyles.miniBoldLabel);
            }

            foreach (CoefficientRow row in Rows)
            {
                DrawCoefficient(row, row.Select(derived));
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCoefficient(CoefficientRow row, float derived)
        {
            SerializedProperty property = serializedObject.FindProperty(row.PropertyName);
            SerializedProperty enabled = property.FindPropertyRelative("_enabled");
            SerializedProperty value = property.FindPropertyRelative("_value");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent($"{row.Symbol}  {row.Name}", row.Tooltip));
                EditorGUILayout.SelectableLabel(derived.ToString(ValueFormat), GUILayout.Width(DerivedWidth),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));

                bool wasEnabled = enabled.boolValue;
                enabled.boolValue = EditorGUILayout.Toggle(wasEnabled, GUILayout.Width(ToggleWidth));

                if (enabled.boolValue && !wasEnabled)
                {
                    value.floatValue = derived;
                }

                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                {
                    value.floatValue = EditorGUILayout.FloatField(value.floatValue);
                }
            }
        }

        private static void DrawHoverBalance(HelicopterParameters parameters)
        {
            RotorParameters main = parameters.MainRotor;
            RotorParameters tail = parameters.TailRotor;
            DrivetrainParameters drivetrain = parameters.Drivetrain;

            float rotorSpeed = drivetrain.NominalRotorSpeed;
            float tailRotorSpeed = rotorSpeed * drivetrain.TailGearRatio;
            float weight = parameters.Mass * Physics.gravity.magnitude;
            float thrust = main.Thrust(rotorSpeed, main.NeutralPitch, 0f);
            float mainTorque = main.DragTorque(rotorSpeed, main.NeutralPitch);
            float tailThrust = tail.Thrust(tailRotorSpeed, tail.NeutralPitch, 0f);
            float tailMoment = tailThrust * -parameters.TailHubOffset.z;
            float hoverPower = parameters.HoverDrivetrainTorque * rotorSpeed;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hover balance (mid collective, centred pedals, nominal rotor speed)", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                DrawReadout("Hover pitch", $"{main.NeutralPitch * Mathf.Rad2Deg:F2}°");
                DrawReadout("Thrust / weight", $"{thrust / 1000f:F2} / {weight / 1000f:F2} kN ({thrust / weight:P1})");
                DrawReadout("Main rotor torque", $"{mainTorque / 1000f:F2} kN·m");
                DrawReadout("Tail rotor thrust", $"{tailThrust / 1000f:F3} kN");
                DrawReadout("Yaw imbalance", $"{tailMoment - mainTorque:F1} N·m");
                DrawReadout("Drivetrain power", $"{hoverPower / 1000f:F0} / {drivetrain.MaxPower / 1000f:F0} kW");
                DrawReadout("Tail rotor speed", $"{tailRotorSpeed:F1} rad/s");
                DrawReadout("Governor target", $"{drivetrain.GovernorTargetSpeed * RpmPerRadianPerSecond:F1} rpm");
            }
        }

        private static void DrawReadout(string label, string value)
        {
            EditorGUILayout.LabelField(label, value);
        }

        private readonly struct CoefficientRow
        {
            public readonly string PropertyName;
            public readonly string Symbol;
            public readonly string Name;
            public readonly string Tooltip;
            public readonly Func<HelicopterCoefficients, float> Select;

            public CoefficientRow(string propertyName, string symbol, string name, string tooltip,
                Func<HelicopterCoefficients, float> select)
            {
                PropertyName = propertyName;
                Symbol = symbol;
                Name = name;
                Tooltip = tooltip;
                Select = select;
            }
        }
    }
}