using UnityEngine;
using UnityEditor;

public class ScaleRandomizer : EditorWindow
{
    private float minScale = 0.5f;
    private float maxScale = 1.5f;
    private bool uniformScale = true;

    [MenuItem("Tools/Scale Randomizer")]
    public static void ShowWindow()
    {
        GetWindow<ScaleRandomizer>("Scale Randomizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Randomize Scale of Selection", EditorStyles.boldLabel);

        minScale = EditorGUILayout.FloatField("Min. Scale", minScale);
        maxScale = EditorGUILayout.FloatField("Max. Scale", maxScale);
        uniformScale = EditorGUILayout.Toggle("Uniform Scale", uniformScale);

        if (GUILayout.Button("Randomize Selected Objects"))
        {
            RandomizeSelectionScales();
        }
    }

    private void RandomizeSelectionScales()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No Objects selected to randomize");
            return;
        }

        Undo.RecordObjects(Selection.transforms, "Randomize Scale");

        foreach (GameObject obj in selectedObjects)
        {
            if (uniformScale)
            {
                float scale = Random.Range(minScale, maxScale);
                obj.transform.localScale = new Vector3(scale, scale, scale);
            }
            else
            {
                float scaleX = Random.Range(minScale, maxScale);
                float scaleY = Random.Range(minScale, maxScale);
                float scaleZ = Random.Range(minScale, maxScale);
                obj.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }
        }
    }
}