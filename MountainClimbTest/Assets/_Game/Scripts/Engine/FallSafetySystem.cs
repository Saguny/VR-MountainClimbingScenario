using UnityEngine;
using System.Collections;
using MountainRescue.Interfaces;
using MountainRescue.UI;

namespace MountainRescue.Systems.Safety
{
    public class FallSafetySystem : MonoBehaviour
    {
        private enum SafetyState
        {
            Grounded,
            Falling,
            Respawning
        }

        [Header("Fall Logic")]
        [SerializeField] private float fatalFallDistance = 5.0f;
        [SerializeField] private float groundScanRange = 2.0f;
        [SerializeField] private LayerMask safeGroundMask;

        [Header("Respawn")]
        [SerializeField] private Transform respawnLocation;
        [SerializeField] private float blackoutDuration = 1.0f;
        [SerializeField] private float fadeRecoverySpeed = 2.0f;

        [Header("Dependencies")]
        [SerializeField] private CharacterController playerController;
        [SerializeField] private Transform headCamera;
        [SerializeField] private HeadsetFader screenFader;
        [SerializeField] private GameObject anchorSystem;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [Tooltip("If true, prints detailed logs to the console.")]
        [SerializeField] private bool verboseLogging = true;

        private SafetyState _currentState = SafetyState.Grounded;
        private IAnchorStateProvider _anchorSystem;
        private float _apexAltitude;

        private void Start()
        {
            if (verboseLogging) Debug.Log("[FallSafety] Initializing System...");

            if (playerController == null) playerController = GetComponentInParent<CharacterController>();
            if (headCamera == null && Camera.main != null) headCamera = Camera.main.transform;

            if (anchorSystem != null)
            {
                _anchorSystem = anchorSystem.GetComponent<IAnchorStateProvider>();
                if (verboseLogging) Debug.Log("[FallSafety] Anchor System Connected.");
            }
            else
            {
                if (verboseLogging) Debug.LogWarning("[FallSafety] No Anchor System assigned! Safety gear will be ignored.");
            }

            if (safeGroundMask == 0)
            {
                Debug.LogError("[FallSafety] Safe Ground Mask is NOT SET! You will fall forever.");
            }
        }

        private void Update()
        {
            if (playerController == null) return;

            switch (_currentState)
            {
                case SafetyState.Grounded:
                    // If we are currently grounded, check if we just walked off a ledge
                    if (!CheckGroundContact())
                    {
                        StartFall();
                    }
                    break;

                case SafetyState.Falling:
                    UpdateFallingState();
                    break;

                case SafetyState.Respawning:
                    // Do nothing, controls are locked
                    break;
            }
        }

        private void StartFall()
        {
            if (verboseLogging) Debug.Log($"[FallSafety] Ground lost. FALL STARTED at altitude: {headCamera.position.y:F2}");

            _currentState = SafetyState.Falling;
            _apexAltitude = headCamera.position.y;
        }

        private void UpdateFallingState()
        {
            // Update highest point if we jumped up
            if (headCamera.position.y > _apexAltitude)
            {
                _apexAltitude = headCamera.position.y;
            }

            // Check if we have hit the ground
            if (CheckGroundContact())
            {
                HandleImpact();
            }
        }

        private void HandleImpact()
        {
            float dropDistance = _apexAltitude - headCamera.position.y;

            if (verboseLogging)
                Debug.Log($"[FallSafety] IMPACT DETECTED. Total Drop: {dropDistance:F2}m (Fatal Threshold: {fatalFallDistance}m)");

            // 1. Check distance
            if (dropDistance < fatalFallDistance)
            {
                if (verboseLogging) Debug.Log("[FallSafety] Fall was safe (distance too short). Returning to Grounded.");
                _currentState = SafetyState.Grounded;
                return;
            }

            // 2. Check gear
            if (_anchorSystem != null)
            {
                bool isAnchored = _anchorSystem.IsAnchored();
                if (verboseLogging) Debug.Log($"[FallSafety] Checking Safety Gear... Is Anchored? {isAnchored}");

                if (isAnchored)
                {
                    if (verboseLogging) Debug.Log("[FallSafety] SAVED BY GEAR! Fatal impact avoided.");
                    _currentState = SafetyState.Grounded;
                    return;
                }
            }

            // 3. Die
            if (verboseLogging) Debug.Log("[FallSafety] FATAL FALL! Starting Respawn Sequence.");
            StartCoroutine(RespawnRoutine());
        }

        private bool CheckGroundContact()
        {
            Vector3 feetPos = playerController.bounds.center - new Vector3(0, playerController.bounds.extents.y, 0);

            // Start slightly above feet to ensure we catch the floor even if slightly clipped
            Vector3 origin = feetPos + Vector3.up * 0.1f;
            float maxDist = 0.1f + groundScanRange;
            float radius = playerController.radius * 0.8f;

            bool hit = Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hitInfo, maxDist, safeGroundMask);

            // Optional: Very verbose raycast logging (comment out if too spammy)
            // if (verboseLogging && hit) Debug.Log($"[FallSafety] Ground detected: {hitInfo.collider.name} at dist {hitInfo.distance}");

            return hit;
        }

        private IEnumerator RespawnRoutine()
        {
            _currentState = SafetyState.Respawning;

            // Lock controls
            playerController.enabled = false;

            // Fade out
            if (screenFader != null)
            {
                if (verboseLogging) Debug.Log("[FallSafety] Fading to black...");
                screenFader.SnapToBlack();
            }

            yield return new WaitForSeconds(blackoutDuration);

            // Teleport
            if (respawnLocation != null)
            {
                if (verboseLogging) Debug.Log($"[FallSafety] Teleporting to {respawnLocation.name}...");

                Transform rigTransform = playerController.transform;
                rigTransform.position = respawnLocation.position;

                // Align rotation
                float headYRotation = headCamera.localEulerAngles.y;
                float targetYRotation = respawnLocation.eulerAngles.y;
                rigTransform.rotation = Quaternion.Euler(0, targetYRotation - headYRotation, 0);

                Physics.SyncTransforms();
            }
            else
            {
                if (verboseLogging) Debug.LogError("[FallSafety] No Respawn Point assigned!");
            }

            // Unlock controls
            playerController.enabled = true;

            // Reset state
            _currentState = SafetyState.Grounded;
            _apexAltitude = headCamera.position.y;

            if (verboseLogging) Debug.Log("[FallSafety] Respawn Complete. Player grounded.");

            // Fade in
            if (screenFader != null) yield return screenFader.FadeIn(fadeRecoverySpeed);
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || playerController == null) return;

            // Color code the state
            switch (_currentState)
            {
                case SafetyState.Grounded: Gizmos.color = Color.green; break;
                case SafetyState.Falling: Gizmos.color = Color.red; break;
                case SafetyState.Respawning: Gizmos.color = Color.yellow; break;
            }

            Vector3 feetPos = playerController.bounds.center - new Vector3(0, playerController.bounds.extents.y, 0);
            Vector3 origin = feetPos + Vector3.up * 0.1f;
            float maxDist = 0.1f + groundScanRange;

            Gizmos.DrawWireSphere(origin, 0.1f);
            Gizmos.DrawLine(origin, origin + Vector3.down * maxDist);
            Gizmos.DrawWireSphere(origin + Vector3.down * maxDist, playerController.radius * 0.8f);
        }
    }
}