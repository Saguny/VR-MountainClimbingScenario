using MountainRescue.Systems;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class StaminaSelectFilter : MonoBehaviour, IXRSelectFilter
{
    private BreathManager _breathManager;

    public bool canProcess => true;

    private void Awake()
    {
        _breathManager = FindFirstObjectByType<BreathManager>();
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        // Only filter climbable objects
        if (interactable.transform.CompareTag("SlipperyStone") || interactable.transform.CompareTag("climbableObjects"))
        {
            // If we don't have at least 2 stamina, block the interaction
            if (_breathManager != null && _breathManager.currentStamina < 2f)
            {
                return false; // The hand will simply pass through the rock
            }
        }
        return true;
    }
}