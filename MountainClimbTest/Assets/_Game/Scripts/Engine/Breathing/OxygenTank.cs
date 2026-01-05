using MountainRescue.Systems;
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

    [Header("Input")]
    public InputActionProperty useTankAction;

    [Header("Oxygen Settings")]
    public float currentTankFuel = 100f;
    public float usageCost = 5f;
    public string emptyTankMessage = "O2 TANK EMPTY";

    [Header("Needle Rotation Constraints")]
    private const float MAX_Z = 156f;
    private const float MIN_Z = -141f;
    private const float FIXED_X = -56.522f;
    private const float FIXED_Y = -14.503f;

    private XRGrabInteractable _grabInteractable;
    private IXRInteractor _socketInteractor;
    private bool _isGrabbed = false;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        _grabInteractable.selectEntered.AddListener(OnSelectEntered);
        _grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        _grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
        _grabInteractable.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (args.interactorObject is XRSocketInteractor)
        {
            _socketInteractor = args.interactorObject;
            _isGrabbed = false;
        }
        else
        {
            _isGrabbed = true;
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject is not XRSocketInteractor)
        {
            _isGrabbed = false;
            StopOxygenEffects();
        }
    }

    private void Update()
    {
        if (_isGrabbed && _socketInteractor != null)
        {
            transform.position = _socketInteractor.transform.position;
            transform.rotation = _socketInteractor.transform.rotation;
        }

        bool isAPressed = useTankAction.action.ReadValue<float>() > 0.1f;

        if (_isGrabbed && isAPressed)
        {
            HandleOxygenUsage();
        }
        else
        {
            StopOxygenEffects();
        }

        UpdateGauge();
    }

    private void HandleOxygenUsage()
    {
        bool inThinAir = breathManager.sensorSuite.GetPressureHPa() < breathManager.thinAirThreshold;

        if (currentTankFuel > 0 && inThinAir)
        {
            currentTankFuel = Mathf.Max(0, currentTankFuel - (usageCost * Time.deltaTime));

            // Set variables so BreathManager allows Focus to happen
            breathManager.hasOxygenTank = true;
            breathManager.currentTankFuel = currentTankFuel; // Sync fuel to manager

            breathManager.SetFocusState(true);
        }
        else
        {
            StopOxygenEffects();
        }

        if (currentTankFuel <= 0 && warningDisplay != null)
            warningDisplay.text = emptyTankMessage;
    }

    private void StopOxygenEffects()
    {
        // Fix: Nur deaktivieren, wenn der Tank tatsächlich gerade benutzt wurde!
        if (breathManager.hasOxygenTank)
        {
            breathManager.hasOxygenTank = false;

            if (breathManager.isFocusing)
            {
                breathManager.SetFocusState(false);
            }
        }
    }

    private void UpdateGauge()
    {
        if (gaugeNeedle == null) return;
        float fuelPercent = currentTankFuel / 100f;
        float targetZ = Mathf.Lerp(MIN_Z, MAX_Z, fuelPercent);
        gaugeNeedle.localRotation = Quaternion.Euler(FIXED_X, FIXED_Y, targetZ);
    }
}