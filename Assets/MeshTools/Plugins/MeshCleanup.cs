using CSG;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshCleanup {
	
	// I had to break this problem down into really excrucitingly verbose helper
	// classes just so I could express my solution without breaking my brain.
	
	class GraphVertex {
		public readonly int src;
		public readonly Vector3 pos;
		public readonly List<GraphTriangle> adj = new List<GraphTriangle>();
		
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
			verts = new GraphVertex[] { a0, a1, a2 };
			edges = new GraphEdge[] { 
				new GraphEdge(this, a0, a1,   a2), 
				new GraphEdge(this, a1, a2,   a0), 
				new GraphEdge(this, a2, a0,   a1) 
			};
			a0.adj.Add(this);
			a1.adj.Add(this);
			a2.adj.Add(this);
		}
		
		public Plane Plane
		{
			get { return new Plane(verts[0].pos, verts[1].pos, verts[2].pos); }
		}
		
	}
	
	
	class Graph {
		public readonly List<GraphVertex> verts;
		public readonly List<GraphEdge> adjEdges;
		public readonly List<GraphTriangle> faces;
		
		public Graph(Mesh mesh) 
		{
			var ibuf = mesh.triangles;
			var vbuf = mesh.vertices;
			var nfaces = ibuf.Length/3;
			
			// create vertices
			verts = new List<GraphVertex>(vbuf.Length);
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
			foreach(var face in faces) {
				if (face.Plane.Approx(tri.Plane)) {
					foreach(var ei in face.edges)
					foreach(var ej in tri.edges) {
						if (ei.TryJoin(ej)) {
							adjEdges.Add (ei);
						}			
					}
					
				}
			}
			faces.Add (tri);
		}
		
		void Remove(GraphTriangle face) 
		{
			foreach(var edge in face.edges) {
				if (edge.adj != null) {
					adjEdges.Remove(edge);
					adjEdges.Remove (edge.adj);
					edge.adj.adj = null;
					edge.adj = null;
				}
			}
			foreach(var vertex in face.verts) {
				vertex.adj.Remove(face);
			}
			faces.Remove(face);
		}
		
		public int RemoveInternalVertices()
		{
			// Keep reducing triangles and quads until there's nothing 
			// more to remove
			int count = 0;
			while(ReduceTriangle() || ReduceQuad()) { ++count; }
			return count;
		}
		
		
		
		bool ReduceTriangle()
		{
			// Look for and edge between two trianglesthat contains
			// a colinear vertex, and replace them with a single triangle
			foreach(var edge in adjEdges) {
				if (ReduceTriangleEdge(edge) || ReduceTriangleEdge(edge.adj)) {
					return true;
				}
			}
			return false;
		}
		
		bool ReduceTriangleEdge(GraphEdge edge)
		{
			// Check if edge.v0 is colinear, and if so replace both triangles
			var v = edge.v0;
			var v0 = edge.opposingVertex;
			var v1 = edge.adj.opposingVertex;
			
			var l = (v0.pos - v.pos).normalized;
			var r = (v1.pos - v.pos).normalized;
			var EPSILON = 0.00333f;
			var isColinear = Vector3.Cross(l,r).sqrMagnitude < EPSILON * EPSILON;
			
			if (isColinear) {
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
			// Look for three triangles with a shared colinear vertex, 
			// and reduce them to two triangles forming a quad
		
			return false;
		}
		
			
	}
	
	public static void CleanupInternalVertices(this Mesh mesh)
	{
		mesh.DedupVertices();
		var graph = new Graph(mesh);
		var count = graph.RemoveInternalVertices();
		if (count > 0) {
			var ibuf = new int[3 * graph.faces.Count];
			var i = 0;
			foreach(var face in graph.faces)
			foreach(var vert in face.verts) {
				ibuf[i++] = vert.src;
			}	
			mesh.triangles = ibuf;
			mesh.RemoveUnusedVertices();
		}
	}
	
}
