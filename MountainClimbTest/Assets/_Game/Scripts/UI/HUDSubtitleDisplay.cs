using UnityEngine;
using TMPro;
using System.Collections;
using MountainRescue.Dialogue;

namespace Game.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class HUDSubtitleDisplay : MonoBehaviour
    {
        [SerializeField] private NPCDialogueController linkedNPC;
        [SerializeField] private TextMeshProUGUI textComponent;
        [SerializeField] private float typingSpeed = 0.04f;
        [SerializeField] private float fadeSpeed = 5f;

        private CanvasGroup _canvasGroup;
        private Coroutine _displayRoutine;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            if (textComponent != null) textComponent.text = "";
        }

        private void OnEnable()
        {
            if (linkedNPC != null) linkedNPC.OnSubtitleUpdated += OnSubtitleReceived;
        }

        private void OnDisable()
        {
            if (linkedNPC != null) linkedNPC.OnSubtitleUpdated -= OnSubtitleReceived;
        }

        private void OnSubtitleReceived(string content)
        {
            StopAllCoroutines();

            if (string.IsNullOrEmpty(content))
            {
                StartCoroutine(FadeCanvas(0f));
            }
            else
            {
                _displayRoutine = StartCoroutine(TypewriterRoutine(content));
            }
        }

        private IEnumerator TypewriterRoutine(string content)
        {
            textComponent.text = "";
            textComponent.maxVisibleCharacters = 0;
            textComponent.text = content;

            StartCoroutine(FadeCanvas(1f));

            int totalChars = content.Length;
            int currentVisible = 0;

            while (currentVisible < totalChars)
            {
                currentVisible++;
                textComponent.maxVisibleCharacters = currentVisible;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        private IEnumerator FadeCanvas(float targetAlpha)
        {
            while (!Mathf.Approximately(_canvasGroup.alpha, targetAlpha))
            {
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
                yield return null;
            }
            _canvasGroup.alpha = targetAlpha;
        }
    }
}