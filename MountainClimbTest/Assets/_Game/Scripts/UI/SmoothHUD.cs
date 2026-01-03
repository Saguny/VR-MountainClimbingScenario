using UnityEngine;

namespace Game.UI
{
    public class SmoothHUD : MonoBehaviour
    {
        [SerializeField] private float distance = 2.0f;
        [SerializeField] private float smoothTime = 0.3f;
        [SerializeField] private float heightOffset = -0.1f;

        private Vector3 _velocity = Vector3.zero;
        private Transform _cam;

        private void Start()
        {
            _cam = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (!_cam) return;

            Vector3 targetPos = _cam.position + (_cam.forward * distance);
            targetPos.y += heightOffset;

            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);

            // This forces the UI to match the camera's tilt exactly
            transform.rotation = _cam.rotation;
        }
    }
}