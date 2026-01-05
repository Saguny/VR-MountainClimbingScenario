using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MountainRescue.UI
{
    public class HeadsetFader : MonoBehaviour
    {
        [SerializeField] private Image fadeImage;
        [SerializeField] private float defaultFadeTime = 1.5f;

        private void Awake()
        {
            if (fadeImage != null)
            {
                fadeImage.raycastTarget = false;
                // Ensure we start clear if not specified otherwise
                if (fadeImage.color.a == 0)
                    fadeImage.color = new Color(0, 0, 0, 0);
            }
        }

        // --- STANDARD METHODS (Defaults to Black) ---

        public void SnapToBlack()
        {
            if (fadeImage) fadeImage.color = Color.black;
        }

        public void SnapToClear()
        {
            if (fadeImage) fadeImage.color = new Color(0, 0, 0, 0);
        }

        public IEnumerator FadeOut(float duration = -1)
        {
            // Default "Fade Out" means "Fade to Black"
            yield return FadeToColor(Color.black, duration);
        }

        public IEnumerator FadeIn(float duration = -1)
        {
            // Default "Fade In" means "Fade to Clear" from whatever color we are currently at
            float time = duration > 0 ? duration : defaultFadeTime;

            if (fadeImage != null)
            {
                Color startColor = fadeImage.color;
                Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
                yield return FadeToTarget(targetColor, time);
            }
        }

        // --- COLOR OVERRIDES (For Flashbangs/Damage) ---

        public void SetColor(Color color)
        {
            if (fadeImage) fadeImage.color = color;
        }

        public void SnapToColor(Color color)
        {
            if (fadeImage) fadeImage.color = color;
        }

        public IEnumerator FadeToColor(Color targetColor, float duration = -1)
        {
            float time = duration > 0 ? duration : defaultFadeTime;
            yield return FadeToTarget(targetColor, time);
        }

        // --- INTERNAL LOGIC ---

        private IEnumerator FadeToTarget(Color targetColor, float duration)
        {
            if (fadeImage == null) yield break;

            Color startColor = fadeImage.color;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                fadeImage.color = Color.Lerp(startColor, targetColor, timer / duration);
                yield return null;
            }

            fadeImage.color = targetColor;
        }
    }
}