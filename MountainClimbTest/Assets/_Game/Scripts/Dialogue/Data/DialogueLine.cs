using UnityEngine;

namespace MountainRescue.Dialogue
{
    [CreateAssetMenu(fileName = "New Dialogue Line", menuName = "RescueSim/Dialogue/Line")]
    public class DialogueLine : ScriptableObject
    {
        [TextArea(3, 10)]
        public string textContent;
        public AudioClip voiceClip;

        [Tooltip("Extra time to wait AFTER the audio or display duration finishes.")]
        public float extraDelay = 0.5f;

        [Header("Animation Sync")]
        [Tooltip("Trigger name for a specific gesture (e.g., _point, _shrug).")]
        public string animationTrigger;

        [Tooltip("How many times to fire the trigger during this line.")]
        public int triggerCount = 1;

        public float GetTotalDuration()
        {
            float baseDuration = (voiceClip != null) ? voiceClip.length : 3f;
            return baseDuration + extraDelay;
        }
    }
}