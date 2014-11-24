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
using System.IO;
using UnityEngine;

public static partial class MeshExtensions
{
	// GIFT IDEAS: 
	// - Coalesce Adjacent Coplanar Faces
	//   - Harder than it looks: need to retriangulate w/o internal verts :P

	// Given the following logical positions and faces, 
	// compute raw vertices, normals, and triangles.
	// TODO: UV sets?
	
	public static void CreateModel(this Mesh mesh, IList<Vector3> positions, IList<IList<int>> faces)
	{
		
		// validate the input while we count up the magnitudes
		int totalVertices = 0; 
		int totalTriangles = 0;
		foreach (var face in faces) { 
			if (face.Count < 3) {
				Debug.LogWarning("Face has Too Few Vertices");
				return;
			}
			foreach (var vert in face) {
				if (vert < 0 || vert >= positions.Count) {
					Debug.LogWarning("Index out of Bounds :P");
					return;
				}
			}
			totalVertices += face.Count; 
			totalTriangles += face.Count - 2;
		}
		
		// plot vertices
		var vbuf = new Vector3[totalVertices]; 
		var nbuf = new Vector3[totalVertices];
		var tbuf = new Vector2[totalVertices];
		var ibuf = new int[3 * totalTriangles];
		int i = 0, j = 0;
		foreach (var face in faces) {
			
			// for now, we make the simplistic assumption that eaech face can
			// be "fanned out" from the first vertex.  This limits us to 
			// convex shapes.  (Perhaps there's some ear-cutting in Unity's
			// 2D API that we can leverage?)
			for (int k=0; k<face.Count-2; ++k) {
				ibuf [j++] = i;
				ibuf [j++] = i + k + 1;
				ibuf [j++] = i + k + 2;
			}
			
			var normal = Vector3.Cross(
				positions [face [1]] - positions [face [0]],
				positions [face [2]] - positions [face [0]]
			).normalized;
			
			foreach (var vert in face) {
				vbuf [i] = positions [vert];
				nbuf [i] = normal;
				tbuf [i] = Vector2.zero;
				++i;
			}
			
		}
		
		mesh.vertices = vbuf;
		mesh.normals = nbuf;
		mesh.uv = tbuf;
		mesh.triangles = ibuf;
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.Optimize();
	}
	
	public static Mesh FreezeTransformation(this MeshFilter filter)
	{
		var xform = filter.GetComponent<Transform>();
		var mat = xform.localToWorldMatrix;
		if (xform.parent != null) {
			mat = mat * xform.parent.worldToLocalMatrix;
		}
		var oldMesh = filter.sharedMesh;
		var verts = oldMesh.vertices;
		var norms = oldMesh.normals;
		for(int i=0, len=verts.Length; i<len; ++i) {
			verts[i] = mat.MultiplyPoint3x4(verts[i]);
			norms[i] = mat.MultiplyVector(norms[i]).normalized;
		}
		var newMesh = new Mesh() {
			name = oldMesh.name+"_frozen",
			vertices = verts,
			normals = norms,
			colors32 = oldMesh.colors32,
			uv = oldMesh.uv,
			triangles = oldMesh.triangles
		};
		
		xform.localPosition = Vector3.zero;
		xform.localRotation = Quaternion.identity;
		xform.localScale = Vector3.one;
		filter.sharedMesh = newMesh;
		
		return newMesh;
	}
	
	public static Mesh MapMesh(this Mesh mesh, Func<Vector3, Vector3> f) {
		var verts = mesh.vertices;
		for(int i=0, len=verts.Length; i<len; ++i) {
			verts[i] = f(verts[i]);
		}
		var result = new Mesh() {
			name = mesh.name+"_mapped",
			vertices = verts,
			colors32 = mesh.colors32,
			uv = mesh.uv,
			triangles = mesh.triangles
		};
		result.RecalculateNormals();
		return result;		
	}

	public static Mesh MapMeshFilter(this MeshFilter filter, Func<Vector3, Vector3> f) {
		var mesh = filter.sharedMesh;
		var xform = filter.GetComponent<Transform>();
		var toWorld = xform.localToWorldMatrix;
		var toLocal = xform.worldToLocalMatrix;
		return mesh.MapMesh(
			v => toLocal.MultiplyPoint(f(toWorld.MultiplyPoint3x4(v)))
		);
	}
	
