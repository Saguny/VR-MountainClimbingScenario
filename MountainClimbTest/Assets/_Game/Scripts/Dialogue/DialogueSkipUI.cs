using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using MountainRescue.Dialogue;

namespace MountainRescue.UI
{
    public class DialogueSkipUI : MonoBehaviour
    {
        [Header("Input Setup")]
        [Tooltip("Assign the Left Stick Press (L3) action here")]
        [SerializeField] private InputActionReference skipAction;

        [Header("UI Elements")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image progressLine;

        [Header("Settings")]
        [Tooltip("How long L3 must be held to skip (in seconds)")]
        [SerializeField] private float requiredHoldTime = 1.0f;
        [Tooltip("How fast the bar empties if you let go early")]
        [SerializeField] private float decayMultiplier = 2.0f;

        private float _currentHoldTime = 0f;
        private bool _hasSkipped = false;

        private void Update()
        {
            // Dynamically grab whichever NPC is currently speaking
            NPCDialogueController currentDialogue = NPCDialogueController.ActiveNPC;

            // Hide UI completely if no dialogue is playing, or if there is no active NPC
            if (currentDialogue == null || !currentDialogue.IsPlaying)
            {
                canvasGroup.alpha = 0f;
                _currentHoldTime = 0f;
                progressLine.fillAmount = 0f;
                _hasSkipped = false; // Reset the flag for the next conversation
                return;
            }

            // If we already triggered the skip for this conversation, keep the UI hidden
            if (_hasSkipped)
            {
                canvasGroup.alpha = 0f;
                return;
            }

            // Show UI
            canvasGroup.alpha = 1f;

            // Check if L3 is being held down
            if (skipAction != null && skipAction.action.IsPressed())
            {
                _currentHoldTime += Time.deltaTime;
                progressLine.fillAmount = _currentHoldTime / requiredHoldTime;

                // Trigger the skip
                if (_currentHoldTime >= requiredHoldTime)
                {
                    currentDialogue.SkipCurrentSequence();
                    _currentHoldTime = 0f;
                    progressLine.fillAmount = 0f;
                    _hasSkipped = true; // Hide the UI until the entire dialogue finishes
                }
            }
            else
            {
                // Smoothly decay the bar if the player lets go
                _currentHoldTime = Mathf.Max(0, _currentHoldTime - Time.deltaTime * decayMultiplier);
                progressLine.fillAmount = _currentHoldTime / requiredHoldTime;
            }
        }
    }
}