using MountainRescue.Dialogue;
using MountainRescue.UI;
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

        [Header("references")]
        [SerializeField] private HeadsetFader fader;
        [SerializeField] private GameObject xrOrigin;
        [SerializeField] private XRBodyTransformer bodyTransformer;
        [SerializeField] private MonoBehaviour dynamicMoveProvider;

        [Header("story settings")]
        [SerializeField] private NPCDialogueController npcController;

        [Header("ui cleanup")]
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

            // 1. Fade Out
            if (fader != null) yield return StartCoroutine(fader.FadeOut());

            // 2. Load Scene
            Debug.Log("[SceneSwitcher] Step 2: Loading Scene Async...");
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;

            Debug.Log("[SceneSwitcher] Step 3: Scene Loaded. Waiting for frames...");

            // 3. Terrain Fix & Safety Wait
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();

            // Debugging Terrain Fix
            Debug.Log($"[SceneSwitcher] Step 4: Fix Terrains. Found {Terrain.activeTerrains.Length} terrains.");
            TerrainPhysicsFix();

            yield return new WaitForFixedUpdate();

            // 4. Spawn Point Check
            Debug.Log($"[SceneSwitcher] Step 5: Looking for SpawnPoint '{spawnPointName}'...");
            GameObject spawnPoint = GameObject.Find(spawnPointName);

            if (spawnPoint == null)
            {
                Debug.LogError($"[SceneSwitcher] CRITICAL ERROR: Could not find GameObject named '{spawnPointName}' in the new scene!");
                // We stop here to prevent further errors, but fade in so you aren't stuck in black
                if (fader != null) yield return StartCoroutine(fader.FadeIn());
                yield break;
            }

            Debug.Log($"[SceneSwitcher] Step 6: SpawnPoint found at {spawnPoint.transform.position}. Starting Ground Check...");

            // Disable Player
            if (bodyTransformer != null) bodyTransformer.enabled = false;
            if (dynamicMoveProvider != null) dynamicMoveProvider.enabled = false;

            CharacterController cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // Move to spawn to start the check
            xrOrigin.transform.position = spawnPoint.transform.position;
            xrOrigin.transform.rotation = spawnPoint.transform.rotation;

            // 5. Ground Check Loop
            float timeout = 3.0f; // Increased timeout
            float timer = 0f;
            bool groundDetected = false;

            // Safety check: Is there a terrain layer?
            int layerMask = LayerMask.GetMask("Default", "Terrain", "Ground");
            if (layerMask == 0) layerMask = ~0; // If no layers defined, hit everything

            while (timer < timeout)
            {
                // Debug Ray
                Debug.DrawRay(spawnPoint.transform.position + Vector3.up * 2f, Vector3.down * 5f, Color.red, 1.0f);

                if (Physics.Raycast(spawnPoint.transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, layerMask))
                {
                    Debug.Log($"[SceneSwitcher] Step 7: SUCCESS! Ground detected at {hit.point.y} on object '{hit.collider.name}'. Snapping.");
                    xrOrigin.transform.position = hit.point;
                    groundDetected = true;
                    break;
                }

                // Print this every 0.5s to avoid spamming, but verify loop is running
                if (timer % 0.5f < Time.deltaTime)
                    Debug.Log($"[SceneSwitcher] ... Waiting for ground (Time: {timer:F1}) ...");

                timer += Time.deltaTime;
                yield return null;
            }

            if (!groundDetected)
            {
                Debug.LogError("[SceneSwitcher] FAIL: Raycast never hit the ground. Check: 1. Is Terrain on 'Ignore Raycast'? 2. Is SpawnPoint too high?");
            }

            // 6. Re-Enable
            Physics.SyncTransforms();
            if (cc != null)
            {
                cc.enabled = true;
                cc.Move(Vector3.zero);
            }

            if (bodyTransformer != null) bodyTransformer.enabled = true;
            if (dynamicMoveProvider != null) dynamicMoveProvider.enabled = true;

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

                // Disable CC to perform the teleport
                if (cc != null) cc.enabled = false;

                xrOrigin.transform.position = spawnPoint.transform.position + Vector3.up * 0.1f;
                xrOrigin.transform.rotation = spawnPoint.transform.rotation;

                // Re-enable CC
                if (cc != null)
                {
                    cc.enabled = true;
                    // Physics Freeze: Reset any accumulated velocity to 0
                    // This prevents the 'velocity carry-over' from the previous scene
                    cc.Move(Vector3.zero);
                }
            }
        }
    }
}