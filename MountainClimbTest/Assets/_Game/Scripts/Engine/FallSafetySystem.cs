using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;
using MountainRescue.Interfaces;
using MountainRescue.UI;
using MountainRescue.Systems; // [INTEGRATION] Added for ToolRespawner

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
        [SerializeField] private AudioMixer targetMixer;
        [SerializeField] private string ambienceLowPassParam = "AmbienceLowPass";
        [SerializeField] private string musicLowPassParam = "MusicLowPass";
        [Space]
        [SerializeField] private float concussedCutoffHz = 600f;
        [SerializeField] private float normalCutoffHz = 22000f;

        [Header("Respawn Visuals")]
        [Tooltip("Automatically updated on Scene Load.")]
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

        [Header("Inventory Safety")] // [INTEGRATION] New Header
        [Tooltip("Reference to the system that resets lost tools.")]
        [SerializeField] private ToolRespawner toolRespawner;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool verboseLogging = true;

        private SafetyState _currentState = SafetyState.Grounded;
        private IAnchorStateProvider _anchorSystem;
        private float _apexAltitude;
        private float _lastFrameY; // Tracks position to detect teleports

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 1. Reset everything immediately
            StopAllCoroutines();
            ResetSafetyState();

            // 2. Try to find the Spawn Point (Start a search routine in case it's not ready yet)
            StartCoroutine(FindSpawnPointRoutine());
        }

        private void Start()
        {
            if (verboseLogging) Debug.Log("[FallSafety] Initializing System...");

            if (playerController == null) playerController = GetComponentInParent<CharacterController>();
            if (headCamera == null && Camera.main != null) headCamera = Camera.main.transform;
            if (anchorSystem != null) _anchorSystem = anchorSystem.GetComponent<IAnchorStateProvider>();

            SetMixerFreq(normalCutoffHz);

            // Init altitude tracking
            if (headCamera)
            {
                _apexAltitude = headCamera.position.y;
                _lastFrameY = headCamera.position.y;
            }

            // Initial spawn point check
            if (respawnLocation == null) StartCoroutine(FindSpawnPointRoutine());
        }

        private void Update()
        {
            if (playerController == null || headCamera == null) return;

            // --- TELEPORT DETECTION ---
            // Detect if we moved an impossible distance in one frame (e.g., Scene Load 110m -> 0m)
            float frameDrop = _lastFrameY - headCamera.position.y;

            if (frameDrop > 4.0f)
            {
                if (verboseLogging) Debug.LogWarning($"[FallSafety] Teleport detected (Drop: {frameDrop:F2}m in 1 frame). Resetting fall logic.");
                ResetSafetyState();
                _lastFrameY = headCamera.position.y;
                return; // Skip the rest of this frame
            }
            _lastFrameY = headCamera.position.y;
            // --------------------------

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

        /// <summary>
        /// Resets the system to a safe, grounded state. Clears effects and fall memory.
        /// </summary>
        private void ResetSafetyState()
        {
            _currentState = SafetyState.Grounded;

            if (headCamera != null)
            {
                _apexAltitude = headCamera.position.y;
                _lastFrameY = headCamera.position.y;
            }

            if (playerController != null) playerController.enabled = true;
            SetMixerFreq(normalCutoffHz);

            if (screenFader != null) screenFader.SetColor(Color.clear);
        }

        private void StartFall()
        {
            // Only log if not just initializing
            if (Time.time > 1.0f && verboseLogging)
                Debug.Log($"[FallSafety] Ground lost. FALL STARTED at altitude: {headCamera.position.y:F2}");

            _currentState = SafetyState.Falling;
            _apexAltitude = headCamera.position.y;
        }

        private void UpdateFallingState()
        {
            // 1. Update Apex
            if (headCamera.position.y > _apexAltitude) _apexAltitude = headCamera.position.y;

            float currentDrop = _apexAltitude - headCamera.position.y;
            bool isAnchored = _anchorSystem != null && _anchorSystem.IsAnchored();

            // 2. MID-AIR DEATH CHECK
            // If falling > 5m with no rope, die immediately (skipping ground check)
            if (!isAnchored && currentDrop > fatalFallDistance)
            {
                if (verboseLogging) Debug.Log($"[FallSafety] FATAL FALL LIMIT EXCEEDED MID-AIR. Drop: {currentDrop:F2}m");
                StartCoroutine(ConcussionRespawnRoutine());
                return;
            }

            // 3. Ground Impact Check
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

        // --- SPAWN POINT SEARCH LOGIC ---
        private IEnumerator FindSpawnPointRoutine()
        {
            // Try immediately
            if (TryFindSpawnPoint()) yield break;

            // Retry for a few frames (sometimes objects initialize slightly later)
            for (int i = 0; i < 5; i++)
            {
                yield return null; // Wait one frame
                if (TryFindSpawnPoint()) yield break;
            }

            if (verboseLogging) Debug.LogError("[FallSafety] COULD NOT FIND SPAWN POINT! Ensure you have an object tagged 'Respawn' or named 'SpawnPoint'.");
        }

        private bool TryFindSpawnPoint()
        {
            // 1. Try Tag
            GameObject spawnObj = GameObject.FindGameObjectWithTag("Respawn");

            // 2. Try Name "SpawnPoint"
            if (spawnObj == null) spawnObj = GameObject.Find("SpawnPoint");

            // 3. Try Name "PlayerSpawn"
            if (spawnObj == null) spawnObj = GameObject.Find("PlayerSpawn");

            if (spawnObj != null)
            {
                respawnLocation = spawnObj.transform;
                if (verboseLogging) Debug.Log($"[FallSafety] Spawn Point Found: {spawnObj.name}");
                return true;
            }
            return false;
        }

        private IEnumerator ConcussionRespawnRoutine()
        {
            if (MountainRescue.Systems.Session.GameSessionManager.Instance != null)
                MountainRescue.Systems.Session.GameSessionManager.Instance.RegisterDeath();

            _currentState = SafetyState.Respawning;
            playerController.enabled = false;

            // Audio FX
            if (audioSource && impactClip) audioSource.PlayOneShot(impactClip);
            SetMixerFreq(concussedCutoffHz);

            // Visual FX
            if (screenFader)
            {
                screenFader.SetColor(new Color(1, 1, 1, 0.6f));
                yield return screenFader.FadeToColor(Color.white, whiteFlashDuration);
                yield return screenFader.FadeToColor(Color.black, fadeToBlackDuration);
            }
            else
            {
                yield return new WaitForSeconds(fadeToBlackDuration);
            }

            yield return new WaitForSeconds(blackoutDuration);

            // Execute Teleport
            if (respawnLocation != null)
            {
                Transform rigTransform = playerController.transform;
                rigTransform.position = respawnLocation.position;

                // Match rotation
                if (headCamera != null)
                {
                    float headY = headCamera.localEulerAngles.y;
                    float targetY = respawnLocation.eulerAngles.y;
                    rigTransform.rotation = Quaternion.Euler(0, targetY - headY, 0);
                }

                Physics.SyncTransforms();
            }
            else
            {
                // Last ditch effort to find it if we somehow missed it earlier
                TryFindSpawnPoint();
                if (respawnLocation != null)
                {
                    playerController.transform.position = respawnLocation.position;
                    Physics.SyncTransforms();
                }
            }

            // [INTEGRATION] Recover Lost Tools
            // We call this immediately after teleporting so tools appear back on the belt instantly.
            if (toolRespawner != null)
            {
                toolRespawner.RecoverDroppedTools();
                if (verboseLogging) Debug.Log("[FallSafety] Tools recovered via ToolRespawner.");
            }
            else if (verboseLogging)
            {
                Debug.LogWarning("[FallSafety] ToolRespawner not assigned! Dropped tools were NOT recovered.");
            }

            // Recovery
            SetMixerFreq(normalCutoffHz);
            ResetSafetyState(); // Ensures Apex is reset to new position

            if (screenFader) yield return screenFader.FadeIn(recoveryFadeSpeed);
        }

        private void SetMixerFreq(float freq)
        {
            if (targetMixer != null)
            {
                targetMixer.SetFloat(ambienceLowPassParam, freq);
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