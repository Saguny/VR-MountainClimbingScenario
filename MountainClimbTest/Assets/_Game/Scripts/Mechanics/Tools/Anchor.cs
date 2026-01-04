using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using MountainRescue.Interfaces;

[RequireComponent(typeof(LineRenderer))]
public class AnchorHook : MonoBehaviour, IAnchorStateProvider
{
    [Header("Settings")]
    public string climbableTag = "climbableObjects";
    public float maxFallDistance = 3.0f;
    public float maxClimbHeight = 0.5f; // Begrenzung nach oben
    public float platformSize = 2.0f;
    public float centeringSpeed = 2.0f;

    [Header("Advanced Boundary")]
    public float deadzoneRadius = 0.2f;
    public float hardLimitRadius = 0.9f;

    [Header("Input")]
    public InputActionProperty detachInput;

    [Header("Assignments")]
    public Transform playerCamera;
    public CharacterController characterController;

    private LineRenderer line;
    private bool isStuck = false;
    private Vector3 anchorPosition;
    private GameObject catchPlatform;

    public bool IsAnchored() => isStuck;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        if (!playerCamera) playerCamera = Camera.main.transform;

        // LineRenderer Grund-Setup
        line.enabled = false;
        line.positionCount = 2;

        CreateCatchPlatform();
    }

    void CreateCatchPlatform()
    {
        catchPlatform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        catchPlatform.name = "SafetyPlatform";
        catchPlatform.transform.localScale = new Vector3(platformSize, 0.5f, platformSize);
        catchPlatform.GetComponent<MeshRenderer>().enabled = false;
        catchPlatform.SetActive(false);
    }

    // WICHTIG: Diese Methode wird vom AnchorProjectile-Script aufgerufen!
    public void RegisterNewAnchor(Vector3 hitPoint)
    {
        isStuck = true;
        anchorPosition = hitPoint;
        line.enabled = true;

        // Plattform positionieren
        float targetY = anchorPosition.y - maxFallDistance - 0.25f;
        catchPlatform.transform.position = new Vector3(anchorPosition.x, targetY, anchorPosition.z);
        catchPlatform.SetActive(true);
    }

    void Update()
    {
        if (!isStuck) return;

        // 1. Manueller Abbruch
        if (detachInput.action != null && detachInput.action.WasPressedThisFrame())
        {
            Unstick();
            return;
        }

        // 2. Höhenbegrenzung (Verhindert zu weites Aufsteigen über den Anker)
        ApplyHeightLimit();

        // 3. Zentrierung (Falls wir auf der Plattform stehen/hängen)
        float playerFeetY = characterController.transform.position.y;
        float platformTopY = catchPlatform.transform.position.y + 0.25f;

        if (playerFeetY <= platformTopY + 0.2f) // Etwas mehr Toleranz (0.2m)
        {
            ApplyCentering();
        }
    }

    void ApplyHeightLimit()
    {
        float currentHeight = playerCamera.position.y;
        float heightLimit = anchorPosition.y + maxClimbHeight;

        if (currentHeight > heightLimit)
        {
            float overshoot = currentHeight - heightLimit;
            characterController.Move(Vector3.down * overshoot);
        }
    }

    void ApplyCentering()
    {
        // 1. Positionen auf der XZ-Ebene (Horizontal)
        Vector3 playerPos = characterController.transform.position;
        Vector3 playerXZ = new Vector3(playerPos.x, 0, playerPos.z);
        Vector3 centerXZ = new Vector3(anchorPosition.x, 0, anchorPosition.z);

        float currentDist = Vector3.Distance(playerXZ, centerXZ);
        Vector3 directionToCenter = (centerXZ - playerXZ).normalized;

        // 2. Sicherheits-Check: Wenn der Spieler bereits ÜBER dem Limit ist (z.B. durch schnelles Laufen)
        // Dann teleportieren wir ihn hart zurück auf die Kante, bevor wir Move aufrufen.
        if (currentDist > hardLimitRadius)
        {
            Vector3 clampedXZ = centerXZ - (directionToCenter * hardLimitRadius);
            Vector3 newPos = new Vector3(clampedXZ.x, playerPos.y, clampedXZ.z);

            // Der entscheidende Trick: Wir setzen die Position kurz vor dem Move hart fest
            characterController.enabled = false;
            characterController.transform.position = newPos;
            characterController.enabled = true;
            return; // Frame beenden, da wir schon am Limit sind
        }

        // 3. Normale Zentrierung innerhalb des Spielraums
        if (currentDist <= deadzoneRadius) return;

        float t = Mathf.InverseLerp(deadzoneRadius, hardLimitRadius, currentDist);
        // Erhöhter Exponent (3) für steilere Kraftkurve am Rand
        float strength = Mathf.Pow(t, 3) * centeringSpeed;

        // 4. Progressive Kraft anwenden
        if (strength > 0)
        {
            characterController.Move(directionToCenter * strength * Time.deltaTime);
        }
    }

    void LateUpdate()
    {
        if (!isStuck) return;

        line.SetPosition(0, anchorPosition);
        line.SetPosition(1, playerCamera.position - Vector3.up * 0.4f);
    }

    public void Unstick()
    {
        isStuck = false;
        line.enabled = false;
        catchPlatform.SetActive(false);
    }
}