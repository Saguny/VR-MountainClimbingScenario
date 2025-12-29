using UnityEngine;
using System.Collections;
using MountainRescue.Interfaces;
using MountainRescue.UI;

namespace MountainRescue.Systems.Safety
{
    public class FallSafetySystem : MonoBehaviour
    {
        [Header("Fall Logic")]
        [Tooltip("If you hit the ground after falling this distance, you blackout.")]
        [SerializeField] private float fallThresholdMeters = 5.0f;
        [SerializeField] private Transform respawnPoint;

        [Header("Timing")]
        [Tooltip("Time to stay blacked out before respawning.")]
        [SerializeField] private float blackoutDuration = 2.0f;
        [Tooltip("How fast the screen fades back IN after respawn.")]
        [SerializeField] private float recoveryFadeSpeed = 2.0f;

        [Header("Dependencies")]
        [SerializeField] private CharacterController playerController;
        [SerializeField] private Transform playerHead;
        [SerializeField] private HeadsetFader fader;
        [SerializeField] private GameObject anchorSystemObject;

        private IAnchorStateProvider _anchorProvider;
        private Vector3 _highestPoint;
        private bool _isAirborne;
        private bool _isConsequenceActive;

        private void Start()
        {
            if (anchorSystemObject != null)
                _anchorProvider = anchorSystemObject.GetComponent<IAnchorStateProvider>();

            if (playerHead == null && Camera.main != null)
                playerHead = Camera.main.transform;
        }

        private void Update()
        {
            if (_isConsequenceActive) return;

            // Robust Ground Check
            bool isGrounded = playerController.isGrounded || playerController.velocity.y > -0.1f;

            // --- LANDING LOGIC ---
            if (isGrounded)
            {
                if (_isAirborne)
                {
                    // We just hit the ground this frame. Check damage.
                    CheckImpact();
                }

                _isAirborne = false;
            }
            // --- AIRBORNE LOGIC ---
            else
            {
                if (!_isAirborne)
                {
                    // Just left the ground
                    _isAirborne = true;
                    _highestPoint = playerHead.position;
                }
                else
                {
                    // Keep tracking highest point while in air
                    if (playerHead.position.y > _highestPoint.y)
                        _highestPoint = playerHead.position;
                }
            }
        }

        private void CheckImpact()
        {
            // 1. Calculate total drop
            float totalDrop = _highestPoint.y - playerHead.position.y;

            // 2. Ignore if fall was small
            if (totalDrop < fallThresholdMeters) return;

            // 3. Ignore if Safe (Rope caught us at the last second)
            if (_anchorProvider != null && _anchorProvider.IsAnchored()) return;

            // 4. TRIGGER INSTANT BLACKOUT
            StartCoroutine(ImpactSequence());
        }

        private IEnumerator ImpactSequence()
        {
            _isConsequenceActive = true;
            Debug.Log("!! IMPACT DETECTED - BLACKOUT !!");

            // A. INSTANT BLACK (No fade time)
            fader.SnapToBlack();

            // B. Optional: Play heavy impact sound here
            // audioSource.PlayOneShot(impactClip);

            // C. Wait in darkness
            yield return new WaitForSeconds(blackoutDuration);

            // D. Respawn Player
            if (respawnPoint != null)
            {
                playerController.enabled = false;
                Transform rig = playerController.transform.parent ?? playerController.transform;
                rig.position = respawnPoint.position;
                rig.rotation = respawnPoint.rotation;
                playerController.enabled = true;
            }

            // E. Reset Physics Logic
            _isAirborne = false;
            _highestPoint = playerHead.position;

            // F. Slow Fade In (Recovery)
            yield return fader.FadeIn(recoveryFadeSpeed);

            _isConsequenceActive = false;
        }
    }
}