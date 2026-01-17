using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.Audio;
using System.Collections;

namespace MountainRescue.Systems
{
    public class BreathManager : MonoBehaviour
    {
        public static BreathManager Instance;

        [Header("Dependencies")]
        public PlayerSensorSuite sensorSuite;
        public LocomotionProvider moveProvider;

        [Header("Audio Integration")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private string ambienceParam = "AmbienceLowPass";
        [SerializeField] private string musicParam = "MusicLowPass";
        [Space]
        [SerializeField] private float minCutoff = 500f;
        [SerializeField] private float maxCutoff = 22000f;

        [Header("Stamina Settings")]
        public float maxStamina = 100f;
        public float currentStamina;
        public float lowStaminaThreshold = 25f;
        public float penaltyHeight = 50f;

        [Header("Costs & Regeneration")]
        public float baseGrabCost = 2f;
        public float focusRegenRate = 5f;
        public float idleHangRegenRate = 1f;
        public float idleHangDelay = 3f;

        [Header("Altitude & Pressure")]
        public float thinAirThreshold = 500f;
        public AnimationCurve drainRateByPressure;
        public float fallbackThinAirDrain = 1.0f;

        [Header("Status")]
        public bool hasOxygenTank = false;
        public bool isFocusing = false;
        private float _timeSinceLastGrab;
        private bool _wasInThinAir = false;
        private bool _lastFocusState;
        private float _logTimer;
        private bool _isInLowStaminaState;

        [Header("Oxygen Tank Settings")]
        public float currentTankFuel = 100f;
        public float maxTankFuel = 100f;
        public float tankUsageCost = 2f;

        [Header("Events")]
        public UnityEvent<float> onStaminaChanged;
        public UnityEvent<bool> onLowStaminaStateChanged;
        public UnityEvent<bool> onFocusStateChanged;
        public UnityEvent onThinAirReached;
        public UnityEvent onStaminaEmpty;
        public UnityEvent onPlayerDeath;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            currentStamina = maxStamina;

            if (sensorSuite == null)
                sensorSuite = FindFirstObjectByType<PlayerSensorSuite>();

            if (moveProvider == null)
                Debug.LogWarning("BreathManager: No MoveProvider assigned.");

            if (masterMixer != null)
            {
                masterMixer.SetFloat(ambienceParam, maxCutoff);
                masterMixer.SetFloat(musicParam, maxCutoff);
            }
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
            UpdateAudioEffects();

            _logTimer += Time.deltaTime;
            if (_logTimer >= 2.0f)
            {
                _logTimer = 0;
            }
        }

        public void SetFocusState(bool state)
        {
            float hPa = sensorSuite.GetPressureHPa();

            if (state && hPa < thinAirThreshold && !hasOxygenTank)
            {
                isFocusing = false;
                return;
            }

            isFocusing = state;
        }

        private void UpdateAudioEffects()
        {
            float staminaPercent = currentStamina / maxStamina;

            if (masterMixer != null)
            {
                float targetCutoff = Mathf.Lerp(minCutoff, maxCutoff, staminaPercent);
                masterMixer.SetFloat(ambienceParam, targetCutoff);
                masterMixer.SetFloat(musicParam, targetCutoff);
            }

            if (MountainRescue.Engine.AmbienceManager.Instance != null)
            {
                float targetVolume = Mathf.Lerp(0.3f, 1.0f, staminaPercent);
                MountainRescue.Engine.AmbienceManager.Instance.SetAmbienceVolume(targetVolume);
            }
        }

        private void HandleMovementLockout()
        {
            if (moveProvider == null) return;

            if (isFocusing && moveProvider.enabled)
            {
                moveProvider.enabled = false;
            }
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
            }
        }

        private void HandlePassiveDrain(float hPa)
        {
            bool isSuccessfullyFocusing = isFocusing && (hPa >= thinAirThreshold || hasOxygenTank);

            if (hPa < thinAirThreshold && !isSuccessfullyFocusing)
            {
                float drain = (drainRateByPressure != null && drainRateByPressure.length > 0)
                    ? drainRateByPressure.Evaluate(hPa)
                    : fallbackThinAirDrain;

                if (drain <= 0) drain = fallbackThinAirDrain;

                ModifyStamina(-drain * Time.deltaTime);
            }
        }

        public void UseOxygenTank()
        {
            if (currentTankFuel > 0)
            {
                currentTankFuel = Mathf.Max(0, currentTankFuel - (tankUsageCost * Time.deltaTime));
                hasOxygenTank = true;
            }
            else
            {
                hasOxygenTank = false;
            }
        }

        private void HandleRegeneration(float hPa)
        {
            bool inThinAir = hPa < thinAirThreshold;

            if (isFocusing)
            {
                if (!inThinAir || hasOxygenTank)
                {
                    ModifyStamina(focusRegenRate * Time.deltaTime);
                }
            }
            else if (!inThinAir && (Time.time - _timeSinceLastGrab > idleHangDelay))
            {
                ModifyStamina(idleHangRegenRate * Time.deltaTime);
            }
        }

        public bool TryConsumeStaminaForGrab()
        {
            if (sensorSuite == null) return true;

            float hPa = sensorSuite.GetPressureHPa();
            float pressureDrop = 700 - hPa;
            float penalty = Mathf.FloorToInt(Mathf.Max(0, pressureDrop) / penaltyHeight);
            float totalCost = baseGrabCost + penalty;

            if (currentStamina >= totalCost)
            {
                ModifyStamina(-totalCost);
                _timeSinceLastGrab = Time.time;
                return true;
            }

            onStaminaEmpty.Invoke();
            return false;
        }

        private void ModifyStamina(float amount)
        {
            float previousStamina = currentStamina;
            currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);

            if (Mathf.Abs(previousStamina - currentStamina) > 0.01f)
                onStaminaChanged.Invoke(currentStamina / maxStamina);

            bool isCurrentlyLow = currentStamina < lowStaminaThreshold;

            if (isCurrentlyLow != _isInLowStaminaState)
            {
                _isInLowStaminaState = isCurrentlyLow;
                onLowStaminaStateChanged.Invoke(_isInLowStaminaState);
            }

            if (currentStamina <= 0f)
            {
                TriggerStaminaDeath();
            }
        }

        private void TriggerStaminaDeath()
        {
            onPlayerDeath.Invoke();
        }

        public void ResetOnDeath()
        {
            currentStamina = maxStamina;
            currentTankFuel = maxTankFuel;
            hasOxygenTank = false;
            isFocusing = false;
            _isInLowStaminaState = false;
            _wasInThinAir = false;

            onStaminaChanged.Invoke(1f);
            onLowStaminaStateChanged.Invoke(false);
            onFocusStateChanged.Invoke(false);

            if (masterMixer != null)
            {
                masterMixer.SetFloat(ambienceParam, maxCutoff);
                masterMixer.SetFloat(musicParam, maxCutoff);
            }
        }

        private void CheckThinAirThreshold(float hPa)
        {
            bool inThinAir = hPa < thinAirThreshold;
            if (inThinAir && !_wasInThinAir)
            {
                onThinAirReached.Invoke();
                _wasInThinAir = true;
            }
            else if (!inThinAir)
            {
                _wasInThinAir = false;
            }
        }
    }
}