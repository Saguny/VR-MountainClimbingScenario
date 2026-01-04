using UnityEngine;

namespace MountainRescue.Engine
{
    public class SystemBootstrapper : MonoBehaviour
    {
        public static SystemBootstrapper Instance;

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
    }
}