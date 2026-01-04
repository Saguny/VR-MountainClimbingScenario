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
            // 1. Fade Out
            if (fader != null)
                yield return StartCoroutine(fader.FadeOut());

            // UI Cleanup
            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
                subtitleText.gameObject.SetActive(false);
            }

            // 2. Szene laden
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!op.isDone)
                yield return null;

            // Warten bis alles initialisiert ist
            yield return new WaitForFixedUpdate();

            // 3. Terrain Fix
            TerrainPhysicsFix();

            // 4. Spieler positionieren (WICHTIG: CC kurz aus, sonst glitched es)
            PositionPlayer(spawnPointName);
            Physics.SyncTransforms();

            // 5. Komponenten resetten (damit sie die neue Umgebung checken)
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

            // 6. Fade In
            if (fader != null)
                yield return StartCoroutine(fader.FadeIn());
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