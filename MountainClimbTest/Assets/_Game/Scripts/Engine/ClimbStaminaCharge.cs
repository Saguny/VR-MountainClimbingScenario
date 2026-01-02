using MountainRescue.Systems;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Game.Mechanics
{
    public class ClimbStaminaCharge : MonoBehaviour
    {
        private XRBaseInteractor _interactor;
        private BreathManager _breathManager;
        private InteractionLayerMask _originalLayers;
        private bool _isExhausted;

        private void Awake()
        {
            _interactor = GetComponent<XRBaseInteractor>();
            _breathManager = FindFirstObjectByType<BreathManager>();
            _originalLayers = _interactor.interactionLayers;
        }

        private void Update()
        {
            if (_breathManager == null) return;

            // Check if we are below the "minimum required to hold on"
            // Catching it at 0.5 or 1.0 is safer than waiting for absolute 0
            if (_breathManager.currentStamina < 0.5f && !_isExhausted)
            {
                StartCoroutine(ExhaustionRoutine());
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactableObject.transform.CompareTag("climbableObjects"))
            {
                if (_breathManager != null)
                {
                    // CHANGE: Call TryConsumeStaminaForGrab() which handles the subtraction logic
                    if (!_breathManager.TryConsumeStaminaForGrab())
                    {
                        // Force release if they don't have enough stamina to even start the grab
                        args.manager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)_interactor);
                    }
                }
            }
        }

        private IEnumerator ExhaustionRoutine()
        {
            _isExhausted = true;

            // 1. FORCE DROP: Cancel the selection of the object being held
            if (_interactor.interactablesSelected.Count > 0)
            {
                var heldObject = _interactor.interactablesSelected[0];
                _interactor.interactionManager.CancelInteractableSelection(heldObject);
            }

            // 2. DISABLE INTERACTION: Strip layers and disable interactor
            _interactor.interactionLayers = 0;
            _interactor.allowSelect = false;

            // 3. WAIT FOR RECOVERY: Stay disabled until stamina is safe (e.g., 5.0)
            // This prevents the hand from "sticky re-grabbing" while falling
            while (_breathManager.currentStamina < 5.0f)
            {
                yield return null;
            }

            // 4. RESTORE: Only after recovery is finished
            _interactor.interactionLayers = _originalLayers;
            _interactor.allowSelect = true;
            _isExhausted = false;
        }
    }
}