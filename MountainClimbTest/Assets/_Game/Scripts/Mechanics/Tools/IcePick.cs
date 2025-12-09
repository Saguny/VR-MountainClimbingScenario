using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class IcePick : LocomotionProvider
{
    // --- GLOBAL STATE ---
    private static int activePicks = 0;

    // BUG FIX: Resets counter when you press Play in the Editor
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

    protected override void Awake()
    {
        base.Awake();
        interactable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        xrOrigin = FindFirstObjectByType<XROrigin>();

        if (characterController == null) characterController = FindFirstObjectByType<CharacterController>();
        if (bodyTransformer == null) bodyTransformer = FindFirstObjectByType<XRBodyTransformer>();
        if (moveProvider == null) moveProvider = FindFirstObjectByType<DynamicMoveProvider>();

        interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);
    }

    void OnEnable() => detachInput.action.Enable();
    void OnDisable() => detachInput.action.Disable();

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnGrab);
            interactable.selectExited.RemoveListener(OnRelease);
        }
        if (isStuck)
        {
            activePicks--;
            UpdatePlayerSystems();
        }
    }

    private void OnGrab(SelectEnterEventArgs args) { currentInteractor = args.interactorObject; Unstick(); }
    private void OnRelease(SelectExitEventArgs args) { currentInteractor = null; Unstick(); }

    void OnCollisionEnter(Collision collision)
    {
        if (isStuck || currentInteractor == null || Time.time < nextStickTime) return;

        if (collision.gameObject.CompareTag(iceTag))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.thisCollider == tipCollider && collision.relativeVelocity.magnitude > hitVelocityThreshold)
                {
                    StickToWall();
                    return;
                }
            }
        }
    }

    private void StickToWall()
    {
        if (TryStartLocomotionImmediately())
        {
            isStuck = true;
            activePicks++;

            rb.isKinematic = true;
            interactable.movementType = XRBaseInteractable.MovementType.Kinematic;
            interactable.trackPosition = false;
            interactable.trackRotation = false;

            transform.position += transform.forward * penetrationDepth;

            // Haptics
            if (currentInteractor != null)
            {
                previousInteractorPosition = currentInteractor.transform.position;
                if (currentInteractor is XRBaseInputInteractor inputInteractor)
                    inputInteractor.SendHapticImpulse(0.7f, 0.15f);
            }

            // Save step offset to prevent popping up
            if (characterController != null && activePicks == 1)
                defaultStepOffset = characterController.stepOffset;

            if (characterController != null) characterController.stepOffset = 0;

            UpdatePlayerSystems();
        }
    }

    private void Unstick()
    {
        if (!isStuck) return;

        TryEndLocomotion();

        isStuck = false;
        activePicks--;
        // Safety clamp
        if (activePicks < 0) activePicks = 0;

        rb.isKinematic = false;
        interactable.trackPosition = true;
        interactable.trackRotation = true;
        interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        nextStickTime = Time.time + detachCooldown;

        // Restore step offset immediately so we can walk over bumps
        if (characterController != null)
        {
            characterController.stepOffset = defaultStepOffset;
        }

        UpdatePlayerSystems();
    }

    private void UpdatePlayerSystems()
    {
        // LOGIC FIX:
        // Only disable movement if picks are actively stuck.
        // If picks are 0, we FORCE enable everything. 
        // We let standard XRI handle the "Hand vs Walk" conflict to avoid getting locked out.

        bool anyPickStuck = activePicks > 0;

        if (anyPickStuck)
        {
            if (moveProvider != null) moveProvider.enabled = false;
            if (bodyTransformer != null) bodyTransformer.enabled = false;
        }
        else
        {
            if (moveProvider != null) moveProvider.enabled = true;
            if (bodyTransformer != null) bodyTransformer.enabled = true;
        }
    }

    void Update()
    {
        // Failsafe: Ensure systems stay off if we are supposed to be climbing
        if (activePicks > 0)
        {
            if (bodyTransformer != null && bodyTransformer.enabled) bodyTransformer.enabled = false;
            if (moveProvider != null && moveProvider.enabled) moveProvider.enabled = false;
        }

        if (isStuck && detachInput.action.WasPressedThisFrame())
        {
            Unstick();
            return;
        }

        if (isStuck && currentInteractor != null && xrOrigin != null)
        {
            Vector3 currentPos = currentInteractor.transform.position;
            Vector3 velocity = (currentPos - previousInteractorPosition) / Time.deltaTime;

            if (useYankToDetach)
            {
                float pullDirMatch = Vector3.Dot(velocity.normalized, -transform.forward);
                if (pullDirMatch > 0.5f && velocity.magnitude > pullOutThreshold)
                {
                    Unstick();
                    return;
                }
            }

            Vector3 handMovement = currentPos - previousInteractorPosition;
            Vector3 climbMove = -handMovement;

            if (characterController != null)
                characterController.Move(climbMove);
            else
                xrOrigin.transform.position += climbMove;

            previousInteractorPosition = currentInteractor.transform.position;
        }
    }
}