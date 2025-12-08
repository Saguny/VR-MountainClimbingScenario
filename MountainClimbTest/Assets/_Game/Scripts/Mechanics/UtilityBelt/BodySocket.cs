using UnityEngine;

public class BodySocket : MonoBehaviour
{
    [Header("Target")]
    public Transform mainCamera;

    [Header("Offsets")]
    public float verticalOffset = 0.5f;
    public float forwardOffset = 0.0f;

    void Update()
    {
        if (mainCamera == null) return;

        Vector3 targetPosition = mainCamera.position;
        targetPosition.y -= verticalOffset;

        Vector3 projectedForward = Vector3.ProjectOnPlane(mainCamera.forward, Vector3.up).normalized;
        if (projectedForward != Vector3.zero)
        {
            targetPosition += projectedForward * forwardOffset;
            transform.rotation = Quaternion.LookRotation(projectedForward, Vector3.up);
        }

        transform.position = targetPosition;
    }
}