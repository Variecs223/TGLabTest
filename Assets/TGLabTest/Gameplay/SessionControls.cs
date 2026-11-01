using System;
using TGLabTest.Gameplay.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace TGLabTest.Gameplay
{
    public sealed class SessionControls : IInitializable, ITickable, IDisposable
    {
        private readonly FlightInput _input;

        private bool _isLeaving;

        public SessionControls(FlightInput input)
        {
            _input = input;
        }

        public void Initialize()
        {
            _input.Session.Enable();
        }

        public void Dispose()
        {
            _input.Session.Disable();
        }

        public void Tick()
        {
            if (_isLeaving)
            {
                return;
            }

            if (_input.Session.Quit.WasPressedThisFrame())
            {
                _isLeaving = true;
                Quit();
            }
            else if (_input.Session.Restart.WasPressedThisFrame())
            {
                _isLeaving = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}