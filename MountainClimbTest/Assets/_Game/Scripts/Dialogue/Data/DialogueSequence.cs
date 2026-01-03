using UnityEngine;
using System.Collections.Generic;

namespace MountainRescue.Dialogue
{
    public enum SequenceAction { None, LockPlayerMovement, UnlockPlayerMovement }

    [CreateAssetMenu(fileName = "New Sequence", menuName = "RescueSim/Dialogue/Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        public List<DialogueLine> lines;

        [Header("Sequence Animations")]
        [Tooltip("The boolean to set to true while the NPC is talking (e.g., isTalking).")]
        public string talkingBool;

        [Header("Movement Settings")]
        public bool moveToPosition;
        public string waypointName;
        public float moveSpeed = 2f;
        public bool talkWhileWalking = false;

        [Header("XR Interactions")]
        public SequenceAction onStartAction = SequenceAction.None;
        public SequenceAction onEndAction = SequenceAction.None;

        [Header("Story Progression")]
        public bool advancesStory = true;

        [Header("Flow Control")]
        public DialogueSequence nextSequence;
    }
}