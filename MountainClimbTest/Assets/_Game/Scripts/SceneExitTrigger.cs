using UnityEngine;

namespace MountainRescue.Engine
{
    public class SceneExitTrigger : MonoBehaviour
    {
        [Header("Target Config")]
        [SerializeField] private string targetScene;
        [SerializeField] private string targetSpawnPointName;

        [Header("Story Gating")]
        [Tooltip("What Story Stage is needed to proceed?")]
        [SerializeField] private int requiredStoryStage = 0;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CharacterController>() != null)
            {
                // Prüfen ob die aktuelle Stage passt
                if (DynamicSceneSwitcher.Instance.GetCurrentStoryStage() >= requiredStoryStage)
                {
                    DynamicSceneSwitcher.Instance.SwitchScene(targetScene, targetSpawnPointName);
                }
                else
                {
                    Debug.Log($"Szenenwechsel blockiert: Benötige Stage {requiredStoryStage}, aktuell ist {DynamicSceneSwitcher.Instance.GetCurrentStoryStage()}");
                }
            }
        }
    }
}