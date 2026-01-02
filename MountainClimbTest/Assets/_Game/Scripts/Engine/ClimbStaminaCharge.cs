using MountainRescue.Systems;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Game.Mechanics
{
    public class ClimbStaminaCharge : MonoBehaviour
    {
        private IXRSelectInteractor _interactor;
        private BreathManager _breathManager;

        private void Awake()
        {
            _interactor = GetComponent<IXRSelectInteractor>();
            _breathManager = FindFirstObjectByType<BreathManager>();
        }

        private void OnEnable() => _interactor.selectEntered.AddListener(OnSelectEntered);
        private void OnDisable() => _interactor.selectEntered.RemoveListener(OnSelectEntered);

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactableObject.transform.CompareTag("climbableObjects"))
            {
                if (_breathManager != null)
                {
                    // If we fail to consume stamina, force the interactor to let go immediately
                    if (!_breathManager.TryConsumeStaminaForGrab())
                    {
                        args.manager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)_interactor);
                    }
                }
            }
        }
    }
}