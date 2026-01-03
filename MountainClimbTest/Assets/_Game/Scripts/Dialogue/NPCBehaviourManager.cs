using UnityEngine;
using System.Collections;
using MountainRescue.Dialogue;

public class NPCBehaviourManager : MonoBehaviour
{
    [SerializeField] private NPCDialogueController controller;
    [SerializeField] private DialogueSequence shockedSequence;

    // Call this from your "Exit Box Collider"
    // Inside NPCBehaviourManager.cs
    public void PlayerLeavingTutorial()
    {
        controller.StopAllCoroutines();
        // Change .PlaySequence to .TriggerSequence
        controller.TriggerSequence(shockedSequence);
    }
}