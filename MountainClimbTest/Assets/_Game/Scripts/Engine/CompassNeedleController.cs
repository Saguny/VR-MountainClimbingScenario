using UnityEngine;
using MountainRescue.Systems;

public class CompassNeedleController : MonoBehaviour
{
    public Transform target;
    public Transform xrOrigin;
    public Transform controllerReference;
    public float speed = 15f;
    public float angleOffset = 0f;

    private void Update()
    {
        // Falls kein Target gesetzt ist, versuchen wir das aktuelle Target vom Manager zu holen
        if (target == null && RescueTargetManager.Instance != null)
        {
            target = RescueTargetManager.Instance.CurrentTarget;
        }

        if (target == null || xrOrigin == null || controllerReference == null) return;

        Vector3 playerPos = xrOrigin.position;
        Vector3 targetPos = target.position;
        Vector3 worldDir = new Vector3(targetPos.x - playerPos.x, 0, targetPos.z - playerPos.z).normalized;

        Vector3 localDir = transform.parent.InverseTransformDirection(worldDir);

        float targetAngle = Mathf.Atan2(-localDir.z, localDir.y) * Mathf.Rad2Deg;

        float finalX = targetAngle + angleOffset;

        Quaternion targetRot = Quaternion.Euler(finalX, 90f, 90f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * speed);
    }
}