using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class AnchorHook : MonoBehaviour
{
    [Header("Interaction")]
    public InputActionProperty detachInput;
    public string anchorSurfaceTag = "AnchorSurface";
    public float hitVelocityThreshold = 0.6f;
    public float penetrationDepth = 0.03f;
    public float detachCooldown = 0.25f;

    [Header("Safety")]
    public float maxFallDistance = 3.0f;       // maximale Sicherung
    public float constraintZoneHeight = 1.0f;  // unterster Meter horizontal
    public float wallRadius = 0.6f;

    [Header("Colliders")]
    public Collider tipCollider;

    [Header("Player")]
    public CharacterController characterController;

    private XRGrabInteractable interactable;
    private Rigidbody rb;

    private bool isStuck = false;
    private float nextStickTime = 0f;

    private IXRSelectInteractor currentInteractor;
    private Vector3 previousInteractorPos;
    private Vector3 previousPlayerPos;

    void Awake()
    {
        interactable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (!characterController)
            characterController = FindFirstObjectByType<CharacterController>();

        interactable.throwOnDetach = false;
        interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);

        previousPlayerPos = characterController.transform.position;
    }

    void OnEnable() => detachInput.action.Enable();
    void OnDisable() => detachInput.action.Disable();

    void OnGrab(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject;
        Unstick();
    }

    void OnRelease(SelectExitEventArgs args)
    {
        currentInteractor = null;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isStuck || currentInteractor == null || Time.time < nextStickTime)
            return;

        if (!collision.gameObject.CompareTag(anchorSurfaceTag))
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

    void Stick(Vector3 hitPoint)
    {
        isStuck = true;
        transform.position = hitPoint + transform.forward * penetrationDepth;

        if (currentInteractor != null)
            previousInteractorPos = currentInteractor.transform.position;

        previousPlayerPos = characterController.transform.position;
    }

    void Unstick()
    {
        if (!isStuck) return;

        isStuck = false;
        nextStickTime = Time.time + detachCooldown;
    }

    // ------------------- CLIMB CHECK -------------------
    private bool IsClimbing()
    {
        var hands = FindObjectsOfType<XRDirectInteractor>();
        foreach (var hand in hands)
        {
            if (hand.hasSelection &&
                hand.firstInteractableSelected is XRGrabInteractable grab &&
                grab.CompareTag("Climbable"))
            {
                return true;
            }
        }
        return false;
    }

    void LateUpdate()
    {
        if (!isStuck || !characterController)
            return;

        Vector3 playerPos = characterController.transform.position;
        Vector3 anchorPos = transform.position;

        // Berechne Y-Velocity des Spielers
        Vector3 playerVelocity = (playerPos - previousPlayerPos) / Time.deltaTime;

        // --- 1) Constraint nur anwenden, wenn Spieler nicht klettert und fällt ---
        if (!IsClimbing() && playerVelocity.y <= 0f)
        {
            float bottomLimit = anchorPos.y - maxFallDistance;
            float constraintStart = bottomLimit + constraintZoneHeight;

            // 1a) Falllimit nach unten
            if (playerPos.y < bottomLimit)
            {
                float correction = bottomLimit - playerPos.y;
                characterController.Move(Vector3.up * correction);
                playerPos = characterController.transform.position;
            }

            // 1b) Unterster Meter: horizontale Clamp
            if (playerPos.y <= constraintStart)
            {
                Vector3 flatOffset = new Vector3(
                    playerPos.x - anchorPos.x,
                    0f,
                    playerPos.z - anchorPos.z
                );

                if (flatOffset.magnitude > wallRadius)
                {
                    Vector3 clamped = flatOffset.normalized * wallRadius;
                    Vector3 target = new Vector3(
                        anchorPos.x + clamped.x,
                        playerPos.y,
                        anchorPos.z + clamped.z
                    );

                    characterController.Move(target - playerPos);
                }
            }
        }

        previousPlayerPos = characterController.transform.position;

        // --- 2) Yank Detach (optional) ---
        if (currentInteractor != null && detachInput.action.WasPressedThisFrame())
        {
            Unstick();
            return;
        }
    }
}
