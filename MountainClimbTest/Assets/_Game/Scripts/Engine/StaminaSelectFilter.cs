using MountainRescue.Systems;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class StaminaSelectFilter : MonoBehaviour, IXRSelectFilter
{
    private BreathManager _breathManager;
    private const float GRAB_COST = 3.0f; // Set this to match your BreathManager's cost

    public bool canProcess => true;

    private void Awake()
    {
        _breathManager = FindFirstObjectByType<BreathManager>();
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
    {
        if (interactable.transform.CompareTag("SlipperyStone") || interactable.transform.CompareTag("climbableObjects"))
        {
            // BLOCK THE GRAB if stamina is less than the actual cost
            if (_breathManager != null && _breathManager.currentStamina < GRAB_COST)
            {
                return false; // Hand ignores the rock completely
            }
        }
        return true;
    }
}