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

        public void FindTargetInScene()
        {
            GameObject targetObj = GameObject.FindWithTag("RescueTarget");
            if (targetObj != null)
            {
                currentTarget = targetObj.transform;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            currentTarget = newTarget;
        }
    }
}