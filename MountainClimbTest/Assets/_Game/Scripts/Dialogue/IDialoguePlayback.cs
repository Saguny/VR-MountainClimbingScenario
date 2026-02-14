using UnityEngine;

namespace MountainRescue.Dialogue
{
    /// <summary>
    /// Unified interface for all dialogue playback systems.
    /// Allows triggers, UI, and other systems to work with any dialogue controller.
    /// </summary>
    public interface IDialoguePlayback
    {
        /// <summary>
        /// Currently playing line index (0-based). -1 if not playing.
        /// </summary>
        int CurrentLineIndex { get; }

        /// <summary>
        /// Is a dialogue sequence currently playing?
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Starts a dialogue sequence.
        /// </summary>
        void TriggerSequence(DialogueSequence sequence);

        /// <summary>
        /// Interrupts current sequence and starts a new one.
        /// </summary>
        void InterruptWithSequence(DialogueSequence sequence);

        /// <summary>
        /// Event fired when subtitle text changes.
        /// </summary>
        event System.Action<string> OnSubtitleUpdated;
    }
}