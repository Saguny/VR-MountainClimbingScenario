using UnityEngine;
using MountainRescue.UI;

namespace MountainRescue.Systems
{
    [RequireComponent(typeof(RescueTargetManager))]
    public class PlayerSensorSuite : MonoBehaviour, IDistanceProvider, IVerticalGuidanceProvider, IPressureProvider
    {
        [Header("Dependencies")]
        [SerializeField] private Transform playerHead;
        [SerializeField] private RescueTargetManager targetManager;

        [Header("Vertical Calibration")]
        [Tooltip("How far down from the camera to measure 'Body Center'. 0.9m is roughly chest/stomach height.")]
        [SerializeField] private float sensorVerticalOffset = 0.9f;

        [Tooltip("The radius of the 'Level' zone. 1.1m means 1.1m up AND 1.1m down from Body Center.")]
        [SerializeField] private float verticalTolerance = 1.1f;

        [Header("Atmospherics")]
        [SerializeField] public float seaLevelPressureHPa = 700f;

        private VerticalGuidanceState _lastState = VerticalGuidanceState.Neutral;
        public event System.Action<VerticalGuidanceState> OnStateChanged;

        private void Awake()
        {
            if (targetManager == null) targetManager = GetComponent<RescueTargetManager>();
            if (playerHead == null && Camera.main != null) playerHead = Camera.main.transform;
        }

        private void Update()
        {
            CheckVerticalState();
        }

        public float GetDistanceToTarget()
        {
            if (!HasValidTarget()) return 0f;

            Vector3 playerPos = playerHead.position;
            Vector3 targetPos = targetManager.CurrentTarget.position;

            // Horizontal distance only (Projected on ground plane)
            playerPos.y = 0;
            targetPos.y = 0;

            return Vector3.Distance(playerPos, targetPos);
        }

        public bool HasValidTarget() => targetManager.CurrentTarget != null;

        public VerticalGuidanceState GetCurrentState()
        {
            if (!HasValidTarget()) return VerticalGuidanceState.Neutral;

            // 1. Determine the "Sensor Center" (Your body, not your eyes)
            float bodyCenterY = playerHead.position.y - sensorVerticalOffset;
            float targetY = targetManager.CurrentTarget.position.y;

            float deltaY = targetY - bodyCenterY;

            // 2. Check if target is within the tolerance radius of your body
            // With Offset 0.9 and Tolerance 1.1:
            // Range extends from [Head + 0.2m] down to [Feet - 0.2m]
            if (Mathf.Abs(deltaY) <= verticalTolerance)
            {
                return VerticalGuidanceState.Neutral;
            }

            return deltaY > 0 ? VerticalGuidanceState.TargetIsAbove : VerticalGuidanceState.TargetIsBelow;
        }

        private void CheckVerticalState()
        {
            var currentState = GetCurrentState();
            // Force update if this is the first frame or state changed
            if (currentState != _lastState)
            {
                _lastState = currentState;
                OnStateChanged?.Invoke(currentState);
            }
        }

        public float GetAltitudeMeters()
        {
            return playerHead.position.y;
        }

        public float GetPressureHPa()
        {
            float altitude = GetAltitudeMeters();
            return seaLevelPressureHPa * Mathf.Exp(-altitude / 8000f);
        }
    }
}