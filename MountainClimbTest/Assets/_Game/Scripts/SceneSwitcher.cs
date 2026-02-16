using MountainRescue.Dialogue;
using MountainRescue.UI;
using MountainRescue.Systems;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

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

        [Header("Story Settings")]
        [SerializeField] private NPCDialogueController npcController;

        [Header("Transition Settings")]
        [SerializeField] private float transitionHoldTime = 1.5f;

        [Header("UI Cleanup")]
        [SerializeField] private TextMeshProUGUI subtitleText;

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
        }

        public int GetCurrentStoryStage()
        {
            return npcController != null ? npcController.CurrentStoryStage : 0;
        }

        public void SwitchScene(string sceneName, string spawnPointName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, spawnPointName));
        }

        // --- NEW: Global Audio Fader ---
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
            Debug.Log($"[SceneSwitcher] Step 1: Starting transition to {sceneName}");

            // FADE OUT VISUALS & MASTER AUDIO TOGETHER
            float fadeOutDuration = 1.5f;
            StartCoroutine(FadeGlobalAudio(1f, 0f, fadeOutDuration));
            if (fader != null) yield return StartCoroutine(fader.FadeOut(fadeOutDuration));

            if (bodyTransformer != null) bodyTransformer.enabled = false;
            if (dynamicMoveProvider != null) dynamicMoveProvider.enabled = false;
            if (subtitleText != null) subtitleText.text = "";

            Debug.Log("[SceneSwitcher] Step 2: Loading Scene Async...");
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            // PREVENT UNITY FROM ACTIVATING THE SCENE (Hides the lag spike)
            op.allowSceneActivation = false;

            // Wait until the scene is fully loaded in the background
            while (op.progress < 0.9f) yield return null;

            Debug.Log("[SceneSwitcher] Step 3: Background load complete. Activating scene.");

            // Allow activation (the freeze happens here, but the screen is pitch black!)
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();

            TerrainPhysicsFix();
            yield return new WaitForFixedUpdate();

            GameObject spawnPoint = GameObject.Find(spawnPointName);

            if (spawnPoint == null)
            {
                Debug.LogError($"[SceneSwitcher] CRITICAL ERROR: Could not find GameObject named '{spawnPointName}'");
                StartCoroutine(FadeGlobalAudio(0f, 1f, 1.5f));
                if (fader != null) yield return StartCoroutine(fader.FadeIn(1.5f));
                yield break;
            }

            CharacterController cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            xrOrigin.transform.position = spawnPoint.transform.position;
            xrOrigin.transform.rotation = spawnPoint.transform.rotation;

            float timeout = 3.0f;
            float timer = 0f;
            bool groundDetected = false;
            int layerMask = LayerMask.GetMask("Default", "Terrain", "Ground");
            if (layerMask == 0) layerMask = ~0;

            while (timer < timeout)
            {
                if (Physics.Raycast(spawnPoint.transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, layerMask))
                {
                    xrOrigin.transform.position = hit.point;
                    groundDetected = true;
                    break;
                }
                timer += Time.deltaTime;
                yield return null;
            }

            Physics.SyncTransforms();
            if (cc != null)
            {
                cc.enabled = true;
                cc.Move(Vector3.zero);
            }

            if (bodyTransformer != null) bodyTransformer.enabled = true;
            if (dynamicMoveProvider != null) dynamicMoveProvider.enabled = true;

            if (toolRespawner != null) toolRespawner.RecoverDroppedTools();

            yield return new WaitForSeconds(transitionHoldTime);

            Debug.Log("[SceneSwitcher] Step 8: Fading In Visuals and Audio.");

            // FADE IN VISUALS & MASTER AUDIO TOGETHER
            StartCoroutine(FadeGlobalAudio(0f, 1f, 1.5f));
            if (fader != null) yield return StartCoroutine(fader.FadeIn(1.5f));
        }

        private void TerrainPhysicsFix()
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                TerrainCollider tc = t.GetComponent<TerrainCollider>();
                if (tc == null) continue;
                tc.enabled = false;
                tc.enabled = true;
            }
        }
    }
}