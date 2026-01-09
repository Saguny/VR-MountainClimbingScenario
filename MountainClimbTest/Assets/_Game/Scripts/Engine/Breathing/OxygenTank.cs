using MountainRescue.Systems;
using MountainRescue.Systems.Session; // Added for GameSessionManager
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class OxygenTank : MonoBehaviour
{
    [Header("Dependencies")]
    public BreathManager breathManager;
    public Transform gaugeNeedle;
    public TextMeshProUGUI warningDisplay;
    public Animator handAnimator;

    [Header("Interaction Settings")]
    [Tooltip("Assign the Main Camera (Head) here.")]
    public Transform playerHead;

    [Tooltip("Optional: Assign manually or rely on Tag 'VictimHead'")]
    public Transform victimHead;

    [Tooltip("How close (in meters) the mask must be to the head.")]
    public float mouthDistanceThreshold = 0.25f;

    [Header("Input")]
    [Tooltip("Bind this to Left Hand Interaction -> Secondary Button (Y Button)")]
    public InputActionProperty focusInput;

    [Header("Rescue Logic")]
    [SerializeField] private float oxygenTransferRate = 10.0f; // Oxygen per second for victim

    [Header("Gauge Constraints")]
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
        // 1. Auto-find victim if missing (e.g. scene change)
        if (victimHead == null)
        {
            GameObject vObj = GameObject.FindGameObjectWithTag("VictimHead");
            if (vObj != null) victimHead = vObj.transform;
        }

        HandleOxygenLogic();
        UpdateGauge();
    }

    private void HandleOxygenLogic()
    {
        if (breathManager == null) return;

        // 1. Must be held
        if (!_isHeld)
        {
            StopOxygenEffects();
            return;
        }

        // 2. Must be pressing Input
        // ReadValue > 0.5f works for both Triggers(float) and Buttons(bool)
        bool isPressed = focusInput.action != null &&
                        (focusInput.action.IsPressed() || focusInput.action.ReadValue<float>() > 0.5f);

        if (!isPressed)
        {
            StopOxygenEffects();
            return;
        }

        // 3. Distance Checks
        if (IsNearMouth())
        {
            // --- PLAYER SELF-HEAL ---
            if (breathManager.currentTankFuel > 0)
            {
                breathManager.UseOxygenTank(); // Handles local logic
                breathManager.SetFocusState(true);

                if (handAnimator) handAnimator.SetBool("IsPumping", true);
                if (warningDisplay) warningDisplay.text = "";
            }
            else
            {
                StopOxygenEffects();
                if (warningDisplay) warningDisplay.text = "TANK EMPTY";
            }
        }
        else if (IsNearVictim())
        {
            // --- VICTIM RESCUE ---
            if (breathManager.currentTankFuel > 0)
            {
                float amount = oxygenTransferRate * Time.deltaTime;

                // Deplete local tank
                breathManager.currentTankFuel -= amount;

                // Send to Game Manager
                if (GameSessionManager.Instance != null)
                {
                    GameSessionManager.Instance.RegisterVictimOxygen(amount);
                }

                if (handAnimator) handAnimator.SetBool("IsPumping", true);
                if (warningDisplay) warningDisplay.text = "RESCUING...";
            }
            else
            {
                StopOxygenEffects();
                if (warningDisplay) warningDisplay.text = "TANK EMPTY";
            }
        }
        else
        {
            // Button pressed but too far from anyone
            StopOxygenEffects();
        }
    }

    private void StopOxygenEffects()
    {
        if (breathManager == null) return;

        if (breathManager.hasOxygenTank)
        {
            breathManager.hasOxygenTank = false;
            if (breathManager.isFocusing) breathManager.SetFocusState(false);
        }

        if (handAnimator) handAnimator.SetBool("IsPumping", false);
        if (warningDisplay) warningDisplay.text = "";
    }

    private bool IsNearMouth()
    {
        if (playerHead == null) return false;
        return Vector3.Distance(transform.position, playerHead.position) <= mouthDistanceThreshold;
    }

    private bool IsNearVictim()
    {
        if (victimHead == null) return false;
        return Vector3.Distance(transform.position, victimHead.position) <= mouthDistanceThreshold;
    }

    // --- Interaction Events ---

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (args.interactorObject is not XRSocketInteractor)
        {
            _isHeld = true;
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
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

        float fuelPercent = breathManager.currentTankFuel / 100f;
        float targetZ = Mathf.Lerp(MIN_Z, MAX_Z, fuelPercent);

        gaugeNeedle.localRotation = Quaternion.Euler(FIXED_X, FIXED_Y, targetZ);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, mouthDistanceThreshold);

        if (playerHead != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerHead.position, mouthDistanceThreshold);
        }
    }
}