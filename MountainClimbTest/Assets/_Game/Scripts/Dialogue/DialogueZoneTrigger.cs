using UnityEngine;
using MountainRescue.Dialogue;

public class DialogueZoneTrigger : MonoBehaviour
{
    [SerializeField] private NPCDialogueController npcController;
    [SerializeField] private DialogueSequence sequenceToTrigger;

    [Header("Gating")]
    [Tooltip("Which Story Stage must be active for this to work? (0=Start, 1=After First Sequence, etc)")]
    [SerializeField] private int requiredStoryStage = 0;

    [SerializeField] private bool triggerOnlyOnce = true;

    private bool _hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && (!_hasTriggered || !triggerOnlyOnce))
        {
            // 1. Check if the NPC is busy (Optional, prevents interrupting)
            // if (npcController.IsSpeaking) return; 

            // 2. THE FIX: Check if we are at the right stage
            if (npcController.CurrentStoryStage != requiredStoryStage)
            {
                // Debug log to help you test
                // Debug.Log($"[Zone Ignored] Entered {gameObject.name} but required Stage {requiredStoryStage} (Current: {npcController.CurrentStoryStage})");
                return;
            }

            // 3. Fire!
            _hasTriggered = true;
            npcController.TriggerSequence(sequenceToTrigger);
        }
    }
}