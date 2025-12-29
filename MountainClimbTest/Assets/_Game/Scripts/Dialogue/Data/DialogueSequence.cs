using System.Collections.Generic;
using UnityEngine;

namespace MountainRescue.Dialogue
{
    [CreateAssetMenu(fileName = "New Sequence", menuName = "RescueSim/Dialogue/Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        public List<DialogueLine> lines = new List<DialogueLine>();
    }
}