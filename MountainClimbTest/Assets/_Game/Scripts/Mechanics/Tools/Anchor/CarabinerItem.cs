using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class CarabinerItem: XRGrabInteractable
{
    [Header("Climbing Settings")]
    public float detectionRadius = 0.2f;
    public LayerMask anchorLayer;

    private bool isAttachedToWall = false;
    private Rigidbody rb;
    private RopeSafetySystem ropeSystem;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        ropeSystem = FindFirstObjectByType<RopeSafetySystem>();
    }

    protected override void OnActivated(ActivateEventArgs args)
    {
        base.OnActivated(args);

        if (!isAttachedToWall)
        {
            TryAttachToNearestAnchor();
        }

        Debug.Log("Trigger gedrückt");
    }

    private void TryAttachToNearestAnchor()
    {
        // Wir ignorieren die LayerMask komplett und nehmen ALLES im Umkreis
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius);

        WallAnchor bestAnchor = null;
        float closestDistance = float.MaxValue;

        foreach (var col in hitColliders)
        {
            // Wir suchen das Skript im Objekt oder den Eltern
            WallAnchor anchor = col.GetComponentInParent<WallAnchor>();

            if (anchor != null)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    bestAnchor = anchor;
                }
            }
        }

        if (bestAnchor != null)
        {
            Debug.Log("Erfolg: WallAnchor gefunden -> " + bestAnchor.name);
            AttachToAnchor(bestAnchor);
        }
        else
        {
            Debug.LogWarning("Trigger gedrückt, aber KEIN WallAnchor im Radius gefunden. Gefundene Objekte: " + hitColliders.Length);
            foreach (var c in hitColliders) Debug.Log("In Reichweite war: " + c.name);
        }
    }

    private void AttachToAnchor(WallAnchor target)
    {
        isAttachedToWall = true;

        if (isSelected)
        {
            interactionManager.SelectExit(interactorsSelecting[0], this);
        }

        this.enabled = false;
        rb.isKinematic = true;
        rb.useGravity = false;

        Transform snap = target.snapPoint != null ? target.snapPoint : target.transform;
        transform.SetPositionAndRotation(snap.position, snap.rotation);
        transform.SetParent(target.transform);

        if (ropeSystem != null)
        {
            ropeSystem.ConnectRope(this.transform);
        }
    }
}