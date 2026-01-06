using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RockScatterTool : EditorWindow
{
    // --- UI VARIABLES ---
    GameObject targetWall;
    List<GameObject> rockPrefabs = new List<GameObject>();

    int spawnCount = 20;
    float minScale = 0.8f;
    float maxScale = 1.2f;

    // Manual tweak if specific rocks still look weird
    Vector3 additionalRotation = Vector3.zero;

    // --- INTERNAL ---
    SerializedObject so;
    SerializedProperty propPrefabs;

    [MenuItem("Tools/Rock Scatterer")]
    public static void ShowWindow()
    {
        GetWindow<RockScatterTool>("Rock Scatterer");
    }

    void OnEnable()
    {
        // Setup for list display in Inspector
        so = new SerializedObject(this);
        propPrefabs = so.FindProperty("rockPrefabs");
    }

    void OnGUI()
    {
        so.Update();

        GUILayout.Label("1. Target Setup", EditorStyles.boldLabel);
        targetWall = (GameObject)EditorGUILayout.ObjectField("Target Wall (Mesh)", targetWall, typeof(GameObject), true);

        GUILayout.Space(10);
        GUILayout.Label("2. Rock Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Ensure these prefabs have their 'Face' on the +X Axis.", MessageType.Info);
        EditorGUILayout.PropertyField(propPrefabs, new GUIContent("Prefabs List"), true);

        GUILayout.Space(10);
        GUILayout.Label("3. Randomization", EditorStyles.boldLabel);
        spawnCount = EditorGUILayout.IntSlider("Count to Spawn", spawnCount, 1, 100);
        minScale = EditorGUILayout.FloatField("Min Scale", minScale);
        maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);

        GUILayout.Space(10);
        GUILayout.Label("4. Fine Tuning", EditorStyles.boldLabel);
        additionalRotation = EditorGUILayout.Vector3Field("Rotation Offset", additionalRotation);

        GUILayout.Space(20);

        if (GUILayout.Button("SCATTER ROCKS (With Undo)", GUILayout.Height(40)))
        {
            ScatterRocks();
        }

        // Helper button to clear previous attempts quickly
        if (GUILayout.Button("Clear All Children of Target"))
        {
            ClearChildren();
        }

        so.ApplyModifiedProperties();
    }

    void ScatterRocks()
    {
        if (targetWall == null || rockPrefabs.Count == 0)
        {
            Debug.LogError("Error: Please assign a Target Wall and at least one Rock Prefab.");
            return;
        }

        Collider wallCollider = targetWall.GetComponent<Collider>();
        if (wallCollider == null)
        {
            Debug.LogError("Error: The Target Wall must have a Collider (MeshCollider or BoxCollider)!");
            return;
        }

        Bounds bounds = wallCollider.bounds;

        // Register Undo so you can Ctrl+Z the whole batch
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Scatter Rocks");
        int undoGroupIndex = Undo.GetCurrentGroup();

        int successCount = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            // 1. Pick a random point roughly within the bounds of the wall
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            // 2. Determine "Out" direction to shoot the ray FROM.
            // Since it's a 2D plane, we can't rely on center-to-point math.
            // We try the object's Forward (Z) first (Standard Quads), then Up (Y) (Standard Planes).

            // Try shooting from Local Forward (+Z)
            if (TrySpawnRay(randomPoint, targetWall.transform.forward, wallCollider))
            {
                successCount++;
                continue;
            }

            // Try shooting from Local Up (+Y) - Common for Planes rotated to be walls
            if (TrySpawnRay(randomPoint, targetWall.transform.up, wallCollider))
            {
                successCount++;
                continue;
            }

            // Try shooting from Local Right (+X) - Just in case
            if (TrySpawnRay(randomPoint, targetWall.transform.right, wallCollider))
            {
                successCount++;
                continue;
            }
        }

        if (successCount == 0) Debug.LogWarning("Could not place any rocks. Check if the Wall Collider is convex or if the Bounds are correct.");

        Undo.CollapseUndoOperations(undoGroupIndex);
    }

    bool TrySpawnRay(Vector3 targetPoint, Vector3 directionOut, Collider collider)
    {
        // Move the origin OUT away from the wall, then shoot BACK IN.
        // We multiply by 2.0f to ensure we are definitely "outside" the mesh before casting back.
        Vector3 rayOrigin = targetPoint + (directionOut * 2.0f);
        Vector3 rayDirection = -directionOut;

        Ray ray = new Ray(rayOrigin, rayDirection);

        // Raycast distance 5.0f covers the 2.0f offset comfortably
        if (collider.Raycast(ray, out RaycastHit hit, 5.0f))
        {
            PlaceOneRock(hit);
            return true;
        }
        return false;
    }

    void PlaceOneRock(RaycastHit hit)
    {
        GameObject prefab = rockPrefabs[Random.Range(0, rockPrefabs.Count)];
        if (prefab == null) return;

        // Create Prefab Link
        GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(rock, "Spawn Rock");

        rock.transform.position = hit.point;

        // --- ROTATION LOGIC ---

        // 1. Align the Prefab's X AXIS (Red Arrow) to the Wall's Normal
        Quaternion alignXToNormal = Quaternion.FromToRotation(Vector3.right, hit.normal);
        rock.transform.rotation = alignXToNormal;

        // 2. Random Spin around the X Axis (the axis sticking out of the wall)
        float randomSpin = Random.Range(0, 360);
        rock.transform.Rotate(Vector3.right, randomSpin, Space.Self);

        // 3. Apply any manual correction from the UI (rarely needed now)
        rock.transform.Rotate(additionalRotation, Space.Self);

        // --- PARENTING & SCALE ---
        rock.transform.parent = targetWall.transform;

        float s = Random.Range(minScale, maxScale);
        rock.transform.localScale = Vector3.one * s;
    }

    void ClearChildren()
    {
        if (targetWall == null) return;

        Undo.IncrementCurrentGroup();
        var children = new List<GameObject>();
        foreach (Transform child in targetWall.transform) children.Add(child.gameObject);

        foreach (var child in children)
        {
            Undo.DestroyObjectImmediate(child);
        }
        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
    }
}