	// Set all the vertices of the given mesh to the given color
	
	public static void SetColor(this Mesh mesh, Color32 c)
	{
		var cbuf = mesh.colors32;
		if (cbuf.Length == 0) {
			cbuf = new Color32[mesh.vertexCount];
		}
		for (int i=0; i<cbuf.Length; ++i) {
			cbuf [i] = c;
		}
		mesh.colors32 = cbuf;
	}
	
	// Recursively look up MeshFilters, and if there's more than one then combine them
	// all into a single mesh in the root.
	
	public static void FlattenMeshes(this GameObject go)
	{
		var filters = go.GetComponentsInChildren<MeshFilter>();
		if (filters.Length <= 1) {
			return;
		}
		
		var filter = go.GetComponent<MeshFilter>();
		if (filter == null) {
			filter = go.AddComponent<MeshFilter>();
		}
		
		var renderer = go.GetComponent<MeshRenderer>();
		if (renderer == null) {
			renderer = go.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = filters [0].GetComponent<Renderer>().sharedMaterial;
		}
		
		var xform = go.GetComponent<Transform>();
		var worldToRoot = xform.worldToLocalMatrix;
		
		var ci = new CombineInstance[filters.Length];
		for (int i=0; i<ci.Length; ++i) {
			ci [i].mesh = filters [i].sharedMesh;
			ci [i].transform = worldToRoot * filters [i].GetComponent<Transform>().localToWorldMatrix;
			filters [i].gameObject.SetActive(false);
		}
		
		var result = new Mesh() { name = go.name+"_concatenated" };
		result.CombineMeshes(ci);
		result.RemoveInternalFaces();
		result.RemoveUnusedVertices();
		result.Optimize();
		filter.sharedMesh = result;
	}
	
	// Search for co-incident triangles, and if they're facing the same way remove one,
	// otherwise if they've facing each other remove both (like they're "cancelling each
	// other out"), and return the number removed.
	
	public static int RemoveInternalFaces(this Mesh mesh)
	{
		
		var vbuf = mesh.vertices;
		var ibuf = mesh.triangles;
		
		// dedup based on positions
		var oldToNew = new int[vbuf.Length];
		for (int i=0; i<vbuf.Length; ++i) {
			oldToNew [i] = i;
		}
		for (int i=0; i<vbuf.Length; ++i) {
			if (oldToNew [i] == i) {
				for (int j=0; j<i; ++j) {
					var offset = vbuf [i] - vbuf [j];
					var epsilon = 0.01f;
					if (offset.sqrMagnitude < epsilon * epsilon) {
						oldToNew [i] = j;
						break;
					}
				}
			}
		}
		
		// compare face-pairs looking for coincident verts, which indicate
		// either a double-face or cancelling-faces
		var nfaces = ibuf.Length / 3;
		var facesToRemove = new HashSet<int>();
		for (int i=0; i<nfaces; ++i) {
			var i0 = oldToNew [ibuf [3 * i]];
			var i1 = oldToNew [ibuf [3 * i + 1]];
			var i2 = oldToNew [ibuf [3 * i + 2]];
			for (int j=0; j<i; ++j) {
				var j0 = oldToNew [ibuf [3 * j]];
				var j1 = oldToNew [ibuf [3 * j + 1]];
				var j2 = oldToNew [ibuf [3 * j + 2]];
				if (Same(i0, i1, i2, j0, j1, j2)) {
					// do we remove just one or both faces?
					facesToRemove.Add(i);
					var dir1 = Vector3.Cross(vbuf [i1] - vbuf [i0], vbuf [i2] - vbuf [i0]);
					var dir2 = Vector3.Cross(vbuf [j1] - vbuf [j0], vbuf [j2] - vbuf [j0]);
					if (Vector3.Dot(dir1, dir2) < 0f) {
						facesToRemove.Add(j);
					}
				}
			}
		}
		
		if (facesToRemove.Count > 0) {
			var inew = new int[3 * (nfaces - facesToRemove.Count)];
			int j = 0;
			for (int i=0; i<nfaces; ++i) {
				if (!facesToRemove.Contains(i)) {
					inew [3 * j] = ibuf [3 * i];
					inew [3 * j + 1] = ibuf [3 * i + 1];
					inew [3 * j + 2] = ibuf [3 * i + 2];
					++j;
				}
			} 
			mesh.triangles = inew;
			return facesToRemove.Count;
		} else {
			return 0;
		}
	}
	
