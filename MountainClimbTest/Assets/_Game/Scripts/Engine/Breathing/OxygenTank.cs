using MountainRescue.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // Use .Interactables for XRI 3.x
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class OxygenTank : MonoBehaviour
{
    [Header("Dependencies")]
    public BreathManager breathManager;
    public Transform gaugeNeedle;
    public TextMeshProUGUI warningDisplay;
    public Animator handAnimator; // Kept to preserve your references

    [Header("New Interaction Settings")]
    [Tooltip("Assign the Main Camera (Head) here.")]
    public Transform playerHead;

    [Tooltip("How close (in meters) the mask must be to the head.")]
    public float mouthDistanceThreshold = 0.25f;

    [Header("Input")]
    [Tooltip("Bind this to Left Hand Interaction -> Secondary Button (Y Button)")]
    public InputActionProperty focusInput;

    [Header("Gauge Constraints (Preserved)")]
    private const float MAX_Z = 156f;
    private const float MIN_Z = -141f;
    private const float FIXED_X = -56.522f;
    private const float FIXED_Y = -14.503f;

    // State
    private XRGrabInteractable _grabInteractable;
    private bool _isHeld = false;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();

        // Auto-find player head if not assigned manually
        if (playerHead == null && Camera.main != null)
        {
            playerHead = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnGrab);
            _grabInteractable.selectExited.AddListener(OnRelease);
        }

        if (focusInput.action != null)
            focusInput.action.Enable();
    }

    private void OnDisable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
            _grabInteractable.selectExited.RemoveListener(OnRelease);
        }

        if (focusInput.action != null)
            focusInput.action.Disable();
    }

    private void Update()
    {
        HandleOxygenLogic();
        UpdateGauge();
    }

    private void HandleOxygenLogic()
    {
        if (breathManager == null) return;

        // 1. Must be held in hand (not in a socket)
        if (!_isHeld)
        {
            StopOxygenEffects();
            return;
        }

        // 2. Must be pressing Y Button
        bool isPressed = focusInput.action != null && focusInput.action.IsPressed();
        if (!isPressed)
        {
            StopOxygenEffects();
            return;
        }

        // 3. Must be near mouth
        if (!IsNearMouth())
        {
            StopOxygenEffects();
            return;
        }

        // --- SUCCESS CONDITION ---

        // Check if we actually have fuel
        if (breathManager.currentTankFuel > 0)
        {
            // Consume fuel (BreathManager handles the subtraction via UseOxygenTank)
            breathManager.UseOxygenTank();

            // Enable Focus (Regenerates Stamina)
            breathManager.SetFocusState(true);

            // Clear warning if valid
            if (warningDisplay != null) warningDisplay.text = "";
        }
        else
        {
            // Fuel Empty
            StopOxygenEffects();
            if (warningDisplay != null) warningDisplay.text = "O2 TANK EMPTY";
        }
    }

    private void StopOxygenEffects()
    {
        if (breathManager == null) return;

        // Only reset if specifically currently using this tank
        if (breathManager.hasOxygenTank)
        {
            breathManager.hasOxygenTank = false;

            // Also stop focusing if we were focusing
            if (breathManager.isFocusing)
            {
                breathManager.SetFocusState(false);
            }
        }
    }

    private bool IsNearMouth()
    {
        if (playerHead == null) return false;
        return Vector3.Distance(transform.position, playerHead.position) <= mouthDistanceThreshold;
    }

    // --- Interaction Events ---

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Check if we grabbed it with a hand, not a socket (belt)
        if (args.interactorObject is not XRSocketInteractor)
        {
            _isHeld = true;
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // When dropped or placed in socket
        if (args.interactorObject is not XRSocketInteractor)
        {
            _isHeld = false;
            StopOxygenEffects();
        }
    }

    // --- Visuals ---

    private void UpdateGauge()
    {
        if (gaugeNeedle == null || breathManager == null) return;

        // Use current fuel from BreathManager
        float fuelPercent = breathManager.currentTankFuel / 100f;

        // Use your specific constraint values
        float targetZ = Mathf.Lerp(MIN_Z, MAX_Z, fuelPercent);

        gaugeNeedle.localRotation = Quaternion.Euler(FIXED_X, FIXED_Y, targetZ);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the mouth distance in Editor
        if (playerHead != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerHead.position, mouthDistanceThreshold);
        }
    }
}