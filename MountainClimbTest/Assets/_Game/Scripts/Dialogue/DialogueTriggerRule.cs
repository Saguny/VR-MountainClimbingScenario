using UnityEngine;

namespace MountainRescue.Dialogue
{
    public enum TriggerCondition
    {
        Always,                    // Always trigger
        LineIndexRange,            // Trigger if current line is within range
        LineIndexBelow,            // Trigger if current line < threshold
        LineIndexAbove,            // Trigger if current line >= threshold
        StoryStage,                // Trigger if specific story stage
        NotPlaying,                // Trigger only if nothing is playing
        IsPlaying                  // Trigger only if something is playing
    }

    [CreateAssetMenu(fileName = "New Trigger Rule", menuName = "RescueSim/Dialogue/Trigger Rule")]
    public class DialogueTriggerRule : ScriptableObject
    {
        [Header("Condition")]
        public TriggerCondition condition = TriggerCondition.Always;

        [Header("Line Index Settings")]
        [Tooltip("For LineIndexBelow/Above: threshold value")]
        public int lineThreshold = 0;

        [Tooltip("For LineIndexRange: min line index (inclusive)")]
        public int minLineIndex = 0;

        [Tooltip("For LineIndexRange: max line index (inclusive)")]
        public int maxLineIndex = 999;

        [Header("Story Stage Settings")]
        [Tooltip("For StoryStage condition")]
        public int requiredStoryStage = 0;

        [Header("Sequence to Play")]
        public DialogueSequence sequenceToTrigger;

        /// <summary>
        /// Evaluates if this rule should trigger based on the dialogue controller state.
        /// </summary>
        public bool EvaluateCondition(IDialoguePlayback controller, int controllerStoryStage)
        {
            switch (condition)
            {
                case TriggerCondition.Always:
                    return true;

                case TriggerCondition.LineIndexBelow:
                    return controller.CurrentLineIndex < lineThreshold;

                case TriggerCondition.LineIndexAbove:
                    return controller.CurrentLineIndex >= lineThreshold;

                case TriggerCondition.LineIndexRange:
                    return controller.CurrentLineIndex >= minLineIndex &&
                           controller.CurrentLineIndex <= maxLineIndex;

                case TriggerCondition.StoryStage:
                    return controllerStoryStage == requiredStoryStage;

                case TriggerCondition.NotPlaying:
                    return !controller.IsPlaying;

                case TriggerCondition.IsPlaying:
                    return controller.IsPlaying;

                default:
                    return false;
            }
        }
    }
}