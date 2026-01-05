using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using Unity.XR.CoreUtils;
using System.Collections;
using MountainRescue.Systems; // Dein Namespace für BreathManager/SafetyManager

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class IcePick : LocomotionProvider
{
    private static int activePicks = 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStaticCounter() { activePicks = 0; }

    [Header("Interaction")]
    public InputActionProperty detachInput;
    public InputActionProperty otherTriggerInput;
    public string iceTag = "Ice";
    public float hitVelocityThreshold = 0.8f;
    public float penetrationDepth = 0.05f;
    public float detachCooldown = 0.2f;
    public float colliderDisableDuration = 0.25f;

    [Header("References")]
    public Collider tipCollider;
    public Collider[] allColliders;
    public CharacterController characterController;
    public DynamicMoveProvider moveProvider;
    public BreathManager breathManager; // Optional

    private XRGrabInteractable interactable;
    private Rigidbody rb;
    private bool isStuck = false;
    private XROrigin xrOrigin;
    private IXRSelectInteractor currentInteractor;
    private Vector3 previousHandLocalPosition;
    private float nextStickTime = 0f;

    // Wir speichern den Original-Speed, um ihn wiederherzustellen
    private float defaultMoveSpeed = 0f;

    protected override void Awake()
    {
        base.Awake();
        interactable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        xrOrigin = FindFirstObjectByType<XROrigin>();

        if (allColliders == null || allColliders.Length == 0)
            allColliders = GetComponentsInChildren<Collider>();

        if (characterController == null) characterController = FindFirstObjectByType<CharacterController>();
        if (moveProvider == null) moveProvider = FindFirstObjectByType<DynamicMoveProvider>();
        if (breathManager == null) breathManager = FindFirstObjectByType<BreathManager>();

        if (moveProvider != null) defaultMoveSpeed = moveProvider.moveSpeed;

        interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);
    }

    void OnEnable()
    {
        if (detachInput.action != null) detachInput.action.Enable();
        if (otherTriggerInput.action != null) otherTriggerInput.action.Enable();
    }

    void OnDisable()
    {
        if (detachInput.action != null) detachInput.action.Disable();
        if (otherTriggerInput.action != null) otherTriggerInput.action.Disable();
    }

    private void OnGrab(SelectEnterEventArgs args) { currentInteractor = args.interactorObject; Unstick(); }
    private void OnRelease(SelectExitEventArgs args) { currentInteractor = null; Unstick(); }

    void OnCollisionEnter(Collision collision)
    {
        if (isStuck || currentInteractor == null || Time.time < nextStickTime) return;
        if (IsBothTriggersHeld()) return; // Sicherheits-Check (beide Trigger)

        // Tag Check
        if (!collision.gameObject.CompareTag(iceTag) && !collision.gameObject.CompareTag("climbableObjects")) return;

        // Velocity Check
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.thisCollider == tipCollider && collision.relativeVelocity.magnitude > hitVelocityThreshold)
            {
                if (breathManager == null || breathManager.TryConsumeStaminaForGrab())
                {
                    StickToWall();
                }
                return;
            }
        }
    }

    private bool IsBothTriggersHeld()
    {
        bool thisHand = detachInput.action != null && detachInput.action.IsPressed();
        bool otherHand = otherTriggerInput.action != null && otherTriggerInput.action.IsPressed();
        return thisHand && otherHand;
    }

    private void StickToWall()
    {
        if (isStuck) return;
        isStuck = true;
        activePicks++;

        // Physik des Pickaxe einfrieren
        rb.isKinematic = true;
        interactable.movementType = XRBaseInteractable.MovementType.Kinematic;
        interactable.trackPosition = false;
        interactable.trackRotation = false;

        // "Eindringen" simulieren
        transform.position += transform.forward * penetrationDepth;

        // Haptic Feedback
        if (currentInteractor is XRBaseInputInteractor input) input.SendHapticImpulse(0.7f, 0.15f);

        // Kletter-Logik starten
        if (currentInteractor != null && xrOrigin != null)
            previousHandLocalPosition = xrOrigin.transform.InverseTransformPoint(currentInteractor.transform.position);

        if (activePicks == 1)
        {
            // Wir sagen dem XR System: "Wir klettern jetzt"
            BeginLocomotion();
        }

        UpdatePlayerSystems();
    }

    private void Unstick()
    {
        if (!isStuck) return;

        isStuck = false;
        activePicks--;
        if (activePicks < 0) activePicks = 0;

        // Physik wieder freigeben
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;

        interactable.trackPosition = true;
        interactable.trackRotation = true;
        interactable.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        nextStickTime = Time.time + detachCooldown;
        StartCoroutine(DisableCollidersTemporarily());

        UpdatePlayerSystems();

        if (activePicks == 0)
        {
            // Klettern beendet
            EndLocomotion();

            // WICHTIG: Erzwinge Physik-Update, damit CC sofort Kollisionen prüft
            Physics.SyncTransforms();

            // Safety Check rufen (falls vorhanden)
            if (PlayerSafetyManager.Instance != null)
                PlayerSafetyManager.Instance.RequestGroundSafetyCheck();
        }
    }

    private void UpdatePlayerSystems()
    {
        bool isClimbing = activePicks > 0;

        if (moveProvider != null)
        {
            // WICHTIG: Niemals 'enabled = false' setzen!
            // Nur Gravity und Speed steuern.
            moveProvider.useGravity = !isClimbing;
            moveProvider.moveSpeed = isClimbing ? 0 : defaultMoveSpeed;
        }

        if (characterController != null)
        {
            // Wenn wir klettern, ignorieren wir Schritte, sonst "stolpert" man die Wand hoch
            characterController.stepOffset = isClimbing ? 0 : 0.3f;
        }
    }

    private IEnumerator DisableCollidersTemporarily()
    {
        ToggleColliders(false);
        yield return new WaitForSeconds(colliderDisableDuration);
        ToggleColliders(true);
    }

    private void ToggleColliders(bool state)
    {
        foreach (var col in allColliders) { if (col != null) col.enabled = state; }
    }

    private void OnDestroy()
    {
        if (isStuck) { activePicks = 0; }
    }

    void Update()
    {
        // Detach Input Check
        if (isStuck && detachInput.action != null && detachInput.action.WasPressedThisFrame())
        {
            Unstick();
            return;
        }

        // Kletter-Bewegung
        if (isStuck && currentInteractor != null && xrOrigin != null)
        {
            Vector3 currentHandLocalPos = xrOrigin.transform.InverseTransformPoint(currentInteractor.transform.position);
            Vector3 localMovement = currentHandLocalPos - previousHandLocalPosition;
            Vector3 worldMovement = xrOrigin.transform.TransformDirection(localMovement);
            Vector3 climbMove = -worldMovement;

            // Wir bewegen den CC. Da Gravity aus ist, schwebt er.
            if (characterController != null && characterController.enabled)
            {
                characterController.Move(climbMove);
            }

            previousHandLocalPosition = currentHandLocalPos;
        }
    }
}