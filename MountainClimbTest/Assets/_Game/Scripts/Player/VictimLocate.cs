using UnityEngine;
using MountainRescue.Systems.Session;

namespace MountainRescue.Systems.Triggers
{
    public class VictimLocatorTrigger : MonoBehaviour
    {
        public string playerTag = "Player";

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                if (GameSessionManager.Instance != null)
                {
                    GameSessionManager.Instance.MarkVictimLocated();
                    gameObject.SetActive(false);
                }
            }
        }
    }
}