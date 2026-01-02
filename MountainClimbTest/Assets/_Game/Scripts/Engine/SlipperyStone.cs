using MountainRescue.Systems;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Game.Mechanics
{
    public class SlipperyStone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string _targetTag = "SlipperyStone";
        [SerializeField] private float _maxGripTime = 5f;
        [SerializeField] private float _cooldownTime = 5f;

        [Header("UI Reference")]
        [SerializeField] private GameObject _radialUIPrefab;

        private XRBaseInteractable _interactable;
        private Coroutine _gripCoroutine;
        private bool _isOnCooldown;
        private GameObject _currentUI;
        private Image _radialImage;
        private BreathManager _breathManager;

        private void Awake()
        {
            _interactable = GetComponent<XRBaseInteractable>();
            _breathManager = FindFirstObjectByType<BreathManager>();
        }

        private void OnEnable()
        {
            _interactable.selectEntered.AddListener(OnGrab);
            _interactable.selectExited.AddListener(OnRelease);
        }

        private void OnDisable()
        {
            _interactable.selectEntered.RemoveListener(OnGrab);
            _interactable.selectExited.RemoveListener(OnRelease);
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            // 1. Immediate rejection if cooldown or wrong tag
            if (!CompareTag(_targetTag) || _isOnCooldown)
            {
                ForceRelease(args.manager, args.interactorObject);
                return;
            }

            // 2. Stamina Check
            if (_breathManager != null)
            {
                if (!_breathManager.TryConsumeStaminaForGrab())
                {
                    ForceRelease(args.manager, args.interactorObject);
                    return;
                }
            }

            // 3. Success logic
            CleanupUI();
            if (_radialUIPrefab != null)
            {
                _currentUI = Instantiate(_radialUIPrefab, transform);
                _radialImage = _currentUI.GetComponentInChildren<Image>();
            }

            _gripCoroutine = StartCoroutine(GripTimer(args.manager, args.interactorObject));
        }

        private void ForceRelease(XRInteractionManager manager, IXRSelectInteractor interactor)
        {
            // This tells the manager to break the bond between THIS hand and THIS stone
            manager.CancelInteractableSelection((IXRSelectInteractable)interactor);
        }

        private void OnRelease(SelectExitEventArgs args) => CleanupUI();

        private void CleanupUI()
        {
            if (_gripCoroutine != null) { StopCoroutine(_gripCoroutine); _gripCoroutine = null; }
            if (_currentUI != null) { Destroy(_currentUI); _radialImage = null; }
        }

        private IEnumerator GripTimer(XRInteractionManager manager, IXRSelectInteractor interactor)
        {
            float elapsed = 0f;
            while (elapsed < _maxGripTime)
            {
                elapsed += Time.deltaTime;
                if (_radialImage != null) _radialImage.fillAmount = 1f - (elapsed / _maxGripTime);
                yield return null;
            }

            CleanupUI();
            ForceRelease(manager, interactor);
            StartCoroutine(CooldownTimer());
        }

        private IEnumerator CooldownTimer()
        {
            _isOnCooldown = true;
            var oldMask = _interactable.interactionLayers;
            _interactable.interactionLayers = 0; // Disable all interactions
            yield return new WaitForSeconds(_cooldownTime);
            _interactable.interactionLayers = oldMask;
            _isOnCooldown = false;
        }
    }
}