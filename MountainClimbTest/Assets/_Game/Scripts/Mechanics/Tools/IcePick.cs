using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Transformers; // NEW: Required for XRBodyTransformer
using Unity.XR.CoreUtils;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class IcePick : LocomotionProvider
{
    [Header("Interaction Settings")]
    public InputActionProperty detachInput;
    public string iceTag = "Ice";

    [Tooltip("Lower value = Easier to stick.")]
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
    public XRBodyTransformer bodyTransformer; // NEW: Replaces CharacterControllerDriver
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

        if (characterController == null)
            characterController = FindFirstObjectByType<CharacterController>();

        // NEW: Find the XRBodyTransformer automatically
        if (bodyTransformer == null)
            bodyTransformer = FindFirstObjectByType<XRBodyTransformer>();

        if (moveProvider == null)
            moveProvider = FindFirstObjectByType<DynamicMoveProvider>();

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

        // SAFETY: Restore control if object is destroyed
        if (bodyTransformer != null) bodyTransformer.enabled = true;
        if (moveProvider != null) moveProvider.enabled = true;
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
        if (isStuck || currentInteractor == null) return;
        if (Time.time < nextStickTime) return;

        if (collision.gameObject.CompareTag(iceTag))
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.thisCollider == tipCollider)
                {
                    if (collision.relativeVelocity.magnitude > hitVelocityThreshold)
                    {
                        StickToWall();
                        return;
                    }
                }
            }
        }
    }

    private void StickToWall()
    {
        if (TryStartLocomotionImmediately())
        {
            isStuck = true;
            rb.isKinematic = true;
            transform.position += transform.forward * penetrationDepth;

            // --- THE FIX ---
            // 1. Disable Joystick (Stop walking)
            if (moveProvider != null) moveProvider.enabled = false;

            // 2. Disable Body Transformer (Stop Gravity & Auto-Body-Movement)
            if (bodyTransformer != null) bodyTransformer.enabled = false;

            if (characterController != null)
            {
                defaultStepOffset = characterController.stepOffset;
                characterController.stepOffset = 0;
            }

            if (currentInteractor != null)
            {
                previousInteractorPosition = currentInteractor.transform.position;
                if (currentInteractor is XRBaseInputInteractor inputInteractor)
                    inputInteractor.SendHapticImpulse(0.7f, 0.15f);
            }
        }
    }

    private void Unstick()
    {
        if (!isStuck) return;

        TryEndLocomotion();

        isStuck = false;
        rb.isKinematic = false;
        nextStickTime = Time.time + detachCooldown;

        // --- RESTORE ---
        if (moveProvider != null) moveProvider.enabled = true;
        if (bodyTransformer != null) bodyTransformer.enabled = true;

        if (characterController != null)
        {
            characterController.stepOffset = defaultStepOffset;
        }
    }

    void Update()
    {
        // SAFETY LOOP: Ensure enemies stay disabled while stuck
        if (isStuck)
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
            if (locomotionState != LocomotionState.Moving)
            {
                Unstick();
                return;
            }

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