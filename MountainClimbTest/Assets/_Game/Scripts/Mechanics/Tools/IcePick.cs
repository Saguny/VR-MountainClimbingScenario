using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using Unity.XR.CoreUtils;
using MountainRescue.Interfaces;
using MountainRescue.Systems;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class IcePick : LocomotionProvider, IAnchorStateProvider
{
    private static int activePicks = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticCounter()
    {
        activePicks = 0;
    }

    [Header("Interaction Settings")]
    public InputActionProperty detachInput;
    public string iceTag = "Ice";
    public float hitVelocityThreshold = 0.8f;
    public float penetrationDepth = 0.05f;
    public float detachCooldown = 0.2f;

    [Header("Yank Settings")]
    public bool useYankToDetach = true;
    public float pullOutThreshold = 2.0f;

    [Header("Colliders")]
    public Collider tipCollider;

    [Header("System References")]
    public CharacterController characterController;
    public XRBodyTransformer bodyTransformer;
    public DynamicMoveProvider moveProvider;
    public BreathManager breathManager;

    private XRGrabInteractable interactable;
    private Rigidbody rb;
    private bool isStuck = false;
    private XROrigin xrOrigin;
    private IXRSelectInteractor currentInteractor;
    private Vector3 previousInteractorPosition;
    private float nextStickTime = 0f;
    private float defaultStepOffset;

    public bool IsAnchored()
    {
        return isStuck;
    }

    protected override void Awake()
    {
        base.Awake();

        interactable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        xrOrigin = FindFirstObjectByType<XROrigin>();

        if (characterController == null) characterController = FindFirstObjectByType<CharacterController>();
        if (bodyTransformer == null) bodyTransformer = FindFirstObjectByType<XRBodyTransformer>();
        if (moveProvider == null) moveProvider = FindFirstObjectByType<DynamicMoveProvider>();
        if (breathManager == null) breathManager = FindFirstObjectByType<BreathManager>();

        interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);
    }

    void OnEnable()
    {
        if (detachInput.action != null) detachInput.action.Enable();
    }

    void OnDisable()
    {
        if (detachInput.action != null) detachInput.action.Disable();
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject;
        Unstick();
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        currentInteractor = null;
        Unstick();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isStuck || currentInteractor == null || Time.time < nextStickTime) return;

        if (!collision.gameObject.CompareTag(iceTag) && !collision.gameObject.CompareTag("climbableObjects")) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.thisCollider == tipCollider && collision.relativeVelocity.magnitude > hitVelocityThreshold)
            {
                // Check with the Stamina System before sticking
                if (breathManager != null)
                {
                    if (breathManager.TryConsumeStaminaForGrab())
                    {
                        StickToWall();
                    }
                    else
                    {
                        // Play a 'fail' sound or effect here if desired
                        Debug.Log("IcePick: Too tired to strike!");
                    }
                }
                else
                {
                    // Fallback if system is missing
                    StickToWall();
                }
                return;
            }
        }
    }

    private void StickToWall()
    {
        isStuck = true;
        activePicks++;

        rb.isKinematic = true;
        interactable.movementType = XRBaseInteractable.MovementType.Kinematic;
        interactable.trackPosition = false;
        interactable.trackRotation = false;

        transform.position += transform.forward * penetrationDepth;

        if (currentInteractor is XRBaseInputInteractor inputInteractor)
        {
            inputInteractor.SendHapticImpulse(0.7f, 0.15f);
        }

        if (currentInteractor != null)
        {
            previousInteractorPosition = currentInteractor.transform.position;
        }

        if (characterController != null && activePicks == 1)
            defaultStepOffset = characterController.stepOffset;

        if (characterController != null) characterController.stepOffset = 0;

        UpdatePlayerSystems();
    }

    private void Unstick()
    {
        if (!isStuck) return;

        isStuck = false;
        activePicks--;
        if (activePicks < 0) activePicks = 0;

        rb.isKinematic = false;
        interactable.trackPosition = true;
        interactable.trackRotation = true;
        interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        nextStickTime = Time.time + detachCooldown;

        if (characterController != null)
        {
            characterController.stepOffset = defaultStepOffset;
        }

        UpdatePlayerSystems();
    }

    private void UpdatePlayerSystems()
    {
        bool climbing = activePicks > 0;

        if (moveProvider != null) moveProvider.enabled = !climbing;
        if (bodyTransformer != null) bodyTransformer.enabled = !climbing;
    }

    void Update()
    {
        if (activePicks > 0)
        {
            if (moveProvider != null && moveProvider.enabled) moveProvider.enabled = false;
        }

        if (isStuck && detachInput.action != null && detachInput.action.WasPressedThisFrame())
        {
            Unstick();
            return;
        }

        if (isStuck && currentInteractor != null && xrOrigin != null)
        {
            Vector3 currentHandPos = currentInteractor.transform.position;
            Vector3 velocity = (currentHandPos - previousInteractorPosition) / Time.deltaTime;

            if (useYankToDetach)
            {
                float pullDirMatch = Vector3.Dot(velocity.normalized, -transform.forward);
                if (pullDirMatch > 0.5f && velocity.magnitude > pullOutThreshold)
                {
                    Unstick();
                    return;
                }
            }

            Vector3 handMovement = currentHandPos - previousInteractorPosition;
            Vector3 climbMove = -handMovement;

            if (characterController != null)
            {
                characterController.Move(climbMove);
            }
            else
            {
                xrOrigin.transform.position += climbMove;
            }

            previousInteractorPosition = currentInteractor.transform.position;
        }
    }
}