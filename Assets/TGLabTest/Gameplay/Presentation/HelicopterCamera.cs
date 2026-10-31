using UnityEngine;
using Zenject;

namespace TGLabTest.Gameplay.Presentation
{
    public sealed class HelicopterCamera : MonoBehaviour
    {
        private const float MinHeadingSqrMagnitude = 1e-4f;

        [SerializeField, Min(0f)] private float _distance = 16f;
        [SerializeField] private float _height = 4f;
        [SerializeField] private float _lookHeight = 1f;
        [SerializeField, Min(0.01f)] private float _headingLag = 0.5f;

        private IActiveHelicopter _activeHelicopter;
        private Quaternion _heading;
        private bool _hasHeading;

        [Inject]
        public void Construct(IActiveHelicopter activeHelicopter)
        {
            _activeHelicopter = activeHelicopter;
        }

        private void LateUpdate()
        {
            if (!_activeHelicopter.IsPresent)
            {
                return;
            }

            Transform target = _activeHelicopter.Transform;
            FollowHeading(target.forward);

            Vector3 position = target.position + _heading * new Vector3(0f, _height, -_distance);
            Vector3 lookPoint = target.position + Vector3.up * _lookHeight;

            transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookPoint - position));
        }

        private void FollowHeading(Vector3 targetForward)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(targetForward, Vector3.up);

            if (flatForward.sqrMagnitude < MinHeadingSqrMagnitude)
            {
                return;
            }

            Quaternion targetHeading = Quaternion.LookRotation(flatForward);
            _heading = _hasHeading
                ? Quaternion.Slerp(_heading, targetHeading, 1f - Mathf.Exp(-Time.deltaTime / _headingLag))
                : targetHeading;
            _hasHeading = true;
        }
    }
}