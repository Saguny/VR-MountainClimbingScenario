using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace MountainRescue.Systems
{
    public class BreathInputController : MonoBehaviour
    {
        public BreathManager breathManager;
        public InputActionProperty focusAction;

        [SerializeField] private float focusDelay = 0.5f;

        private Coroutine focusRoutine;

        private void OnEnable()
        {
            focusAction.action.started += OnFocusStarted;
            focusAction.action.canceled += OnFocusCanceled;
            focusAction.action.Enable();
        }

        private void OnDisable()
        {
            focusAction.action.started -= OnFocusStarted;
            focusAction.action.canceled -= OnFocusCanceled;
            focusAction.action.Disable();
        }

        private void OnFocusStarted(InputAction.CallbackContext ctx)
        {
            if (focusRoutine == null && !breathManager.isFocusing)
            {
                focusRoutine = StartCoroutine(FocusDelayRoutine());
            }
        }

        private void OnFocusCanceled(InputAction.CallbackContext ctx)
        {
            if (focusRoutine != null)
            {
                StopCoroutine(focusRoutine);
                focusRoutine = null;
            }

            if (breathManager.isFocusing)
            {
                breathManager.SetFocusState(false);
            }
        }

        private IEnumerator FocusDelayRoutine()
        {
            yield return new WaitForSecondsRealtime(focusDelay);
            breathManager.SetFocusState(true);
            focusRoutine = null;
        }

    }
}
