using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(LineRenderer))]
public class AnchorHook : MonoBehaviour
{
    [Header("Settings")]
    public string climbableTag = "Climbable";
    public float maxFallDistance = 3.0f;
    public float wallRadius = 0.8f;
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
    private bool isStuck = false;
    private Vector3 lastValidPlayerPos;
    private Vector3 anchorPosition;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        interactable = GetComponent<XRGrabInteractable>();
        if (!playerCamera) playerCamera = Camera.main.transform;

        line.enabled = false;
        line.positionCount = 2;
    }

    void OnCollisionEnter(Collision col)
    {
        if (isStuck || !col.gameObject.CompareTag("Anchor")) return;
        Stick(col.contacts[0].point);
    }

    void Stick(Vector3 point)
    {
        isStuck = true;
        GetComponent<Rigidbody>().isKinematic = true;
        anchorPosition = point;
        transform.position = point;

        if (interactable.isSelected)
        {
            var interactors = new List<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor>(interactable.interactorsSelecting);
            foreach (var i in interactors) interactionManager.SelectExit(i, interactable);
        }

        line.enabled = true;
        lastValidPlayerPos = characterController.transform.position;
    }

    private bool IsNearClimbable()
    {
        return CheckHand(leftController) || CheckHand(rightController);
    }

    private bool CheckHand(Transform hand)
    {
        if (!hand) return false;
        Collider[] hits = Physics.OverlapSphere(hand.position, reachThreshold);
        foreach (var h in hits)
        {
            if (h.CompareTag(climbableTag)) return true;
        }
        return false;
    }

    // Wir nutzen FixedUpdate für die physikalische Korrektur, 
    // um stabilere Interaktionen während der Stick-Bewegung zu haben.
    void FixedUpdate()
    {
        if (!isStuck || !characterController) return;

        if (IsNearClimbable())
        {
            // Wenn wir klettern wollen, "speichern" wir die aktuelle Position als sicher.
            // Das erlaubt dem Stick-Movement, sich frei zu bewegen, ohne sofort zurückgezogen zu werden.
            lastValidPlayerPos = characterController.transform.position;
            return;
        }

        Vector3 currentPos = characterController.transform.position;
        Vector3 correction = Vector3.zero;

        // 1. Vertikale Sicherung (Fallen)
        float limitY = anchorPosition.y - maxFallDistance;
        if (currentPos.y < limitY)
        {
            correction.y = limitY - currentPos.y;
        }

        // 2. Horizontale Sicherung (Pendel-Radius)
        // Wir berechnen den Abstand zum Anker auf der XZ-Ebene
        Vector3 flatPlayerPos = new Vector3(currentPos.x, 0, currentPos.z);
        Vector3 flatAnchorPos = new Vector3(anchorPosition.x, 0, anchorPosition.z);
        float horizontalDist = Vector3.Distance(flatPlayerPos, flatAnchorPos);

        if (horizontalDist > wallRadius)
        {
            Vector3 dir = (flatPlayerPos - flatAnchorPos).normalized;
            Vector3 targetPos = flatAnchorPos + dir * wallRadius;
            correction.x = targetPos.x - currentPos.x;
            correction.z = targetPos.z - currentPos.z;
        }

        // WICHTIG: Nur korrigieren, wenn wir außerhalb der Grenzen sind.
        // Wir nutzen eine kleine Toleranz (0.01f), um Zittern zu vermeiden.
        if (correction.magnitude > 0.01f)
        {
            characterController.Move(correction);
        }
    }

    void LateUpdate()
    {
        if (!isStuck) return;

        // Visualisierung (immer im LateUpdate für flüssige Optik)
        line.SetPosition(0, anchorPosition);
        line.SetPosition(1, playerCamera.position - Vector3.up * 0.4f);

        if (IsNearClimbable())
        {
            line.startColor = line.endColor = Color.green;
        }
        else
        {
            line.startColor = line.endColor = Color.red;
        }

        if (detachInput.action.WasPressedThisFrame()) Unstick();
    }

    void Unstick()
    {
        isStuck = false;
        GetComponent<Rigidbody>().isKinematic = false;
        line.enabled = false;
    }
}