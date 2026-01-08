using UnityEngine;
using UnityEngine.SceneManagement;

namespace MountainRescue.Systems
{
    public class RescueTargetManager : MonoBehaviour
    {
        public static RescueTargetManager Instance;

        [SerializeField] private Transform currentTarget;
        public Transform CurrentTarget => currentTarget;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindTargetInScene();
        }

        // Inside RescueTargetManager.cs
        public void FindTargetInScene()
        {
            GameObject targetObj = GameObject.FindWithTag("RescueTarget");
            if (targetObj != null)
            {
                SetTarget(targetObj.transform);
            }
            else
            {
                currentTarget = null; // Clear target if none exists in the new scene
            }
        }

        public void SetTarget(Transform newTarget)
        {
            currentTarget = newTarget;
        }
    }
}