using MountainRescue.Dialogue;
using UnityEditor;
using UnityEngine;

public class DialogueLinker : EditorWindow
{
    [MenuItem("Tools/Mountain Rescue/Link Audio to SO")]
    public static void LinkAudio()
    {
        // Get all selected Scriptable Objects in the Project window
        Object[] selectedSOs = Selection.objects;
        int count = 0;

        foreach (Object obj in selectedSOs)
        {
            // Only process if it's your specific Scriptable Object type
            if (obj is DialogueLine so)
            {
                // Search for an AudioClip that has the SAME name as the SO
                string[] guids = AssetDatabase.FindAssets($"{so.name} t:AudioClip");

                if (guids.Length > 0)
                {
                    // Get the path of the first matching clip found
                    string clipPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);

                    // Assign the clip to the SO
                    so.voiceClip = clip;

                    // Tell Unity the SO has changed so it saves
                    EditorUtility.SetDirty(so);
                    count++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Successfully linked {count} Audio Clips to their matching SOs.");
    }


}