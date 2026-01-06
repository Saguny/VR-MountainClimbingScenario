using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using MountainRescue.Systems.Session;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace MountainRescue.UI
{
    public class EndSceneUI : MonoBehaviour
    {
        [Header("Separate Text Slots")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI deathText;
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Settings")]
        [SerializeField] private float waitTime = 15f;
        [SerializeField] private string mainMenuName = "MainMenu";

        private void Start()
        {
            // Bewegung auf 0 setzen
            var moveProvider = FindFirstObjectByType<DynamicMoveProvider>();
            if (moveProvider != null) moveProvider.moveSpeed = 0f;

            if (GameSessionManager.Instance != null)
            {
                var results = GameSessionManager.Instance.GetFinalResults();

                if (rankText) rankText.text = results.rank;
                if (scoreText) scoreText.text = results.score.ToString();

                float t = GameSessionManager.Instance.playTime;
                if (timeText) timeText.text = string.Format("{0:00}:{1:00}", Mathf.Floor(t / 60), Mathf.Floor(t % 60));

                if (deathText) deathText.text = GameSessionManager.Instance.deathCount.ToString();
            }

            StartCoroutine(ReturnToMenuRoutine());
        }

        private IEnumerator ReturnToMenuRoutine()
        {
            float remaining = waitTime;
            while (remaining > 0)
            {
                if (countdownText) countdownText.text = "Menu in: " + Mathf.CeilToInt(remaining) + "s";
                yield return new WaitForSeconds(1f);
                remaining -= 1f;
            }

            // Zerstört das gesamte [SYSTEMS] GameObject inklusive aller Kinder und Manager
            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.DestroySystems();
            }

            SceneManager.LoadScene(mainMenuName);
        }
    }
}