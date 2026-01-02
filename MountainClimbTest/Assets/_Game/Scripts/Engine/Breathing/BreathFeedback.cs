using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MountainRescue.Systems
{
    public class BreathFeedback : MonoBehaviour
    {
        public Volume globalVolume;
        private Vignette _vignette;

        private bool _isLowStamina;
        private bool _isFocusing;

        private void Awake()
        {
            if (globalVolume.profile.TryGet(out _vignette))
            {
                _vignette.active = false;
                _vignette.intensity.overrideState = true;
                _vignette.intensity.value = 0.6f;
                _vignette.smoothness.value = 1.0f;
            }
        }

        public void ToggleLowStaminaVignette(bool active)
        {
            _isLowStamina = active;
            UpdateVignetteState();
        }

        public void ToggleFocusVignette(bool active)
        {
            _isFocusing = active;
            UpdateVignetteState();
        }

        private void UpdateVignetteState()
        {
            if (_vignette != null)
            {
                // Active if we are low on breath OR focusing
                _vignette.active = _isLowStamina || _isFocusing;
            }
        }
    }
}