using UnityEngine;
using UnityEngine.SceneManagement;

namespace MountainRescue.Systems.Session
{
    public class GameSessionManager : MonoBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        [Header("Scene Config")]
        public string tutorialSceneName = "TutorialScene";
        public string endSceneName = "EndScene";

        [Header("Live Data")]
        public float playTime;
        public int deathCount;
        private bool isTracking = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // Falls [SYSTEMS] noch kein DDOL hat, erzwingen wir es hier
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (isTracking)
            {
                playTime += Time.deltaTime;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == tutorialSceneName)
            {
                playTime = 0f;
                deathCount = 0;
                isTracking = false;
            }
            else if (scene.name == endSceneName || scene.name == "MainMenu")
            {
                isTracking = false;
            }
            else
            {
                isTracking = true;
            }
        }

        public void RegisterDeath()
        {
            if (isTracking) deathCount++;
        }

        public (string rank, int score) GetFinalResults()
        {
            float timePenalty = playTime * 2f;
            int deathPenalty = deathCount * 500;
            int finalScore = Mathf.Max(0, 10000 - (int)timePenalty - deathPenalty);

            string rank = "C";
            if (finalScore > 9000) rank = "S";
            else if (finalScore > 7500) rank = "A";
            else if (finalScore > 5000) rank = "B";

            return (rank, finalScore);
        }

        public void DestroySystems()
        {
            Destroy(this.gameObject);
        }
    }
}