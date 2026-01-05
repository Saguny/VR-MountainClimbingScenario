using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MountainRescue.Systems
{
    [RequireComponent(typeof(AudioSource))] // Ensures we have an audio source
    public class BreathFeedback : MonoBehaviour
    {
        [Header("Rendering")]
        public Volume globalVolume;
        private Vignette _vignette;

        [Header("Low Stamina Settings (Warning)")]
        [ColorUsage(false, true)] public Color lowStaminaColor = Color.red;
        [Range(0f, 1f)] public float lowStaminaIntensity = 0.55f;
        public AudioClip lowStaminaClip; // Heavy breathing/Heartbeat

        [Header("Focus Settings (Concentration)")]
        [ColorUsage(false, true)] public Color focusColor = Color.white;
        [Range(0f, 1f)] public float focusIntensity = 0.35f;
        public AudioClip focusClip; // Deep breathing/Zen sound

        [Header("Audio Configuration")]
        [SerializeField] private float fadeSpeed = 2.0f;
        private AudioSource _audioSource;
        private float _targetVolume;

        // State tracking
        private bool _isLowStamina;
        private bool _isFocusing;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.loop = true; // Breathing sounds should usually loop
            _audioSource.playOnAwake = false;
            _audioSource.volume = 0;

            if (globalVolume.profile.TryGet(out _vignette))
            {
                _vignette.active = false;

                // Ensure overrides are enabled so we can control them via code
                _vignette.intensity.overrideState = true;
                _vignette.color.overrideState = true;
                _vignette.smoothness.overrideState = true;

                _vignette.smoothness.value = 1.0f; // Soft edges
            }
        }

        private void Update()
        {
            // Smoothly fade audio volume in/out
            if (_audioSource.isPlaying)
            {
                _audioSource.volume = Mathf.MoveTowards(_audioSource.volume, _targetVolume, Time.deltaTime * fadeSpeed);

                // If volume hits 0, actually stop the clip
                if (_audioSource.volume <= 0.01f && _targetVolume == 0)
                {
                    _audioSource.Stop();
                }
            }
        }

        public void ToggleLowStaminaVignette(bool active)
        {
            if (_isLowStamina == active) return; // No change
            _isLowStamina = active;
            UpdateFeedbackState();
        }

        public void ToggleFocusVignette(bool active)
        {
            if (_isFocusing == active) return; // No change
            _isFocusing = active;
            UpdateFeedbackState();
        }

        private void UpdateFeedbackState()
        {
            if (_vignette == null) return;

            // PRIORITY LOGIC: Low Stamina overrides Focus
            if (_isLowStamina)
            {
                // Visuals
                _vignette.active = true;
                _vignette.color.value = lowStaminaColor;
                _vignette.intensity.value = lowStaminaIntensity;

                // Audio
                PlayClip(lowStaminaClip);
            }
            else if (_isFocusing)
            {
                // Visuals
                _vignette.active = true;
                _vignette.color.value = focusColor;
                _vignette.intensity.value = focusIntensity;

                // Audio
                PlayClip(focusClip);
            }
            else
            {
                // Visuals: Turn off
                _vignette.active = false;

                // Audio: Fade out
                _targetVolume = 0f;
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip == null) return;

            // If we are already playing this clip, just ensure volume is up
            if (_audioSource.clip == clip && _audioSource.isPlaying)
            {
                _targetVolume = 1.0f;
                return;
            }

            // Swap clips
            _audioSource.clip = clip;
            _audioSource.volume = 0f; // Start at 0 to fade in
            _audioSource.Play();
            _targetVolume = 1.0f;
        }
    }
}