using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MeshFillerCollider : EditorWindow
{
    private GameObject targetObject;
    private float plateSize = 0.5f;
    private float thickness = 0.2f;
    private bool addRigidbody = true;

    [Range(0.1f, 2.0f)]
    private float fillDensity = 0.8f;

    [MenuItem("Tools/Sagun's Fixed Filler")]
    public static void ShowWindow() => GetWindow<MeshFillerCollider>("Fixed Filler");

    private void OnGUI()
    {
        GUILayout.Label("Surface Filler (Scale 176 Fix)", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Target Mesh", targetObject, typeof(GameObject), true);

        GUILayout.Space(5);
        plateSize = EditorGUILayout.FloatField("Plate Size (World Units)", plateSize);
        thickness = EditorGUILayout.FloatField("Thickness", thickness);
        addRigidbody = EditorGUILayout.Toggle("Add Rigidbody", addRigidbody);
        fillDensity = EditorGUILayout.Slider("Fill Density", fillDensity, 0.1f, 1.5f);

        if (GUILayout.Button("Fill Mesh Surface")) Generate();
    }

    private void Generate()
    {
        if (!targetObject) return;
        MeshFilter mf = targetObject.GetComponent<MeshFilter>();
        if (!mf) return;

        Mesh mesh = mf.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = mesh.normals;

        // Container erstellen (erstmal ohne Parent, damit Scale 1,1,1 ist)
        GameObject container = new GameObject("FilledColliders_" + targetObject.name);
        Undo.RegisterCreatedObjectUndo(container, "Generate Filled Colliders");

        // Position und Rotation übernehmen, aber Scale auf 1 lassen (World Scale)
        container.transform.position = targetObject.transform.position;
        container.transform.rotation = targetObject.transform.rotation;
        container.transform.localScale = Vector3.one;

        Vector3 parentLossyScale = targetObject.transform.lossyScale;
        int totalBoxes = 0;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Indizes
            int i1 = triangles[i];
            int i2 = triangles[i + 1];
            int i3 = triangles[i + 2];

            // Local Space Vertices
            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 v3 = vertices[i3];

            // Normalen
            Vector3 n1 = normals[i1];
            Vector3 n2 = normals[i2];
            Vector3 n3 = normals[i3];
            Vector3 localNormal = (n1 + n2 + n3).normalized;

            // Echte World Positionen der Ecken berechnen (hier kommt der Fix für Scale 176)
            Vector3 w1 = targetObject.transform.TransformPoint(v1);
            Vector3 w2 = targetObject.transform.TransformPoint(v2);
            Vector3 w3 = targetObject.transform.TransformPoint(v3);

            // Fläche des echten World-Dreiecks berechnen
            float triangleArea = Vector3.Cross(w2 - w1, w3 - w1).magnitude * 0.5f;

            float boxArea = plateSize * plateSize;
            int boxesNeeded = Mathf.CeilToInt((triangleArea / boxArea) * fillDensity);

            for (int b = 0; b < boxesNeeded; b++)
            {
                // Zufallspunkt auf Dreieck (Barycentric)
                float r1 = Random.value;
                float r2 = Random.value;

                if (r1 + r2 > 1)
                {
                    r1 = 1 - r1;
                    r2 = 1 - r2;
                }

                // WICHTIG: Wir interpolieren jetzt zwischen den WORLD Positionen
                Vector3 worldPos = w1 + r1 * (w2 - w1) + r2 * (w3 - w1);

                CreateBox(container, worldPos, localNormal);
                totalBoxes++;
            }
        }

        // Am Ende parenten wir den Container an das Mesh.
        // "true" sorgt dafür, dass Unity den Scale des Containers runterrechnet (z.B. auf 0.005),
        // damit die World-Size der Boxen erhalten bleibt.
        container.transform.SetParent(targetObject.transform, true);

        Debug.Log($"Fertig! {totalBoxes} Boxen in korrekter Größe generiert.");
    }

    private void CreateBox(GameObject container, Vector3 worldPos, Vector3 localNormal)
    {
        GameObject box = new GameObject("Plate");
        // Wir setzen Parent auf container (der aktuell noch Scale 1,1,1 hat)
        box.transform.SetParent(container.transform, true);
        box.transform.position = worldPos;

        // Rotation: Local Normal in World Normal wandeln
        Vector3 worldNormal = targetObject.transform.TransformDirection(localNormal);
        if (worldNormal != Vector3.zero)
            box.transform.rotation = Quaternion.LookRotation(worldNormal);

        // Da wir im World Space arbeiten, setzen wir die Größe direkt
        // Thickness als Z (Tiefe), PlateSize als X/Y
        box.transform.localScale = new Vector3(plateSize, plateSize, thickness);

        BoxCollider bc = box.AddComponent<BoxCollider>();
        bc.size = Vector3.one;

        if (addRigidbody)
        {
            Rigidbody rb = box.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}