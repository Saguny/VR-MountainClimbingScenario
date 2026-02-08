using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

public class TreeReplacer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Drag the new Tree Prefabs here (e.g., SnowTree1, SnowTree2, SnowTree3)")]
    public List<GameObject> newTreePrefabs;

    [Tooltip("Drag the parent object that holds all your OLD trees here.")]
    public Transform oldTreesParent;

    [Tooltip("If true, keeps the random rotation of the old tree.")]
    public bool keepRotation = true;

    [Tooltip("If true, keeps the scale of the old tree.")]
    public bool keepScale = false;

    // This adds a button to the component right-click menu
    [ContextMenu("Replace All Trees Now")]
    public void ReplaceTrees()
    {
        if (newTreePrefabs == null || newTreePrefabs.Count == 0)
        {
            Debug.LogError("Error: No new prefabs assigned!");
            return;
        }

        if (oldTreesParent == null)
        {
            Debug.LogError("Error: No Old Trees Parent assigned!");
            return;
        }

        // We loop backwards so we can destroy objects without breaking the loop
        int childCount = oldTreesParent.childCount;
        Undo.IncrementCurrentGroup(); // Group undo for Ctrl+Z support
        Undo.SetCurrentGroupName("Replace Trees");

        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform oldTree = oldTreesParent.GetChild(i);

            // 1. Pick a random new prefab
            GameObject randomPrefab = newTreePrefabs[Random.Range(0, newTreePrefabs.Count)];

            // 2. Instantiate it safely (linked as a Prefab)
            GameObject newTree = (GameObject)PrefabUtility.InstantiatePrefab(randomPrefab);

            // 3. Match position and settings
            newTree.transform.position = oldTree.position;
            newTree.transform.parent = oldTreesParent; // Keep hierarchy clean

            if (keepRotation) newTree.transform.rotation = oldTree.rotation;
            if (keepScale) newTree.transform.localScale = oldTree.localScale;

            // 4. Register Undo so we can revert
            Undo.RegisterCreatedObjectUndo(newTree, "Create New Tree");
            Undo.DestroyObjectImmediate(oldTree.gameObject);
        }

        Debug.Log($"Success! Replaced {childCount} trees.");
    }
}
#endif