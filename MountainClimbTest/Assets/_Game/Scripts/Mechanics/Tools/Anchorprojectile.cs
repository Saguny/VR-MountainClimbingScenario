using UnityEngine;

public class AnchorProjectile : MonoBehaviour
{
    private bool hasHit = false;

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        if (collision.gameObject.CompareTag("climbableObjects"))
        {
            hasHit = true;
            // Physik stoppen
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            transform.position = collision.contacts[0].point;
            transform.forward = -collision.contacts[0].normal;

            // Den Hook am Spieler finden und aktivieren
            AnchorHook hook = Object.FindFirstObjectByType<AnchorHook>();
            if (hook != null) hook.RegisterNewAnchor(transform.position);
        }
    }
}