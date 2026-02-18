using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using Unity.XR.CoreUtils;
using System.Collections;
using MountainRescue.Systems;

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
    public BreathManager breathManager;

    [Header("Audio")]
    public AudioClip stickSound;
    [Range(0f, 1f)] public float stickVolume = 1f;
    public Vector2 pitchRange = new Vector2(0.85f, 1.15f);
    public Vector2 volumeVariation = new Vector2(0.9f, 1f);

    [Header("AudioSource")]
    public AudioSource audioSource;

    private XRGrabInteractable interactable;
    private Rigidbody rb;
    private bool isStuck = false;
    private XROrigin xrOrigin;
    private IXRSelectInteractor currentInteractor;
    private Vector3 previousHandLocalPosition;
    private float nextStickTime = 0f;
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

    public void ForceReset()
    {
        // 1. Logically unstick
        if (isStuck)
        {
            Unstick();
        }

        // 2. Force Physics Reset
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3. Reset internal cooldowns
        nextStickTime = 0f;
    }

    private void OnGrab(SelectEnterEventArgs args) { currentInteractor = args.interactorObject; Unstick(); }
    private void OnRelease(SelectExitEventArgs args) { currentInteractor = null; Unstick(); }

    void OnCollisionEnter(Collision collision)
    {
        if (isStuck || currentInteractor == null || Time.time < nextStickTime) return;
        if (IsBothTriggersHeld()) return;

        if (!collision.gameObject.CompareTag(iceTag) && !collision.gameObject.CompareTag("climbableObjects")) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.thisCollider == tipCollider && collision.relativeVelocity.magnitude > hitVelocityThreshold)
            {
                if (breathManager == null || breathManager.TryConsumeStaminaForGrab())
                {
                    StickToWall(collision.relativeVelocity.magnitude);
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

    private void StickToWall(float hitVelocity = 1f)
    {
        if (isStuck) return;
        isStuck = true;
        activePicks++;

        rb.isKinematic = true;
        interactable.movementType = XRBaseInteractable.MovementType.Kinematic;
        interactable.trackPosition = false;
        interactable.trackRotation = false;

        transform.position += transform.forward * penetrationDepth;

        if (currentInteractor is XRBaseInputInteractor input) input.SendHapticImpulse(0.7f, 0.15f);

        PlayStickSound(hitVelocity);

        if (currentInteractor != null && xrOrigin != null)
            previousHandLocalPosition = xrOrigin.transform.InverseTransformPoint(currentInteractor.transform.position);

        if (activePicks == 1)
        {
            BeginLocomotion();
        }

        UpdatePlayerSystems();
    }

    
    private void PlayStickSound(float hitVelocity = 1f)
    {
        if (audioSource == null || stickSound == null) return;

        // Map hit velocity to a small extra pitch boost (soft hit = no boost, hard hit = +0.15)
        float velocityPitchBoost = Mathf.InverseLerp(hitVelocityThreshold, hitVelocityThreshold * 3f, hitVelocity) * 0.15f;

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y) + velocityPitchBoost;
        audioSource.volume = stickVolume * Random.Range(volumeVariation.x, volumeVariation.y);
        audioSource.PlayOneShot(stickSound);
    }

    private void Unstick()
    {
        if (!isStuck) return;

        isStuck = false;
        activePicks--;
        if (activePicks < 0) activePicks = 0;

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
            EndLocomotion();
            Physics.SyncTransforms();

            if (PlayerSafetyManager.Instance != null)
                PlayerSafetyManager.Instance.RequestGroundSafetyCheck();
        }
    }

    private void UpdatePlayerSystems()
    {
        bool isClimbing = activePicks > 0;

        if (moveProvider != null)
        {
            moveProvider.useGravity = !isClimbing;
            moveProvider.moveSpeed = isClimbing ? 0 : defaultMoveSpeed;
        }

        if (characterController != null)
        {
            characterController.stepOffset = isClimbing ? 0 : 0.3f;
        }

        if (isClimbing)
            ClimbingColliderAdjuster.Instance?.AddClimbSource();
        else
            ClimbingColliderAdjuster.Instance?.RemoveClimbSource();
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

            if (characterController != null && characterController.enabled)
            {
                characterController.Move(climbMove);
            }

            previousHandLocalPosition = currentHandLocalPos;
        }
    }
}