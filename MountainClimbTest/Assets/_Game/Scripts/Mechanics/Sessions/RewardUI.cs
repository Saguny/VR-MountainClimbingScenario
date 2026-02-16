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
        [SerializeField] private TextMeshProUGUI timeToLocateText;
        [SerializeField] private TextMeshProUGUI deathText;
        [SerializeField] private TextMeshProUGUI safetyViolationsText;
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private TextMeshProUGUI victimResultText;

        [Header("Text Formats")]
        [SerializeField] private string rankFormat = "Rank\n{0}";
        [SerializeField] private string scoreFormat = "Score\n{0}";
        [SerializeField] private string timeFormat = "Total Time\n{0:00}:{1:00}";
        [SerializeField] private string timeToLocateFormat = "Locate Time\n{0:00}:{1:00}";
        [SerializeField] private string deathFormat = "Death\n{0}";
        [SerializeField] private string safetyViolationsFormat = "Tools Dropped\n{0} times";
        [SerializeField] private string countdownFormat = "Menu in: {0}s";

        [Header("Victim Feedback")]
        [SerializeField] private string victimSuccessMessage = "Success! You saved the victim.";
        [SerializeField] private string victimFailMessage = "Failed! Victim didn't receive enough oxygen.";
        [SerializeField] private Color successColor = Color.green;
        [SerializeField] private Color failColor = Color.red;

        [Header("Settings")]
        [SerializeField] private float waitTime = 15f;
        [SerializeField] private string mainMenuName = "MainMenu";

        private void Start()
        {
            var moveProvider = FindFirstObjectByType<DynamicMoveProvider>();
            if (moveProvider != null) moveProvider.moveSpeed = 0f;

            if (GameSessionManager.Instance != null)
            {
                var results = GameSessionManager.Instance.GetFinalResults();

                if (rankText) rankText.text = string.Format(rankFormat, results.rank);
                if (scoreText) scoreText.text = string.Format(scoreFormat, results.score);

                float t = GameSessionManager.Instance.playTime;
                if (timeText)
                {
                    timeText.text = string.Format(timeFormat, Mathf.Floor(t / 60), Mathf.Floor(t % 60));
                }

                float locTime = GameSessionManager.Instance.timeToLocate;
                if (timeToLocateText)
                {
                    timeToLocateText.text = string.Format(timeToLocateFormat, Mathf.Floor(locTime / 60), Mathf.Floor(locTime % 60));
                }

                if (deathText) deathText.text = string.Format(deathFormat, GameSessionManager.Instance.deathCount);

                if (safetyViolationsText)
                {
                    safetyViolationsText.text = string.Format(safetyViolationsFormat, GameSessionManager.Instance.safetyViolations);
                }

                if (victimResultText)
                {
                    bool saved = GameSessionManager.Instance.IsVictimSaved();
                    victimResultText.text = saved ? victimSuccessMessage : victimFailMessage;
                    victimResultText.color = saved ? successColor : failColor;
                }
            }

            StartCoroutine(ReturnToMenuRoutine());
        }

        private IEnumerator ReturnToMenuRoutine()
        {
            float remaining = waitTime;
            while (remaining > 0)
            {
                if (countdownText) countdownText.text = string.Format(countdownFormat, Mathf.CeilToInt(remaining));
                yield return new WaitForSeconds(1f);
                remaining -= 1f;
            }

            if (GameSessionManager.Instance != null)
            {
                GameSessionManager.Instance.DestroySystems();
            }

            SceneManager.LoadScene(mainMenuName);
        }
    }
}