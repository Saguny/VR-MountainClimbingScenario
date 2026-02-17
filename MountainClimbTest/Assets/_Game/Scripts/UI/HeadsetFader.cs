using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace MountainRescue.UI
{
    public class HeadsetFader : MonoBehaviour
    {
        [SerializeField] private Image fadeImage;
        [SerializeField] private float defaultFadeTime = 1.5f;
        [SerializeField] private bool debugMode = true;

        private Canvas canvas;
        private Coroutine holdBlackCoroutine;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();

            if (canvas != null && canvas.sortingOrder < 999)
                canvas.sortingOrder = 9999;

            if (fadeImage != null)
            {
                fadeImage.raycastTarget = false;

                RectTransform rt = fadeImage.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                }

                if (fadeImage.color.a == 0)
                    fadeImage.color = new Color(0, 0, 0, 0);
            }
        }

        public void ReattachCameraIfNeeded()
        {
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                if (canvas.worldCamera == null)
                    canvas.worldCamera = Camera.main;
            }
        }

        public void SnapToBlack()
        {
            if (fadeImage == null) return;
            fadeImage.color = Color.black;
            fadeImage.SetAllDirty();
        }

        public void SnapToClear()
        {
            if (fadeImage == null) return;
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.SetAllDirty();
        }

        public void StartHoldingBlack()
        {
            if (holdBlackCoroutine != null)
                StopCoroutine(holdBlackCoroutine);
            holdBlackCoroutine = StartCoroutine(HoldBlackRoutine());
        }

        public void StopHoldingBlack()
        {
            if (holdBlackCoroutine != null)
            {
                StopCoroutine(holdBlackCoroutine);
                holdBlackCoroutine = null;
            }
        }

        private IEnumerator HoldBlackRoutine()
        {
            while (true)
            {
                if (fadeImage != null)
                    fadeImage.color = Color.black;
                yield return null;
            }
        }

        public IEnumerator FadeOut(float duration = -1)
        {
            yield return FadeToColor(Color.black, duration);
        }

        public IEnumerator FadeIn(float duration = -1)
        {
            if (fadeImage != null)
            {
                Color targetColor = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0f);
                yield return FadeToColor(targetColor, duration);
            }
        }

        public void SetColor(Color color)
        {
            if (fadeImage != null)
                fadeImage.color = color;
        }

        public void SnapToColor(Color color)
        {
            if (fadeImage != null)
                fadeImage.color = color;
        }

        public IEnumerator FadeToColor(Color targetColor, float duration = -1)
        {
            float time = duration > 0 ? duration : defaultFadeTime;
            yield return FadeToTarget(targetColor, time);
        }

        private IEnumerator FadeToTarget(Color targetColor, float duration)
        {
            if (fadeImage == null) yield break;

            Color startColor = fadeImage.color;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                fadeImage.color = Color.Lerp(startColor, targetColor, timer / duration);
                yield return null;
            }

            fadeImage.color = targetColor;
        }
    }
}