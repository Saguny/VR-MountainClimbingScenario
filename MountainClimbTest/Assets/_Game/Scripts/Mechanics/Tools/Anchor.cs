using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class AnchorHook : LocomotionProvider
{
    [Header("Interaction")]
    public InputActionProperty detachInput;
    public string anchorTag = "AnchorSurface";
    public float hitVelocityThreshold = 0.6f;
    public float penetrationDepth = 0.03f;
    public float detachCooldown = 0.25f;

    [Header("Soft Rope")]
    public float ropeSlack = 0.1f;
    public float ropeCorrectionSpeed = 10f;
    public float fallActivationVelocity = -0.5f;

    [Header("Yank Detach")]
    public bool useYankToDetach = true;
    public float pullOutThreshold = 2.0f;

    [Header("Colliders")]
    public Collider tipCollider;

    [Header("Player")]
    public CharacterController characterController;
    public DynamicMoveProvider moveProvider;

    // ---------- INTERNAL ----------
    private XRGrabInteractable interactable;
    private Rigidbody rb;

    private bool isStuck = false;
    private bool ropeActive = false;
    private float ropeLength;
    private float nextStickTime;

    private IXRSelectInteractor currentInteractor;
    private Vector3 previousInteractorPosition;
    private Vector3 previousPlayerPosition;

    protected override void Awake()
    {
        base.Awake();

        interactable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (!characterController)
            characterController = FindFirstObjectByType<CharacterController>();

        if (!moveProvider)
            moveProvider = FindFirstObjectByType<DynamicMoveProvider>();

        // XR Socket kompatibel
        interactable.throwOnDetach = false;
        interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);
    }

    void OnEnable() => detachInput.action.Enable();
    void OnDisable() => detachInput.action.Disable();

    // ---------- INTERACTION ----------
    void OnGrab(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject;

        // Wenn aus Wand gezogen ? l�sen
        Unstick();

        rb.isKinematic = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        currentInteractor = null;
        // NICHTS tun ? Socket entscheidet, ob er snappt
    }

    // ---------- COLLISION ----------
    void OnCollisionEnter(Collision collision)
    {
        if (isStuck || currentInteractor == null || Time.time < nextStickTime)
            return;

        if (!collision.gameObject.CompareTag(anchorTag))
            return;

        foreach (var contact in collision.contacts)
        {
            if (contact.thisCollider == tipCollider &&
                collision.relativeVelocity.magnitude > hitVelocityThreshold)
            {
                Stick(contact.point);
                return;
            }
        }
    }

    // ---------- STICK ----------
    void Stick(Vector3 hitPoint)
    {
        if (!TryStartLocomotionImmediately())
            return;

        isStuck = true;
        ropeActive = true;

        // XR KOMPLETT RAUS
        interactable.enabled = false;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = hitPoint + transform.forward * penetrationDepth;

        ropeLength = Vector3.Distance(
            characterController.transform.position,
            transform.position
        );

        previousPlayerPosition = characterController.transform.position;

        if (currentInteractor != null)
            previousInteractorPosition = currentInteractor.transform.position;
    }

    // ---------- UNSTICK ----------
    void Unstick()
    {
        if (!isStuck) return;

        TryEndLocomotion();

        isStuck = false;
        ropeActive = false;
        nextStickTime = Time.time + detachCooldown;

        rb.isKinematic = false;

        // XR wieder erlauben ? Socket kann snappen
        interactable.enabled = true;
    }

    // ---------- SOFT ROPE ----------
    void LateUpdate()
    {
        if (!ropeActive || !characterController) return;

        Vector3 playerPos = characterController.transform.position;
        Vector3 anchorPos = transform.position;

        Vector3 toPlayer = playerPos - anchorPos;
        float dist = toPlayer.magnitude;

        float verticalVelocity =
            (playerPos.y - previousPlayerPosition.y) / Time.deltaTime;

        previousPlayerPosition = playerPos;

        if (verticalVelocity < fallActivationVelocity &&
            dist > ropeLength + ropeSlack)
        {
            float excess = dist - ropeLength;
            Vector3 correction =
                toPlayer.normalized * excess * ropeCorrectionSpeed * Time.deltaTime;

            characterController.Move(-correction);
        }

        // Yank detach
        if (useYankToDetach && currentInteractor != null)
        {
            Vector3 handVel =
                (currentInteractor.transform.position - previousInteractorPosition) / Time.deltaTime;

            if (Vector3.Dot(handVel.normalized, -transform.forward) > 0.5f &&
                handVel.magnitude > pullOutThreshold)
            {
                Unstick();
            }

            previousInteractorPosition = currentInteractor.transform.position;
        }
    }
}
