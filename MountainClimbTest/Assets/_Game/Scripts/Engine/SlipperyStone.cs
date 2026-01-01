using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

        private void Awake()
        {
            _interactable = GetComponent<XRBaseInteractable>();
        }

        private void OnEnable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.AddListener(OnGrab);
                _interactable.selectExited.AddListener(OnRelease);
            }
        }

        private void OnDisable()
        {
            if (_interactable != null)
            {
                _interactable.selectEntered.RemoveListener(OnGrab);
                _interactable.selectExited.RemoveListener(OnRelease);
            }
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            if (!CompareTag(_targetTag) || _isOnCooldown) return;

            if (_radialUIPrefab != null)
            {
                // 1. Instantiate the UI on the hand
                Transform handTransform = args.interactorObject.transform;
                _currentUI = Instantiate(_radialUIPrefab, handTransform);

                // 2. Find the specific "Fill" image by name to avoid grabbing the BG or Icon
                // We search through all images in the instantiated prefab
                Image[] allImages = _currentUI.GetComponentsInChildren<Image>(true);
                foreach (Image img in allImages)
                {
                    if (img.gameObject.name == "Fill")
                    {
                        _radialImage = img;
                        break;
                    }
                }

                // Fallback: If no object named "Fill" exists, try to find any Filled type image
                if (_radialImage == null)
                {
                    foreach (Image img in allImages)
                    {
                        if (img.type == Image.Type.Filled)
                        {
                            _radialImage = img;
                            break;
                        }
                    }
                }

                // 3. Reset Fill Amount
                if (_radialImage != null) _radialImage.fillAmount = 1f;

                // 4. Position UI
                _currentUI.transform.localPosition = new Vector3(0, 0.05f, 0);
                _currentUI.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }

            _gripCoroutine = StartCoroutine(GripTimer(args.manager));
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            CleanupUI();
        }

        private void CleanupUI()
        {
            if (_gripCoroutine != null)
            {
                StopCoroutine(_gripCoroutine);
                _gripCoroutine = null;
            }

            if (_currentUI != null)
            {
                Destroy(_currentUI);
                _radialImage = null;
            }
        }

        private IEnumerator GripTimer(XRInteractionManager manager)
        {
            float elapsed = 0f;

            while (elapsed < _maxGripTime)
            {
                elapsed += Time.deltaTime;

                if (_radialImage != null)
                {
                    // Updates the Fill Amount from 1.0 to 0.0
                    _radialImage.fillAmount = 1f - (elapsed / _maxGripTime);
                }

                yield return null;
            }

            CleanupUI();

            // Force Release (XRI 3.1)
            if (manager != null)
                manager.CancelInteractableSelection((IXRSelectInteractable)_interactable);

            StartCoroutine(CooldownTimer());
        }

        private IEnumerator CooldownTimer()
        {
            _isOnCooldown = true;
            InteractionLayerMask originalMask = _interactable.interactionLayers;
            _interactable.interactionLayers = 0;

            yield return new WaitForSeconds(_cooldownTime);

            _interactable.interactionLayers = originalMask;
            _isOnCooldown = false;
        }
    }
}