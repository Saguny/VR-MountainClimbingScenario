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

            // Calculate target position in front of face
            Vector3 targetPos = _cam.position + (_cam.forward * distance);
            targetPos.y += heightOffset;

            // Smooth Damp for buttery smooth movement
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);

            // Always face the player
            transform.LookAt(transform.position + _cam.rotation * Vector3.forward, _cam.rotation * Vector3.up);
        }
    }
}