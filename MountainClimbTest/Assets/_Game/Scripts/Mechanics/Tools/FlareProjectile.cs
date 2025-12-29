using System.Collections;
using UnityEngine;

namespace Game.Mechanics.Tools
{
    [RequireComponent(typeof(Rigidbody))]
    public class FlareProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 25f;
        [SerializeField] private float targetHeight = 100f;
        [SerializeField] private float hangTime = 30f;

        [Header("FX")]
        [SerializeField] private ParticleSystem trailParticles;
        [SerializeField] private Light flareLight;

        private Rigidbody _rb;
        private float _startY;
        private bool _hanging;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false; // We control flight manually
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        public void Launch()
        {
            _startY = transform.position.y;
            // Force straight up direction
            _rb.linearVelocity = Vector3.up * speed;
        }

        private void FixedUpdate()
        {
            if (_hanging) return;

            // Check height
            if (transform.position.y >= _startY + targetHeight)
            {
                StartCoroutine(HangRoutine());
            }
            else
            {
                // Maintain upward velocity
                _rb.linearVelocity = Vector3.up * speed;
            }
        }

        private IEnumerator HangRoutine()
        {
            _hanging = true;
            _rb.isKinematic = true; // Freeze physics

            yield return new WaitForSeconds(hangTime);

            // Cleanup visuals before destroy
            if (trailParticles) trailParticles.Stop();
            if (flareLight) flareLight.enabled = false;

            yield return new WaitForSeconds(2f);
            Destroy(gameObject);
        }
    }
}