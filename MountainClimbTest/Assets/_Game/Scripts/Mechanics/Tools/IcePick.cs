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

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class IcePick : LocomotionProvider, IAnchorStateProvider
{
    // --- GLOBAL STATE ---
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
        base.Awake(); // Initialize LocomotionProvider

        interactable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        xrOrigin = FindFirstObjectByType<XROrigin>();

        if (characterController == null) characterController = FindFirstObjectByType<CharacterController>();
        if (bodyTransformer == null) bodyTransformer = FindFirstObjectByType<XRBodyTransformer>();
        if (moveProvider == null) moveProvider = FindFirstObjectByType<DynamicMoveProvider>();

        // We use VelocityTracking so it feels heavy/real until it sticks
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
        Unstick(); // Grab = Detach from wall
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        currentInteractor = null;
        Unstick(); // Drop = Detach from wall
    }

    void OnCollisionEnter(Collision collision)
    {
        // 1. Must be holding it
        // 2. Must not be stuck
        // 3. Must be cooldown ready
        if (isStuck || currentInteractor == null || Time.time < nextStickTime) return;

        // 4. Tag Check
        if (!collision.gameObject.CompareTag(iceTag) && !collision.gameObject.CompareTag("Climbable")) return;

        // 5. Tip Collider & Velocity Check
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.thisCollider == tipCollider && collision.relativeVelocity.magnitude > hitVelocityThreshold)
            {
                StickToWall();
                return;
            }
        }
    }

    private void StickToWall()
    {
        // NO "TryStartLocomotionImmediately" HERE - This was the blocker.
        // We just force the state.

        isStuck = true;
        activePicks++;

        // Physics: Freeze
        rb.isKinematic = true;
        interactable.movementType = XRBaseInteractable.MovementType.Kinematic;
        interactable.trackPosition = false;
        interactable.trackRotation = false;

        // Visuals
        transform.position += transform.forward * penetrationDepth;

        // Haptics
        if (currentInteractor is XRBaseInputInteractor inputInteractor)
        {
            inputInteractor.SendHapticImpulse(0.7f, 0.15f);
        }

        // Initialize Movement
        if (currentInteractor != null)
        {
            previousInteractorPosition = currentInteractor.transform.position;
        }

        // Disable Step Offset (Prevent popping up)
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

        // Physics: Release
        rb.isKinematic = false;
        interactable.trackPosition = true;
        interactable.trackRotation = true;
        interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        nextStickTime = Time.time + detachCooldown;

        // Restore Step Offset
        if (characterController != null)
        {
            characterController.stepOffset = defaultStepOffset;
        }

        UpdatePlayerSystems();
    }

    private void UpdatePlayerSystems()
    {
        // If climbing with ANY pick, disable walking.
        bool climbing = activePicks > 0;

        if (moveProvider != null) moveProvider.enabled = !climbing;
        if (bodyTransformer != null) bodyTransformer.enabled = !climbing;
    }

    void Update()
    {
        // Fail-safe to ensure walking stays off while climbing
        if (activePicks > 0)
        {
            if (moveProvider != null && moveProvider.enabled) moveProvider.enabled = false;
        }

        // Manual Detach
        if (isStuck && detachInput.action != null && detachInput.action.WasPressedThisFrame())
        {
            Unstick();
            return;
        }

        // Climbing Logic
        if (isStuck && currentInteractor != null && xrOrigin != null)
        {
            Vector3 currentHandPos = currentInteractor.transform.position;
            Vector3 velocity = (currentHandPos - previousInteractorPosition) / Time.deltaTime;

            // Yank Detach
            if (useYankToDetach)
            {
                float pullDirMatch = Vector3.Dot(velocity.normalized, -transform.forward);
                if (pullDirMatch > 0.5f && velocity.magnitude > pullOutThreshold)
                {
                    Unstick();
                    return;
                }
            }

            // Move Player
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