using UnityEngine;

namespace MountainRescue.Dialogue
{
    [CreateAssetMenu(fileName = "New Line", menuName = "RescueSim/Dialogue/Line")]
    public class DialogueLine : ScriptableObject
    {
        [TextArea(3, 10)]
        public string textContent;

        [Tooltip("Optional: Drag a voiceover clip here.")]
        public AudioClip voiceClip;

        [Tooltip("How long this specific line stays on screen.")]
        public float displayDuration = 4f;
    }
}