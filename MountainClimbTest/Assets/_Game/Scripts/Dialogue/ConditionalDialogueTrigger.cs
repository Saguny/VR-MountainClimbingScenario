using UnityEngine;
using MountainRescue.Dialogue;
using System.Collections.Generic;

/// <summary>
/// Advanced dialogue trigger that evaluates multiple rules to determine which sequence to play.
/// Perfect for dynamic scenarios like victim rescue where different dialogues play based on timing.
/// </summary>
public class ConditionalDialogueTrigger : MonoBehaviour
{
    [Header("Target Controller")]
    [Tooltip("The dialogue controller to interact with (can be NPC, Victim, Radio, etc.)")]
    [SerializeField] private MonoBehaviour targetController;

    [Header("Trigger Rules")]
    [Tooltip("Rules are evaluated in order. First matching rule wins.")]
    [SerializeField] private List<DialogueTriggerRule> rules = new List<DialogueTriggerRule>();

    [Header("Fallback")]
    [Tooltip("Play this if no rules match (optional)")]
    [SerializeField] private DialogueSequence fallbackSequence;

    [Header("Settings")]
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool interruptCurrentDialogue = true;
    [SerializeField] private bool requirePlayerTag = true;

    private bool _hasTriggered = false;
    private IDialoguePlayback _playbackInterface;

    private void Awake()
    {
        // Get the IDialoguePlayback interface from target controller
        if (targetController != null)
        {
            _playbackInterface = targetController as IDialoguePlayback;

            if (_playbackInterface == null)
            {
                Debug.LogError($"[ConditionalDialogueTrigger] Target controller {targetController.name} does not implement IDialoguePlayback!", this);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Player tag check
        if (requirePlayerTag && !other.CompareTag("Player")) return;

        // Once-only check
        if (_hasTriggered && triggerOnlyOnce) return;

        // Interface check
        if (_playbackInterface == null) return;

        // Get story stage if controller supports it
        int storyStage = 0;
        if (targetController is NPCDialogueController npcController)
        {
            storyStage = npcController.CurrentStoryStage;
        }

        // Evaluate rules in order
        DialogueSequence sequenceToPlay = EvaluateRules(storyStage);

        if (sequenceToPlay != null)
        {
            _hasTriggered = true;

            if (interruptCurrentDialogue && _playbackInterface.IsPlaying)
            {
                _playbackInterface.InterruptWithSequence(sequenceToPlay);
                Debug.Log($"[Trigger] Interrupted dialogue with: {sequenceToPlay.name}");
            }
            else
            {
                _playbackInterface.TriggerSequence(sequenceToPlay);
                Debug.Log($"[Trigger] Started dialogue: {sequenceToPlay.name}");
            }
        }
        else
        {
            Debug.Log($"[Trigger] No matching rules for current state (Line: {_playbackInterface.CurrentLineIndex}, Playing: {_playbackInterface.IsPlaying})");
        }
    }

    /// <summary>
    /// Evaluates all rules and returns the first matching sequence.
    /// </summary>
    private DialogueSequence EvaluateRules(int storyStage)
    {
        foreach (var rule in rules)
        {
            if (rule == null) continue;

            bool matches = rule.EvaluateCondition(_playbackInterface, storyStage);

            if (matches)
            {
                Debug.Log($"[Trigger] Rule matched: {rule.name} (Condition: {rule.condition})");
                return rule.sequenceToTrigger;
            }
        }

        // No rules matched, use fallback
        if (fallbackSequence != null)
        {
            Debug.Log($"[Trigger] Using fallback sequence: {fallbackSequence.name}");
        }

        return fallbackSequence;
    }

    // Reset trigger state (useful for testing)
    public void ResetTrigger()
    {
        _hasTriggered = false;
    }

    // Editor helper
    private void OnDrawGizmos()
    {
        Gizmos.color = _hasTriggered ? Color.gray : Color.cyan;

        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.DrawWireCube(transform.position, collider.bounds.size);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Show rule count
        Gizmos.color = Color.cyan;

        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.DrawWireCube(transform.position, collider.bounds.size);
        }
    }
}