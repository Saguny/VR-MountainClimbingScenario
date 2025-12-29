using UnityEngine;
using TMPro;
using System.Collections;

namespace MountainRescue.UI.Views
{
    [RequireComponent(typeof(CanvasGroup))]
    public class SpatialSubtitleView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MountainRescue.Dialogue.NPCDialogueController linkedNPC;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Settings")]
        [SerializeField] private float fadeSpeed = 5f;
        [Tooltip("Time in seconds between each letter appearing.")]
        [SerializeField] private float typingSpeed = 0.04f;

        [Tooltip("If text appears backwards, check this box.")]
        [SerializeField] private bool flipDirection = false;

        private CanvasGroup _canvasGroup;
        private Transform _mainCamera;
        private Coroutine _typewriterRoutine;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            if (Camera.main != null) _mainCamera = Camera.main.transform;
        }

        private void OnEnable()
        {
            if (linkedNPC != null) linkedNPC.OnSubtitleUpdated += UpdateText;
        }

        private void OnDisable()
        {
            if (linkedNPC != null) linkedNPC.OnSubtitleUpdated -= UpdateText;
        }

        private void LateUpdate()
        {
            if (_mainCamera == null) return;

            // 1. Billboard Logic (Keep facing the player)
            Vector3 directionToCamera = _mainCamera.position - transform.position;
            directionToCamera.y = 0; // Lock vertical tilt

            Vector3 lookDir = flipDirection ? directionToCamera : -directionToCamera;

            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        private void UpdateText(string content)
        {
            // Stop any active fading or typing
            StopAllCoroutines();

            if (string.IsNullOrEmpty(content))
            {
                // If text is empty, just fade out
                StartCoroutine(FadeTo(0f));
            }
            else
            {
                // 1. Prepare for Typewriter
                subtitleText.text = content;
                subtitleText.maxVisibleCharacters = 0; // Hide all text initially

                // 2. Start Fading In AND Typing
                StartCoroutine(FadeTo(1f));
                _typewriterRoutine = StartCoroutine(TypewriterLogic(content.Length));
            }
        }

        private IEnumerator TypewriterLogic(int totalChars)
        {
            // Small delay before typing starts so the box has time to fade in a bit
            yield return new WaitForSeconds(0.1f);

            int currentVisible = 0;

            while (currentVisible < totalChars)
            {
                currentVisible++;
                subtitleText.maxVisibleCharacters = currentVisible;

                // Optional: Play a tiny "blip" sound here if you want audio typing

                yield return new WaitForSeconds(typingSpeed);
            }
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            while (!Mathf.Approximately(_canvasGroup.alpha, targetAlpha))
            {
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
                yield return null;
            }
        }
    }
}