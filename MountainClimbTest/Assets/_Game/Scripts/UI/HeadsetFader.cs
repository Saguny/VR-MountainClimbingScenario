using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace MountainRescue.UI
{
    public class HeadsetFader : MonoBehaviour
    {
        [SerializeField] private Image fadeImage;

        [Header("Fader Settings")]
        [Tooltip("Default speed (seconds) for normal transitions.")]
        [SerializeField] private float defaultFadeTime = 1.5f;

        private void Awake()
        {
            if (fadeImage == null) fadeImage = GetComponentInChildren<Image>();
            SetAlpha(0f);
        }

        // --- NEW METHOD: Instant Blackout ---
        public void SnapToBlack()
        {
            StopAllCoroutines(); // Cancel any existing fades
            SetAlpha(1.0f);      // Instant black
        }
        // ------------------------------------

        public IEnumerator FadeOut(float duration) => FadeTo(1f, duration);
        public IEnumerator FadeIn(float duration) => FadeTo(0f, duration);

        // Default overloads
        public IEnumerator FadeOut() => FadeTo(1f, defaultFadeTime);
        public IEnumerator FadeIn() => FadeTo(0f, defaultFadeTime);

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float startAlpha = fadeImage.color.a;
            float time = 0f;

            // Safety check to prevent divide by zero
            if (duration <= 0f)
            {
                SetAlpha(targetAlpha);
                yield break;
            }

            while (time < duration)
            {
                time += Time.deltaTime;
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, time / duration));
                yield return null;
            }
            SetAlpha(targetAlpha);
        }

        private void SetAlpha(float alpha)
        {
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = alpha;
                fadeImage.color = c;
            }
        }
    }
}