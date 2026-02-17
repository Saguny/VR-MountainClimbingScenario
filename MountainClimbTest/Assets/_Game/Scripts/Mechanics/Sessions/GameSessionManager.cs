using UnityEngine;
using UnityEngine.SceneManagement;

namespace MountainRescue.Systems.Session
{
    public class GameSessionManager : MonoBehaviour
    {
        public static GameSessionManager Instance { get; private set; }

        // --- NEU: Sperre während des Szenenwechsels ---
        private bool isChangingScene = false;

        [Header("Scene Config")]
        public string tutorialSceneName = "TutorialScene";
        public string endSceneName = "EndScene";

        [Header("Live Data")]
        public float playTime;
        public int deathCount;

        [Header("Victim Rescue Data")]
        public float victimOxygenSupplied = 0f;
        public float victimOxygenRequired = 50f;

        [Header("Extended Sim Data")]
        public float timeToLocate;
        public int safetyViolations;
        private bool hasLocatedVictim;

        private bool isTracking = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            // Tracke nur, wenn wir nicht gerade laden und Tracking aktiv ist
            if (isTracking && !isChangingScene)
            {
                playTime += Time.deltaTime;
            }
        }

        // --- NEU: Methoden für den SceneSwitcher ---
        public void StartSceneTransition()
        {
            isChangingScene = true;
            Debug.Log("[GameSessionManager] Scoring LOCKED.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isChangingScene = false; // Entsperren, wenn Szene bereit
            Debug.Log("[GameSessionManager] Scoring UNLOCKED.");

            if (scene.name == tutorialSceneName)
            {
                ResetSession();
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

        private void ResetSession()
        {
            playTime = 0f;
            deathCount = 0;
            victimOxygenSupplied = 0f;
            timeToLocate = 0f;
            safetyViolations = 0;
            hasLocatedVictim = false;
            isTracking = false;
        }

        // --- ÜBERARBEITETE REGISTRIERUNGEN ---
        // Wir prüfen jetzt überall: !isChangingScene

        public void RegisterDeath()
        {
            if (isTracking && !isChangingScene) deathCount++;
        }

        public void RegisterVictimOxygen(float amount)
        {
            if (isTracking && !isChangingScene)
            {
                victimOxygenSupplied += amount;
            }
        }

        public void MarkVictimLocated()
        {
            if (isTracking && !isChangingScene && !hasLocatedVictim)
            {
                timeToLocate = playTime;
                hasLocatedVictim = true;
            }
        }

        public void RegisterSafetyViolation()
        {
            if (isTracking && !isChangingScene) safetyViolations++;
        }

        // ... Rest bleibt gleich ...
        public bool IsVictimSaved() => victimOxygenSupplied >= victimOxygenRequired;

        public (string rank, int score) GetFinalResults()
        {
            if (deathCount > 0) return ("F", 0);
            if (!IsVictimSaved())
            {
                int partialScore = Mathf.Max(0, 5000 - (int)(playTime * 2f));
                return ("D", partialScore);
            }

            int baseScore = 12000;
            float timePenalty = playTime * 2f;
            int finalScore = Mathf.Max(0, baseScore - (int)timePenalty);

            string rank = "C";
            if (finalScore >= 10500) rank = "S";
            else if (finalScore >= 10000) rank = "A";
            else if (finalScore >= 8000) rank = "B";

            return (rank, finalScore);
        }

        public void DestroySystems() => Destroy(this.gameObject);
    }
}