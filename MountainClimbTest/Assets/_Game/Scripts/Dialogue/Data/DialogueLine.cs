using UnityEngine;

namespace MountainRescue.Dialogue
{
    [CreateAssetMenu(fileName = "New Dialogue Line", menuName = "RescueSim/Dialogue/Line")]
    public class DialogueLine : ScriptableObject
    {
        public string textContent;
        public AudioClip voiceClip;
        public float displayDuration = 3f;

        [Header("Animation Sync")]
        [Tooltip("Trigger name: _jumping, _speaking, _speakingWorried")]
        public string animationTrigger;
    }
}