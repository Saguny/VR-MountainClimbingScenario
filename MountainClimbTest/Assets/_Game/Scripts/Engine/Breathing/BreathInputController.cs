using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace MountainRescue.Systems
{
    public class BreathInputController : MonoBehaviour
    {
        public BreathManager breathManager;
        public InputActionProperty focusAction; // X Button

        private Coroutine _focusRoutine;
        private float _focusDelay = 0.5f;

        void Update()
        {
            bool isButtonDown = focusAction.action.ReadValue<float>() > 0.1f;

            if (isButtonDown && _focusRoutine == null && !breathManager.isFocusing)
            {
                _focusRoutine = StartCoroutine(FocusDelayRoutine());
            }
            else if (!isButtonDown)
            {
                if (_focusRoutine != null)
                {
                    StopCoroutine(_focusRoutine);
                    _focusRoutine = null;
                }
                breathManager.isFocusing = false;
            }
        }

        private IEnumerator FocusDelayRoutine()
        {
            yield return new WaitForSeconds(_focusDelay);
            breathManager.isFocusing = true;
            _focusRoutine = null;
        }
    }
}