using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixer
using System.Collections;
using MountainRescue.Interfaces;
using MountainRescue.UI;

namespace MountainRescue.Systems.Safety
{
    public class FallSafetySystem : MonoBehaviour
    {
        private enum SafetyState { Grounded, Falling, Respawning }

        [Header("Fall Logic")]
        [SerializeField] private float fatalFallDistance = 5.0f;
        [SerializeField] private float groundScanRange = 2.0f;
        [SerializeField] private LayerMask safeGroundMask;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip impactClip;

        [Header("Audio Mixer Settings")]
        [Tooltip("Assign your MasterMixer here.")]
        [SerializeField] private AudioMixer targetMixer;

        [Tooltip("Exposed Parameter name for Ambience.")]
        [SerializeField] private string ambienceLowPassParam = "AmbienceLowPass";

        [Tooltip("Exposed Parameter name for Music.")]
        [SerializeField] private string musicLowPassParam = "MusicLowPass";

        [Space]
        [SerializeField] private float concussedCutoffHz = 600f; // Muffled sound
        [SerializeField] private float normalCutoffHz = 22000f;  // Clear sound

        [Header("Respawn Visuals")]
        [SerializeField] private Transform respawnLocation;
        [SerializeField] private float whiteFlashDuration = 0.2f;
        [SerializeField] private float fadeToBlackDuration = 2.0f;
        [SerializeField] private float blackoutDuration = 1.0f;
        [SerializeField] private float recoveryFadeSpeed = 2.0f;

        [Header("Dependencies")]
        [SerializeField] private CharacterController playerController;
        [SerializeField] private Transform headCamera;
        [SerializeField] private HeadsetFader screenFader;
        [SerializeField] private GameObject anchorSystem;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool verboseLogging = true;

        private SafetyState _currentState = SafetyState.Grounded;
        private IAnchorStateProvider _anchorSystem;
        private float _apexAltitude;

        private void Start()
        {
            if (verboseLogging) Debug.Log("[FallSafety] Initializing System...");

            if (playerController == null) playerController = GetComponentInParent<CharacterController>();
            if (headCamera == null && Camera.main != null) headCamera = Camera.main.transform;
            if (anchorSystem != null) _anchorSystem = anchorSystem.GetComponent<IAnchorStateProvider>();

            // Ensure sound starts clear (resetting any stuck state from previous runs)
            SetMixerFreq(normalCutoffHz);
        }

        private void Update()
        {
            if (playerController == null) return;

            switch (_currentState)
            {
                case SafetyState.Grounded:
                    if (!CheckGroundContact()) StartFall();
                    break;
                case SafetyState.Falling:
                    UpdateFallingState();
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
            if (headCamera.position.y > _apexAltitude) _apexAltitude = headCamera.position.y;

            if (CheckGroundContact()) HandleImpact();
        }

        private void HandleImpact()
        {
            float dropDistance = _apexAltitude - headCamera.position.y;

            if (verboseLogging)
                Debug.Log($"[FallSafety] IMPACT. Drop: {dropDistance:F2}m (Fatal: {fatalFallDistance}m)");

            if (dropDistance < fatalFallDistance)
            {
                _currentState = SafetyState.Grounded;
                return;
            }

            if (_anchorSystem != null && _anchorSystem.IsAnchored())
            {
                if (verboseLogging) Debug.Log("[FallSafety] Saved by Safety Gear.");
                _currentState = SafetyState.Grounded;
                return;
            }

            StartCoroutine(ConcussionRespawnRoutine());
        }

        private bool CheckGroundContact()
        {
            Vector3 feetPos = playerController.bounds.center - new Vector3(0, playerController.bounds.extents.y, 0);
            Vector3 origin = feetPos + Vector3.up * 0.1f;
            return Physics.SphereCast(origin, playerController.radius * 0.8f, Vector3.down, out _, 0.1f + groundScanRange, safeGroundMask);
        }

        private IEnumerator ConcussionRespawnRoutine()
        {
            _currentState = SafetyState.Respawning;
            playerController.enabled = false;

            // 1. IMPACT FX (Audio + Mixer)
            if (audioSource && impactClip) audioSource.PlayOneShot(impactClip);

            // Muffle BOTH Ambience and Music immediately
            SetMixerFreq(concussedCutoffHz);

            // 2. FLASH WHITE
            if (screenFader)
            {
                screenFader.SetColor(new Color(1, 1, 1, 0.6f));
                yield return screenFader.FadeToColor(Color.white, whiteFlashDuration);
            }

            // 3. FADE TO BLACK
            if (screenFader)
            {
                yield return screenFader.FadeToColor(Color.black, fadeToBlackDuration);
            }
            else
            {
                yield return new WaitForSeconds(fadeToBlackDuration);
            }

            // 4. TELEPORT
            yield return new WaitForSeconds(blackoutDuration);

            if (respawnLocation != null)
            {
                Transform rigTransform = playerController.transform;
                rigTransform.position = respawnLocation.position;

                float headY = headCamera.localEulerAngles.y;
                float targetY = respawnLocation.eulerAngles.y;
                rigTransform.rotation = Quaternion.Euler(0, targetY - headY, 0);

                Physics.SyncTransforms();
            }
            else if (verboseLogging)
            {
                Debug.LogError("[FallSafety] No Respawn Point Assigned!");
            }

            // 5. RECOVERY
            // Restore audio clarity to both groups
            SetMixerFreq(normalCutoffHz);

            playerController.enabled = true;
            _currentState = SafetyState.Grounded;
            _apexAltitude = headCamera.position.y;

            if (screenFader) yield return screenFader.FadeIn(recoveryFadeSpeed);
        }

        private void SetMixerFreq(float freq)
        {
            if (targetMixer != null)
            {
                // Set Ambience
                targetMixer.SetFloat(ambienceLowPassParam, freq);

                // Set Music
                targetMixer.SetFloat(musicLowPassParam, freq);
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || playerController == null) return;

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