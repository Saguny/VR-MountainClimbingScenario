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

    private Vector3 previousHandLocalPosition;
    private float nextStickTime = 0f;
    private float defaultStepOffset;
    private bool defaultGravityState;

    public bool IsAnchored() => isStuck;

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
                if (breathManager != null)
                {
                    if (breathManager.TryConsumeStaminaForGrab()) StickToWall();
                    else Debug.Log("IcePick: Too tired to strike!");
                }
                else
                {
                    StickToWall();
                }
                return;
            }
        }
    }

    private void StickToWall()
    {
        if (isStuck) return;

        isStuck = true;
        activePicks++;

        rb.isKinematic = true;
        interactable.movementType = XRBaseInteractable.MovementType.Kinematic;
        interactable.trackPosition = false;
        interactable.trackRotation = false;

        transform.position += transform.forward * penetrationDepth;

        if (currentInteractor is XRBaseInputInteractor inputInteractor)
            inputInteractor.SendHapticImpulse(0.7f, 0.15f);

        if (currentInteractor != null)
        {
            previousHandLocalPosition = xrOrigin.transform.InverseTransformPoint(currentInteractor.transform.position);
        }

        if (characterController != null && activePicks == 1)
        {
            defaultStepOffset = characterController.stepOffset;
            if (moveProvider != null) defaultGravityState = moveProvider.useGravity;
        }

        if (activePicks == 1) BeginLocomotion();

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

        UpdatePlayerSystems();

        if (activePicks == 0)
        {
            EndLocomotion();
            if (characterController != null) characterController.stepOffset = defaultStepOffset;
        }
    }

    private void UpdatePlayerSystems()
    {
        bool isClimbing = activePicks > 0;

        if (moveProvider != null)
        {
            moveProvider.enabled = !isClimbing;
            moveProvider.useGravity = !isClimbing && defaultGravityState;
        }

        if (bodyTransformer != null)
            bodyTransformer.enabled = !isClimbing;

        if (characterController != null)
        {
            characterController.stepOffset = isClimbing ? 0 : defaultStepOffset;
        }
    }

    void Update()
    {
        if (isStuck && detachInput.action != null && detachInput.action.WasPressedThisFrame())
        {
            Unstick();
            return;
        }

        if (isStuck && currentInteractor != null && xrOrigin != null)
        {
            Vector3 currentHandLocalPos = xrOrigin.transform.InverseTransformPoint(currentInteractor.transform.position);
            Vector3 localMovement = currentHandLocalPos - previousHandLocalPosition;
            Vector3 worldMovement = xrOrigin.transform.TransformDirection(localMovement);

            Vector3 climbMove = -worldMovement;

            if (characterController != null)
            {
                characterController.Move(climbMove);
            }
            else
            {
                xrOrigin.transform.position += climbMove;
            }

            previousHandLocalPosition = currentHandLocalPos;
        }
    }
}