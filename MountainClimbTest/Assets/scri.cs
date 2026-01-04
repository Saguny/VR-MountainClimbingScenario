using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class scri : MonoBehaviour
{

    void Start()
    {
#if UNITY_EDITOR
        GameObject root = GameObject.Find("Tryall_WinterScene");
        if (root == null) return;

        int NumberOfChild = root.transform.childCount;

        for (int i = 0; i < NumberOfChild; i++)
        {
            GameObject t = root.transform.GetChild(i).gameObject;

            // Hinweis: CreateEmptyPrefab und ReplacePrefab sind veraltet (Obsolete)
            // In neueren Unity Versionen nutzt man PrefabUtility.SaveAsPrefabAsset
            string path = "Assets/Fantasy Adventure Environment/Tryall_Winter_Scene" + t.name + ".prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(t, path, InteractionMode.AutomatedAction);
        }
#endif
    }
}