using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SnugColliderGenerator : EditorWindow
{
    private GameObject targetObject;
    private float plateSize = 2f;    // Width/Height of the box
    private float thickness = 0.5f;  // How thick the collider is
    private float offset = 0.0f;     // Fine-tune depth
    private bool addRigidbody = true;

    [MenuItem("Tools/Sagun's Snug Plater")]
    public static void ShowWindow() => GetWindow<SnugColliderGenerator>("Snug Collider Gen");

    private void OnGUI()
    {
        GUILayout.Label("Surface Plater (Rotated to Mesh)", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Mesh", targetObject, typeof(GameObject), true);

        GUILayout.Space(10);
        plateSize = EditorGUILayout.FloatField("Plate Size (Width)", plateSize);
        thickness = EditorGUILayout.FloatField("Thickness (Depth)", thickness);
        offset = EditorGUILayout.FloatField("Depth Offset", offset);
        addRigidbody = EditorGUILayout.Toggle("Add Rigidbody", addRigidbody);

        if (GUILayout.Button("Generate Snug Colliders")) Generate();
    }

    private void Generate()
    {
        if (!targetObject) return;
        MeshCollider mc = targetObject.GetComponent<MeshCollider>();
        if (!mc)
        {
            EditorUtility.DisplayDialog("Error", "Target needs a MeshCollider!", "OK");
            return;
        }

        Transform parent = targetObject.transform;

        // 1. Create a container
        GameObject container = new GameObject("SnugColliders_" + targetObject.name);
        Undo.RegisterCreatedObjectUndo(container, "Generate Snug Colliders");
        container.transform.SetParent(parent, false); // Parent to mesh
        container.transform.localPosition = Vector3.zero;
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one; // Reset scale to handle the 176 properly

        Bounds bounds = mc.bounds;

        // We calculate step size based on World Scale to ensure we cover the area properly
        // If parent scale is 176, we need to adjust our loop so we don't take 10 million steps.
        float stepX = plateSize;
        float stepY = plateSize;
        float stepZ = plateSize;

        List<Vector3> placedPositions = new List<Vector3>();
        int count = 0;

        // 2. Loop through a grid SURROUNDING the object
        for (float x = bounds.min.x; x <= bounds.max.x; x += stepX)
        {
            for (float y = bounds.min.y; y <= bounds.max.y; y += stepY)
            {
                for (float z = bounds.min.z; z <= bounds.max.z; z += stepZ)
                {

                    Vector3 scanPoint = new Vector3(x, y, z);

                    // 3. Find the closest point on the actual mesh surface from our grid point
                    Vector3 closestSurfacePoint = mc.ClosestPoint(scanPoint);

                    // Optimization: If the grid point is too far from the surface, skip it
                    if (Vector3.Distance(scanPoint, closestSurfacePoint) > plateSize * 2) continue;

                    // 4. Raycast to get the Normal (Rotation)
                    // ClosestPoint gives position, but not the Normal. We raycast FROM slightly outside TO the surface.
                    Vector3 directionIn = (closestSurfacePoint - scanPoint).normalized;

                    // Fallback: if scanPoint is exactly on surface, offset it slightly
                    if (directionIn == Vector3.zero) directionIn = Vector3.up;

                    Ray ray = new Ray(scanPoint, directionIn);
                    if (mc.Raycast(ray, out RaycastHit hit, plateSize * 3))
                    {

                        // Check if we already placed a box too close to this spot (prevent stacking)
                        if (IsTooClose(hit.point, placedPositions, plateSize * 0.5f)) continue;

                        CreateBox(hit, container);
                        placedPositions.Add(hit.point);
                        count++;
                    }
                }
            }
        }

        Debug.Log($"Generated {count} snug colliders.");
    }

    private void CreateBox(RaycastHit hit, GameObject container)
    {
        GameObject box = new GameObject("Plate");
        box.transform.SetParent(container.transform, true); // Keep world pos

        // A. ALIGNMENT: Rotate the box so its "Up" faces the wall normal
        box.transform.rotation = Quaternion.LookRotation(hit.normal);

        // B. POSITION: Place at hit point, then move INWARD by half thickness so it's flush
        // We also apply the user 'offset' if they want to bury it deeper
        float depthAdjustment = (thickness / 2) + offset;
        box.transform.position = hit.point - (hit.normal * depthAdjustment);

        // C. SCALING: We must compensate for the parent's massive scale (176)
        // Since the box is a child of the 176-scale parent, we divide our desired size by that scale.
        Vector3 parentScale = targetObject.transform.lossyScale;

        // We make it a flat "Plate" (Wide X/Y, Thin Z)
        box.transform.localScale = new Vector3(
            plateSize / parentScale.x,
            plateSize / parentScale.y, // Using Y for width/height symmetry
            thickness / parentScale.z
        );

        // D. PHYSICS
        BoxCollider bc = box.AddComponent<BoxCollider>();
        // Since we scaled the transform, the collider size can just be 1,1,1
        bc.size = Vector3.one;

        if (addRigidbody)
        {
            Rigidbody rb = box.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private bool IsTooClose(Vector3 pos, List<Vector3> list, float minDist)
    {
        // Simple distance check to prevent overlapping boxes on the exact same spot
        foreach (Vector3 p in list)
        {
            if (Vector3.SqrMagnitude(pos - p) < minDist * minDist) return true;
        }
        return false;
    }
}