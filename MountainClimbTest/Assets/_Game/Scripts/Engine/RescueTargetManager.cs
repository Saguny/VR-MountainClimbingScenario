using UnityEngine;

namespace MountainRescue.Systems
{
    public class RescueTargetManager : MonoBehaviour
    {
        [Tooltip("The current objective the player needs to reach.")]
        [SerializeField] private Transform currentTarget;

        public Transform CurrentTarget => currentTarget;

        // Call this via game events to change the target dynamically
        public void SetTarget(Transform newTarget)
        {
            currentTarget = newTarget;
        }
    }
}