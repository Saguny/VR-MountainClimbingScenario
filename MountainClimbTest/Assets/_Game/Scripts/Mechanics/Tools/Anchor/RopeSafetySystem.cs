using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(CharacterController))]
public class RopeSafetySystem : MonoBehaviour
{
    [Header("Rope Settings")]
    public Transform harnessPoint;
    public Transform ropeAnchor;
    public float maxRopeLength = 5f;
    public InputActionProperty detachInput;

    [Header("Visual Feedback")]
    public Color slackColor = Color.white;
    public Color tensionColor = Color.red;

    [Header("References")]
    public DynamicMoveProvider moveProvider;

    private LineRenderer lineRenderer;
    private CharacterController characterController;
    private bool isHanging = false;
    private float defaultMoveSpeed;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        characterController = GetComponent<CharacterController>();

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Simple visual fix

        if (moveProvider == null)
            moveProvider = FindFirstObjectByType<DynamicMoveProvider>();

        if (harnessPoint == null) harnessPoint = transform;
    }

    void OnEnable()
    {
        if (detachInput.action != null) detachInput.action.Enable();
    }

    void OnDisable()
    {
        if (detachInput.action != null) detachInput.action.Disable();
    }

    void Update()
    {
        if (ropeAnchor != null && detachInput.action != null && detachInput.action.WasPressedThisFrame())
        {
            DetachRope();
        }

        if (ropeAnchor != null)
        {
            lineRenderer.SetPosition(0, ropeAnchor.position);
            lineRenderer.SetPosition(1, harnessPoint.position);

            // Visual Feedback: Rot wenn gespannt, Weiss wenn locker
            float currentDist = Vector3.Distance(harnessPoint.position, ropeAnchor.position);
            Color targetColor = (currentDist >= maxRopeLength - 0.1f) ? tensionColor : slackColor;
            lineRenderer.startColor = targetColor;
            lineRenderer.endColor = targetColor;
        }
    }

    void LateUpdate()
    {
        if (ropeAnchor != null)
        {
            EnforceRopePhysics();
        }
        else if (isHanging)
        {
            StopHanging();
        }
    }

    public void ConnectRope(Transform anchor)
    {
        ropeAnchor = anchor;
        lineRenderer.enabled = true;
    }

    public void DetachRope()
    {
        ropeAnchor = null;
        lineRenderer.enabled = false;
        StopHanging();
    }

    private void EnforceRopePhysics()
    {
        Vector3 vectorToAnchor = harnessPoint.position - ropeAnchor.position;
        float currentDist = vectorToAnchor.magnitude;

        // Wenn Seil gespannt ist (Max Height oder Max Fall erreicht)
        if (currentDist > maxRopeLength)
        {
            if (!isHanging) StartHanging();

            Vector3 direction = vectorToAnchor.normalized;
            Vector3 targetHarnessPos = ropeAnchor.position + (direction * maxRopeLength);

            Vector3 harnessToRoot = transform.position - harnessPoint.position;
            Vector3 newRootPos = targetHarnessPos + harnessToRoot;

            // Hartes Limit anwenden
            characterController.enabled = false;
            transform.position = newRootPos;
            characterController.enabled = true;
        }
        else
        {
            if (isHanging) StopHanging();
        }
    }

    private void StartHanging()
    {
        isHanging = true;

        if (moveProvider != null)
        {
            defaultMoveSpeed = moveProvider.moveSpeed;
            moveProvider.moveSpeed = 0;
            moveProvider.useGravity = false;
        }

        // Optional: Hände lösen bei extremem Ruck, 
        // aber beim Klettern am Limit ist es besser, dies auskommentiert zu lassen,
        // damit man sich nicht versehentlich loslässt, wenn man das Limit nur berührt.
        // ForceReleaseHands(); 
    }

    private void StopHanging()
    {
        isHanging = false;

        if (moveProvider != null)
        {
            moveProvider.moveSpeed = (defaultMoveSpeed > 0) ? defaultMoveSpeed : 2.0f;
            moveProvider.useGravity = true;
        }
    }

    private void ForceReleaseHands()
    {
        var interactors = GetComponentsInChildren<XRBaseInteractor>();
        foreach (var interactor in interactors)
        {
            if (interactor.hasSelection)
                interactor.EndManualInteraction();
        }
    }
}