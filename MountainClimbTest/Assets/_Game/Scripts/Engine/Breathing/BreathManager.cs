using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using System.Collections;

namespace MountainRescue.Systems
{
    public class BreathManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public PlayerSensorSuite sensorSuite;
        public LocomotionProvider moveProvider; // Drag your DynamicMoveProvider here

        [Header("Stamina Settings")]
        public float maxStamina = 100f;
        public float currentStamina;
        public float lowStaminaThreshold = 25f;

        [Header("Costs & Regeneration")]
        public float baseGrabCost = 2f;
        public float focusRegenRate = 5f;
        public float idleHangRegenRate = 1f;
        public float idleHangDelay = 3f;

        [Header("Altitude & Pressure")]
        public float thinAirThreshold = 500f;
        public AnimationCurve drainRateByPressure; // X: hPa, Y: drain/sec

        [Header("Status")]
        public bool hasOxygenTank = false;
        public bool isFocusing = false;
        private float _timeSinceLastGrab;
        private bool _wasInThinAir = false;
        private bool _lastFocusState;
        private float _logTimer;

        [Header("Events")]
        public UnityEvent<float> onStaminaChanged;
        public UnityEvent<bool> onLowStaminaStateChanged;
        public UnityEvent<bool> onFocusStateChanged;
        public UnityEvent onThinAirReached;
        public UnityEvent onStaminaEmpty;

        private void Start()
        {
            currentStamina = maxStamina;

            if (sensorSuite == null)
                sensorSuite = FindFirstObjectByType<PlayerSensorSuite>();

            if (moveProvider == null)
                Debug.LogWarning("BreathManager: No MoveProvider assigned. Movement lockout will not work.");
        }

        private void Update()
        {
            if (sensorSuite == null) return;

            float currentHPa = sensorSuite.GetPressureHPa();

            HandleMovementLockout();
            HandleFocusEvents();
            HandlePassiveDrain(currentHPa);
            HandleRegeneration(currentHPa);
            CheckThinAirThreshold(currentHPa);
        }

        private void HandleMovementLockout()
        {
            if (moveProvider == null) return;

            // Lock movement if focusing
            if (isFocusing && moveProvider.enabled)
            {
                moveProvider.enabled = false;
            }
            // Re-enable movement if focus is released (and not currently stuck to ice axes)
            else if (!isFocusing && !moveProvider.enabled)
            {
                moveProvider.enabled = true;
            }
        }

        private void HandleFocusEvents()
        {
            if (isFocusing != _lastFocusState)
            {
                onFocusStateChanged.Invoke(isFocusing);
                _lastFocusState = isFocusing;

                if (isFocusing) Debug.Log("<color=green>[BreathManager]</color> Focus Active. Movement Locked.");
            }
        }

        private void HandlePassiveDrain(float hPa)
        {
            if (hPa < thinAirThreshold)
            {
                float drain = drainRateByPressure.Evaluate(hPa);
                ModifyStamina(-drain * Time.deltaTime);
            }
        }

        private void HandleRegeneration(float hPa)
        {
            bool inThinAir = hPa < thinAirThreshold;

            if (isFocusing)
            {
                // In thin air, focus only works with O2 Tank
                if (!inThinAir || hasOxygenTank)
                {
                    ModifyStamina(focusRegenRate * Time.deltaTime);
                    DebugRegen("Focusing");
                }
            }
            else if (Time.time - _timeSinceLastGrab > idleHangDelay)
            {
                ModifyStamina(idleHangRegenRate * Time.deltaTime);

                if (Time.time > _logTimer)
                {
                    DebugRegen("Idle Hanging");
                    _logTimer = Time.time + 1.0f;
                }
            }
        }

        public bool TryConsumeStaminaForGrab()
        {
            if (sensorSuite == null) return true;

            float hPa = sensorSuite.GetPressureHPa();

            // MATH: Every full 20 hPa drop from Sea Level (1013.25) adds +1 cost
            float pressureDrop = 1013.25f - hPa;
            float penalty = Mathf.FloorToInt(Mathf.Max(0, pressureDrop) / 20f);
            float totalCost = baseGrabCost + penalty;

            if (currentStamina >= totalCost)
            {
                ModifyStamina(-totalCost);
                _timeSinceLastGrab = Time.time;

                Debug.Log($"<color=cyan>[BreathManager]</color> Grab Success! Cost: {totalCost} (Base: {baseGrabCost} + Alt: {penalty}) | Remaining: {currentStamina:F1}");
                return true;
            }

            Debug.LogWarning($"<color=red>[BreathManager]</color> Grab Failed! Required: {totalCost}, Current: {currentStamina:F1}");
            onStaminaEmpty.Invoke();
            return false;
        }

        private void ModifyStamina(float amount)
        {
            float previousStamina = currentStamina;
            currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);

            if (Mathf.Abs(previousStamina - currentStamina) > 0.01f)
                onStaminaChanged.Invoke(currentStamina / maxStamina);

            // Trigger Low Breath Vignette state
            if (previousStamina >= lowStaminaThreshold && currentStamina < lowStaminaThreshold)
                onLowStaminaStateChanged.Invoke(true);
            else if (previousStamina < lowStaminaThreshold && currentStamina >= lowStaminaThreshold)
                onLowStaminaStateChanged.Invoke(false);
        }

        private void CheckThinAirThreshold(float hPa)
        {
            bool inThinAir = hPa < thinAirThreshold;
            if (inThinAir && !_wasInThinAir)
            {
                onThinAirReached.Invoke();
                _wasInThinAir = true;
                Debug.Log("<color=orange>[BreathManager]</color> Entered Thin Air Zone!");
            }
            else if (!inThinAir)
            {
                _wasInThinAir = false;
            }
        }

        private void DebugRegen(string source)
        {
            Debug.Log($"<color=green>[BreathManager]</color> {source}... Stamina: {currentStamina:F1}");
        }
    }
}