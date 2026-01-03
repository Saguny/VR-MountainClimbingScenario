using UnityEngine;
using UnityEditor;

public class DialogueRenamer : EditorWindow
{
    [MenuItem("Tools/Mountain Rescue/Rename Selected Dialogue")]
    public static void RenameSelected()
    {
        // Get all selected assets in the Project window
        Object[] selectedObjects = Selection.objects;

        // Note: Selection.objects doesn't guarantee the order you see in the folder.
        // It's better to sort them by their current name first.
        System.Array.Sort(selectedObjects, (a, b) => a.name.CompareTo(b.name));

        int startNumber = 28;

        for (int i = 0; i < selectedObjects.Length; i++)
        {
            string newName = (startNumber + i).ToString();
            AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(selectedObjects[i]), newName);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Renamed {selectedObjects.Length} assets to be sequential starting from 28.");
    }
}