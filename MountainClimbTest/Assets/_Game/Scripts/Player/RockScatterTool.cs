using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RockScatterTool : EditorWindow
{
    // --- UI VARIABLES ---
    [SerializeField] GameObject targetWall;
    [SerializeField] List<GameObject> rockPrefabs = new List<GameObject>();

    [SerializeField] int spawnCount = 20;
    [SerializeField] float minScale = 0.8f;
    [SerializeField] float maxScale = 1.2f;

    [SerializeField] Vector3 additionalRotation = Vector3.zero;

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
        if (propPrefabs != null)
        {
            EditorGUILayout.PropertyField(propPrefabs, new GUIContent("Prefabs List"), true);
        }

        GUILayout.Space(10);
        GUILayout.Label("3. Settings", EditorStyles.boldLabel);
        spawnCount = EditorGUILayout.IntSlider("Count", spawnCount, 1, 100);
        minScale = EditorGUILayout.FloatField("Min Scale", minScale);
        maxScale = EditorGUILayout.FloatField("Max Scale", maxScale);

        GUILayout.Space(10);
        GUILayout.Label("4. Fine Tuning", EditorStyles.boldLabel);
        additionalRotation = EditorGUILayout.Vector3Field("Rotation Offset", additionalRotation);

        GUILayout.Space(20);

        if (GUILayout.Button("SCATTER ROCKS", GUILayout.Height(40)))
        {
            ScatterRocks();
        }

        if (GUILayout.Button("Clear Last Generated Batch"))
        {
            ClearLastBatch();
        }

        so.ApplyModifiedProperties();
    }

    void ScatterRocks()
    {
        if (targetWall == null || rockPrefabs.Count == 0)
        {
            Debug.LogError("Error: Please assign a Wall and a Prefab.");
            return;
        }

        Collider wallCollider = targetWall.GetComponent<Collider>();
        if (wallCollider == null)
        {
            Debug.LogError("Error: Target Wall needs a Collider!");
            return;
        }

        Bounds bounds = wallCollider.bounds;

        // Create a clean container so rocks don't get skewed by Wall scale
        GameObject container = GameObject.Find("Generated_Rocks");
        if (container == null) container = new GameObject("Generated_Rocks");

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Scatter Rocks");
        int undoGroupIndex = Undo.GetCurrentGroup();
        Undo.RegisterCreatedObjectUndo(container, "Create Container");

        int actuallySpawned = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            bool placed = false;

            // RETRY LOOP: Try 20 times to find a valid spot for THIS specific rock
            // (Fixes the issue where picking a random point in bounds misses the mesh)
            for (int attempt = 0; attempt < 20; attempt++)
            {
                Vector3 randomPoint = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                // Try shooting from different local directions
                if (TrySpawnRay(randomPoint, targetWall.transform.forward, wallCollider, container)) { placed = true; break; }
                if (TrySpawnRay(randomPoint, targetWall.transform.up, wallCollider, container)) { placed = true; break; }
                if (TrySpawnRay(randomPoint, targetWall.transform.right, wallCollider, container)) { placed = true; break; }
            }

            if (placed) actuallySpawned++;
        }

        if (actuallySpawned < spawnCount)
        {
            Debug.LogWarning($"Only managed to place {actuallySpawned}/{spawnCount} rocks. The wall might be too thin or the bounds too large relative to the mesh.");
        }

        Undo.CollapseUndoOperations(undoGroupIndex);
    }

    bool TrySpawnRay(Vector3 targetPoint, Vector3 directionOut, Collider collider, GameObject container)
    {
        // Move point OUT, shoot IN
        Vector3 rayOrigin = targetPoint + (directionOut * 4.0f); // Increased distance to 4.0 to catch more hits
        Vector3 rayDirection = -directionOut;

        Ray ray = new Ray(rayOrigin, rayDirection);

        if (collider.Raycast(ray, out RaycastHit hit, 10.0f))
        {
            PlaceOneRock(hit, container);
            return true;
        }
        return false;
    }

    void PlaceOneRock(RaycastHit hit, GameObject container)
    {
        GameObject prefab = rockPrefabs[Random.Range(0, rockPrefabs.Count)];
        if (prefab == null) return;

        GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(rock, "Spawn Rock");

        rock.transform.position = hit.point;

        // 1. Align X (Face) to Normal
        Quaternion alignXToNormal = Quaternion.FromToRotation(Vector3.right, hit.normal);
        rock.transform.rotation = alignXToNormal;

        // 2. Random Spin
        float randomSpin = Random.Range(0, 360);
        rock.transform.Rotate(Vector3.right, randomSpin, Space.Self);

        // 3. Manual Offset
        rock.transform.Rotate(additionalRotation, Space.Self);

        // 4. PARENTING FIX: Parent to the clean container, NOT the wall
        rock.transform.parent = container.transform;

        // 5. Apply Scale (Now safe from parent distortion)
        float s = Random.Range(minScale, maxScale);
        rock.transform.localScale = Vector3.one * s;
    }

    void ClearLastBatch()
    {
        GameObject container = GameObject.Find("Generated_Rocks");
        if (container != null)
        {
            Undo.DestroyObjectImmediate(container);
        }
    }
}