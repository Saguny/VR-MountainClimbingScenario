using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class scri : MonoBehaviour {



	void Start(){
		
		int NumberOfChild = GameObject.Find("Tryall_WinterScene").transform.childCount;

		for (int i = 0; i < NumberOfChild; i++){
		GameObject t = GameObject.Find("Tryall_WinterScene").transform.GetChild(i).gameObject;

		Object prefab = PrefabUtility.CreateEmptyPrefab("Assets/Fantasy Adventure Environment/Tryall_Winter_Scene"+ t.gameObject.name+".prefab");
		PrefabUtility.ReplacePrefab(t.gameObject, prefab, ReplacePrefabOptions.ConnectToPrefab);
		}
	}
}
