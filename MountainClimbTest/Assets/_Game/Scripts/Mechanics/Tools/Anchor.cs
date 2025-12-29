using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using MountainRescue.Interfaces;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class AnchorHook : MonoBehaviour, IAnchorStateProvider
{
    [Header("Settings")]
    [Tooltip("Tag of the wall objects where the anchor can stick.")]
    public string climbableTag = "Climbable"; // Can also use "Anchor" tag if you prefer

    [Tooltip("Max rope length before physics pulls you back.")]
    public float maxFallDistance = 3.0f;

    [Tooltip("Radius around the anchor point where the player can move freely.")]
    public float wallRadius = 0.8f;

    [Tooltip("How close the hook needs to be to attach.")]
    public float reachThreshold = 0.6f;

    [Header("Input")]
    public InputActionProperty detachInput;

    [Header("Mandatory Assignments")]
    public Transform leftController;
    public Transform rightController;
    public Transform playerCamera;
    public CharacterController characterController;
    public XRInteractionManager interactionManager;

    private LineRenderer line;
    private XRGrabInteractable interactable;
    private Rigidbody rb;
    private bool isStuck = false;
    private Vector3 lastValidPlayerPos;
    private Vector3 anchorPosition;

    // --- INTERFACE IMPLEMENTATION ---
    public bool IsAnchored()
    {
        // The player is safe if the hook is physically stuck in the wall.
        return isStuck;
    }
    // --------------------------------

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        interactable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (!playerCamera && Camera.main != null)
            playerCamera = Camera.main.transform;

        // Initialize Line Renderer
        line.enabled = false;
        line.positionCount = 2;
        line.startWidth = 0.02f;
        line.endWidth = 0.02f;
    }

    void OnEnable()
    {
        // Optional: Subscribe to select events if needed for logic
    }

    void OnCollisionEnter(Collision col)
    {
        // Only stick if not already stuck and hitting a valid anchor surface
        if (isStuck) return;

        // Check for specific tag (e.g., "Anchor" or "Climbable")
        if (col.gameObject.CompareTag("Anchor") || col.gameObject.CompareTag(climbableTag))
        {
            Stick(col.contacts[0].point);
        }
    }

    void Stick(Vector3 point)
    {
        isStuck = true;

        // Physics: Lock in place
        rb.isKinematic = true;
        rb.useGravity = false;

        // Visuals: Snap to point
        transform.position = point;
        anchorPosition = point; // Store the exact hit point

        // Logic: Force drop from hand so it stays on wall
        if (interactionManager != null && interactable.isSelected)
        {
            // Note: In XRI 3.x this syntax might vary slightly, but this is standard
            interactionManager.SelectExit(interactable.interactorsSelecting[0], interactable);
        }

        // Disable interaction so you can't grab it while it's holding you (optional choice)
        interactable.enabled = false;

        // Turn on the rope visual
        line.enabled = true;
    }

    void Unstick()
    {
        isStuck = false;

        // Physics: Release
        rb.isKinematic = false;
        rb.useGravity = true;

        // Interaction: Re-enable
        interactable.enabled = true;

        // Visuals: Hide rope
        line.enabled = false;
    }

    void Update()
    {
        if (!isStuck) return;

        // --- SAFETY ROPE LOGIC ---
        // 1. Calculate rope length
        float dist = Vector3.Distance(playerCamera.position, anchorPosition);

        // 2. Physics Correction (Prevent falling too far)
        // If player goes beyond max length, pull them back (rudimentary physics)
        if (dist > maxFallDistance)
        {
            Vector3 direction = (playerCamera.position - anchorPosition).normalized;
            Vector3 clampedPos = anchorPosition + direction * maxFallDistance;

            // Move CC to the clamped position (ignoring Y if you want to swing, but this is a hard stop)
            Vector3 correction = clampedPos - playerCamera.position;

            // Apply immediately to stop the fall
            characterController.Move(correction);
        }

        // 3. Wall Hugging Logic (Prevent swinging out too far from wall)
        // Check distance on XZ plane only
        Vector3 flatPlayerPos = new Vector3(playerCamera.position.x, 0, playerCamera.position.z);
        Vector3 flatAnchorPos = new Vector3(anchorPosition.x, 0, anchorPosition.z);
        float horizontalDist = Vector3.Distance(flatPlayerPos, flatAnchorPos);
        Vector3 wallCorrection = Vector3.zero;

        if (horizontalDist > wallRadius)
        {
            Vector3 dir = (flatPlayerPos - flatAnchorPos).normalized;
            Vector3 targetPos = flatAnchorPos + dir * wallRadius;
            wallCorrection.x = targetPos.x - flatPlayerPos.x;
            wallCorrection.z = targetPos.z - flatPlayerPos.z;
        }

        // Apply Wall Correction
        if (wallCorrection.magnitude > 0.01f)
        {
            characterController.Move(wallCorrection);
        }
    }

    void LateUpdate()
    {
        if (!isStuck) return;

        // Visuals: Draw the rope from Hook to Player Harness (Camera - Offset)
        line.SetPosition(0, anchorPosition);
        line.SetPosition(1, playerCamera.position - Vector3.up * 0.4f); // Approx chest height

        // Color Logic: Green if safe/near, Red if stretching limit
        float dist = Vector3.Distance(playerCamera.position, anchorPosition);
        if (dist < maxFallDistance * 0.8f)
        {
            line.startColor = Color.green;
            line.endColor = Color.green;
        }
        else
        {
            line.startColor = Color.yellow;
            line.endColor = Color.red;
        }

        // Input: Manual Detach
        if (detachInput.action != null && detachInput.action.WasPressedThisFrame())
        {
            Unstick();
        }
    }
}