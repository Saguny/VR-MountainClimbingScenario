using UnityEngine;

public class CompassNeedleController : MonoBehaviour
{
    public Transform target;
    public Transform xrOrigin;
    public Transform controllerReference;
    public float speed = 15f;
    public float angleOffset = 0f;

    private void Update()
    {
        if (target == null || xrOrigin == null || controllerReference == null) return;

        // 1. Get world direction from Player to Target (X and Z only)
        Vector3 playerPos = xrOrigin.position;
        Vector3 targetPos = target.position;
        Vector3 worldDir = new Vector3(targetPos.x - playerPos.x, 0, targetPos.z - playerPos.z).normalized;

        // 2. Project world direction into the LOCAL space of the needle
        // We use the needle's parent to see where the target is relative to the watch face
        Vector3 localDir = transform.parent.InverseTransformDirection(worldDir);

        // 3. Calculate angle
        // Swapped parameters and added a negative sign to fix "Wrong Direction"
        // In your specific hierarchy (X-rotation), Z and Y are the 2D plane coordinates
        float targetAngle = Mathf.Atan2(-localDir.z, localDir.y) * Mathf.Rad2Deg;

        // 4. Handle the specific (90, 90, 90) base rotation
        float finalX = targetAngle + angleOffset;

        // 5. Apply with Slerp for stability
        Quaternion targetRot = Quaternion.Euler(finalX, 90f, 90f);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRot,
            Time.deltaTime * speed
        );
    }
}