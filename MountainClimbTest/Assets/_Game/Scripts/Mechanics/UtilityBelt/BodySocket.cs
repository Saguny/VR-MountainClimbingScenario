using UnityEngine;

public class BodySocket : MonoBehaviour
{
    [Header("Target")]
    public Transform mainCamera;

    [Header("Offsets")]
    public float verticalOffset = 0.5f;
    public float forwardOffset = 0.1f;

    [Header("Follow Settings")]
    public float angleThreshold = 30f; // Belt stays still until head turns this many degrees
    public float followSpeed = 5f;    // How fast the belt catches up

    private float _targetYRotation;

    void Start()
    {
        if (mainCamera != null)
            _targetYRotation = mainCamera.eulerAngles.y;
    }

    void Update()
    {
        if (mainCamera == null) return;

        // 1. Handle Position (Follows head position with offsets)
        Vector3 headPosition = mainCamera.position;
        Vector3 headForward = mainCamera.forward;
        headForward.y = 0; // Project to floor plane
        headForward.Normalize();

        Vector3 targetPosition = headPosition + (Vector3.down * verticalOffset) + (headForward * forwardOffset);
        transform.position = targetPosition;

        // 2. Handle Rotation 
        float headAngle = mainCamera.eulerAngles.y;
        float angleDiff = Mathf.DeltaAngle(transform.eulerAngles.y, headAngle);

        // Only update the target rotation if we exceed the threshold
        if (Mathf.Abs(angleDiff) > angleThreshold)
        {
            _targetYRotation = headAngle;
        }

        // Smoothly rotate the belt toward the target rotation
        float smoothedAngle = Mathf.LerpAngle(transform.eulerAngles.y, _targetYRotation, Time.deltaTime * followSpeed);
        transform.rotation = Quaternion.Euler(0, smoothedAngle, 0);
    }
}