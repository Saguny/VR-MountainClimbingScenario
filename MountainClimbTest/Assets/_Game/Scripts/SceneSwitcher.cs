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
            if (fader != null) yield return StartCoroutine(fader.FadeOut()); //

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); //
            while (!op.isDone) yield return null; //

            // Wait 2 frames for PhysX to settle the new scene geometry
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            TerrainPhysicsFix(); //

            // Position the player
            PositionPlayer(spawnPointName); //

            // Crucial: Sync AFTER enabling the CC again to bake it into the new position
            Physics.SyncTransforms();

            // Reset providers
            if (bodyTransformer != null) { bodyTransformer.enabled = false; yield return null; bodyTransformer.enabled = true; } //

            if (fader != null) yield return StartCoroutine(fader.FadeIn()); //
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

                // CC muss deaktiviert sein für harten Teleport
                if (cc != null) cc.enabled = false;

                xrOrigin.transform.position = spawnPoint.transform.position + Vector3.up * 0.1f;
                xrOrigin.transform.rotation = spawnPoint.transform.rotation;

                if (cc != null) cc.enabled = true;
            }
        }
    }
}