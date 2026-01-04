using UnityEngine;
using System.Collections;
using MountainRescue.Interfaces;
using MountainRescue.UI;
using MountainRescue.Engine;

namespace MountainRescue.Systems.Safety
{
    public class FallSafetySystem : MonoBehaviour
    {
        [Header("Fall Logic")]
        [SerializeField] private float fallThresholdMeters = 5.0f;
        [SerializeField] private Transform respawnPoint;

        [Header("Timing")]
        [SerializeField] private float blackoutDuration = 2.0f;
        [SerializeField] private float recoveryFadeSpeed = 2.0f;

        [Header("Dependencies")]
        [SerializeField] private Transform playerHead;
        [SerializeField] private HeadsetFader fader;
        [SerializeField] private GameObject anchorSystemObject;

        private CharacterController playerController;
        private IAnchorStateProvider _anchorProvider;

        private Vector3 _highestPoint;
        private bool _isAirborne;
        private bool _isConsequenceActive;

        private void OnEnable()
        {
            DynamicSceneSwitcher.OnCharacterControllerRebuilt += SetCharacterController;
        }

        private void OnDisable()
        {
            DynamicSceneSwitcher.OnCharacterControllerRebuilt -= SetCharacterController;
        }

        private void Start()
        {
            if (anchorSystemObject != null)
                _anchorProvider = anchorSystemObject.GetComponent<IAnchorStateProvider>();

            if (playerHead == null && Camera.main != null)
                playerHead = Camera.main.transform;

            // initial fallback
            playerController = GetComponentInParent<CharacterController>();
        }

        private void SetCharacterController(CharacterController newCC)
        {
            playerController = newCC;
        }

        private void Update()
        {
            if (_isConsequenceActive || playerController == null)
                return;

            bool isGrounded =
                playerController.isGrounded ||
                playerController.velocity.y > -0.1f;

            if (isGrounded)
            {
                if (_isAirborne)
                    CheckImpact();

                _isAirborne = false;
            }
            else
            {
                if (!_isAirborne)
                {
                    _isAirborne = true;
                    _highestPoint = playerHead.position;
                }
                else if (playerHead.position.y > _highestPoint.y)
                {
                    _highestPoint = playerHead.position;
                }
            }
        }

        private void CheckImpact()
        {
            float totalDrop = _highestPoint.y - playerHead.position.y;
            if (totalDrop < fallThresholdMeters) return;
            if (_anchorProvider != null && _anchorProvider.IsAnchored()) return;

            StartCoroutine(ImpactSequence());
        }

        private IEnumerator ImpactSequence()
        {
            _isConsequenceActive = true;

            fader.SnapToBlack();
            yield return new WaitForSeconds(blackoutDuration);

            if (respawnPoint != null && playerController != null)
            {
                playerController.enabled = false;

                Transform rig = playerController.transform;
                rig.position = respawnPoint.position;
                rig.rotation = respawnPoint.rotation;

                playerController.enabled = true;
            }

            _isAirborne = false;
            _highestPoint = playerHead.position;

            yield return fader.FadeIn(recoveryFadeSpeed);

            _isConsequenceActive = false;
        }
    }
}
