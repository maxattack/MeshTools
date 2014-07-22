using CSG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MeshTools {
	
	static Vector3 vec(float x, float y, float z) { return new Vector3(x,y,z); }
	static Face face(params int[] aVertices) { return new Face(Color.white, aVertices); }
	
	[MenuItem("MeshTools/Flatten Meshes")]
	static void DoFlattenMeshes()
	{
		Selection.activeGameObject.FlattenMeshes();
	}
	
	[MenuItem("MeshTools/Test CreateModel()")]
	static void TestCreateModel()
	{
		var result = new Mesh();
		result.CreateModel(new Vector3[] {
			
			vec ( 0, 0, 0 ), // 0
			vec ( 1, 0, 0 ), // 1
			vec ( 0, 1, 0 ), // 2
			vec ( 0, 0, 1 )  // 3
				
		}, new Face[] {
			
			face (0, 2, 1),
			face (0, 3, 2),
			face (0, 1, 3),
			face (2, 3, 1)
				
		});
		
		new GameObject("Test", typeof(MeshFilter), typeof(MeshRenderer))
			.GetComponent<MeshFilter>().sharedMesh = result;
		
	}
	
	[MenuItem("CONTEXT/MeshFilter/Create Asset")]
	static void CreateAssetForMesh()
	{
		var mesh = Selection.activeGameObject.GetComponent<MeshFilter>().sharedMesh;
		if (mesh == null) { Debug.Log ("derp"); return; }		
		var path = EditorUtility.SaveFilePanel("Mesh Path", "Assets", "mesh", "asset");
		if (path.Length > 0) {
			var idx = path.IndexOf("Assets");
			if (idx >= 0) {
				try {
					AssetDatabase.CreateAsset(mesh, path.Substring(idx));
				} catch(UnityException) {
					AssetDatabase.CreateAsset(Mesh.Instantiate(mesh) as Mesh, path.Substring(idx));
				}
			}
		}
	}

	[MenuItem("MeshTools/Test CSG")]
	static void TestCSG() {
		var solids = Selection.gameObjects
			.Select(go => go.GetComponent<MeshFilter>())
			.Where(filter => filter != null)
			.Select(filter => filter.CreateSolid())
			.ToArray();
		
		if (solids.Length == 1) {
			solids[0].CreateGameObject("Dup");
		} else if (solids.Length == 2) {
			
			var intersection = solids[0].Intersect(solids[1]);
			intersection.CreateGameObject("Intersection");
			
			
		} else if (solids.Length > 2) {
			
			var accum = solids[0];
			for(int i=1; i<solids.Length; ++i) {
				accum = accum.Union(solids[i]);
			}
			accum.CreateGameObject("Union");
			
		}

		foreach(var go in Selection.gameObjects) {
			go.SetActive(false);
		}
		
	}

}
