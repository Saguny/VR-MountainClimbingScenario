using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MountainRescue.UI
{
    public class HeadsetFader : MonoBehaviour
    {
        [SerializeField] private Image fadeImage;
        [SerializeField] private float defaultFadeTime = 1.5f;
        [SerializeField] private bool debugMode = true; // Enable by default for debugging

        private Canvas canvas;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();

            // CRITICAL: Validate Canvas Setup
            if (canvas != null)
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    Debug.LogWarning("[HeadsetFader] Canvas is not in Screen Space - Overlay mode! Fixing...");
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }

                // Ensure canvas is on top of everything
                if (canvas.sortingOrder < 999)
                {
                    Debug.LogWarning($"[HeadsetFader] Canvas sortingOrder was {canvas.sortingOrder}, setting to 9999");
                    canvas.sortingOrder = 9999;
                }
            }
            else
            {
                Debug.LogError("[HeadsetFader] No Canvas found! Fader will not work!");
            }

            if (fadeImage != null)
            {
                fadeImage.raycastTarget = false;

                // Ensure the image covers the entire screen
                RectTransform rt = fadeImage.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                }

                // Start clear if not specified otherwise
                if (fadeImage.color.a == 0)
                    fadeImage.color = new Color(0, 0, 0, 0);

                if (debugMode)
                    Debug.Log($"[HeadsetFader] Initialized. Starting color: {fadeImage.color}");
            }
            else
            {
                Debug.LogError("[HeadsetFader] No Fade Image assigned! Assign an Image component in the inspector.");
            }
        }

        // --- STANDARD METHODS (Defaults to Black) ---

        public void SnapToBlack()
        {
            if (fadeImage)
            {
                fadeImage.color = Color.black;
                if (debugMode) Debug.Log("[HeadsetFader] Snapped to BLACK");
            }
        }

        public void SnapToClear()
        {
            if (fadeImage)
            {
                fadeImage.color = new Color(0, 0, 0, 0);
                if (debugMode) Debug.Log("[HeadsetFader] Snapped to CLEAR");
            }
        }

        public IEnumerator FadeOut(float duration = -1)
        {
            // Default "Fade Out" means "Fade to Black"
            if (debugMode) Debug.Log($"[HeadsetFader] Starting FadeOut to BLACK (duration: {duration})");
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

                if (debugMode) Debug.Log($"[HeadsetFader] Starting FadeIn to CLEAR (duration: {time})");
                yield return FadeToTarget(targetColor, time);
            }
        }

        // --- COLOR OVERRIDES (For Flashbangs/Damage) ---

        public void SetColor(Color color)
        {
            if (fadeImage)
            {
                fadeImage.color = color;
                if (debugMode) Debug.Log($"[HeadsetFader] Set color to {color}");
            }
        }

        public void SnapToColor(Color color)
        {
            if (fadeImage)
            {
                fadeImage.color = color;
                if (debugMode) Debug.Log($"[HeadsetFader] Snapped to color {color}");
            }
        }

        public IEnumerator FadeToColor(Color targetColor, float duration = -1)
        {
            float time = duration > 0 ? duration : defaultFadeTime;
            if (debugMode) Debug.Log($"[HeadsetFader] Starting fade to {targetColor} (duration: {time})");
            yield return FadeToTarget(targetColor, time);
        }

        // --- INTERNAL LOGIC ---

        private IEnumerator FadeToTarget(Color targetColor, float duration)
        {
            if (fadeImage == null)
            {
                Debug.LogError("[HeadsetFader] Cannot fade - fadeImage is null!");
                yield break;
            }

            Color startColor = fadeImage.color;
            float timer = 0f;

            if (debugMode)
                Debug.Log($"[HeadsetFader] Fading from {startColor} to {targetColor} over {duration}s");

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;
                fadeImage.color = Color.Lerp(startColor, targetColor, t);

                if (debugMode && timer % 0.5f < Time.deltaTime)
                    Debug.Log($"[HeadsetFader] Progress: {t * 100:F0}% (Color: {fadeImage.color})");

                yield return null;
            }

            fadeImage.color = targetColor;

            if (debugMode)
                Debug.Log($"[HeadsetFader] ✓ Fade complete. Final color: {fadeImage.color}");
        }
    }
}