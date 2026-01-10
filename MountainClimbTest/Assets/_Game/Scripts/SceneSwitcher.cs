using MountainRescue.Dialogue;
using MountainRescue.UI;
using MountainRescue.Systems; // Added for ToolRespawner
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

        // Added Reference to ToolRespawner
        [SerializeField] private ToolRespawner toolRespawner;

        [Header("Story Settings")]
        [SerializeField] private NPCDialogueController npcController;

        [Header("Transition Settings")]
        [SerializeField] private float transitionHoldTime = 3.0f;

        [Header("UI Cleanup")]
        [SerializeField] private TextMeshProUGUI subtitleText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Auto-assign if missing, usually found on the same Systems object
            if (toolRespawner == null)
            {
                toolRespawner = GetComponentInChildren<ToolRespawner>();
            }
        }

        public int GetCurrentStoryStage()
        {
            return npcController != null ? npcController.CurrentStoryStage : 0;
        }

        public void SwitchScene(string sceneName, string spawnPointName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName, spawnPointName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, string spawnPointName)
        {
            Debug.Log($"[SceneSwitcher] Step 1: Starting transition to {sceneName}");

            if (fader != null) yield return StartCoroutine(fader.FadeOut());

            // Disable Locomotion during load to prevent falling/glitching
            if (bodyTransformer != null) bodyTransformer.enabled = false;
            if (dynamicMoveProvider != null) dynamicMoveProvider.enabled = false;

            // Clear UI
            if (subtitleText != null) subtitleText.text = "";

            Debug.Log("[SceneSwitcher] Step 2: Loading Scene Async...");
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;

            Debug.Log("[SceneSwitcher] Step 3: Scene Loaded. Waiting for frames...");

            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();

            Debug.Log($"[SceneSwitcher] Step 4: Fix Terrains. Found {Terrain.activeTerrains.Length} terrains.");
            TerrainPhysicsFix();

            yield return new WaitForFixedUpdate();

            Debug.Log($"[SceneSwitcher] Step 5: Looking for SpawnPoint '{spawnPointName}'...");
            GameObject spawnPoint = GameObject.Find(spawnPointName);

            if (spawnPoint == null)
            {
                Debug.LogError($"[SceneSwitcher] CRITICAL ERROR: Could not find GameObject named '{spawnPointName}' in the new scene!");
                if (fader != null) yield return StartCoroutine(fader.FadeIn());
                yield break;
            }

            Debug.Log($"[SceneSwitcher] Step 6: SpawnPoint found at {spawnPoint.transform.position}. Starting Ground Check...");

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
                Debug.DrawRay(spawnPoint.transform.position + Vector3.up * 2f, Vector3.down * 5f, Color.red, 1.0f);

                if (Physics.Raycast(spawnPoint.transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, layerMask))
                {
                    Debug.Log($"[SceneSwitcher] Step 7: SUCCESS! Ground detected at {hit.point.y} on object '{hit.collider.name}'. Snapping.");
                    xrOrigin.transform.position = hit.point;
                    groundDetected = true;
                    break;
                }

                if (timer % 0.5f < Time.deltaTime)
                    Debug.Log($"[SceneSwitcher] ... Waiting for ground (Time: {timer:F1}) ...");

                timer += Time.deltaTime;
                yield return null;
            }

            if (!groundDetected)
            {
                Debug.LogError("[SceneSwitcher] FAIL: Raycast never hit the ground. Check: 1. Is Terrain on 'Ignore Raycast'? 2. Is SpawnPoint too high?");
            }

            Physics.SyncTransforms();
            if (cc != null)
            {
                cc.enabled = true;
                cc.Move(Vector3.zero);
            }

            // Re-enable locomotion
            if (bodyTransformer != null) bodyTransformer.enabled = true;
            if (dynamicMoveProvider != null) dynamicMoveProvider.enabled = true;

            // NEW STEP: Recover Tools
            // We call this NOW, after the player has been safely moved to the new scene position.
            // This ensures tools snap to the belt's new location, not the old scene's location.
            if (toolRespawner != null)
            {
                Debug.Log("[SceneSwitcher] Respawning Tools to Belt...");
                toolRespawner.RecoverDroppedTools();
            }

            yield return new WaitForSeconds(transitionHoldTime);

            Debug.Log("[SceneSwitcher] Step 8: Fading In.");
            if (fader != null) yield return StartCoroutine(fader.FadeIn());
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

        private void PositionPlayer(string spawnPointName)
        {
            GameObject spawnPoint = GameObject.Find(spawnPointName);
            if (spawnPoint != null && xrOrigin != null)
            {
                CharacterController cc = xrOrigin.GetComponent<CharacterController>();

                if (cc != null) cc.enabled = false;

                xrOrigin.transform.position = spawnPoint.transform.position + Vector3.up * 0.1f;
                xrOrigin.transform.rotation = spawnPoint.transform.rotation;

                if (cc != null)
                {
                    cc.enabled = true;
                    cc.Move(Vector3.zero);
                }
            }
        }
    }
}