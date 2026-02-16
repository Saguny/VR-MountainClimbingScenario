using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
// REQUIRED for DynamicMoveProvider
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace MountainRescue.Dialogue
{
    public class NPCDialogueController : MonoBehaviour, IDialoguePlayback
    {
        [Header("Story Status")]
        [Tooltip("Increments automatically when a sequence with 'advancesStory' finishes.")]
        public int CurrentStoryStage = 0;

        [Header("Manual UI Setup")]
        [Tooltip("Leave empty to auto-find using Subtitle Object Name below")]
        [SerializeField] private TextMeshProUGUI subtitleTMP;

        [Header("DDOL Subtitle Search")]
        [Tooltip("Name of the subtitle GameObject in the DDOL rig (e.g., 'NPCSubtitle')")]
        [SerializeField] private string subtitleObjectName = "";
        [Tooltip("Optional: Tag to search for (leave empty to use name only)")]
        [SerializeField] private string subtitleTag = "";
        [Tooltip("Optional: Name of the DDOL parent object (e.g., 'XR Origin' or 'PlayerRig')")]
        [SerializeField] private string ddolParentName = "";
        [Tooltip("Number of frames to wait before searching for DDOL subtitle")]
        [SerializeField] private int ddolSearchDelay = 2;
        [Tooltip("Max attempts to find DDOL subtitle")]
        [SerializeField] private int maxSearchAttempts = 5;

        [Header("NPC Components")]
        [SerializeField] private AudioSource speechSource;
        [SerializeField] private Animator npcAnimator;
        [SerializeField] private string walkingBool = "isWalking";
        [SerializeField] private float delayBetweenLines = 0.5f;

        [Header("Player Movement Control")]
        [Tooltip("Drag the XROrigin (or the object with DynamicMoveProvider) here.")]
        public DynamicMoveProvider moveProvider;

        [Header("Manual Head Tracking (Non-Humanoid)")]
        [Tooltip("Drag the specific bone for the Head here.")]
        [SerializeField] private Transform headBone;
        [SerializeField] private float bodyTurnSpeed = 2f;
        [Tooltip("How far can he turn his head before the body follows? (Degrees)")]
        [SerializeField] private float maxHeadAngle = 60f;
        [SerializeField] private float trackingRange = 10f;

        [Header("Head Tracking Settings")]
        [Tooltip("0 = Just Animation, 1 = Laser Focus. 0.6 is a good 'Natural' blend.")]
        [Range(0f, 1f)]
        [SerializeField] private float lookWeight = 0.6f;

        [Header("Initial Content")]
        [SerializeField] private DialogueSequence initialSequence;

        [Header("Auto-Play Settings")]
        [Tooltip("Auto-start this sequence on Start(). Leave empty to disable.")]
        [SerializeField] private DialogueSequence autoPlaySequence;
        [Tooltip("Delay before auto-play starts (seconds).")]
        [SerializeField] private float autoPlayDelay = 0f;

        // --- INTERNAL VARIABLES ---
        public static NPCDialogueController ActiveNPC { get; private set; } 
        private Transform _waypointContainer;
        private Coroutine _activeRoutine;
        private Transform _playerHead;
        private bool _isWalking = false;
        private bool _skipRequested = false; // Added for skip functionality

        // Speed Caching
        private float _cachedMoveSpeed = 0f;

        // Line Tracking (for IDialoguePlayback)
        private int _currentLineIndex = -1;
        private bool _isPlaying = false;

        // Interface Properties
        public int CurrentLineIndex => _currentLineIndex;
        public bool IsPlaying => _isPlaying;

        public event System.Action<string> OnSubtitleUpdated;

        private void Awake()
        {
            GameObject container = GameObject.Find("NPC_Waypoints");
            if (container != null) _waypointContainer = container.transform;

            if (Camera.main != null) _playerHead = Camera.main.transform;

            // Initialize cached speed to avoid 0 bugs if we unlock before locking
            if (moveProvider != null)
            {
                _cachedMoveSpeed = moveProvider.moveSpeed;
            }
        }

        private void Start()
        {
            // Search for DDOL subtitle if not manually assigned
            if (subtitleTMP == null)
            {
                StartCoroutine(FindDDOLSubtitleDelayed());
            }

            // Auto-play if sequence is assigned
            if (autoPlaySequence != null)
            {
                if (autoPlayDelay > 0f)
                    StartCoroutine(AutoPlayRoutine());
                else
                    TriggerSequence(autoPlaySequence);
            }
        }

        private IEnumerator FindDDOLSubtitleDelayed()
        {
            // Wait a couple frames for DDOL objects to initialize
            for (int i = 0; i < ddolSearchDelay; i++)
            {
                yield return null;
            }

            int attempts = 0;
            while (subtitleTMP == null && attempts < maxSearchAttempts)
            {
                FindDDOLSubtitle();

                if (subtitleTMP == null)
                {
                    attempts++;
                    yield return new WaitForSeconds(0.1f); // Wait 0.1s between attempts
                }
            }

            if (subtitleTMP == null)
            {
                Debug.LogWarning($"[NPCDialogue] Failed to find DDOL subtitle after {maxSearchAttempts} attempts");
            }
        }

        private void FindDDOLSubtitle()
        {
            GameObject subtitleObject = null;

            // Method 1: Search by tag (only works if active)
            if (!string.IsNullOrEmpty(subtitleTag))
            {
                subtitleObject = GameObject.FindGameObjectWithTag(subtitleTag);
                if (subtitleObject != null)
                {
                    Debug.Log($"[NPCDialogue] Found subtitle via tag '{subtitleTag}': {subtitleObject.name}");
                }
            }

            // Method 2: Search by name (only works if active)
            if (subtitleObject == null && !string.IsNullOrEmpty(subtitleObjectName))
            {
                subtitleObject = GameObject.Find(subtitleObjectName);
                if (subtitleObject != null)
                {
                    Debug.Log($"[NPCDialogue] Found subtitle via name '{subtitleObjectName}': {subtitleObject.name}");
                }
            }

            // Method 3: Search through DDOL parent (works even if child is inactive)
            if (subtitleObject == null && !string.IsNullOrEmpty(ddolParentName))
            {
                GameObject parentObject = GameObject.Find(ddolParentName);
                if (parentObject != null)
                {
                    // Search all children, including inactive ones
                    Transform found = FindChildRecursive(parentObject.transform, subtitleObjectName);
                    if (found != null)
                    {
                        subtitleObject = found.gameObject;
                        Debug.Log($"[NPCDialogue] Found subtitle via parent search in '{ddolParentName}': {subtitleObject.name}");
                    }
                }
            }

            // Method 4: Search ALL objects including inactive (last resort - expensive!)
            if (subtitleObject == null && !string.IsNullOrEmpty(subtitleObjectName))
            {
                TextMeshProUGUI[] allTMPs = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
                foreach (var tmp in allTMPs)
                {
                    if (tmp.gameObject.name == subtitleObjectName)
                    {
                        // Make sure it's not a prefab or asset
                        if (tmp.gameObject.scene.name != null)
                        {
                            subtitleObject = tmp.gameObject;
                            Debug.Log($"[NPCDialogue] Found subtitle via FindObjectsOfTypeAll: {subtitleObject.name}");
                            break;
                        }
                    }
                }
            }

            // Get TextMeshProUGUI component
            if (subtitleObject != null)
            {
                subtitleTMP = subtitleObject.GetComponent<TextMeshProUGUI>();

                if (subtitleTMP == null)
                {
                    // Try to find in children
                    subtitleTMP = subtitleObject.GetComponentInChildren<TextMeshProUGUI>(true); // true = include inactive
                }

                if (subtitleTMP != null)
                {
                    Debug.Log($"[NPCDialogue] Successfully connected to DDOL subtitle: {subtitleTMP.name} (Active: {subtitleTMP.gameObject.activeInHierarchy})");
                    // Don't force it inactive here - respect its current state
                }
                else
                {
                    Debug.LogWarning($"[NPCDialogue] Found GameObject '{subtitleObject.name}' but no TextMeshProUGUI component!");
                }
            }
            else if (!string.IsNullOrEmpty(subtitleObjectName) || !string.IsNullOrEmpty(subtitleTag))
            {
                Debug.LogWarning($"[NPCDialogue] Could not find DDOL subtitle (Tag: '{subtitleTag}', Name: '{subtitleObjectName}', Parent: '{ddolParentName}')");
            }
        }

        // Helper method to search children recursively (including inactive)
        private Transform FindChildRecursive(Transform parent, string childName)
        {
            // Check direct children first
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;
            }

            // Then search grandchildren recursively
            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private IEnumerator AutoPlayRoutine()
        {
            yield return new WaitForSeconds(autoPlayDelay);
            TriggerSequence(autoPlaySequence);
        }

        private void LateUpdate()
        {
            // Do not look at player if currently walking
            if (headBone == null || _playerHead == null || _isWalking) return;

            float dist = Vector3.Distance(transform.position, _playerHead.position);
            if (dist > trackingRange) return;

            // 1. Get Directions
            Vector3 dirToPlayer = (_playerHead.position - headBone.position).normalized;
            Vector3 flatDir = (_playerHead.position - transform.position).normalized;
            flatDir.y = 0;

            // 2. Body Rotation (Keep this slow and smooth)
            float angle = Vector3.Angle(transform.forward, flatDir);
            if (angle > maxHeadAngle)
            {
                Quaternion bodyTarget = Quaternion.LookRotation(flatDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, bodyTarget, Time.deltaTime * bodyTurnSpeed);
            }

            // 3. Head Rotation BLENDING
            if (angle < 100f)
            {
                Quaternion animationRotation = headBone.rotation;
                Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);
                headBone.rotation = Quaternion.Slerp(animationRotation, targetRotation, lookWeight);
            }
        }

        public void Interact()
        {
            TriggerSequence(initialSequence);
        }

        // ADDED: Skip Functionality
        public void SkipCurrentSequence()
        {
            if (_isPlaying) _skipRequested = true;
        }

        public void TriggerSequence(DialogueSequence sequence)
        {
            if (sequence == null) return;
            ActiveNPC = this;
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);

            _skipRequested = false; // Reset skip flag for a fresh interaction

            _activeRoutine = StartCoroutine(PlaySequenceRoutine(sequence));
        }

        public void InterruptWithSequence(DialogueSequence sequence)
        {
            if (sequence == null) return;

            ActiveNPC = this;

            // Stop current routine
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);

            // Stop animations
            if (npcAnimator != null)
            {
                if (!string.IsNullOrEmpty(walkingBool))
                    npcAnimator.SetBool(walkingBool, false);
            }

            // Reset state
            _isWalking = false;
            _currentLineIndex = -1;

            _skipRequested = false; // Reset skip flag for a fresh interruption

            // Start new sequence
            _activeRoutine = StartCoroutine(PlaySequenceRoutine(sequence));
        }

        private void SetSubtitleText(string text, bool active)
        {
            // Lazy initialization - try to find subtitle if still null
            if (subtitleTMP == null && (!string.IsNullOrEmpty(subtitleObjectName) || !string.IsNullOrEmpty(subtitleTag)))
            {
                FindDDOLSubtitle();
            }

            Debug.Log($"[Subtitle Debug] Setting Text to: '{text}' | Active: {active} | Time: {Time.time}");
            if (subtitleTMP != null)
            {
                subtitleTMP.gameObject.SetActive(active);
                if (active)
                {
                    subtitleTMP.text = text;
                    subtitleTMP.ForceMeshUpdate();
                }
            }
            else
            {
                Debug.LogWarning($"[NPCDialogue] Subtitle TMP still null when trying to display: '{text}'");
            }
            OnSubtitleUpdated?.Invoke(text);
        }

        // --- FIXED METHOD: Modifies MoveSpeed instead of disabling components ---
        private void ApplyXRAction(SequenceAction action)
        {
            if (action == SequenceAction.None) return;
            if (moveProvider == null) return;

            // True = Unlock (Restore Speed), False = Lock (Set Speed 0)
            bool shouldUnlock = (action == SequenceAction.UnlockPlayerMovement);

            if (shouldUnlock)
            {
                // Restore the speed we cached earlier
                moveProvider.moveSpeed = _cachedMoveSpeed;
                Debug.Log($"[Dialogue] Player Movement RESTORED to {_cachedMoveSpeed}");
            }
            else
            {
                // Cache the current speed and set to 0
                // We check if speed > 0 to prevent caching "0" if we accidentally lock twice
                if (moveProvider.moveSpeed > 0)
                {
                    _cachedMoveSpeed = moveProvider.moveSpeed;
                }

                moveProvider.moveSpeed = 0f;
                Debug.Log("[Dialogue] Player Movement STOPPED (Speed set to 0)");
            }
        }

        // --- UPDATED METHOD: Supports Skipping ---
        private IEnumerator PlaySequenceRoutine(DialogueSequence sequence)
        {
            if (sequence == null) yield break;

            _isPlaying = true;
            _currentLineIndex = 0;

            ApplyXRAction(sequence.onStartAction);

            // Start Movement
            if (sequence.moveToPosition)
            {
                if (sequence.talkWhileWalking)
                    StartCoroutine(MoveToRoutine(sequence.waypointName, sequence.moveSpeed));
                else
                    yield return StartCoroutine(MoveToRoutine(sequence.waypointName, sequence.moveSpeed));
            }

            // Start sequence-wide talking animation
            if (!string.IsNullOrEmpty(sequence.talkingBool))
                npcAnimator.SetBool(sequence.talkingBool, true);

            for (int i = 0; i < sequence.lines.Count; i++)
            {
                if (_skipRequested) break; // Exit the loop instantly if skipped

                _currentLineIndex = i;
                var line = sequence.lines[i];

                SetSubtitleText(line.textContent, true);

                if (speechSource != null && line.voiceClip != null)
                {
                    speechSource.Stop();
                    speechSource.PlayOneShot(line.voiceClip);
                }

                // Line Triggers (Gestures)
                if (!string.IsNullOrEmpty(line.animationTrigger))
                {
                    if (!string.IsNullOrEmpty(sequence.talkingBool))
                        npcAnimator.SetBool(sequence.talkingBool, false);

                    yield return StartCoroutine(LineTriggerRoutine(line.animationTrigger, line.triggerCount));

                    if (!string.IsNullOrEmpty(sequence.talkingBool))
                        npcAnimator.SetBool(sequence.talkingBool, true);
                }

                // Wait for dynamic duration with skip check
                yield return StartCoroutine(WaitSkippable(line.GetTotalDuration()));

                SetSubtitleText("", false);

                // Delay between lines with skip check
                yield return StartCoroutine(WaitSkippable(delayBetweenLines));
            }

            // Clean up if a skip occurred
            if (_skipRequested)
            {
                if (speechSource != null) speechSource.Stop();
                SetSubtitleText("", false);
            }

            // Stop speaking loop
            if (!string.IsNullOrEmpty(sequence.talkingBool))
                npcAnimator.SetBool(sequence.talkingBool, false);

            // SYNC FIX: Wait for physical movement to finish before closing the routine
            while (_isWalking)
            {
                yield return null;
            }

            if (sequence.advancesStory) CurrentStoryStage++;
            ApplyXRAction(sequence.onEndAction);

            if (sequence.nextSequence != null)
            {
                yield return StartCoroutine(PlaySequenceRoutine(sequence.nextSequence));
            }
            else
            {
                SetSubtitleText("", false);
                _isPlaying = false;
                _currentLineIndex = -1;
                _activeRoutine = null;
                _skipRequested = false; // Reset flag when the entire chain is fully complete
            }
        }

        // --- UPDATED METHOD: Supports Skipping ---
        private IEnumerator LineTriggerRoutine(string trigger, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_skipRequested) break; // Halt animation triggers if skipped
                npcAnimator.SetTrigger(trigger);
                yield return StartCoroutine(WaitSkippable(0.5f));
            }
        }

        // --- ADDED: Custom Skippable Wait Coroutine ---
        private IEnumerator WaitSkippable(float duration)
        {
            float timer = 0f;
            while (timer < duration)
            {
                if (_skipRequested) break;
                timer += Time.deltaTime;
                yield return null;
            }
        }

        // --- ORIGINAL METHOD PRESERVED ---
        private IEnumerator MoveToRoutine(string waypointName, float speed)
        {
            if (_waypointContainer == null) yield break;
            Transform target = _waypointContainer.Find(waypointName);
            if (target == null) yield break;

            CharacterController characterController = GetComponent<CharacterController>();
            _isWalking = true;
            npcAnimator.SetBool(walkingBool, true);

            // Precision distance check
            while (Vector3.Distance(transform.position, target.position) > 0.15f)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                direction.y = 0;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
                }

                if (characterController != null && characterController.enabled)
                    characterController.Move(direction * speed * Time.deltaTime);
                else
                    transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

                yield return null;
            }

            // Snap to final and wait a tiny buffer for animator to catch up
            transform.position = new Vector3(target.position.x, transform.position.y, target.position.z);
            yield return new WaitForSeconds(0.1f);

            npcAnimator.SetBool(walkingBool, false);
            _isWalking = false;
        }
    }
}