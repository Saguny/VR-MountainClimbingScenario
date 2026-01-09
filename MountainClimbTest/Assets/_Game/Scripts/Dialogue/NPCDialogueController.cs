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
    public class NPCDialogueController : MonoBehaviour
    {
        [Header("Story Status")]
        [Tooltip("Increments automatically when a sequence with 'advancesStory' finishes.")]
        public int CurrentStoryStage = 0;

        [Header("Manual UI Setup")]
        [SerializeField] private TextMeshProUGUI subtitleTMP;

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

        // --- INTERNAL VARIABLES ---
        private Transform _waypointContainer;
        private Coroutine _activeRoutine;
        private Transform _playerHead;
        private bool _isWalking = false;

        // Speed Caching
        private float _cachedMoveSpeed = 0f;

        public delegate void DialogueEvent(string text);
        public event DialogueEvent OnSubtitleUpdated;

        private void Awake()
        {
            GameObject container = GameObject.Find("NPC_Waypoints");
            if (container != null) _waypointContainer = container.transform;

            // UI starts disabled
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

            if (Camera.main != null) _playerHead = Camera.main.transform;

            // Initialize cached speed to avoid 0 bugs if we unlock before locking
            if (moveProvider != null)
            {
                _cachedMoveSpeed = moveProvider.moveSpeed;
            }
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

        public void TriggerSequence(DialogueSequence sequence)
        {
            if (sequence == null) return;
            if (_activeRoutine != null) StopCoroutine(_activeRoutine);
            _activeRoutine = StartCoroutine(PlaySequenceRoutine(sequence));
        }

        private void SetSubtitleText(string text, bool active)
        {
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

        // --- ORIGINAL METHOD PRESERVED ---
        private IEnumerator PlaySequenceRoutine(DialogueSequence sequence)
        {
            if (sequence == null) yield break;

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

            foreach (var line in sequence.lines)
            {
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

                // Wait for dynamic duration (Audio + Extra Delay)
                yield return new WaitForSeconds(line.GetTotalDuration());

                SetSubtitleText("", false);
                yield return new WaitForSeconds(delayBetweenLines);
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
                yield return StartCoroutine(PlaySequenceRoutine(sequence.nextSequence));
            else
            {
                SetSubtitleText("", false);
                _activeRoutine = null;
            }
        }

        // --- ORIGINAL METHOD PRESERVED ---
        private IEnumerator LineTriggerRoutine(string trigger, int count)
        {
            for (int i = 0; i < count; i++)
            {
                npcAnimator.SetTrigger(trigger);
                yield return new WaitForSeconds(0.5f);
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