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
using UnityEngine;

public static partial class MeshExtensions {
	
	// Given a mesh with lots of unnecesary interior-vertices and subdivided edges
	// (which is a common result from BSP-based CSG), try to remove as much garbage
	// geometry as we can.  There's probably a perfect solution, but this hack (a few
	// special cases) seems to work well-enough, and it's definitely out-performing
	// the commercial solutions I investigated.
	
	public static void RemoveGarbageGeometry(this Mesh mesh)
	{
		// for some dumb reason (translation: I wrote a bug), this command needs
		// to run a few times just to find every reduction :P
		int accum = 0;
		do {
			accum = mesh.DedupVertices();
			var graph = new Graph(mesh);
			var count = graph.RemoveInternalVertices();
			accum += count;
			if (count > 0) {
				var ibuf = new int[3 * graph.faces.Count];
				var i = 0;
				foreach(var face in graph.faces)
				foreach(var vert in face.verts) {
					ibuf[i++] = vert.src;
				}	
				mesh.triangles = ibuf;
				accum += mesh.RemoveUnusedVertices();
			}
		} while(accum > 0);
	}	
	
	//--------------------------------------------------------------------------------
	// I had to break this problem down into really excrucitingly verbose helper
	// classes just so I could express my solution without breaking my brain.
	
	class GraphVertex {
		public readonly int src;
		public readonly Vector3 pos;
		
		public GraphVertex(int s, Vector3 p) { src = s; pos = p; }
		
	}
	
	class GraphEdge {
		public readonly GraphTriangle face;
		public readonly GraphVertex v0, v1;
		public readonly GraphVertex opposingVertex;
		internal GraphEdge adj;
		
		public GraphEdge(GraphTriangle aFace, GraphVertex a0, GraphVertex a1, GraphVertex a2)
		{
			face = aFace;
			v0 = a0; v1 = a1;
			opposingVertex = a2;
		}
		
		internal bool TryJoin(GraphEdge e) 
		{
			// two edges are adjacent if they trace the same vertices in 
			// opposite order.  Callers ensure their respective
			// faces are coplanar prior to this.
			
			if (adj == null && e.adj == null && e.v0 == v1 && e.v1 == v0) {
				adj = e;
				e.adj = this;
				return true;
			} else {
				return false;
			}
		}
	}
	
	class GraphTriangle {
		public readonly GraphVertex[] verts;
		public readonly GraphEdge[] edges;
		
		public GraphTriangle(GraphVertex a0, GraphVertex a1, GraphVertex a2)
		{
			verts = new GraphVertex[] { 
				a0, a1, a2 
			};
			edges = new GraphEdge[] { 
				new GraphEdge(this, a0, a1,   a2), 
				new GraphEdge(this, a1, a2,   a0), 
				new GraphEdge(this, a2, a0,   a1) 
			};
		}
		
		public Plane Plane
		{
			get { return new Plane(verts[0].pos, verts[1].pos, verts[2].pos); }
		}
		
		public bool FindQuadReductionPivot(out GraphVertex v, out GraphEdge ea, out GraphEdge eb)
		{
			// Helper function for ReduceQuad().  Check our vertices to see if we have one that
			// lies along two adjacent edges and is colinear with those edges' opposite vertices.
			v = null;
			ea = null;
			eb = null;
			for(int i=0; i<3; ++i) {
				v = verts[i];
				ea = edges[(i+2)%3];
				eb = edges[i];
				if (ea.adj != null && eb.adj != null && v.pos.IsColinear(
					ea.adj.opposingVertex.pos, 
					eb.adj.opposingVertex.pos
				)) {
					return true;
				}
			}
			return false;
		}
		
	}
	
	
	class Graph {
		public readonly List<GraphTriangle> faces;
		public readonly List<GraphEdge> adjEdges;
		
		public Graph(Mesh mesh) 
		{
			var ibuf = mesh.triangles;
			var vbuf = mesh.vertices;
			var nfaces = ibuf.Length/3;
			
			// create vertices
			var verts = new List<GraphVertex>(vbuf.Length);
			for(int i=0; i<vbuf.Length; ++i) {
				verts.Add(new GraphVertex(i, vbuf[i]));
			}
			
			// create faces
			faces = new List<GraphTriangle>(nfaces);
			adjEdges = new List<GraphEdge>(3*nfaces);			
			for(int i=0; i<nfaces; ++i) {
				Add(new GraphTriangle(
					verts[ibuf[3*i]], verts[ibuf[3*i+1]], verts[ibuf[3*i+2]]
				));
			}
		}
		