	static bool Same(int i0, int i1, int i2, int j0, int j1, int j2)
	{
		// used in deduping faces
		return (i0 == j0 && i1 == j1 && i2 == j2) || // all same
			(i0 == j0 && i1 == j2 && i2 == j1) || // just 0 same
			(i0 == j2 && i1 == j1 && i2 == j0) || // just 1 same
			(i0 == j1 && i1 == j0 && i2 == j2) || // just 2 same
			(i0 == j1 && i1 == j2 && i2 == j0) || // rotated right
			(i0 == j2 && i1 == j0 && i2 == j1);  // rotated left
	}
	
	// Identify and remove vertices that are not references by the 
	// index buffer, and return the number removed.
			
	public static int RemoveUnusedVertices(this Mesh mesh)
	{
		var vbuf = mesh.vertices;
		var cbuf = mesh.colors32;
		var nbuf = mesh.normals;
		var tbuf = mesh.uv;
		var ibuf = mesh.triangles;
		
		var unusedVertices = new HashSet<int>();
		for (int i=0; i<vbuf.Length; ++i) {
			unusedVertices.Add(i);
		}
		foreach (var i in ibuf) {
			unusedVertices.Remove(i);
		}
		
		if (unusedVertices.Count > 0) {
			var len = vbuf.Length - unusedVertices.Count;
			var oldToNew = new int[vbuf.Length];
			var v2 = new Vector3[len];
			var c2 = new Color32[len];
			var n2 = new Vector3[len];
			var t2 = new Vector2[len];
			int j = 0;
			for (int i=0; i<vbuf.Length; ++i) {
				if (unusedVertices.Contains(i)) {
					oldToNew [i] = -1;
				} else {
					oldToNew [i] = j;
					v2 [j] = vbuf [i];
					c2 [j] = cbuf [i];
					n2 [j] = nbuf [i];
					t2 [j] = tbuf [i];
					++j;
				}
			}
			for (int i=0; i<ibuf.Length; ++i) {
				ibuf [i] = oldToNew [ibuf [i]];
			}
			mesh.Clear();
			mesh.vertices = v2;
			mesh.colors32 = c2;
			mesh.normals = n2;
			mesh.uv = t2;
			mesh.triangles = ibuf;
		}
		return unusedVertices.Count;
		
	}
	
	// Search the vertex/normal/texture/color buffers and dedup values,
	// updating the index buffer to reflect the compacted array.  Returns
	// the number of dups removed.
	
	public static int DedupVertices(this Mesh mesh)
	{
		var vbuf = mesh.vertices;
		var cbuf = mesh.colors32;
		var nbuf = mesh.normals;
		var tbuf = mesh.uv;
		var ibuf = mesh.triangles;
		
		var oldToNew = new int[vbuf.Length];
		
		int dedupCount = 0;
		
		for (int i=0; i<vbuf.Length; ++i) {
			oldToNew [i] = i;
			
			// check to see if we match any vertices we've already seen
			for (int j=0; j<i; ++j) {
				if (
					vbuf [i].Approx(vbuf [j]) && 
					(cbuf.Length == 0 || cbuf [i].ARGB() == cbuf [j].ARGB()) &&
					Vector3.Dot(nbuf [i], nbuf [j]).Approx(1f) && 
					tbuf [i].Approx(tbuf [j])
				) {
					oldToNew [j] = i;
					++dedupCount;
					break;
				}
			}
		}
		
		if (dedupCount > 0) {
			for (int i=0; i<ibuf.Length; ++i) {
				ibuf [i] = oldToNew [ibuf [i]];
			}
			mesh.triangles = ibuf;
			return mesh.RemoveUnusedVertices();
		} else {
			return 0;
		}
	}
	
	static int ARGB(this Color32 c)
	{ 
		return (c.a << 24) + (c.r << 16) + (c.g << 8) + c.b; 
	}
	
	
}

