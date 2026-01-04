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
    public class DynamicSceneSwitcher : MonoBehaviour
    {
        public static DynamicSceneSwitcher Instance;
        public static event System.Action<CharacterController> OnCharacterControllerRebuilt;

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
                Instance = this;
            else
                Destroy(gameObject);
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
            if (fader != null)
                yield return StartCoroutine(fader.FadeOut());

            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
                subtitleText.gameObject.SetActive(false);
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!op.isDone)
                yield return null;

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            TerrainPhysicsFix();

            PositionPlayer(spawnPointName);
            Physics.SyncTransforms();

            yield return StartCoroutine(RebuildCharacterController());

            if (bodyTransformer != null)
            {
                bodyTransformer.enabled = false;
                yield return null;
                bodyTransformer.enabled = true;
            }

            if (dynamicMoveProvider != null)
            {
                dynamicMoveProvider.enabled = false;
                yield return null;
                dynamicMoveProvider.enabled = true;
            }

            if (fader != null)
                yield return StartCoroutine(fader.FadeIn());
        }

        private IEnumerator RebuildCharacterController()
        {
            CharacterController oldCC = xrOrigin.GetComponent<CharacterController>();
            if (oldCC == null)
                yield break;

            float height = oldCC.height;
            Vector3 center = oldCC.center;
            float radius = oldCC.radius;
            float stepOffset = oldCC.stepOffset;
            float slopeLimit = oldCC.slopeLimit;

            Destroy(oldCC);

            yield return null;
            yield return new WaitForFixedUpdate();

            CharacterController newCC = xrOrigin.AddComponent<CharacterController>();
            newCC.height = height;
            newCC.center = center;
            newCC.radius = radius;
            newCC.stepOffset = stepOffset;
            newCC.slopeLimit = slopeLimit;

            OnCharacterControllerRebuilt?.Invoke(newCC);
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
                xrOrigin.transform.position = spawnPoint.transform.position + Vector3.up * 0.1f;
                xrOrigin.transform.rotation = spawnPoint.transform.rotation;
            }
        }
    }
}
