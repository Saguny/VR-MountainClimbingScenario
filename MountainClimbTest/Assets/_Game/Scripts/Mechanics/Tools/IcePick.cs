using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class IcePick : MonoBehaviour
{
    [Header("Interaction Settings")]
    public InputActionProperty detachInput;
    public string iceTag = "Ice";

    [Tooltip("Lower value = Easier to stick.")]
    public float hitVelocityThreshold = 0.8f;
    public float penetrationDepth = 0.05f;
    public float detachCooldown = 1.0f;

    [Header("Yank Settings")]
    [Tooltip("Set to TRUE if you want to pull it out by force. Leave FALSE for button-only (safer).")]
    public bool useYankToDetach = false;
    public float pullOutThreshold = 2.0f;

    [Header("Colliders")]
    public Collider tipCollider;

    [Header("System References")]
    public DynamicMoveProvider moveProvider;
    public CharacterController characterController;

    private XRGrabInteractable interactable;
    private Rigidbody rb;
    private bool isStuck = false;
    private XROrigin xrOrigin;
    private IXRSelectInteractor currentInteractor;
    private Vector3 previousInteractorPosition;
    private float nextStickTime = 0f;
    private float defaultStepOffset;

    void Awake()
    {
        interactable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        xrOrigin = FindFirstObjectByType<XROrigin>();

        if (moveProvider == null)
            moveProvider = FindFirstObjectByType<DynamicMoveProvider>();

        if (characterController == null)
            characterController = FindFirstObjectByType<CharacterController>();

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
            // --- FIX: Check ALL contacts, not just the first one ---
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.thisCollider == tipCollider)
                {
                    // Found the tip! Check speed.
                    if (collision.relativeVelocity.magnitude > hitVelocityThreshold)
                    {
                        StickToWall();
                        return; // Stop checking contacts
                    }
                }
            }
        }
    }

    private void StickToWall()
    {
        isStuck = true;
        rb.isKinematic = true;
        transform.position += transform.forward * penetrationDepth;

        // Disable movement systems
        if (moveProvider != null) moveProvider.enabled = false;

        if (characterController != null)
        {
            defaultStepOffset = characterController.stepOffset;
            characterController.stepOffset = 0;
        }

        if (currentInteractor != null)
        {
            previousInteractorPosition = currentInteractor.transform.position;
            if (currentInteractor is XRBaseInputInteractor inputInteractor)
                inputInteractor.SendHapticImpulse(0.7f, 0.15f); // Stronger haptic for satisfaction
        }
    }

    private void Unstick()
    {
        if (!isStuck) return;

        isStuck = false;
        rb.isKinematic = false;
        nextStickTime = Time.time + detachCooldown;

        // Restore movement systems
        if (moveProvider != null) moveProvider.enabled = true;

        if (characterController != null)
        {
            characterController.stepOffset = defaultStepOffset;
        }
    }

    void Update()
    {
        // 1. Manual Detach (Left Trigger) - Always works
        if (isStuck && detachInput.action.WasPressedThisFrame())
        {
            Unstick();
            return;
        }

        if (isStuck && currentInteractor != null && xrOrigin != null)
        {
            Vector3 currentPos = currentInteractor.transform.position;

            // 2. Yank Logic (Optional)
            if (useYankToDetach)
            {
                Vector3 velocity = (currentPos - previousInteractorPosition) / Time.deltaTime;
                float pullBackStrength = Vector3.Dot(velocity, -transform.forward);

                if (pullBackStrength > pullOutThreshold)
                {
                    Unstick();
                    return;
                }
            }

            // 3. Movement Logic
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