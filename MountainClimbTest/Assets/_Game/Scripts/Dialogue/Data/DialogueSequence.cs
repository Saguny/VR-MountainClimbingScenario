using UnityEngine;
using System.Collections.Generic;

namespace MountainRescue.Dialogue
{
    [CreateAssetMenu(fileName = "New Sequence", menuName = "RescueSim/Dialogue/Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        public List<DialogueLine> lines;

        [Header("Movement Settings")]
        public bool moveToPosition;
        public string waypointName;
        public float moveSpeed = 2f;
        public bool talkWhileWalking = false;

        [Header("Story Progression")]
        [Tooltip("If true, the Story Stage counter will go up by 1 when this sequence finishes.")]
        public bool advancesStory = true; // <--- NEW

        [Header("Flow Control")]
        public DialogueSequence nextSequence;
    }
}