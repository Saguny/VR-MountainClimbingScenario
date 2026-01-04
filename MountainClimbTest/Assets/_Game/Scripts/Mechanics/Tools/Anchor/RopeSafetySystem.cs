using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using MountainRescue.Engine;

[RequireComponent(typeof(LineRenderer))]
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

    [Header("References (Auto-Found)")]
    public DynamicMoveProvider moveProvider;
    public CharacterController characterController;

    private LineRenderer lineRenderer;
    private bool isHanging = false;
    private float defaultMoveSpeed = 2.0f;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;

        // Ensure we have a material
        if (lineRenderer.sharedMaterial == null)
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        FindPlayerReferences();
    }

    void OnEnable()
    {
        if (detachInput.action != null) detachInput.action.Enable();

        SceneManager.sceneLoaded += OnSceneLoaded;
        
    }

    void OnDisable()
    {
        if (detachInput.action != null) detachInput.action.Disable();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DetachRope();
        FindPlayerReferences();
    }

    // This handles the specific moment DynamicSceneSwitcher finishes rebuilding the player
    private void HandleCCRebuilt(CharacterController newCC)
    {
        characterController = newCC;
        harnessPoint = newCC.transform;

        // Refresh move provider as it might have been reset/re-enabled
        moveProvider = FindFirstObjectByType<DynamicMoveProvider>();

        Debug.Log("RopeSystem: References updated via Rebuild Event.");
    }

    private void FindPlayerReferences()
    {
        if (characterController == null)
            characterController = FindFirstObjectByType<CharacterController>();

        if (moveProvider == null)
            moveProvider = FindFirstObjectByType<DynamicMoveProvider>();

        if (harnessPoint == null && characterController != null)
        {
            harnessPoint = characterController.transform;
        }
    }

    void Update()
    {
        if (ropeAnchor != null && detachInput.action != null && detachInput.action.WasPressedThisFrame())
        {
            DetachRope();
        }

        if (ropeAnchor != null && harnessPoint != null)
        {
            lineRenderer.SetPosition(0, ropeAnchor.position);
            lineRenderer.SetPosition(1, harnessPoint.position);

            float currentDist = Vector3.Distance(harnessPoint.position, ropeAnchor.position);
            Color targetColor = (currentDist >= maxRopeLength - 0.1f) ? tensionColor : slackColor;
            lineRenderer.startColor = targetColor;
            lineRenderer.endColor = targetColor;
        }
    }

    void LateUpdate()
    {
        // CRITICAL: If the CC is null or disabled, DO NOT run any rope logic.
        // This prevents the rope from trying to teleport a player that is currently being "rebuilt".
        if (characterController == null || !characterController.enabled)
        {
            return;
        }

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
        if (characterController == null) FindPlayerReferences();

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
        if (harnessPoint == null || characterController == null) return;

        Vector3 vectorToAnchor = harnessPoint.position - ropeAnchor.position;
        float currentDist = vectorToAnchor.magnitude;

        if (currentDist > maxRopeLength)
        {
            if (!isHanging) StartHanging();

            Vector3 direction = vectorToAnchor.normalized;
            Vector3 targetHarnessPos = ropeAnchor.position + (direction * maxRopeLength);

            Transform rigTransform = characterController.transform;
            Vector3 harnessToRoot = rigTransform.position - harnessPoint.position;
            Vector3 newRootPos = targetHarnessPos + harnessToRoot;

            // Simple check to prevent teleporting if CC was just destroyed
            if (characterController != null)
            {
                characterController.enabled = false;
                rigTransform.position = newRootPos;
                characterController.enabled = true;
            }
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
            if (moveProvider.moveSpeed > 0) defaultMoveSpeed = moveProvider.moveSpeed;
            moveProvider.moveSpeed = 0;
            moveProvider.useGravity = false;
        }

        ForceReleaseHands();
    }

    private void StopHanging()
    {
        isHanging = false;

        if (moveProvider != null)
        {
            moveProvider.moveSpeed = defaultMoveSpeed;
            moveProvider.useGravity = true;
        }
    }

    private void ForceReleaseHands()
    {
        if (characterController != null)
        {
            var interactors = characterController.GetComponentsInChildren<XRBaseInteractor>();
            foreach (var interactor in interactors)
            {
                if (interactor is IXRSelectInteractor selectInteractor && selectInteractor.hasSelection)
                {
                    // Using modern XRI method to drop items
                    interactor.interactionManager.SelectExit(selectInteractor, selectInteractor.firstInteractableSelected);
                }
            }
        }
    }
}