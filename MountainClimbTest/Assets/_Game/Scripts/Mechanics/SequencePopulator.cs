using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SequencePopulatorWindow : EditorWindow
{
    private Object sequenceSO;
    private string soFolderPath = "Assets/ScriptableObjects/Dialogue";
    private int startID = 28;
    private int endID = 63;
    private string listVariableName = "lines"; // Updated to match your variable name
    private bool appendMode = false;

    [MenuItem("Tools/Mountain Rescue/Sequence Populator Pro")]
    public static void ShowWindow()
    {
        GetWindow<SequencePopulatorWindow>("Sequence Populator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Sequence Settings", EditorStyles.boldLabel);

        sequenceSO = EditorGUILayout.ObjectField("Target Sequence SO", sequenceSO, typeof(ScriptableObject), false);
        listVariableName = EditorGUILayout.TextField("List Variable Name", listVariableName);
        soFolderPath = EditorGUILayout.TextField("Dialogue SO Folder", soFolderPath);

        EditorGUILayout.Space();
        GUILayout.Label("ID Range", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        startID = EditorGUILayout.IntField("Start ID", startID);
        endID = EditorGUILayout.IntField("End ID", endID);
        EditorGUILayout.EndHorizontal();

        appendMode = EditorGUILayout.Toggle("Append to existing list?", appendMode);

        EditorGUILayout.Space();
        GUI.enabled = (sequenceSO != null);
        if (GUILayout.Button("Populate Sequence"))
        {
            ProcessPopulation();
        }
        GUI.enabled = true;
    }

    private void ProcessPopulation()
    {
        SerializedObject so = new SerializedObject(sequenceSO);
        SerializedProperty listProperty = so.FindProperty(listVariableName);

        if (listProperty == null || !listProperty.isArray)
        {
            Debug.LogError($"Error: Could not find a List or Array named '{listVariableName}' on {sequenceSO.name}. Check the variable name in your script!");
            return;
        }

        if (!appendMode)
        {
            listProperty.ClearArray();
        }

        int addedCount = 0;
        for (int i = startID; i <= endID; i++)
        {
            // Tries to find the asset named "28", "29", etc.
            string path = $"{soFolderPath}/{i}.asset";
            Object lineAsset = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (lineAsset != null)
            {
                int newIndex = listProperty.arraySize;
                listProperty.InsertArrayElementAtIndex(newIndex);
                listProperty.GetArrayElementAtIndex(newIndex).objectReferenceValue = lineAsset;
                addedCount++;
            }
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        string mode = appendMode ? "added to" : "set in";
        Debug.Log($"Success: {addedCount} lines {mode} {sequenceSO.name}!");
    }
}