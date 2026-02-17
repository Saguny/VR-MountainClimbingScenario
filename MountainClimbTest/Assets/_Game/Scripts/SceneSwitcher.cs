using MountainRescue.Dialogue;
using MountainRescue.UI;
using MountainRescue.Systems;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using MountainRescue.Systems.Session;

namespace MountainRescue.Engine
{
    public class SceneSwitcher : MonoBehaviour
    {
        public static SceneSwitcher Instance;

        [Header("References")]
        [SerializeField] private HeadsetFader fader;
        [SerializeField] private GameObject xrOrigin;
        [SerializeField] private XRBodyTransformer bodyTransformer;
        [SerializeField] private MonoBehaviour dynamicMoveProvider;
        [SerializeField] private ToolRespawner toolRespawner;

        [Header("Loading UI")]
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private float pulseSpeed = 2.0f;

        [Header("Story Settings")]
        [SerializeField] private NPCDialogueController npcController;

        [Header("Transition Settings")]
        [SerializeField] private float transitionHoldTime = 1.5f;

        [Header("UI Cleanup")]
        [SerializeField] private TextMeshProUGUI subtitleText;

        private Coroutine pulseCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (toolRespawner == null) toolRespawner = GetComponentInChildren<ToolRespawner>();
            if (loadingText != null) loadingText.gameObject.SetActive(false);
        }

        public int GetCurrentStoryStage()
        {
            return npcController != null ? npcController.CurrentStoryStage : 0;
        }

        private IEnumerator PulseLoadingText()
        {
            if (loadingText == null) yield break;
            loadingText.gameObject.SetActive(true);
            while (true)
            {
                float alpha = (Mathf.Sin(Time.time * pulseSpeed) + 1.0f) / 2.0f;
                Color c = loadingText.color;
                c.a = alpha;
                loadingText.color = c;
                yield return null;
            }
        }

        public void SwitchScene(string sceneName, string spawnPointName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, spawnPointName));
        }

        private IEnumerator FadeGlobalAudio(float startVolume, float targetVolume, float duration)
        {
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                AudioListener.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
                yield return null;
            }
            AudioListener.volume = targetVolume;
        }

        private IEnumerator LoadSceneRoutine(string sceneName, string spawnPointName)
        {

            // Lock
            if (GameSessionManager.Instance != null) {
                GameSessionManager.Instance.StartSceneTransition();
            }
            // 1. FADE OUT
            float fadeOutDuration = 1.5f;
            StartCoroutine(FadeGlobalAudio(1f, 0f, fadeOutDuration));
            if (fader != null) yield return StartCoroutine(fader.FadeOut(fadeOutDuration));

            // 2. SHOW LOADING
            if (loadingText != null)
            {
                if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                pulseCoroutine = StartCoroutine(PulseLoadingText());
            }

            // 3. ASYNC LOAD
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            op.allowSceneActivation = false;
            while (op.progress < 0.9f) yield return null;
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            // 4. FIX PHYSICS & REFS
            yield return new WaitForEndOfFrame();

            // --- DER FIX ---
            // Wir zwingen den Fader hier nochmal auf Schwarz, falls die neue Szene 
            // das Image-Component resettet hat.
            if (fader != null) fader.SnapToBlack();

            TerrainPhysicsFix();
            yield return new WaitForFixedUpdate();

            // 5. POSITION PLAYER
            GameObject spawnPoint = GameObject.Find(spawnPointName);
            if (spawnPoint == null)
            {
                StopPulse();
                if (fader != null) yield return StartCoroutine(fader.FadeIn(1.5f));
                yield break;
            }

            CharacterController cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            xrOrigin.transform.position = spawnPoint.transform.position;
            xrOrigin.transform.rotation = spawnPoint.transform.rotation;
            Physics.SyncTransforms();
            if (cc != null) { cc.enabled = true; cc.Move(Vector3.zero); }

            if (bodyTransformer != null) bodyTransformer.enabled = true;
            if (dynamicMoveProvider != null) dynamicMoveProvider.enabled = true;

            // 6. HOLD & FADE IN
            yield return new WaitForSeconds(transitionHoldTime);
            StopPulse();

            StartCoroutine(FadeGlobalAudio(0f, 1f, 1.5f));
            if (fader != null) yield return StartCoroutine(fader.FadeIn(1.5f));
        }

        private void StopPulse()
        {
            if (pulseCoroutine != null) { StopCoroutine(pulseCoroutine); pulseCoroutine = null; }
            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(false);
                Color c = loadingText.color;
                c.a = 0f;
                loadingText.color = c;
            }
        }

        private void TerrainPhysicsFix()
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                TerrainCollider tc = t.GetComponent<TerrainCollider>();
                if (tc != null) { tc.enabled = false; tc.enabled = true; }
            }
        }
    }
}