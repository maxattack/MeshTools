//The MIT License (MIT)
//	
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
// 
// ORIGINALLY DEVELOPED BY MAX KAUFMANN
// https://github.com/maxattack/MeshTools

using CSG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MeshTools {
	
	[MenuItem("MeshTools/Flatten Meshes")]
	static void DoFlattenMeshes()
	{
		Selection.activeGameObject.FlattenMeshes();
	}
	
	[MenuItem("CONTEXT/MeshFilter/Remove Garbage")]
	static void Cleanup()
	{
		var mesh = Selection.activeGameObject.GetComponent<MeshFilter>().mesh;
		mesh.RemoveGarbageGeometry();
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
			var filters = GameObject.FindObjectsOfType<MeshFilter>();
			var subs = filters
				.Where(filter => !Selection.gameObjects.Contains(filter.gameObject))
				.Select(filter => filter.CreateSolid())
				.ToArray();
			
			if (subs.Length == 1) {
				solids[0].Subtract(subs[0]).CreateGameObject("Difference");
				filters[0].gameObject.SetActive(false);
			}
				
			
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
