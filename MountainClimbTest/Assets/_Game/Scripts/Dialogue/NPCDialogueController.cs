using UnityEngine;
using System.Collections;

namespace MountainRescue.Dialogue
{
    public class NPCDialogueController : MonoBehaviour
    {
        [Header("Dialogue Content")]
        [SerializeField] private DialogueSequence introSequence;
        [SerializeField] private DialogueSequence reminderSequence;

        [Header("NPC Components")]
        [SerializeField] private AudioSource speechSource;
        [SerializeField] private Animator npcAnimator;
        [SerializeField] private string animatorTalkingParam = "IsTalking";

        [Header("Settings")]
        [SerializeField] private float delayBetweenLines = 0.5f;

        // State Tracking
        private bool _hasCompletedIntro = false;
        private Coroutine _activeRoutine;

        // Events for the UI
        public delegate void DialogueEvent(string text);
        public event DialogueEvent OnSubtitleUpdated;

        public void Interact()
        {
            // Prevent spamming while already talking
            if (_activeRoutine != null) return;

            if (!_hasCompletedIntro)
            {
                _activeRoutine = StartCoroutine(PlaySequence(introSequence, true));
            }
            else
            {
                _activeRoutine = StartCoroutine(PlaySequence(reminderSequence, false));
            }
        }

        private IEnumerator PlaySequence(DialogueSequence sequence, bool isIntro)
        {
            if (sequence == null || sequence.lines.Count == 0) yield break;

            // 1. Start Animation (Enter Talking State)
            if (npcAnimator != null)
                npcAnimator.SetBool(animatorTalkingParam, true);

            foreach (var line in sequence.lines)
            {
                // 2. Update Visuals
                OnSubtitleUpdated?.Invoke(line.textContent);

                // 3. Play Audio
                if (speechSource != null && line.voiceClip != null)
                {
                    speechSource.Stop();
                    speechSource.PlayOneShot(line.voiceClip);
                }

                // 4. Wait for the line duration
                yield return new WaitForSeconds(line.displayDuration);

                // 5. Clear Text & Wait Delay
                OnSubtitleUpdated?.Invoke(""); // Clear
                yield return new WaitForSeconds(delayBetweenLines); // 0.5s Pause
            }

            // 6. Stop Animation (Return to Idle)
            if (npcAnimator != null)
                npcAnimator.SetBool(animatorTalkingParam, false);

            // Mark intro as done
            if (isIntro)
            {
                _hasCompletedIntro = true;
            }

            _activeRoutine = null;
        }
    }
}