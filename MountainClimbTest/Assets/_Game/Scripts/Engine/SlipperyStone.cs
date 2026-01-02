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
            if (!CompareTag(_targetTag) || _isOnCooldown)
            {
                ForceRelease(args.manager, args.interactorObject);
                return;
            }

            if (_breathManager != null)
            {
                if (!_breathManager.TryConsumeStaminaForGrab())
                {
                    ForceRelease(args.manager, args.interactorObject);
                    return;
                }
            }

            // --- IMPROVED UI LOGIC FROM OLD SCRIPT ---
            CleanupUI();
            if (_radialUIPrefab != null)
            {
                // Attach UI to the HAND so it follows movement
                Transform handTransform = args.interactorObject.transform;
                _currentUI = Instantiate(_radialUIPrefab, handTransform);

                // Find the specific "Fill" image in the prefab
                Image[] allImages = _currentUI.GetComponentsInChildren<Image>(true);
                foreach (Image img in allImages)
                {
                    if (img.gameObject.name == "Fill")
                    {
                        _radialImage = img;
                        break;
                    }
                }

                // UI Offset so it's visible above the hand
                _currentUI.transform.localPosition = new Vector3(0, 0.05f, 0);
                _currentUI.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            // ----------------------------------------

            _gripCoroutine = StartCoroutine(GripTimer(args.manager, args.interactorObject));
        }

        private void ForceRelease(XRInteractionManager manager, IXRSelectInteractor interactor)
        {
            if (_interactable is IXRSelectInteractable selectInteractable)
            {
                manager.CancelInteractableSelection(selectInteractable);
            }

            if (interactor is XRBaseInteractor baseInteractor)
            {
                StartCoroutine(TemporaryHandKill(baseInteractor));
            }
        }

        private IEnumerator TemporaryHandKill(XRBaseInteractor interactor)
        {
            var layers = interactor.interactionLayers;
            interactor.interactionLayers = 0;
            interactor.allowSelect = false;
            yield return new WaitForSeconds(0.2f);
            interactor.interactionLayers = layers;
            interactor.allowSelect = true;
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

            ForceRelease(manager, interactor);
            CleanupUI();
            StartCoroutine(CooldownTimer());
        }

        private IEnumerator CooldownTimer()
        {
            _isOnCooldown = true;
            var oldMask = _interactable.interactionLayers;
            _interactable.interactionLayers = 0;
            yield return new WaitForSeconds(_cooldownTime);
            _interactable.interactionLayers = oldMask;
            _isOnCooldown = false;
        }
    }
}