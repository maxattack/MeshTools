using System.Collections;
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
		
		//		new GameObject("Test", typeof(MeshFilter), typeof(MeshRenderer))
		//			.GetComponent<MeshFilter>().sharedMesh = result;
		
		//		var path = EditorUtility.SaveFilePanel("Mesh Path", "Assets", "testModel", "asset");
		var path = "Assets/mesh.asset";
		if (path.Length > 0) {
			Debug.Log("Saving " + result + " to path: " + path);
			AssetDatabase.CreateAsset(result, path);
		} 
		
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
				AssetDatabase.CreateAsset(mesh, path.Substring(idx));
			}
		}
	}

}