		void Add(GraphTriangle tri)
		{
			// Register the face and look up if we made any
			// new adjacent edge relationships.
			faces.Add (tri);
			foreach(var face in faces) {
				if (face.Plane.Approx(tri.Plane)) {
					foreach(var ei in face.edges)
					foreach(var ej in tri.edges) {
						if (ei.TryJoin(ej)) {
							adjEdges.Add(ei);
							adjEdges.Add(ej);
						}			
					}
					
				}
			}
		}
		
		void Remove(GraphTriangle face) 
		{
			// Break adjacent edge relationships and 
			// unregister the face.
			foreach(var edge in face.edges) {
				if (edge.adj != null) {
					adjEdges.Remove(edge);
					adjEdges.Remove(edge.adj);
					edge.adj.adj = null;
					edge.adj = null;
				}
			}
			faces.Remove(face);
		}
		
		public int RemoveInternalVertices()
		{
			// Keep reducing triangles/quads until there's no candidates left
			int count = 0;
			while(ReduceTriangle() || ReduceQuad()) { ++count; }
			return count;
		}
		
		
		
		bool ReduceTriangle()
		{
			// Look for and edge between two trianglesthat contains
			// a colinear vertex, and replace them with a single triangle
			foreach(var edge in adjEdges) {
				if (ReduceTriangleEdge(edge)) { return true; }
			}
			return false;
		}
		
		bool ReduceTriangleEdge(GraphEdge edge)
		{
			// Check if edge.v0 is colinear (edge.v1 will be inspected when we look
			// at the edge going "the other way" on the adjacent vertex)
			
			var v = edge.v0;
			var v0 = edge.opposingVertex;
			var v1 = edge.adj.opposingVertex;			
			if (v.pos.IsColinear(v0.pos, v1.pos)) {
				
				// remove 2 triangles, add combined triangle
				var t0 = edge.face;
				var t1 = edge.adj.face;
				var tv = edge.v1;
				Remove(t0);
				Remove(t1);
				Add(new GraphTriangle(v1, tv, v0));
				
				return true;
			} else {
				return false;
			}
		}
		
		bool ReduceQuad()
		{
			// Find a triangle with two adjacent neighbors, all three of whom share a single
			// vertex that is colinear between the neighbors' opposite vertices, and replace 
			// them with two triangles forming a quad
			GraphVertex v;
			GraphEdge ea, eb;
			foreach(var face in faces) {
				if (face.FindQuadReductionPivot(out v, out ea, out eb)) {
					
					// three triangles to remove
					var t0 = ea.face;
					var t1 = ea.adj.face;
					var t2 = eb.adj.face;
					
					// four vertices or new 2-triangle quad to add
					var ua = ea.adj.opposingVertex;
					var va = ea.v0;
					var ub = eb.adj.opposingVertex;
					var vb = eb.v1;
					
					Remove(t0);
					Remove(t1);
					Remove(t2);
					
					// now we need to determine which way to draw the diagonal across the quad.
					// if the quad is convex it doesn't matter, but it there's a concave vertex,
					// then we need to draw it that way.  We'll test iv ua or vb is concave, and
					// if so draw the diagonal that way, otherwise in all other cases we draw it
					// the other way
					var c1 = Vector3.Cross(va.pos - ua.pos, ub.pos - ua.pos);
					var c2 = Vector3.Cross(ub.pos - vb.pos, va.pos - vb.pos);
					if (Vector3.Dot(c1,c2) < 0) {
						Add(new GraphTriangle(va, ua, vb));
						Add(new GraphTriangle(ub, vb, ua));
					} else {
						Add(new GraphTriangle(va, ua, ub));
						Add(new GraphTriangle(va, ub, vb));
					}
					
					return true;
				}
			}
							
			return false;
		}
		
			
	}
	
	// Is this point colinear with the segment [p0,p1]?
	
	internal static bool IsColinear(this Vector3 p, Vector3 p0, Vector3 p1) 
	{
		const float EPSILON_SQ = 0.00333f * 0.00333f;
		var u = (p0 - p).normalized;
		var v = (p1 - p).normalized;
		return Vector3.Cross(u,v).sqrMagnitude < EPSILON_SQ;
	}
	
}
