using UnityEngine;
using System.Collections;
using TMPro;

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

        [Header("Manual Head Tracking (Non-Humanoid)")]
        [Tooltip("Drag the specific bone for the Head here.")]
        [SerializeField] private Transform headBone;
        [SerializeField] private float lookSpeed = 5f;
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

        private Transform _waypointContainer;
        private Coroutine _activeRoutine;
        private Transform _playerHead;
        private bool _isWalking = false; // Internal flag to coordinate tracking

        public delegate void DialogueEvent(string text);
        public event DialogueEvent OnSubtitleUpdated;

        private void Awake()
        {
            GameObject container = GameObject.Find("NPC_Waypoints");
            if (container != null) _waypointContainer = container.transform;
            if (subtitleTMP != null) subtitleTMP.text = "";

            if (Camera.main != null) _playerHead = Camera.main.transform;
        }

        // --- TRACKING LOGIC (LateUpdate runs after Animation) ---
        private void LateUpdate()
        {
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

            // 3. Head Rotation BLENDING (The Magic Fix)
            if (angle < 100f)
            {
                // A. Where the ANIMATION wants the head to be right now (The "Whip")
                Quaternion animationRotation = headBone.rotation;

                // B. Where WE want the head to be (The Player)
                Quaternion targetRotation = Quaternion.LookRotation(dirToPlayer);

                // C. Blend them! 
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

        private IEnumerator PlaySequenceRoutine(DialogueSequence sequence)
        {
            if (sequence == null)
            {
                Debug.LogError($"[NPC Controller] Triggered with NULL sequence on {gameObject.name}");
                yield break;
            }

            Debug.Log($"[NPC Controller] Starting Sequence: {sequence.name}");

            // 1. Handle Movement
            if (sequence.moveToPosition)
            {
                if (sequence.talkWhileWalking)
                    StartCoroutine(MoveToRoutine(sequence.waypointName, sequence.moveSpeed));
                else
                    yield return StartCoroutine(MoveToRoutine(sequence.waypointName, sequence.moveSpeed));
            }

            // 2. Play Dialogue Lines
            if (sequence.lines == null || sequence.lines.Count == 0)
                Debug.LogWarning($"[NPC Controller] Sequence {sequence.name} has NO dialogue lines!");

            foreach (var line in sequence.lines)
            {
                // Animation
                if (npcAnimator != null && !string.IsNullOrEmpty(line.animationTrigger))
                    npcAnimator.SetTrigger(line.animationTrigger);

                // UI Event
                OnSubtitleUpdated?.Invoke(line.textContent);

                // Audio
                if (speechSource != null && line.voiceClip != null)
                {
                    speechSource.Stop();
                    speechSource.PlayOneShot(line.voiceClip);
                }

                yield return new WaitForSeconds(line.displayDuration);
                OnSubtitleUpdated?.Invoke("");
                yield return new WaitForSeconds(delayBetweenLines);
            }

            Debug.Log($"[NPC Controller] Sequence {sequence.name} Finished.");

            // 3. Advance Story Stage (NEW LOGIC)
            if (sequence.advancesStory)
            {
                CurrentStoryStage++;
                Debug.Log($"[Story] Stage Advanced to: {CurrentStoryStage}");
            }

            // 4. Chaining
            if (sequence.nextSequence != null)
            {
                yield return StartCoroutine(PlaySequenceRoutine(sequence.nextSequence));
            }
            else
            {
                _activeRoutine = null;
            }
        }

        private IEnumerator MoveToRoutine(string waypointName, float speed)
        {
            if (_waypointContainer == null) yield break;
            Transform target = _waypointContainer.Find(waypointName);

            if (target == null)
            {
                Debug.LogError($"[NPC] Could not find waypoint: {waypointName}");
                yield break;
            }

            CharacterController characterController = GetComponent<CharacterController>();

            // Enable Walking State (Disables Head Tracking)
            _isWalking = true;
            npcAnimator.SetBool(walkingBool, true);

            float timeOut = 10f;
            float timer = 0f;

            while (Vector3.Distance(transform.position, target.position) > 0.3f)
            {
                timer += Time.deltaTime;
                if (timer > timeOut) break;

                // --- SMOOTH ROTATION FIX ---
                Vector3 direction = (target.position - transform.position).normalized;
                direction.y = 0; // Keep it flat

                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    // Smoothly rotate body towards waypoint
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
                }
                // ---------------------------

                // MOVEMENT
                if (characterController != null && characterController.enabled)
                {
                    Vector3 moveDir = (target.position - transform.position).normalized;
                    characterController.Move(moveDir * speed * Time.deltaTime);
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
                }

                yield return null;
            }

            // Disable Walking State (Re-enables Head Tracking)
            npcAnimator.SetBool(walkingBool, false);
            _isWalking = false;
        }
    }
}