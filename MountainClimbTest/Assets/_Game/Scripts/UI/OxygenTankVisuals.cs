using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class OxygenTubeVisual : MonoBehaviour
{
    [Header("Connections")]
    [Tooltip("The point on the Tank/Belt.")]
    public Transform startPoint;

    [Tooltip("The point on the Mask (Hand).")]
    public Transform endPoint;

    [Header("Settings")]
    public int resolution = 20;
    [Tooltip("How far the tube hangs down.")]
    public float slack = 0.3f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // CRITICAL FIX: Ensure the line draws in World Space coordinates
        lineRenderer.useWorldSpace = true;

        // Optional: Make corners smoother
        lineRenderer.numCapVertices = 5;
        lineRenderer.numCornerVertices = 5;
    }

    private void LateUpdate()
    {
        if (startPoint == null || endPoint == null) return;

        DrawTube();
    }

    private void DrawTube()
    {
        lineRenderer.positionCount = resolution;

        Vector3 p0 = startPoint.position;
        Vector3 p2 = endPoint.position;

        // Calculate a mid-point (p1) that is between the two points but lower, to simulate gravity
        Vector3 midPos = (p0 + p2) / 2f;
        midPos.y -= slack;
        Vector3 p1 = midPos;

        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            Vector3 position = CalculateBezierPoint(t, p0, p1, p2);
            lineRenderer.SetPosition(i, position);
        }
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        // Quadratic Bezier Formula: B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }
}