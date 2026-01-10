using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace MountainRescue.Systems
{
    [RequireComponent(typeof(AudioSource))]
    public class BreathFeedback : MonoBehaviour
    {
        [Header("Global References")]
        [Tooltip("The Global Volume in the scene (Must have Vignette, Chromatic Aberration, Bloom, & Film Grain).")]
        public Volume globalVolume;

        [Header("Low Stamina (The Struggle)")]
        [ColorUsage(false, true)]
        public Color panicColor = new Color(1f, 0f, 0f, 1f); // Red bloodshot eyes
        public AudioClip panicBreathClip;
        public AudioClip heartbeatClip;

        [Space(10)]
        [Range(0f, 1f)] public float panicVignetteIntensity = 0.6f;
        [Range(0f, 5f)] public float panicAberrationIntensity = 1.0f; // Increased for stronger dizzy effect
        [Range(0f, 50f)] public float panicBloomIntensity = 15.0f; // Flashes of bright light
        [Range(0f, 1f)] public float panicGrainIntensity = 0.8f; // Visual "static/noise"

        [Tooltip("Pulses per second")]
        public float heartbeatSpeed = 1.7f;

        [Header("Focus (Flow State)")]
        [ColorUsage(false, true)]
        public Color focusColor = new Color(1f, 1f, 1f, 1f); // Pure White "Frost" effect
        public AudioClip focusBreathClip;
        [Range(0f, 1f)] public float focusVignetteIntensity = 0.55f;

        [Header("Audio Immersion")]
        public AudioMixer mainMixer;
        public string mixerCutoffParam = "AmbienceLowPass";
        public float normalFreq = 22000f;
        public float muffledFreq = 600f;
        public float audioFadeSpeed = 2.0f;

        [Header("Haptics")]
        public XRBaseInputInteractor leftHand;
        public XRBaseInputInteractor rightHand;

        // Internal State
        private AudioSource _breathSource;
        private AudioSource _heartbeatSource;

        // Volume Components
        private Vignette _vignette;
        private ChromaticAberration _aberration;
        private Bloom _bloom;
        private FilmGrain _filmGrain;

        private float _targetBreathVol;
        private float _targetHeartVol;
        private bool _isLowStamina;
        private bool _isFocusing;

        // Pulse Math
        private float _pulseTimer;
        private float _defaultBloomIntensity; // To return to normal after panic

        private void Awake()
        {
            SetupAudio();
            SetupPostProcessing();
        }

        private void SetupAudio()
        {
            _breathSource = GetComponent<AudioSource>();
            _breathSource.loop = true;
            _breathSource.playOnAwake = false;
            _breathSource.volume = 0;

            _heartbeatSource = gameObject.AddComponent<AudioSource>();
            _heartbeatSource.clip = heartbeatClip;
            _heartbeatSource.loop = true;
            _heartbeatSource.playOnAwake = false;
            _heartbeatSource.volume = 0;
            _heartbeatSource.spatialBlend = 0; // 2D Sound (Head)
        }

        private void SetupPostProcessing()
        {
            if (globalVolume == null) return;

            // Get Components
            if (!globalVolume.profile.TryGet(out _vignette))
                Debug.LogWarning("[BreathFeedback] Missing Vignette override!");

            if (!globalVolume.profile.TryGet(out _aberration))
                Debug.LogWarning("[BreathFeedback] Missing Chromatic Aberration override!");

            if (!globalVolume.profile.TryGet(out _bloom))
                Debug.LogWarning("[BreathFeedback] Missing Bloom override!");
            else
                _defaultBloomIntensity = _bloom.intensity.value; // Remember the scene's normal bloom

            if (!globalVolume.profile.TryGet(out _filmGrain))
                Debug.LogWarning("[BreathFeedback] Missing Film Grain override!");

            ResetEffects();
        }

        private void Update()
        {
            HandleAudioFading();
            HandleVisualsAndHaptics();
        }

        // -----------------------------------------------------------------------
        // LOGIC
        // -----------------------------------------------------------------------

        private void HandleAudioFading()
        {
            // Fade Volumes
            _breathSource.volume = Mathf.MoveTowards(_breathSource.volume, _targetBreathVol, Time.deltaTime * audioFadeSpeed);
            if (_breathSource.volume <= 0.01f && _targetBreathVol == 0) _breathSource.Stop();

            _heartbeatSource.volume = Mathf.MoveTowards(_heartbeatSource.volume, _targetHeartVol, Time.deltaTime * audioFadeSpeed);
            if (_heartbeatSource.volume <= 0.01f && _targetHeartVol == 0) _heartbeatSource.Stop();

            // Handle Mixer Muffling
            if (mainMixer != null)
            {
                mainMixer.GetFloat(mixerCutoffParam, out float currentFreq);
                float targetFreq = _isLowStamina ? muffledFreq : normalFreq;
                float newFreq = Mathf.Lerp(currentFreq, targetFreq, Time.deltaTime * 3f);
                mainMixer.SetFloat(mixerCutoffParam, newFreq);
            }
        }

        private void HandleVisualsAndHaptics()
        {
            if (_vignette == null) return;

            if (_isLowStamina)
            {
                // -- PANIC MODE (Throbbing Red + Distorted + Bright Flashes) --
                _pulseTimer += Time.deltaTime * heartbeatSpeed;

                // Sharp Pulse: rapid rise, slow fall
                float pulse = (Mathf.Sin(_pulseTimer * Mathf.PI * 2) + 1f) * 0.5f;

                // 1. Vignette (Throb Red)
                _vignette.active = true;
                _vignette.color.value = panicColor;
                float targetVig = Mathf.Lerp(panicVignetteIntensity * 0.7f, panicVignetteIntensity, pulse);
                _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, targetVig, Time.deltaTime * 10f);

                // 2. Chromatic Aberration (Dizziness)
                if (_aberration)
                {
                    _aberration.active = true;
                    // Syncs with pulse to feel like a headache throb
                    _aberration.intensity.value = Mathf.Lerp(0.2f, panicAberrationIntensity, pulse);
                }

                // 3. Bloom (Light Sensitivity/Headache)
                if (_bloom)
                {
                    _bloom.active = true;
                    // Flashes brighter on the heartbeat
                    float flashIntensity = _defaultBloomIntensity + (panicBloomIntensity * pulse);
                    _bloom.intensity.value = Mathf.Lerp(_bloom.intensity.value, flashIntensity, Time.deltaTime * 8f);
                }

                // 4. Film Grain (Fainting/Visual Noise)
                if (_filmGrain)
                {
                    _filmGrain.active = true;
                    _filmGrain.intensity.value = Mathf.Lerp(_filmGrain.intensity.value, panicGrainIntensity, Time.deltaTime * 2f);
                }

                // 5. Haptics
                if (pulse > 0.95f)
                {
                    SendHapticPulse(0.7f, 0.15f); // Stronger thud
                }
            }
            else if (_isFocusing)
            {
                // -- FOCUS STATE (Steady White Frost) --
                _vignette.active = true;
                _vignette.color.value = focusColor; // White
                _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, focusVignetteIntensity, Time.deltaTime * 2f);

                // Disable Chaos Effects
                if (_aberration) _aberration.active = false;
                if (_filmGrain) _filmGrain.active = false;
                if (_bloom) _bloom.intensity.value = Mathf.Lerp(_bloom.intensity.value, _defaultBloomIntensity, Time.deltaTime * 2f);
            }
            else
            {
                // -- NORMAL STATE --
                // Cleanup Vignette
                if (_vignette.intensity.value > 0.01f)
                    _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, 0f, Time.deltaTime * 5f);
                else
                    _vignette.active = false;

                // Cleanup Chaos Effects
                if (_aberration) _aberration.active = false;
                if (_filmGrain) _filmGrain.active = false;
                if (_bloom) _bloom.intensity.value = Mathf.Lerp(_bloom.intensity.value, _defaultBloomIntensity, Time.deltaTime * 2f);
            }
        }

        private void SendHapticPulse(float amplitude, float duration)
        {
            if (leftHand != null) leftHand.SendHapticImpulse(amplitude, duration);
            if (rightHand != null) rightHand.SendHapticImpulse(amplitude, duration);
        }

        private void ResetEffects()
        {
            if (_vignette) _vignette.active = false;
            if (_aberration) _aberration.active = false;
            if (_filmGrain) _filmGrain.active = false;
            if (_bloom) _bloom.intensity.value = _defaultBloomIntensity;
            if (mainMixer != null) mainMixer.SetFloat(mixerCutoffParam, normalFreq);
        }

        // -----------------------------------------------------------------------
        // PUBLIC API
        // -----------------------------------------------------------------------

        public void SetLowStaminaState(bool active)
        {
            if (_isLowStamina == active) return;
            _isLowStamina = active;
            UpdateAudioState();
        }

        public void SetFocusState(bool active)
        {
            if (_isFocusing == active) return;
            _isFocusing = active;
            UpdateAudioState();
        }

        private void UpdateAudioState()
        {
            if (_isLowStamina)
            {
                _targetBreathVol = 1.0f;
                _targetHeartVol = 1.0f;
                SwitchBreathClip(panicBreathClip);
                if (!_heartbeatSource.isPlaying) _heartbeatSource.Play();
            }
            else if (_isFocusing)
            {
                _targetBreathVol = 1.0f;
                _targetHeartVol = 0.0f;
                SwitchBreathClip(focusBreathClip);
            }
            else
            {
                _targetBreathVol = 0.0f;
                _targetHeartVol = 0.0f;
            }
        }

        private void SwitchBreathClip(AudioClip clip)
        {
            if (clip == null) return;
            if (_breathSource.clip == clip && _breathSource.isPlaying) return;

            _breathSource.clip = clip;
            _breathSource.time = 0f;
            _breathSource.Play();
        }

        private void OnDisable()
        {
            ResetEffects();
        }
    }
}