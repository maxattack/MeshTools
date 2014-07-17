using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public struct Face {
	public Color32 color;
	public IList<int> vertices;
	
	public Face(Color32 aColor, IList<int> aVertices) {
		color = aColor;
		vertices = aVertices;
	}
	
}

public static class MeshTools
 {


	// Given the following logical positions and faces, 
	// compute raw vertices, normals, and triangles.
	// TODO: UV sets?
	public static void CreateModel(this Mesh mesh, IList<Vector3> positions, IList<Face> faces) 
	{
		
		// validate the input while we count up the magnitudes
		int totalVertices = 0; 
		int totalTriangles = 0;
		foreach (var face in faces) { 
			if (face.vertices.Count < 3) {
				Debug.LogWarning("Face has Too Few Vertices");
				return;
			}
			foreach(var vert in face.vertices) {
				if (vert < 0 || vert >= positions.Count) {
					Debug.LogWarning("Index out of Bounds :P");
					return;
				}
			}
			totalVertices += face.vertices.Count; 
			totalTriangles += face.vertices.Count-2;
		}
		
		// plot vertices
		var vbuf = new Vector3[totalVertices]; 
		var cbuf = new Color32[totalVertices];
		var tbuf = new Vector2[totalVertices];
		var ibuf = new int[3 * totalTriangles];
		int i=0, j=0;
		foreach(var face in faces)  {
			
			// for now, we make the simplistic assumption that eaech face can
			// be "fanned out" from the first vertex.  This limits us to 
			// convex shapes.  (Perhaps there's some ear-cutting in Unity's
			// 2D API that we can leverage?)
			for(int k=0; k<face.vertices.Count-2; ++k) {
				ibuf[j++] = i;
				ibuf[j++] = i+k+1;
				ibuf[j++] = i+k+2;
			}
			
			foreach(var vert in face.vertices) {
				vbuf[i] = positions[vert];
				cbuf[i] = face.color;
				tbuf[i] = Vector2.zero;
				++i;
			}
			
		}
		
		mesh.vertices = vbuf;
		mesh.colors32 = cbuf;
		mesh.uv = tbuf;
		mesh.triangles = ibuf;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.Optimize();
		
		
	}
	
	#if UNITY_EDITOR	
	
	static Vector3 vec(float x, float y, float z)
	{
		return new Vector3(x,y,z);
	}
	
	static Face face(params int[] aVertices) {
		return new Face(Color.white, aVertices);
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
	
	#endif
}

