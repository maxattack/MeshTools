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
// ORIGINAL JAVASCRIPT IMPLEMENTATION BY EVAN WALLACE
// https://github.com/evanw/csg.js/
//
// ADAPRTED TO UNITY/C# BY MAX KAUFMANN
// - Renamed CSG to Solid
// - Added Polygon.Simplify(), Solid.Simplify()
// - Various Microoptimizations
// - Added UnityEngine Mesh Asset Import / Export
// https://github.com/maxattack/MeshTools


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CSG {
	
	// A simple value-type representing polygon a polygon vertex.  Unlike Unity Mesh vertices:
	//   - pverts are interleaved in a single array
	//   - pverts can form faces with shapes other than triangles
	public struct Vertex
	{
		public Vector3 pos;
		public Vector3 normal;
		public Color32 color;
		public Vector2 uv;
		
		public Vertex(Vector3 aPos, Vector3 aNormal, Color32 aColor, Vector2 aUV)
		{
			pos = aPos;
			normal = aNormal;
			color = aColor;
			uv = aUV;
		}
		
		public Vertex Flipped 
		{ 
			get { return new Vertex(pos, -normal, color, uv); } 
		}
		
		public Vertex Interpolate(Vertex v, float t)
		{
			return new Vertex(
				Vector3.Lerp(pos, v.pos, t),
				Vector3.Slerp(normal, v.normal, t),
				Color.Lerp(color, v.color, t),
				Vector2.Lerp(uv, v.uv, t)
			);
		}
	}
	
	// A generalization of a mesh face that can have >3 points
	public class Polygon
	{
		public Vertex[] vertices;
		public object shared = null;
		Polygon flipped = null;
		
		public Polygon(params Vertex[] args)
		{
			vertices = args;
		}
		
		public Plane Plane {
			get { return new Plane(vertices[0].pos, vertices[1].pos, vertices[2].pos); }
		}
		
		// Compute (and memoize) a flipped version of this polygon
		
		Vertex[] DoFlipVerts()
		{
			var verts = new Vertex[vertices.Length];
			for(int i=0; i<verts.Length; ++i) {
				verts[i] = vertices[vertices.Length-1-i].Flipped;
			}
			return verts;
		}
		
		public Polygon Flipped()
		{
			if (flipped == null) {
				flipped = new Polygon(DoFlipVerts()) { shared = shared };
				flipped.flipped = this;
			}
			return flipped;
		}
		
		public bool IsDegenerate()
		{
			for(int i=0; i<vertices.Length; ++i) {
				var i1 = (i + 1) % vertices.Length;
				if (vertices[i].pos.Approx(vertices[i1].pos)) {
					return true;
				}
			}
			return false;
		}
		
		public bool IsConvex()
		{
			// A polygon is convex is all the angles are "the same way", i.e., the 
			// cross products are all pointing in the same direction
			var c0 = Vector3.Cross(vertices[1].pos - vertices[0].pos, vertices[2].pos - vertices[0].pos); 
			for(int i=1; i<vertices.Length; ++i) {
				var v0 = vertices[i];
				var v1 = vertices[(i+1) % vertices.Length];
				var v2 = vertices[(i+2) % vertices.Length];
				var c = Vector3.Cross(v1.pos - v0.pos, v2.pos - v0.pos);
				if (Vector3.Dot(c, c0) < 0f) { return false; }
			}
			return true;
		}
		
		// Attempt to join this polygon with another, otherwise return null.
		
		public Polygon TryJoin(Polygon other) {
			
			// must be coplanar
			if (this != other && this.shared == other.shared && this.Plane.Approx(other.Plane)) {
				// must have a common edge
				for(int i=0; i<vertices.Length; ++i) {
					int i1 = (i + 1) % vertices.Length;
					for(int j=0; j<other.vertices.Length; ++j) {
						int j1 = (j + 1) % other.vertices.Length;
						if (
							vertices[i].pos.Approx(other.vertices[j1].pos) && 
							vertices[i1].pos.Approx(other.vertices[j].pos)
						) {
							
							// splice vertices together
							var newVertices = new Vertex[vertices.Length + other.vertices.Length - 2];
							int n = 0;
							for(int k=0; k<vertices.Length; ++k) { 
								newVertices[n++] = vertices[(i1+k)%vertices.Length]; 
							}
							for(int k=other.vertices.Length-2; k>0; --k) {
								newVertices[n++] = other.vertices[(j1+k) % other.vertices.Length];
							}
							var poly = new Polygon(newVertices) { shared = shared };
							
							// must be convex
							if (poly.IsConvex()) { 
								return poly; 
							}
							
						}
							
					}
				}
			}
			
			// could not join
			return null;
		}
		
		static float Inv(float from, float to, float t) { return (t - from) / (to - from); }
		
		// Remove colinear vertices (all vertex attribs, not just position)
		
		public Polygon Simplified()
		{
			const float EPSILON_SQ = 0.001f * 0.001f;
			var verts = new List<Vertex>(vertices.Length); verts.AddRange(vertices);
			for(int i=0; verts.Count > 3 && i < verts.Count; ) {
				
				// is vertex "i" colinear?
				var i0 = (i + verts.Count - 1) % verts.Count;
				var i1 = (i + 1) % verts.Count;
				var v = verts[i].pos;
				var v0 = verts[i0].pos;
				var v1 = verts[i1].pos;
				if (Vector3.Cross(v0-v, v1-v).sqrMagnitude < EPSILON_SQ) {
					verts.RemoveAt(i);
				} else {
					++i;
				}
			}
			
			return new Polygon(verts.ToArray()) { shared = shared };

		}
	}
	
	// Holds a binary space partition tree representing a 3D solid. 
	// Two solids can be combined using the union(), subtract(), and intersect() methods.
	public class Solid 
	{
		public readonly Polygon[] polygons;
		
		public Solid(params Polygon[] args)
		{
			polygons = args;
		}

		public GameObject CreateGameObject(string name="Result")
		{
			if (polygons.Length == 0) {
				Debug.LogWarning("Empty Solid :*(");
				return null;
			}
		
			// For now just make one mesh; for multiple materials we'll need
			// to make multiple meshes (or submeshes?) :P
			
			var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
			go.GetComponent<MeshRenderer>().sharedMaterial = polygons[0].shared as Material;
			
			var vbuf = new List<Vector3>();
			var nbuf = new List<Vector3>();
			var tbuf = new List<Vector2>();
			var cbuf = new List<Color32>();
			var ibuf = new List<int>();
			
			foreach(var poly in polygons) {
				int ntriangles = poly.vertices.Length-2;
				for(int i=0; i<ntriangles; ++i) {
					ibuf.Add (vbuf.Count);
					ibuf.Add (vbuf.Count + 1 + i);
					ibuf.Add (vbuf.Count + 2 + i);
				}
				foreach(var vertex in poly.vertices) {
					vbuf.Add(vertex.pos);
					nbuf.Add(vertex.normal);
					tbuf.Add (vertex.uv);
					cbuf.Add (vertex.color);
				}
			}
			
			var mesh = new Mesh() {
				name = name,
				vertices = vbuf.ToArray(),
				normals = nbuf.ToArray(),
				uv = tbuf.ToArray(),
				colors32 = cbuf.ToArray(),
				triangles = ibuf.ToArray()
			};
			
			mesh.DedupVertices();
			mesh.Optimize();
			go.GetComponent<MeshFilter>().sharedMesh = mesh;
			return go;
		}
		
		// Return a new CSG solid representing space in either this solid or in the solid csg. 
		// Neither this solid nor the solid csg are modified.
		//
		// A.union(B)
		//
		// +-------+            +-------+
		// |       |            |       |
		// |   A   |            |       |
		// |    +--+----+   =   |       +----+
		// +----+--+    |       +----+       |
		//      |   B   |            |       |
		//      |       |            |       |
		//      +-------+            +-------+	
		
		public Solid Union(Solid csg)
		{
			var a = new Node();
			var b = new Node();
			a.Build(polygons);
			b.Build(csg.polygons);
			a.ClipTo(b);
			b.ClipTo(a);
			b.Invert();
			b.ClipTo(a);
			b.Invert();
			a.Build(b.AllPolygons());
			return new Solid(a.AllPolygons().ToArray()).Simplified();		
		}
		
		// Return a new CSG solid representing space in this solid but not in the solid csg. 
		// Neither this solid nor the solid csg are modified.
		//
		// A.subtract(B)
		//
		// +-------+            +-------+
		// |       |            |       |
		// |   A   |            |       |
		// |    +--+----+   =   |    +--+
		// +----+--+    |       +----+
		//      |   B   |
		//      |       |
		//      +-------+	
		
		public Solid Subtract(Solid csg)
		{
			var a = new Node();
			var b = new Node();
			a.Build(polygons);
			b.Build(csg.polygons);
			a.Invert();
			a.ClipTo(b);
			b.ClipTo(a);
			b.Invert();
			b.ClipTo(a);
			b.Invert();
			a.Build(b.AllPolygons());
			a.Invert();
			return new Solid(a.AllPolygons().ToArray()).Simplified();
		}
		
		// Return a new CSG solid representing space both this solid and in the solid csg. 
		// Neither this solid nor the solid csg are modified.
		//
		// A.intersect(B)
		//
		// +-------+
		// |       |
		// |   A   |
		// |    +--+----+   =   +--+
		// +----+--+    |       +--+
		//      |   B   |
		//      |       |
		//      +-------+	
		
		public Solid Intersect(Solid csg)
		{
			var a = new Node();
			var b = new Node();
			a.Build(polygons);
			b.Build(csg.polygons);
			a.Invert();
			b.ClipTo(a);
			b.Invert();
			a.ClipTo(b);
			b.ClipTo(a);
			a.Build(b.AllPolygons());
			a.Invert();
			return new Solid(a.AllPolygons().ToArray()).Simplified();
		}
		
		// Return a new CSG solid with solid and empty space switched. 
		// This solid is not modified.	
		
		public Solid Inverse()
		{
			var result = new Polygon[polygons.Length];
			for(int i=0; i<polygons.Length; ++i) {
				result[i] = polygons[i].Flipped();
			}
			return new Solid(result);
		}
		
		public Solid Simplified()
		{
			List<Polygon> polygons = new List<Polygon>(this.polygons.Length);
			foreach(var poly in this.polygons) {
				if (!poly.IsDegenerate()) {
					polygons.Add(poly.Simplified());
				}
			}
			
			// go through pairs in the list, looking for polygons we can join
			var match = true;
			do {
				match = false;
				for(int i=0; i<polygons.Count; ++i)
				for(int j=0; j<i; ++j) {
					var result = polygons[i].TryJoin(polygons[j]);
					if (result != null) {
						polygons[j] = result;
						polygons.RemoveAt(i);
						match = true;
						goto BreakOut;
					}
				}
				BreakOut:;
			} while(match);
			
			Debug.Log(polygons.Count + " / " + this.polygons.Length);
			
			return new Solid(polygons.ToArray());
		}
	}
	
	// Holds a node in a BSP tree. A BSP tree is built from a collection of 
	// polygons by picking a polygon to split along. That polygon (and all 
	// other coplanar polygons) are added directly to that node and the other 
	// polygons are added to the front and/or back subtrees. This is not a leafy 
	// BSP tree since there is no distinction between internal and leaf nodes.	
	
	internal class Node 
	{
		internal Plane? plane = null;
		internal List<Polygon> polygons = new List<Polygon>();
		internal Node front = null, back = null;
		
		// Convert solid space to empty space and empty space to solid space.
		
		public void Invert()
		{
			// flip individual attributes
			for(int i=0; i<polygons.Count; ++i) {
				polygons[i] = polygons[i].Flipped();
			}
			if (plane != null) { plane = plane.Value.Flipped(); }
			if (front != null) { front.Invert(); }
			if (back != null) { back.Invert(); }
			
			// swap front and back
			var temp = front;
			front = back;
			back = temp;
		}
		
		// Recursively remove all polygons in polygons that are inside this BSP tree.
		
		public List<Polygon> ClipPolygons(List<Polygon> polygons)
		{
			if (plane == null) { return new List<Polygon>(polygons); }
			var front = new List<Polygon>();
			var back = new List<Polygon>();
			foreach(var polygon in polygons) {
				plane.Value.SplitPolygon(polygon, front, back, front, back);
			}
			if (this.front != null) { front = this.front.ClipPolygons(front); }
			if (this.back != null) { back = this.back.ClipPolygons(back); } else { back.Clear(); }
			front.AddRange(back);
			return front;
		}
		
		// Remove all polygons in this BSP tree that are inside the other BSP tree bsp.
		
		public void ClipTo(Node node)
		{
			polygons = node.ClipPolygons(polygons);
			if (front != null) { front.ClipTo(node); }
			if (back != null) { back.ClipTo(node); }
		}
		
		// Return a list of all polygons in this BSP tree.

		public List<Polygon> AllPolygons()
		{
			var polygons = new List<Polygon>(this.polygons);
			if (front != null) { polygons.AddRange(front.AllPolygons()); }
			if (back != null) { polygons.AddRange(back.AllPolygons()); }
			return polygons;
		}
		
		// Build a BSP tree out of polygons. When called on an existing tree, the new 
		// polygons are filtered down to the bottom of the tree and become new nodes there. 
		// Each set of polygons is partitioned using the first polygon (no heuristic is 
		// used to pick a good split).
		
		public void Build(IList<Polygon> aPolygons)
		{
			if (aPolygons.Count == 0) { return; }
			if (plane == null) { 
				plane = aPolygons[0].Plane; 
			}
			var front = new List<Polygon>();
			var back = new List<Polygon>();
			foreach(var polygon in aPolygons) {
				plane.Value.SplitPolygon(polygon, polygons, polygons, front, back);
			}
			if (front.Count > 0) {
				if (this.front == null) { this.front = new Node(); }
				this.front.Build(front);
			}
			if (back.Count > 0) {
				if (this.back == null) { this.back = new Node(); }
				this.back.Build(back);
			}
		}
		
	}
	
	public static class ExtensionMethods
	{
		internal static Plane Flipped(this Plane plane) 
		{ 
			var result = new Plane();
			result.normal = -plane.normal;
			result.distance = -plane.distance;
			return result;
		}
		
		public static bool Approx(float a, float b)
		{
			var diff = a - b;
			return diff > -EPSILON && diff < EPSILON;
		}
		
		public static bool Approx(this Plane p0, Plane p1)
		{
			return 
				Approx(p0.distance, p1.distance) && 
				Approx(Vector3.Dot(p0.normal, p1.normal), 1f);
		}
		
		public static bool Approx(this Vector2 u, Vector2 v) 
		{
			return 
				Approx(u.x, v.x) && 
				Approx(u.y, v.y);
		}
		
		public static bool Approx(this Vector3 u, Vector3 v) 
		{
			return 
				Approx(u.x, v.x) && 
				Approx(u.y, v.y) && 
				Approx(u.z, v.z);
		}
		
		
		
		const int COPLANAR = 0;
		const int FRONT = 1;
		const int BACK = 2;
		const int SPANNING = 3;
		const float EPSILON = 0.001f;
		static List<Vertex> frontBuf = new List<Vertex>();
		static List<Vertex> backBuf = new List<Vertex>();

		internal static void SplitPolygon(this Plane plane, Polygon polygon, 
		                                  List<Polygon> coplanarFront, List<Polygon> coplanarBack, 
		                                  List<Polygon> front, List<Polygon> back) 
		{
			var polygonType = 0;
			var pTypes = new List<int>(polygon.vertices.Length);
			foreach(var vertex in polygon.vertices) {
				var t = plane.GetDistanceToPoint(vertex.pos);
				var pType = (t < -EPSILON) ? BACK : (t > EPSILON) ? FRONT : COPLANAR;
				polygonType |= pType;
				pTypes.Add(pType);
			}
			
			switch(polygonType) {
			case COPLANAR:
				if (Vector3.Dot(plane.normal, polygon.Plane.normal) > 0f) {
					coplanarFront.Add(polygon);
				} else {
					coplanarBack.Add(polygon);
				}
				break;
			case FRONT:
				front.Add(polygon);
				break;
			case BACK:
				back.Add(polygon);
				break;
			case SPANNING:
				frontBuf.Clear();
				backBuf.Clear();
				for(int i=0; i<polygon.vertices.Length; ++i) {
					var j = (i + 1) % polygon.vertices.Length;
					var ti = pTypes[i];
					var tj = pTypes[j];
					var vi = polygon.vertices[i]; 
					var vj = polygon.vertices[j];
					if (ti != BACK) { frontBuf.Add(vi); }
					if (ti != FRONT) { backBuf.Add(vi); }
					if ((ti | tj) == SPANNING) {
						var t = (-plane.distance - Vector3.Dot(plane.normal, vi.pos)) / 
						        Vector3.Dot(plane.normal, vj.pos - vi.pos);
						var v = vi.Interpolate(vj, t);
						frontBuf.Add(v);
						backBuf.Add(v);
					}					
				}
				if (frontBuf.Count >= 3) { front.Add(new Polygon(frontBuf.ToArray()) { shared = polygon.shared }); }
				if (backBuf.Count >= 3) { back.Add(new Polygon(backBuf.ToArray()) { shared = polygon.shared }); }
				break;
			}
		}
		
		public static Solid CreateSolid(this MeshFilter filter)
		{
			var mesh = filter.sharedMesh;
			var xform = filter.GetComponent<Transform>();
			var renderer = filter.GetComponent<MeshRenderer>();
			var mat = renderer == null ? null : renderer.sharedMaterial;
			
			var vbuf = mesh.vertices;
			var nbuf = mesh.normals;
			var tbuf = mesh.uv;
			var cbuf = mesh.colors32;
			if (cbuf.Length == 0) {
				cbuf = new Color32[vbuf.Length];
				for(int i=0; i<vbuf.Length; ++i) {
					cbuf[i] = Color.white;
				}
			}
			var ibuf = mesh.triangles;
			
			var nfaces = ibuf.Length/3;
			var polygons = new Polygon[nfaces];
			for(int i=0; i<nfaces; ++i) {
				var i0 = ibuf[3*i];
				var i1 = ibuf[3*i+1];
				var i2 = ibuf[3*i+2];
				var v0 = new Vertex(xform.TransformPoint(vbuf[i0]), xform.TransformDirection(nbuf[i0]), cbuf[i0], tbuf[i0]);
				var v1 = new Vertex(xform.TransformPoint(vbuf[i1]), xform.TransformDirection(nbuf[i1]), cbuf[i1], tbuf[i1]);
				var v2 = new Vertex(xform.TransformPoint(vbuf[i2]), xform.TransformDirection(nbuf[i2]), cbuf[i2], tbuf[i2]);
				polygons[i] = new Polygon(v0, v1, v2) { shared = mat };
			}	
			
			return new Solid(polygons);
		}
	}	

}
